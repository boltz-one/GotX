using GotX.Text;

namespace GotX;

/// <summary>
/// Sở hữu mọi entity, hàng đợi effect và các cửa sổ. Effect (notify, emit) được xếp hàng
/// rồi chạy trong <see cref="Flush"/>: callback có thể sinh thêm effect, chúng được xử lý
/// tiếp trong cùng lượt ("effect cycle"), tránh gọi lồng nhau khi đang cập nhật.
/// </summary>
public sealed partial class App
{
    readonly record struct Effect(int Id, object? Event);

    int _nextId = 1, _sweepAt = 256;
    bool _flushing;
    readonly Queue<Effect> _effects = new();
    readonly HashSet<int> _pendingNotify = new();
    // Mảng bất biến (copy-on-write): đăng ký/huỷ ngay trong callback không làm hỏng vòng lặp đang chạy.
    readonly Dictionary<int, Subscription[]> _observers = new();
    readonly Dictionary<(int, Type), Subscription[]> _subscribers = new();
    // Tham chiếu yếu: app biết entity nào còn sống mà không giữ chúng lại.
    readonly Dictionary<int, WeakReference<Entity>> _entities = new();
    readonly List<Window> _windows = new();

    public App(IPlatform? platform = null)
    {
        Platform = platform;
        Clipboard = platform?.Clipboard ?? new MemoryClipboard();
    }

    internal IPlatform? Platform { get; }

    /// <summary>Hệ thống chữ dùng chung cho các cửa sổ mở qua <see cref="OpenWindow"/> (do Application.Run gán).</summary>
    public TextSystem? Text { get; set; }

    /// <summary>Clipboard của hệ điều hành; không có platform (headless) thì là clipboard trong bộ nhớ.</summary>
    public IClipboard Clipboard { get; set; }

    /// <summary>
    /// Số effect tối đa trong một lượt <see cref="Flush"/>. Vượt quá nghĩa là các callback đang
    /// Notify/Emit lẫn nhau không dừng: hàng đợi bị xoá và ném <see cref="InvalidOperationException"/>.
    /// </summary>
    public int MaxEffectsPerFlush { get; set; } = 100_000;

    public T New<T>(T entity) where T : Entity
    {
        if (entity.IsAttached) throw new InvalidOperationException("Entity đã được gắn vào app.");
        int id = _nextId++;
        entity.Attach(this, new EntityId(id));
        _entities[id] = new WeakReference<Entity>(entity);
        if (_entities.Count >= _sweepAt) SweepDeadEntities();
        entity.OnCreated();
        Flush();
        return entity;
    }

    /// <summary>Mở thêm một cửa sổ hiển thị <paramref name="root"/> (cần platform, tức là app chạy qua Application.Run).</summary>
    public Window OpenWindow(WindowOptions options, View root) => Application.OpenWindow(this, options, root);

    /// <summary>
    /// Gỡ entity khỏi app ngay khi không dùng nữa: xoá mọi observer/subscriber đang theo dõi nó, bỏ focus
    /// nếu nó là ô nhập đang focus. Sau đó Notify/Emit của nó không còn tác dụng. Subscription mà
    /// chính entity này đã đăng ký lên entity khác thì nó phải tự Dispose. Entity bị bỏ quên không Release
    /// vẫn được dọn sau khi GC thu hồi nó.
    /// </summary>
    public void Release(Entity entity)
    {
        if (!ReferenceEquals(entity.App, this)) return;
        foreach (var w in _windows) w.OnEntityReleased(entity);
        Forget(entity.Id.Value);
        entity.Detach();
    }

    /// <summary>Entity còn được gắn và chưa bị GC thu hồi.</summary>
    internal bool IsAlive(int id) => _entities.TryGetValue(id, out var weak) && weak.TryGetTarget(out _);

    /// <summary>Entity theo id nếu còn sống (dùng cho công cụ chẩn đoán).</summary>
    internal Entity? Find(int id) => _entities.TryGetValue(id, out var weak) && weak.TryGetTarget(out var e) ? e : null;

    void Forget(int id)
    {
        _entities.Remove(id);
        _pendingNotify.Remove(id);
        if (_observers.Remove(id, out var observers))
            foreach (var s in observers) s.Deactivate();
        List<(int, Type)>? keys = null;
        foreach (var key in _subscribers.Keys)
            if (key.Item1 == id) (keys ??= new()).Add(key);
        if (keys is null) return;
        foreach (var key in keys)
            if (_subscribers.Remove(key, out var subscribers))
                foreach (var s in subscribers) s.Deactivate();
    }

    /// <summary>Dọn bảng đăng ký của các entity đã bị GC thu hồi mà không được Release.</summary>
    void SweepDeadEntities()
    {
        List<int>? dead = null;
        foreach (var kv in _entities)
            if (!kv.Value.TryGetTarget(out _)) (dead ??= new()).Add(kv.Key);
        if (dead is not null)
            foreach (int id in dead) Forget(id);
        _sweepAt = Math.Max(256, _entities.Count * 2);
    }

    static void AddHandler<TKey>(Dictionary<TKey, Subscription[]> map, TKey key, Subscription item) where TKey : notnull =>
        map[key] = map.TryGetValue(key, out var old) ? [.. old, item] : [item];

    static void RemoveHandler<TKey>(Dictionary<TKey, Subscription[]> map, TKey key, Subscription item) where TKey : notnull
    {
        if (!map.TryGetValue(key, out var old)) return;
        int i = Array.IndexOf(old, item);
        if (i < 0) return;
        if (old.Length == 1) { map.Remove(key); return; }
        var next = new Subscription[old.Length - 1];
        Array.Copy(old, 0, next, 0, i);
        Array.Copy(old, i + 1, next, i, old.Length - 1 - i);
        map[key] = next;
    }

    internal void AddWindow(Window w) => _windows.Add(w);
    internal void RemoveWindow(Window w) => _windows.Remove(w);

    public IReadOnlyList<Window> Windows => _windows;

    /// <summary>Gọi <see cref="Window.InvalidateAll"/> cho mọi cửa sổ của app.</summary>
    public void InvalidateAllWindows()
    {
        foreach (var w in _windows) w.InvalidateAll();
    }

    internal void Notify(EntityId id)
    {
        if (_pendingNotify.Add(id.Value)) _effects.Enqueue(new Effect(id.Value, null));
    }

    internal void Emit(EntityId id, object e) => _effects.Enqueue(new Effect(id.Value, e));

    /// <summary>Gọi <paramref name="callback"/> mỗi khi <paramref name="target"/> Notify.</summary>
    public Subscription Observe(Entity target, Action callback)
    {
        int id = target.Id.Value;
        var sub = new Subscription(callback, null, s => RemoveHandler(_observers, id, s));
        AddHandler(_observers, id, sub);
        return sub;
    }

    /// <summary>Nhận sự kiện kiểu <typeparamref name="TEvent"/> do <paramref name="emitter"/> phát.</summary>
    public Subscription Subscribe<TEvent>(Entity emitter, Action<TEvent> handler)
    {
        var key = (emitter.Id.Value, typeof(TEvent));
        // lambda tạo một lần khi đăng ký, không dùng reflection
        var sub = new Subscription(null, o => handler((TEvent)o), s => RemoveHandler(_subscribers, key, s));
        AddHandler(_subscribers, key, sub);
        return sub;
    }

    /// <summary>Chạy một việc trên main thread rồi xử lý effect (dùng từ thread nền).</summary>
    public void Post(Action action)
    {
        if (Platform is null) { action(); Flush(); return; }
        Platform.PostToMain(() => { action(); Flush(); });
    }

    /// <summary>Xử lý hết hàng đợi effect. Window gọi sau mỗi sự kiện input.</summary>
    public void Flush()
    {
        if (_flushing) return;
        _flushing = true;
        try
        {
            int processed = 0;
            while (_effects.TryDequeue(out var e))
            {
                if (++processed > MaxEffectsPerFlush)
                {
                    _effects.Clear();
                    _pendingNotify.Clear();
                    PerfLog.Shared?.Warn("effect-loop", $"hơn {MaxEffectsPerFlush} effect trong một lượt Flush (entity {e.Id})");
                    throw new InvalidOperationException(
                        $"Vòng lặp effect: hơn {MaxEffectsPerFlush} effect trong một lượt Flush (entity {e.Id}). " +
                        "Kiểm tra các callback Observe/Subscribe đang Notify/Emit lẫn nhau.");
                }
                if (e.Event is null)
                {
                    _pendingNotify.Remove(e.Id);
                    foreach (var w in _windows) w.MarkViewDirty(e.Id);
                    if (_observers.TryGetValue(e.Id, out var observers))
                        foreach (var s in observers)
                            if (s.Active) s.OnNotify!();
                }
                else if (_subscribers.TryGetValue((e.Id, e.Event.GetType()), out var subscribers))
                {
                    foreach (var s in subscribers)
                        if (s.Active) s.OnEvent!(e.Event);
                }
            }
        }
        finally { _flushing = false; }
    }
}

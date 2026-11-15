using GotX.Text;

namespace GotX;

public readonly record struct EntityId(int Value);

/// <summary>
/// Đối tượng có state sống lâu (model hoặc view), do App cấp Id và quản lý vòng đời.
/// Tạo bằng <c>app.New(new Foo())</c> để được cấp Id. Sau khi đổi state, gọi <see cref="Notify"/>:
/// thông báo không chạy ngay mà vào hàng đợi effect, được xử lý một lượt ở cuối mỗi sự kiện.
/// </summary>
public abstract class Entity
{
    public EntityId Id { get; private set; }
    public App App { get; private set; } = null!;
    public bool IsAttached => App is not null;

    internal void Attach(App app, EntityId id) { App = app; Id = id; }
    internal void Detach() { App = null!; }

    /// <summary>Gọi một lần sau khi entity được gắn vào app: nơi tạo entity con, đăng ký observe/subscribe.</summary>
    protected internal virtual void OnCreated() { }

    /// <summary>Báo state đã đổi: view chứa entity này (và các view cha) sẽ render lại ở frame sau.</summary>
    public void Notify() => App?.Notify(Id);

    /// <summary>Phát sự kiện có kiểu tới các subscriber.</summary>
    protected void Emit<TEvent>(TEvent e) where TEvent : notnull => App?.Emit(Id, e);
}

/// <summary>Entity có thể vẽ. Kết quả vẽ được cache cho tới khi view (hoặc view con) bị Notify.</summary>
public abstract class View : Entity
{
    public abstract Div Render(Window window);
}

/// <summary>Một đăng ký observe/subscribe. Dispose để huỷ: từ lúc đó callback không được gọi nữa, kể cả trong lượt Flush đang chạy.</summary>
public sealed class Subscription : IDisposable
{
    Action<Subscription>? _remove;
    internal readonly Action? OnNotify;
    internal readonly Action<object>? OnEvent;
    internal bool Active = true;

    internal Subscription(Action? onNotify, Action<object>? onEvent, Action<Subscription> remove)
    {
        OnNotify = onNotify; OnEvent = onEvent; _remove = remove;
    }

    public void Dispose()
    {
        Active = false;
        Interlocked.Exchange(ref _remove, null)?.Invoke(this); // gọi nhiều lần vẫn an toàn
    }

    /// <summary>Entity đích đã bị gỡ: bảng đăng ký đã xoá sẵn, chỉ cần tắt.</summary>
    internal void Deactivate() { Active = false; _remove = null; }
}

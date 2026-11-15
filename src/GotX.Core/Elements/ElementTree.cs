using GotX.Widgets;

namespace GotX;

/// <summary>
/// Arena chứa element của một frame. Lưu theo các khối (chunk) cố định 1024 node:
/// thêm node không bao giờ di chuyển node cũ, nên `ref Node` luôn hợp lệ kể cả khi
/// view được render muộn (giữa lúc layout).
/// </summary>
public sealed class ElementTree
{
    const int ChunkBits = 10, ChunkSize = 1 << ChunkBits, ChunkMask = ChunkSize - 1;
    Node[][] _chunks = [new Node[ChunkSize]];
    int _count;

    internal int Count => _count;

    /// <summary>Ngữ cảnh cho dark:/sm:/md:… do Window đặt mỗi frame.</summary>
    internal VariantContext Variants = new(false, 1024, 768, 16);

    internal ref Node this[int id] => ref _chunks[id >> ChunkBits][id & ChunkMask];

    // Mỗi chuỗi .Key() nhận một số riêng, không bao giờ cấp lại (không dùng hash nên không thể trùng).
    // Bit 30 tách khoá đặt tay khỏi khoá theo thứ tự xuất hiện.
    readonly Dictionary<string, (int Id, int Frame)> _keyIds = new(StringComparer.Ordinal);
    int _nextKeyId;

    /// <summary>Số thứ tự frame hiện tại (Window đặt): đánh dấu khoá nào còn được dùng.</summary>
    internal int Frame;

    internal int KeyCount => _keyIds.Count;

    /// <summary>Bật thì mỗi node nhớ chuỗi class gốc (cho công cụ soi cây); mặc định tắt để không cấp phát.</summary>
    internal bool Diagnostics;

    /// <summary>Tên .Key() ứng với một số khoá (tra ngược, chỉ dùng cho chẩn đoán).</summary>
    internal string? KeyName(int id)
    {
        if (id == 0) return null;
        foreach (var kv in _keyIds) if (kv.Value.Id == id) return kv.Key;
        return null;
    }

    internal int KeyId(string key)
    {
        ref var entry = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(_keyIds, key, out bool exists);
        if (!exists) entry.Id = ++_nextKeyId | 0x40000000;
        entry.Frame = Frame;
        return entry.Id;
    }

    internal bool TryKeyId(string key, out int id)
    {
        bool found = _keyIds.TryGetValue(key, out var entry);
        id = entry.Id;
        return found;
    }

    /// <summary>Bỏ các khoá không dùng trong frame này và không còn state nào gắn với chúng (khoá sinh động, vd. theo id dòng).</summary>
    internal void PruneKeys(HashSet<int> idsWithState)
    {
        foreach (var kv in _keyIds) // Dictionary cho phép Remove trong lúc duyệt (.NET Core 3.0+)
            if (kv.Value.Frame != Frame && !idsWithState.Contains(kv.Value.Id)) _keyIds.Remove(kv.Key);
    }

    internal void Reset()
    {
        for (int c = 0; c * ChunkSize < _count; c++)
            Array.Clear(_chunks[c], 0, Math.Min(ChunkSize, _count - c * ChunkSize));
        _count = 0;
    }

    internal int Alloc(NodeKind kind)
    {
        int chunk = _count >> ChunkBits;
        if (chunk == _chunks.Length)
        {
            Array.Resize(ref _chunks, _chunks.Length * 2);
            for (int i = chunk; i < _chunks.Length; i++) _chunks[i] = new Node[ChunkSize];
        }
        ref var n = ref this[_count];
        n.Kind = kind;
        n.Style = Style.Default;
        n.FirstChild = n.LastChild = n.NextSibling = -1;
        return _count++;
    }

    public Div NewDiv() => new(this, Alloc(NodeKind.Div));

    internal int NewText(string text)
    {
        int id = Alloc(NodeKind.Text);
        this[id].Text = text;
        return id;
    }

    internal Div NewView(View view)
    {
        int id = Alloc(NodeKind.View);
        this[id].View = view;
        return new Div(this, id);
    }

    internal Div NewInput(TextInput input)
    {
        int id = Alloc(NodeKind.Input);
        this[id].Input = input;
        return new Div(this, id);
    }

    internal void Append(int parent, int child)
    {
        ref var p = ref this[parent];
        if (p.LastChild < 0) p.FirstChild = child;
        else this[p.LastChild].NextSibling = child;
        p.LastChild = child;
    }
}

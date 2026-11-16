namespace GotX;

/// <summary>Tổng hợp vùng cắt <c>overflow-hidden</c> bên trong một view, đủ để quyết định phát lại (chép + tịnh tiến) khi view bị dời hoặc vùng cắt ngoài đổi.</summary>
internal record struct ClipAcc
{
    public bool Cut;              // có vùng cắt bị vùng cắt ngoài xén mà vẫn nhìn thấy (view vắt qua mép khung): phần bị xén có thể lộ ra khi dời
    public bool HasHid, HasIn;
    public Bounds Hid, In;        // hợp các vùng cắt nằm hẳn ngoài vùng cắt ngoài (rỗng) / nằm trọn trong nó (không gian logic)

    public readonly ClipAcc Offset(float dx, float dy) => this with { Hid = Hid.Offset(dx, dy), In = In.Offset(dx, dy) };

    public void Merge(in ClipAcc o)
    {
        Cut |= o.Cut;
        if (o.HasHid) { Hid = HasHid ? Union(Hid, o.Hid) : o.Hid; HasHid = true; }
        if (o.HasIn) { In = HasIn ? Union(In, o.In) : o.In; HasIn = true; }
    }

    public static Bounds Union(Bounds a, Bounds b)
    {
        float x0 = MathF.Min(a.X, b.X), y0 = MathF.Min(a.Y, b.Y), x1 = MathF.Max(a.Right, b.Right), y1 = MathF.Max(a.Bottom, b.Bottom);
        return new Bounds(x0, y0, x1 - x0, y1 - y0);
    }
}

/// <summary>
/// Phát lại view có <c>overflow-hidden</c> bên trong khi view bị dời (cuộn) hoặc vùng cắt ngoài đổi (mép khung cuộn, thẻ bao bị cắt dần):
/// primitive có vùng cắt bằng vùng cắt ngoài cũ chuyển sang vùng cắt ngoài mới, vùng cắt trong được dời rồi giao với vùng cắt ngoài mới.
/// Chỉ đúng khi không có vùng cắt nào đang bị xén (<c>Cut</c>) và mỗi vùng cắt trong vẫn nằm trọn trong vùng cắt ngoài mới (<c>In</c>)
/// hoặc vẫn nằm hẳn ngoài nó (<c>Hid</c>).
/// </summary>
public sealed partial class Window
{
    readonly List<ClipAcc> _clipAcc = new();

    /// <summary>Gặp một vùng cắt hình chữ nhật: ghi vào mọi view đang mở (view cha cũng phụ thuộc vào nó).</summary>
    void NoteClipRect(Bounds rect, Bounds clip, Bounds childClip)
    {
        bool within = Within(rect, clip), hidden = !within && (childClip.Width <= 0 || childClip.Height <= 0);
        for (int i = 0; i < _clipAcc.Count; i++)
        {
            var a = _clipAcc[i];
            if (within) { a.In = a.HasIn ? ClipAcc.Union(a.In, rect) : rect; a.HasIn = true; }
            else if (hidden) { a.Hid = a.HasHid ? ClipAcc.Union(a.Hid, rect) : rect; a.HasHid = true; }
            else a.Cut = true;
            _clipAcc[i] = a;
        }
    }

    /// <summary>Ra khỏi view: lấy tổng hợp của view và gộp vào view bao ngoài.</summary>
    ClipAcc PopClipAcc()
    {
        var a = _clipAcc[^1];
        _clipAcc.RemoveAt(_clipAcc.Count - 1);
        if (_clipAcc.Count > 0) { var p = _clipAcc[^1]; p.Merge(a); _clipAcc[^1] = p; }
        return a;
    }

    /// <summary>View đã phát lại (đã dời <paramref name="dx"/>,<paramref name="dy"/>) gộp vào view bao ngoài.</summary>
    void MergeReplayedClips(in ClipAcc a, float dx, float dy)
    {
        if (_clipAcc.Count == 0) return;
        var p = _clipAcc[^1]; p.Merge(a.Offset(dx, dy)); _clipAcc[^1] = p;
    }

    /// <summary>Điều kiện phát lại view bị dời <paramref name="dx"/>,<paramref name="dy"/> (logic) dưới vùng cắt ngoài mới <paramref name="clip"/>.</summary>
    static bool ClipsAllowReplay(in ClipAcc a, float dx, float dy, Bounds clip)
    {
        if (a.Cut) return false;
        if (a.HasHid)
        {
            var h = a.Hid.Offset(dx, dy).Inflate(1).Intersect(clip); // dư 1px cho làm tròn thiết bị
            if (h.Width > 0 && h.Height > 0) return false;
        }
        return !a.HasIn || Within(a.In.Offset(dx, dy), clip.Inflate(0.01f));
    }
}

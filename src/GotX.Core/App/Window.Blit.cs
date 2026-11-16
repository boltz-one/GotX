namespace GotX;

/// <summary>Vùng cuộn đã vẽ ở frame: khoá, hộp cắt của nội dung (px thiết bị) và độ dời cuộn đã làm tròn theo pixel thiết bị.</summary>
internal readonly record struct ScrollRec(long Key, Bounds Region, int OffX, int OffY, bool Simple);

/// <summary>
/// Gợi ý cuộn cho renderer có canvas giữ lại (SceneDamage.TryShift kiểm chứng rồi mới dùng): khung đầy đủ mà vùng cuộn chính chỉ dời theo chiều dọc
/// so với frame trước thì Full kèm (hộp, dy, scene cũ) thay vì Full trơn. Mặc định TẮT (đo: không lời trên pixel phẳng, memmove 4 MB ≈ một raster rẻ); bật bằng <c>GOTX_SCROLLBLIT=1</c> hoặc <see cref="ScrollBlit"/>.
/// </summary>
public sealed partial class Window
{
    public static bool ScrollBlitDefault { get; set; } = Environment.GetEnvironmentVariable("GOTX_SCROLLBLIT") == "1";
    public bool ScrollBlit { get; set; } = ScrollBlitDefault;

    List<ScrollRec> _scrollRecs = new(), _prevScrollRecs = new();
    bool _recScroll;

    void BeginScrollRecs() { (_scrollRecs, _prevScrollRecs) = (_prevScrollRecs, _scrollRecs); _scrollRecs.Clear(); _recScroll = true; }

    void NoteScroller(long key, ScrollState st, in Xf nodeXf, Bounds childClip, float px)
    {
        if (_recScroll) _scrollRecs.Add(new ScrollRec(key, childClip.Scale(_scale), (int)MathF.Round(st.X * px), (int)MathF.Round(st.Y * px), nodeXf.IsSimple));
    }

    /// <summary>Hết khung đầy đủ: báo vùng hỏng. Vùng cuộn lớn nhất vừa đổi vị trí dọc (cùng hộp) → kèm gợi ý cuộn, còn lại Full.</summary>
    void EndFullFrameDamage()
    {
        _recScroll = false;
        if (!ScrollBlit || !Damage.Active || _prevScrollRecs.Count == 0) { Damage.MarkFull(); return; }
        ScrollRec best = default; int dy = 0; float area = 0;
        foreach (var r in _scrollRecs)
        {
            if (!r.Simple) continue;
            foreach (var o in _prevScrollRecs)
            {
                if (o.Key != r.Key) continue;
                if (o.Region != r.Region || o.OffX != r.OffX || o.OffY == r.OffY || !o.Simple) break;
                float a = r.Region.Width * r.Region.Height;
                if (a > area) { area = a; best = r; dy = o.OffY - r.OffY; }
                break;
            }
        }
        if (dy == 0) Damage.MarkFull(); else Damage.MarkFull(_prevScene, best.Region, dy);
    }
}

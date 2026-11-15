namespace GotX;

/// <summary>Vị trí của phần tử nổi so với anchor: cạnh (Bottom/Top/Right/Left) + căn theo trục còn lại (giữa/Start/End).</summary>
public enum Placement : byte
{
    Bottom, BottomStart, BottomEnd, Top, TopStart, TopEnd, Right, RightStart, RightEnd, Left, LeftStart, LeftEnd,
}

/// <summary>
/// Định vị phần tử nổi (dropdown, popover, tooltip) quanh một anchor – tầng "Floating Positioner":
/// đặt theo <see cref="Placement"/>, lật sang cạnh đối diện khi không đủ chỗ (flip), rồi trượt dọc cạnh
/// để nằm trọn trong viewport (shift). Hàm thuần: Window gọi lại mỗi frame nên vị trí tự cập nhật khi anchor dời.
/// </summary>
public static class Floating
{
    /// <param name="offset">Khoảng hở giữa anchor và phần tử nổi.</param>
    /// <param name="padding">Lề tối thiểu so với mép viewport.</param>
    /// <param name="used">Placement thực tế sau khi lật.</param>
    public static Bounds Place(Bounds anchor, Size content, Bounds viewport, Placement placement,
        float offset, float padding, out Placement used)
    {
        int side = (int)placement / 3, align = (int)placement % 3; // side: 0 dưới, 1 trên, 2 phải, 3 trái
        bool vertical = side < 2;
        float need = (vertical ? content.Height : content.Width) + offset;

        // Flip: không đủ chỗ ở cạnh yêu cầu và cạnh đối diện rộng hơn
        float Room(int s) => s switch
        {
            0 => viewport.Bottom - padding - anchor.Bottom,
            1 => anchor.Y - viewport.Y - padding,
            2 => viewport.Right - padding - anchor.Right,
            _ => anchor.X - viewport.X - padding,
        };
        int opposite = side ^ 1;
        if (need > Room(side) && Room(opposite) > Room(side)) side = opposite;
        used = (Placement)(side * 3 + align);

        float x, y;
        if (vertical)
        {
            y = side == 0 ? anchor.Bottom + offset : anchor.Y - offset - content.Height;
            x = align switch
            {
                1 => anchor.X,
                2 => anchor.Right - content.Width,
                _ => anchor.X + (anchor.Width - content.Width) / 2,
            };
        }
        else
        {
            x = side == 2 ? anchor.Right + offset : anchor.X - offset - content.Width;
            y = align switch
            {
                1 => anchor.Y,
                2 => anchor.Bottom - content.Height,
                _ => anchor.Y + (anchor.Height - content.Height) / 2,
            };
        }

        // Shift: giữ trong viewport; phần tử lớn hơn viewport thì bám mép đầu
        x = Clamp(x, viewport.X + padding, viewport.Right - padding - content.Width);
        y = Clamp(y, viewport.Y + padding, viewport.Bottom - padding - content.Height);
        return new Bounds(x, y, content.Width, content.Height);
    }

    static float Clamp(float v, float min, float max) => max < min ? min : Math.Clamp(v, min, max);
}

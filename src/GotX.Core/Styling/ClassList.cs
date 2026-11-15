using System.Collections.Concurrent;
using System.Globalization;

namespace GotX;

/// <summary>Variant trạng thái, đánh giá lúc vẽ (chỉ đổi thuộc tính vẽ, không đổi layout).</summary>
[Flags]
public enum StateVariant : byte { None = 0, Hover = 1, Active = 2, Focus = 4, GroupHover = 8, Disabled = 16, FocusVisible = 32 }

/// <summary>Ngữ cảnh cho variant điều kiện (dark:, sm:, md:…), đánh giá lúc dựng cây.</summary>
public readonly record struct VariantContext(bool Dark, float ViewportWidth, float ViewportHeight, float Rem)
{
    /// <summary>Chỉ số breakpoint hiện tại (0 = nhỏ hơn sm). Đổi chỉ số → xoá cache view.</summary>
    public int BreakpointBucket
    {
        get
        {
            int n = 0;
            foreach (var bp in Theme.Breakpoints.Values) if (ViewportWidth >= bp * Rem) n++;
            foreach (var bp in Theme.CustomBreakpoints) if (ViewportWidth >= bp * Rem) n++;
            return n;
        }
    }
}

internal enum Prop : byte
{
    Display, Direction, Justify, Items, Self, Grow, Shrink, Basis, Gap,
    Padding, Margin, Width, Height, MinW, MinH, MaxW, MaxH, Position, Inset, Overflow,
    Bg, TextColor, BorderColor, BorderWidth, Radius, Shadow, ShadowColor, CustomShadow,
    RingWidth, RingColor, RingOffsetWidth, RingOffsetColor, Opacity,
    FontSize, LineHeight, Weight, TextAlign, NoWrap, Ellipsis, Cursor,
    Scroll, Wrap, GridCols, GridRows, ColSpan, RowSpan, Translate, Scale,
    Transition, Duration, Delay, Ease, Animation, Rotate, DivideX, DivideY, DivideColor,
    Skew, Origin, BorderStyle, OutlineWidth, OutlineColor, OutlineOffset, OutlineStyle,
    FontFamily, Italic, Decoration, TextCase, Tracking, ObjectFit,
    Gradient, GradFrom, GradVia, GradTo, GradPos, LineClamp, TextWrap, Aspect, GridLine, GridFlow,
    NumericSpacing, UnderlineOffset, DecorationColor, DecorationThickness,
    ScrollbarMode, ScrollbarWidth, ScrollbarColor,
}

/// <summary>Một lệnh gán thuộc tính đã biên dịch từ một class (kèm điều kiện variant).</summary>
internal struct UtilityOp
{
    public StateVariant State;
    public bool Dark, Important;
    public float MinRem, MaxRem; // khoảng breakpoint [Min, Max)
    public Prop Prop;
    public byte E;               // enum / mặt nạ cạnh / cờ đặc biệt
    public float F;
    public Length L;
    public Rgba C;
    public Rgba D;               // màu ở chế độ tối (token ngữ nghĩa như bg-primary)
    public bool HasDark;
    public BoxShadow S;
    public object? Ref;          // GridTemplate

    public readonly bool Matches(in VariantContext ctx)
    {
        if (Dark && !ctx.Dark) return false;
        float w = ctx.ViewportWidth / ctx.Rem;
        return w >= MinRem && w < MaxRem;
    }
}

/// <summary>
/// Kết quả biên dịch một chuỗi class. Được cache theo chuỗi (giống JIT của Tailwind: chỉ sinh
/// cho class thực sự dùng), nên mỗi frame chỉ tốn một lần tra từ điển, không cấp phát.
/// </summary>
public sealed class ClassList
{
    internal readonly UtilityOp[] Ops;
    internal readonly StateVariant States;
    public bool IsGroup { get; }
    public IReadOnlyList<string> Unknown { get; }
    internal bool HasState => States != StateVariant.None;

    internal ClassList(UtilityOp[] ops, bool group, string[] unknown)
    {
        Ops = ops; IsGroup = group; Unknown = unknown;
        foreach (var o in ops) States |= o.State;
    }

    /// <summary>Áp các class không có variant trạng thái (lúc dựng cây).</summary>
    internal void ApplyBase(ref Style s, in VariantContext ctx)
    {
        for (int pass = 0; pass < 2; pass++)
            foreach (ref readonly var o in Ops.AsSpan())
                if (o.State == StateVariant.None && o.Important == (pass == 1) && o.Matches(ctx))
                    Tailwind.Apply(ref s, o, ctx);
    }

    /// <summary>Áp class có variant trạng thái đang bật (lúc vẽ). Class "!important" ở base vẫn thắng.</summary>
    internal void ApplyState(ref Style s, StateVariant active, in VariantContext ctx)
    {
        foreach (ref readonly var o in Ops.AsSpan())
            if (o.State != StateVariant.None && !o.Important && (o.State & ~active) == 0 && o.Matches(ctx))
                Tailwind.Apply(ref s, o, ctx);
        foreach (ref readonly var o in Ops.AsSpan())
            if (o.Important && (o.State & ~active) == 0 && o.Matches(ctx))
                Tailwind.Apply(ref s, o, ctx);
    }
}

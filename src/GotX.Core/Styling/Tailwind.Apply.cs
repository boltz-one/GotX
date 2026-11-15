using System.Collections.Concurrent;
using System.Globalization;

namespace GotX;

public static partial class Tailwind
{
    // ====================== Áp dụng ======================

    internal static void Apply(ref Style s, in UtilityOp o, in VariantContext ctx)
    {
        // w-screen / h-screen: quy ra kích thước viewport tại thời điểm dựng cây
        var len = o.E == ScreenW && o.Prop is Prop.Width or Prop.MinW or Prop.MaxW ? Length.Px(ctx.ViewportWidth)
                : o.E == ScreenH && o.Prop is Prop.Height or Prop.MinH or Prop.MaxH ? Length.Px(ctx.ViewportHeight)
                : o.E == ViewportW && o.Prop is Prop.Width or Prop.Height or Prop.MinW or Prop.MinH or Prop.MaxW or Prop.MaxH ? AddViewport(o.L, o.F * ctx.ViewportWidth)
                : o.E == ViewportH && o.Prop is Prop.Width or Prop.Height or Prop.MinW or Prop.MinH or Prop.MaxW or Prop.MaxH ? AddViewport(o.L, o.F * ctx.ViewportHeight)
                : o.L;
        switch (o.Prop)
        {
            case Prop.Display: s.Display = (Display)o.E; break;
            case Prop.Direction: s.FlexDirection = (FlexDirection)o.E; break;
            case Prop.Justify: s.Justify = (Justify)o.E; break;
            case Prop.Items: s.AlignItems = (Align)o.E; break;
            case Prop.Self: s.AlignSelf = o.E == 255 ? null : (Align)o.E; break;
            case Prop.Grow: s.FlexGrowValue = o.F; break;
            case Prop.Shrink: s.FlexShrinkValue = o.F; break;
            case Prop.Basis: s.FlexBasis = o.L; break;
            case Prop.Gap:
                if ((o.E & 1) != 0) s.ColumnGap = o.L;
                if ((o.E & 2) != 0) s.RowGap = o.L;
                break;
            case Prop.Padding: SetEdges(ref s.Padding, o.E, o.L); break;
            case Prop.Margin: SetEdges(ref s.Margin, o.E, o.L); break;
            case Prop.Inset: SetEdges(ref s.Inset, o.E, o.L); break;
            case Prop.Width: s.Width = len; break;
            case Prop.Height: s.Height = len; break;
            case Prop.MinW: s.MinWidth = len; break;
            case Prop.MinH: s.MinHeight = len; break;
            case Prop.MaxW: s.MaxWidth = len; break;
            case Prop.MaxH: s.MaxHeight = len; break;
            case Prop.Position: s.Position = (Position)o.E; break;
            case Prop.Overflow: s.Overflow = (Overflow)o.E; break;
            case Prop.Bg: s.Background = Col(o, ctx); break;
            case Prop.TextColor: s.TextColorValue = Col(o, ctx); break;
            case Prop.BorderColor:
                if (o.E == AllSides)
                {   // border-red-500 đặt cả 4 cạnh (xoá màu riêng đã đặt trước đó), như CSS border-color
                    s.BorderColorValue = Col(o, ctx);
                    s.BorderTopColor = s.BorderRightColor = s.BorderBottomColor = s.BorderLeftColor = null;
                    break;
                }
                if ((o.E & Top) != 0) s.BorderTopColor = Col(o, ctx);
                if ((o.E & Right) != 0) s.BorderRightColor = Col(o, ctx);
                if ((o.E & Bottom) != 0) s.BorderBottomColor = Col(o, ctx);
                if ((o.E & Left) != 0) s.BorderLeftColor = Col(o, ctx);
                break;
            case Prop.BorderWidth:
                if ((o.E & Top) != 0) s.Border.Top = o.F;
                if ((o.E & Right) != 0) s.Border.Right = o.F;
                if ((o.E & Bottom) != 0) s.Border.Bottom = o.F;
                if ((o.E & Left) != 0) s.Border.Left = o.F;
                break;
            case Prop.Rotate: s.Rotate = o.F; break;
            case Prop.DivideX: s.DivideX = o.F; break;
            case Prop.DivideY: s.DivideY = o.F; break;
            case Prop.DivideColor: s.DivideColor = Col(o, ctx); break;
            case Prop.Radius:
                if ((o.E & TL) != 0) s.CornerRadii.TopLeft = o.F;
                if ((o.E & TR) != 0) s.CornerRadii.TopRight = o.F;
                if ((o.E & BR) != 0) s.CornerRadii.BottomRight = o.F;
                if ((o.E & BL) != 0) s.CornerRadii.BottomLeft = o.F;
                break;
            case Prop.Shadow: s.Shadow = (ShadowPreset)o.E; break;
            case Prop.CustomShadow: s.Shadow = ShadowPreset.Custom; s.CustomShadow = o.S; break;
            case Prop.ShadowColor: s.ShadowColor = Col(o, ctx); break;
            case Prop.RingWidth: s.RingWidth = o.F; break;
            case Prop.RingColor: s.RingColor = Col(o, ctx); break;
            case Prop.RingOffsetWidth: s.RingOffsetWidth = o.F; break;
            case Prop.RingOffsetColor: s.RingOffsetColor = Col(o, ctx); break;
            case Prop.Opacity: s.Opacity = o.F; break;
            case Prop.FontSize: s.FontSizeValue = o.L; break;
            case Prop.LineHeight: s.LineHeightValue = o.L; break;
            case Prop.Weight: s.FontWeightValue = (FontWeight)(ushort)o.F; break;
            case Prop.TextAlign: s.TextAlignValue = (TextAlign)o.E; break;
            case Prop.NoWrap: s.NoWrapValue = o.E != 0; break;
            case Prop.Ellipsis: s.EllipsisValue = o.E != 0; break;
            case Prop.Cursor: s.Cursor = (CursorKind)o.E; break;
            case Prop.Scroll: s.ScrollX = (o.E & 1) != 0; s.ScrollY = (o.E & 2) != 0; break;
            case Prop.Wrap: s.FlexWrap = o.E != 0; break;
            case Prop.GridCols: s.GridColumns = (GridTemplate?)o.Ref; break;
            case Prop.GridRows: s.GridRows = (GridTemplate?)o.Ref; break;
            case Prop.ColSpan: s.ColSpan = (short)o.F; break;
            case Prop.RowSpan: s.RowSpan = (short)o.F; break;
            case Prop.Translate:
                if ((o.E & 1) != 0) s.TranslateX = o.L;
                if ((o.E & 2) != 0) s.TranslateY = o.L;
                break;
            case Prop.Scale:
                if ((o.E & 1) != 0) s.ScaleX = o.F;
                if ((o.E & 2) != 0) s.ScaleY = o.F;
                break;
            case Prop.Skew:
                if ((o.E & 1) != 0) s.SkewX = o.F;
                if ((o.E & 2) != 0) s.SkewY = o.F;
                break;
            case Prop.Origin:
                if (o.E == 1) s.OriginX = o.L; else s.OriginY = o.L;
                break;
            case Prop.BorderStyle: s.BorderStyle = (BorderStyle)o.E; break;
            case Prop.OutlineWidth: s.OutlineWidth = o.F; break;
            case Prop.OutlineColor: s.OutlineColor = Col(o, ctx); break;
            case Prop.OutlineOffset: s.OutlineOffset = o.F; break;
            case Prop.OutlineStyle: s.OutlineStyle = (BorderStyle)o.E; break;
            case Prop.Transition: s.Transition = (TransitionProps)o.E; break;
            case Prop.Duration: s.DurationMs = o.F; break;
            case Prop.Delay: s.DelayMs = o.F; break;
            case Prop.Ease: s.Easing = (Easing)o.E; break;
            case Prop.Animation: s.Animation = (AnimationKind)o.E; break;
            case Prop.FontFamily: s.FontFamilyValue = (FontFamily)o.E; break;
            case Prop.Italic: s.ItalicValue = o.E != 0; break;
            case Prop.Decoration: s.DecorationValue = (TextDecoration)o.E; break;
            case Prop.TextCase: s.CaseValue = (TextCase)o.E; break;
            case Prop.Tracking:
                if (o.E == 1) s.TrackingPxValue = o.F; else { s.TrackingValue = o.F; s.TrackingPxValue = null; }
                break;
            case Prop.ObjectFit: s.ObjectFit = (ObjectFit)o.E; break;
            case Prop.ScrollbarMode: s.ScrollbarMode = (ScrollbarMode)o.E; break;
            case Prop.ScrollbarWidth: s.ScrollbarWidth = o.F; break;
            case Prop.ScrollbarColor: s.ScrollbarColor = Col(o, ctx); break;
            case Prop.Gradient or Prop.GradFrom or Prop.GradVia or Prop.GradTo or Prop.GradPos: ApplyGradient(ref s, o, Col(o, ctx)); break;
            default: ApplyText(ref s, o, ctx); break;
        }
    }

    /// <summary>Màu của op: token có biến thể dark (Theme.DarkColors) thì chọn theo chế độ hiện tại.</summary>
    static Rgba Col(in UtilityOp o, in VariantContext ctx) => ctx.Dark && o.HasDark ? o.D : o.C;

    static void SetEdges(ref EdgesLength e, byte mask, Length l)
    {
        if ((mask & Top) != 0) e.Top = l;
        if ((mask & Right) != 0) e.Right = l;
        if ((mask & Bottom) != 0) e.Bottom = l;
        if ((mask & Left) != 0) e.Left = l;
    }
}

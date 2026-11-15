using System.Collections.Concurrent;
using System.Globalization;

namespace GotX;

public static partial class Tailwind
{
    // ====================== Utility ======================

    const byte Top = 1, Right = 2, Bottom = 4, Left = 8, AllSides = 15;
    const byte TL = 1, TR = 2, BR = 4, BL = 8;
    const byte ScreenW = 1, ScreenH = 2;

    /// <summary>Biên dịch một utility (đã tách variant và dấu âm) thành các lệnh gán. Trả về false nếu không hỗ trợ.</summary>
    static bool Utility(string u, bool neg, UtilityOp t, List<UtilityOp> ops) =>
        StaticUtility(u, t, ops) || LayoutUtility(u, neg, t, ops) || GradientUtility(u, neg, t, ops)
        || ExtraUtility(u, neg, t, ops) || VisualUtility(u, neg, t, ops);

    static void AddOp(List<UtilityOp> ops, UtilityOp t, Prop p, byte e, float f, Length l, Rgba c)
    {
        t.Prop = p; t.E = e; t.F = f; t.L = l; t.C = c;
        if (t_dark is { } dark && p is Prop.Bg or Prop.TextColor or Prop.BorderColor or Prop.RingColor or Prop.RingOffsetColor
            or Prop.ShadowColor or Prop.DivideColor or Prop.OutlineColor or Prop.ScrollbarColor or Prop.GradFrom or Prop.GradVia or Prop.GradTo or Prop.DecorationColor)
        {
            t.D = dark; t.HasDark = true;
        }
        t_dark = null;
        ops.Add(t);
    }

    /// <summary>Utility không có giá trị: <c>flex</c>, <c>hidden</c>, <c>truncate</c>…</summary>
    static bool StaticUtility(string u, UtilityOp t, List<UtilityOp> ops)
    {
        void Emit(Prop p, byte e = 0, float f = 0, Length l = default, Rgba c = default) => AddOp(ops, t, p, e, f, l, c);

        switch (u)
        {
            case "flex": Emit(Prop.Display, (byte)Display.Flex); Emit(Prop.InlineLevel, 0); return true;
            case "inline-flex": Emit(Prop.Display, (byte)Display.Flex); Emit(Prop.InlineLevel, 1); return true;
            case "block": Emit(Prop.Display, (byte)Display.Block); Emit(Prop.InlineLevel, 0); return true;
            case "inline-block": Emit(Prop.Display, (byte)Display.Block); Emit(Prop.InlineLevel, 1); return true;
            case "inline": Emit(Prop.Display, (byte)Display.Block); return true;
            case "hidden": Emit(Prop.Display, (byte)Display.None); return true;
            case "flex-row": Emit(Prop.Direction, (byte)FlexDirection.Row); return true;
            case "flex-col": Emit(Prop.Direction, (byte)FlexDirection.Column); return true;
            case "flex-1": Emit(Prop.Grow, f: 1); Emit(Prop.Shrink, f: 1); Emit(Prop.Basis, l: Length.Px(0)); return true;
            case "flex-auto": Emit(Prop.Grow, f: 1); Emit(Prop.Shrink, f: 1); Emit(Prop.Basis, l: Length.Auto); return true;
            case "flex-initial": Emit(Prop.Grow, f: 0); Emit(Prop.Shrink, f: 1); Emit(Prop.Basis, l: Length.Auto); return true;
            case "flex-none": Emit(Prop.Grow, f: 0); Emit(Prop.Shrink, f: 0); Emit(Prop.Basis, l: Length.Auto); return true;
            case "grow": Emit(Prop.Grow, f: 1); return true;
            case "grow-0": Emit(Prop.Grow, f: 0); return true;
            case "shrink": Emit(Prop.Shrink, f: 1); return true;
            case "shrink-0": Emit(Prop.Shrink, f: 0); return true;
            case "absolute": Emit(Prop.Position, (byte)Position.Absolute); return true;
            case "relative": Emit(Prop.Position, (byte)Position.Relative); return true;
            case "static": Emit(Prop.Position, (byte)Position.Static); return true;
            case "overflow-hidden": case "overflow-clip": case "overflow-x-hidden": case "overflow-y-hidden":
                Emit(Prop.Overflow, (byte)Overflow.Hidden); Emit(Prop.Scroll, 0); return true;
            case "overflow-auto": case "overflow-scroll":
                Emit(Prop.Overflow, (byte)Overflow.Hidden); Emit(Prop.Scroll, 3); return true;
            case "overflow-x-auto": case "overflow-x-scroll":
                Emit(Prop.Overflow, (byte)Overflow.Hidden); Emit(Prop.Scroll, 1); return true;
            case "overflow-y-auto": case "overflow-y-scroll":
                Emit(Prop.Overflow, (byte)Overflow.Hidden); Emit(Prop.Scroll, 2); return true;
            case "flex-wrap": Emit(Prop.Wrap, 1); return true;
            case "flex-nowrap": Emit(Prop.Wrap, 0); return true;
            case "grid": Emit(Prop.Display, (byte)Display.Grid); Emit(Prop.InlineLevel, 0); return true;
            case "inline-grid": Emit(Prop.Display, (byte)Display.Grid); Emit(Prop.InlineLevel, 1); return true;
            case "col-span-full": Emit(Prop.ColSpan, f: -1); return true;
            case "row-span-full": Emit(Prop.RowSpan, f: -1); return true;
            case "transition": case "transition-all": Emit(Prop.Transition, (byte)TransitionProps.All); return true;
            case "transition-colors": Emit(Prop.Transition, (byte)TransitionProps.Colors); return true;
            case "transition-opacity": Emit(Prop.Transition, (byte)TransitionProps.Opacity); return true;
            case "transition-shadow": Emit(Prop.Transition, (byte)TransitionProps.Shadow); return true;
            case "transition-transform": Emit(Prop.Transition, (byte)TransitionProps.Transform); return true;
            case "transition-none": Emit(Prop.Transition, (byte)TransitionProps.None); return true;
            case "ease-linear": Emit(Prop.Ease, (byte)Easing.Linear); return true;
            case "ease-in": Emit(Prop.Ease, (byte)Easing.In); return true;
            case "ease-out": Emit(Prop.Ease, (byte)Easing.Out); return true;
            case "ease-in-out": Emit(Prop.Ease, (byte)Easing.InOut); return true;
            case "animate-pulse": Emit(Prop.Animation, (byte)AnimationKind.Pulse); return true;
            case "animate-ping": Emit(Prop.Animation, (byte)AnimationKind.Ping); return true;
            case "animate-bounce": Emit(Prop.Animation, (byte)AnimationKind.Bounce); return true;
            case "animate-none": Emit(Prop.Animation, (byte)AnimationKind.None); return true;
            case "overflow-visible": Emit(Prop.Overflow, (byte)Overflow.Visible); return true;
            case "truncate":
                Emit(Prop.Overflow, (byte)Overflow.Hidden); Emit(Prop.NoWrap, 1); Emit(Prop.Ellipsis, 1); return true;
            case "whitespace-nowrap": Emit(Prop.NoWrap, 1); return true;
            case "whitespace-normal": Emit(Prop.NoWrap, 0); return true;
            case "text-ellipsis": Emit(Prop.Ellipsis, 1); return true;
            case "text-clip": Emit(Prop.Ellipsis, 0); return true;
            case "text-left": case "text-start": Emit(Prop.TextAlign, (byte)TextAlign.Left); return true;
            case "text-center": Emit(Prop.TextAlign, (byte)TextAlign.Center); return true;
            case "text-right": case "text-end": Emit(Prop.TextAlign, (byte)TextAlign.Right); return true;
            case "cursor-pointer": Emit(Prop.Cursor, (byte)CursorKind.Pointer); return true;
            case "cursor-text": Emit(Prop.Cursor, (byte)CursorKind.Text); return true;
            case "cursor-default": case "cursor-auto": Emit(Prop.Cursor, (byte)CursorKind.Default); return true;
            case "border": Emit(Prop.BorderWidth, AllSides, 1); return true;
            case "divide-x": Emit(Prop.DivideX, f: 1); return true;
            case "divide-y": Emit(Prop.DivideY, f: 1); return true;
            case "animate-spin": Emit(Prop.Animation, (byte)AnimationKind.Spin); return true;
            case "border-solid": Emit(Prop.BorderStyle, (byte)BorderStyle.Solid); return true;
            case "border-dashed": Emit(Prop.BorderStyle, (byte)BorderStyle.Dashed); return true;
            case "border-dotted": Emit(Prop.BorderStyle, (byte)BorderStyle.Dotted); return true;
            case "border-none": case "border-hidden": Emit(Prop.BorderStyle, (byte)BorderStyle.None); return true;
            case "outline": Emit(Prop.OutlineWidth, f: 1); Emit(Prop.OutlineStyle, (byte)BorderStyle.Solid); return true;
            case "outline-none": case "outline-hidden": Emit(Prop.OutlineWidth, f: 0); return true;
            case "outline-solid": Emit(Prop.OutlineStyle, (byte)BorderStyle.Solid); return true;
            case "outline-dashed": Emit(Prop.OutlineStyle, (byte)BorderStyle.Dashed); return true;
            case "outline-dotted": Emit(Prop.OutlineStyle, (byte)BorderStyle.Dotted); return true;
            case "rounded": Emit(Prop.Radius, TL | TR | BR | BL, Theme.Radius[""]); return true;
            case "shadow": Emit(Prop.Shadow, (byte)ShadowPreset.Sm); return true;
            case "ring": Emit(Prop.RingWidth, f: 1); return true;
        }
        return TextUtility(u, t, ops);
    }

    /// <summary>Utility về kiểu chữ và ảnh không có giá trị số: font-mono, italic, underline, uppercase, tracking-*, object-*.</summary>
    static bool TextUtility(string u, UtilityOp t, List<UtilityOp> ops)
    {
        void Emit(Prop p, byte e = 0, float f = 0) => AddOp(ops, t, p, e, f, default, default);
        switch (u)
        {
            case "font-sans": Emit(Prop.FontFamily, (byte)FontFamily.Sans); return true;
            case "font-mono": Emit(Prop.FontFamily, (byte)FontFamily.Mono); return true;
            case "italic": Emit(Prop.Italic, 1); return true;
            case "not-italic": Emit(Prop.Italic, 0); return true;
            case "underline": Emit(Prop.Decoration, (byte)TextDecoration.Underline); return true;
            case "line-through": Emit(Prop.Decoration, (byte)TextDecoration.LineThrough); return true;
            case "no-underline": Emit(Prop.Decoration, (byte)TextDecoration.None); return true;
            case "uppercase": Emit(Prop.TextCase, (byte)TextCase.Upper); return true;
            case "lowercase": Emit(Prop.TextCase, (byte)TextCase.Lower); return true;
            case "capitalize": Emit(Prop.TextCase, (byte)TextCase.Capitalize); return true;
            case "normal-case": Emit(Prop.TextCase, (byte)TextCase.None); return true;
            case "tracking-tighter": Emit(Prop.Tracking, f: -0.05f); return true;
            case "tracking-tight": Emit(Prop.Tracking, f: -0.025f); return true;
            case "tracking-normal": Emit(Prop.Tracking, f: 0); return true;
            case "tracking-wide": Emit(Prop.Tracking, f: 0.025f); return true;
            case "tracking-wider": Emit(Prop.Tracking, f: 0.05f); return true;
            case "tracking-widest": Emit(Prop.Tracking, f: 0.1f); return true;
            case "object-contain": Emit(Prop.ObjectFit, (byte)ObjectFit.Contain); return true;
            case "object-cover": Emit(Prop.ObjectFit, (byte)ObjectFit.Cover); return true;
            case "object-fill": Emit(Prop.ObjectFit, (byte)ObjectFit.Fill); return true;
        }
        return false;
    }
}

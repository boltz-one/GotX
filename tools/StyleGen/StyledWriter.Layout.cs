// Phần display / flex / grid / transform / transition / opacity của Styled.g.cs.
static partial class StyledWriter
{
    static void Layout()
    {
        // ---- Display / flex ----
        Util("Block", "s.Display = Display.Block;", "display: block");
        Util("Flex", "s.Display = Display.Flex;", "display: flex");
        Util("Hidden", "s.Display = Display.None;", "display: none");
        Util("FlexRow", "s.Display = Display.Flex; s.FlexDirection = FlexDirection.Row;", "flex-row");
        Util("FlexCol", "s.Display = Display.Flex; s.FlexDirection = FlexDirection.Column;", "flex-col");
        Util("Flex1", "s.FlexGrowValue = 1; s.FlexShrinkValue = 1; s.FlexBasis = Length.Px(0);", "flex: 1 1 0%");
        Util("FlexAuto", "s.FlexGrowValue = 1; s.FlexShrinkValue = 1; s.FlexBasis = Length.Auto;", "flex: 1 1 auto");
        Util("FlexNone", "s.FlexGrowValue = 0; s.FlexShrinkValue = 0; s.FlexBasis = Length.Auto;", "flex: none");
        Util("Grow", "s.FlexGrowValue = 1;", "flex-grow: 1");
        Util("Grow0", "s.FlexGrowValue = 0;", "flex-grow: 0");
        Util("Shrink", "s.FlexShrinkValue = 1;", "flex-shrink: 1");
        Util("Shrink0", "s.FlexShrinkValue = 0;", "flex-shrink: 0");
        Util("Basis", "s.FlexBasis = v;", "flex-basis", "Length v", "v");
        foreach (var (n, v) in new[] { ("Start", "Start"), ("Center", "Center"), ("End", "End"),
            ("Between", "SpaceBetween"), ("Around", "SpaceAround"), ("Evenly", "SpaceEvenly") })
            Util("Justify" + n, $"s.Justify = Justify.{v};", $"justify-content: {v}");
        foreach (var n in new[] { "Start", "Center", "End", "Stretch" })
            Util("Items" + n, $"s.AlignItems = Align.{n};", $"align-items: {n}");
        Util("OverflowHidden", "s.Overflow = Overflow.Hidden; s.ScrollX = s.ScrollY = false;", "overflow: hidden");
        Util("OverflowVisible", "s.Overflow = Overflow.Visible;", "overflow: visible");
        Util("Absolute", "s.Position = Position.Absolute;", "position: absolute");
        Util("Relative", "s.Position = Position.Relative;", "position: relative");
        Util("SelfAuto", "s.AlignSelf = null;", "align-self: auto");
        foreach (var n in new[] { "Start", "Center", "End", "Stretch" })
            Util("Self" + n, $"s.AlignSelf = Align.{n};", $"align-self: {n}");
        Util("Truncate", "s.Overflow = Overflow.Hidden; s.NoWrapValue = true; s.EllipsisValue = true;", "truncate");
        Util("WhitespaceNowrap", "s.NoWrapValue = true;", "white-space: nowrap");
        Util("WhitespaceNormal", "s.NoWrapValue = false;", "white-space: normal");
        foreach (var n in new[] { "Default", "Pointer", "Text" })
            Util("Cursor" + n, $"s.Cursor = CursorKind.{n};", $"cursor: {n.ToLowerInvariant()}");
        Util("OverflowAuto", "s.Overflow = Overflow.Hidden; s.ScrollX = true; s.ScrollY = true;", "overflow: auto (cuộn 2 chiều)");
        Util("OverflowXAuto", "s.Overflow = Overflow.Hidden; s.ScrollX = true;", "overflow-x: auto");
        Util("OverflowYAuto", "s.Overflow = Overflow.Hidden; s.ScrollY = true;", "overflow-y: auto");
        Util("FlexWrap", "s.FlexWrap = true;", "flex-wrap: wrap");
        Util("FlexNowrap", "s.FlexWrap = false;", "flex-wrap: nowrap");
        Util("Grid", "s.Display = Display.Grid;", "display: grid");
        Util("GridCols", "s.GridColumns = GridTemplate.Repeat(n);", "grid-template-columns: repeat(n, 1fr)", "int n", "n");
        for (int n = 1; n <= 12; n++) Util($"GridCols{n}", $"s.GridColumns = GridTemplate.Repeat({n});", $"grid-cols-{n}");
        Util("GridRows", "s.GridRows = GridTemplate.Repeat(n);", "grid-template-rows: repeat(n, 1fr)", "int n", "n");
        for (int n = 1; n <= 6; n++) Util($"GridRows{n}", $"s.GridRows = GridTemplate.Repeat({n});", $"grid-rows-{n}");
        Util("ColSpan", "s.ColSpan = (short)n;", "grid-column: span n", "int n", "n");
        for (int n = 1; n <= 12; n++) Util($"ColSpan{n}", $"s.ColSpan = {n};", $"col-span-{n}");
        Util("ColSpanFull", "s.ColSpan = -1;", "col-span-full");
        Util("RowSpan", "s.RowSpan = (short)n;", "grid-row: span n", "int n", "n");
        for (int n = 1; n <= 6; n++) Util($"RowSpan{n}", $"s.RowSpan = {n};", $"row-span-{n}");
        Util("TranslateX", "s.TranslateX = v;", "translate-x", "Length v", "v");
        Util("TranslateY", "s.TranslateY = v;", "translate-y", "Length v", "v");
        Util("Scale", "s.ScaleX = v; s.ScaleY = v;", "scale (1 = 100%)", "float v", "v");
        Util("ScaleX", "s.ScaleX = v;", "scale-x", "float v", "v");
        Util("ScaleY", "s.ScaleY = v;", "scale-y", "float v", "v");
        foreach (var n in new[] { 0, 50, 75, 90, 95, 100, 105, 110, 125, 150 }) Util($"Scale{n}", $"s.ScaleX = s.ScaleY = {F(n / 100f)};", $"scale-{n}");
        Util("SkewX", "s.SkewX = deg;", "skew-x (độ)", "float deg", "deg");
        Util("SkewY", "s.SkewY = deg;", "skew-y (độ)", "float deg", "deg");
        Util("Origin", "s.OriginX = x; s.OriginY = y;", "transform-origin", "Length x, Length y", "x, y");
        foreach (var (n, x, y) in new[] { ("Center", "0.5f", "0.5f"), ("Top", "0.5f", "0f"), ("Bottom", "0.5f", "1f"), ("Left", "0f", "0.5f"), ("Right", "1f", "0.5f"),
            ("TopLeft", "0f", "0f"), ("TopRight", "1f", "0f"), ("BottomLeft", "0f", "1f"), ("BottomRight", "1f", "1f") })
            Util("Origin" + n, $"s.OriginX = Length.Fraction({x}); s.OriginY = Length.Fraction({y});", $"origin-{n.ToLowerInvariant()}");
        foreach (var n in new[] { "Solid", "Dashed", "Dotted", "None" })
        {
            Util("Border" + n, $"s.BorderStyle = BorderStyle.{n};", $"border-{n.ToLowerInvariant()}");
            Util("Outline" + n, n == "None" ? "s.OutlineWidth = 0;" : $"s.OutlineStyle = BorderStyle.{n};", $"outline-{n.ToLowerInvariant()}");
        }
        Util("Outline", "s.OutlineWidth = px;", "outline-width (px)", "float px", "px");
        Util("OutlineColor", "s.OutlineColor = c;", "outline-color", "Rgba c", "c");
        Util("OutlineOffset", "s.OutlineOffset = px;", "outline-offset (px)", "float px", "px");
        foreach (var (n, v) in new[] { ("", "All"), ("All", "All"), ("Colors", "Colors"), ("Opacity", "Opacity"), ("Shadow", "Shadow"), ("Transform", "Transform"), ("None", "None") })
            Util("Transition" + n, $"s.Transition = TransitionProps.{v};", $"transition{(n == "" ? "" : "-" + n.ToLowerInvariant())}");
        Util("Duration", "s.DurationMs = ms;", "transition-duration (ms)", "float ms", "ms");
        Util("Delay", "s.DelayMs = ms;", "transition-delay (ms)", "float ms", "ms");
        foreach (var n in new[] { 75, 100, 150, 200, 300, 500, 700, 1000 })
        {
            Util($"Duration{n}", $"s.DurationMs = {n};", $"duration-{n}");
            Util($"Delay{n}", $"s.DelayMs = {n};", $"delay-{n}");
        }
        foreach (var n in new[] { "Linear", "In", "Out", "InOut" }) Util("Ease" + n, $"s.Easing = Easing.{n};", $"ease-{n.ToLowerInvariant()}");
        foreach (var n in new[] { "Spin", "Pulse", "Ping", "Bounce", "None" }) Util("Animate" + n, $"s.Animation = AnimationKind.{n};", $"animate-{n.ToLowerInvariant()}");
        Util("Opacity", "s.Opacity = v;", "opacity (0..1)", "float v", "v");
        for (int o = 0; o <= 100; o += 5)
            Util($"Opacity{o}", $"s.Opacity = {F(o / 100f)};", $"opacity: {o}%");
    }
}

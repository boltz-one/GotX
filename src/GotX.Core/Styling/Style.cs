namespace GotX;

public enum Display : byte { Block, Flex, Grid, None }
public enum FlexDirection : byte { Row, Column }
public enum Justify : byte { Start, Center, End, SpaceBetween, SpaceAround, SpaceEvenly }
public enum Align : byte { Stretch, Start, Center, End }
public enum Overflow : byte { Visible, Hidden }
public enum Position : byte { Static, Relative, Absolute }
public enum CursorKind : byte { Default, Pointer, Text }

/// <summary>Khi nào hiện thanh cuộn: luôn luôn; khi con trỏ ở trên vùng cuộn (như ScrollArea type="hover"); khi đang cuộn; không bao giờ.</summary>
public enum ScrollbarMode : byte { Always, Hover, Scroll, None }

/// <summary>Nhóm thuộc tính được transition (transition-colors, -opacity, -shadow, -transform, transition = tất cả).</summary>
[Flags]
public enum TransitionProps : byte { None = 0, Colors = 1, Opacity = 2, Shadow = 4, Transform = 8, All = 15 }

public enum Easing : byte { Default, Linear, In, Out, InOut }

/// <summary>Animation dựng sẵn của Tailwind (keyframes): animate-spin, animate-pulse, animate-ping, animate-bounce.</summary>
public enum AnimationKind : byte { None, Pulse, Ping, Bounce, Spin }

public enum TrackKind : byte { Fr, Fixed, Auto }

/// <summary>Kiểu nét viền / outline (border-solid, border-dashed, border-dotted, border-none).</summary>
public enum BorderStyle : byte { Solid, Dashed, Dotted, None }

public enum FontWeight : ushort { Thin = 100, ExtraLight = 200, Light = 300, Normal = 400, Medium = 500, Semibold = 600, Bold = 700, ExtraBold = 800, Black = 900 }
public enum TextAlign : byte { Left, Center, Right }
public enum FontFamily : byte { Sans, Mono }
public enum TextDecoration : byte { None, Underline, LineThrough }
public enum TextCase : byte { None, Upper, Lower, Capitalize }

/// <summary>text-wrap-style: text-balance chia đều độ dài các dòng, text-pretty tránh dòng cuối chỉ có một từ.</summary>
public enum TextWrapStyle : byte { Auto, Balance, Pretty }

/// <summary>Cách ảnh / path lấp hộp của nó (object-contain, object-cover, object-fill).</summary>
public enum ObjectFit : byte { Contain, Cover, Fill }

/// <summary>Các bậc bóng đổ của Tailwind v4 (giá trị nằm trong <see cref="Theme"/>).</summary>
public enum ShadowPreset : byte { None, TwoXs, Xs, Sm, Md, Lg, Xl, TwoXl, Custom }

/// <summary>Một lớp bóng đổ kiểu CSS box-shadow, đơn vị logical pixel.</summary>
public readonly record struct BoxShadow(Rgba Color, float OffsetX, float OffsetY, float Blur, float Spread);

/// <summary>
/// Toàn bộ thuộc tính hiển thị của một element. Struct thuần dữ liệu nằm trong arena, không cấp phát.
/// Giống cách Tailwind ghép nhiều utility vào cùng một thuộc tính CSS qua biến (--tw-shadow, --tw-ring…),
/// bóng đổ và ring được lưu tách (preset, màu, độ rộng) rồi mới ghép thành các lớp khi vẽ.
/// Field có hậu tố "Value" để không bị trùng tên với utility (vd. <c>TextColor()</c>).
/// </summary>
public struct Style
{
    // Layout
    public Display Display;
    public FlexDirection FlexDirection;
    public Justify Justify;
    public Align AlignItems;
    public Align? AlignSelf;
    public Length ColumnGap, RowGap;
    public EdgesLength Padding;
    public EdgesLength Margin;
    public Length Width, Height, MinWidth, MinHeight, MaxWidth, MaxHeight;
    public float FlexGrowValue;
    public float FlexShrinkValue;
    public Length FlexBasis;
    public Overflow Overflow;
    public bool ScrollX, ScrollY;     // overflow-x/y-auto: cuộn theo trục
    public bool FlexWrap;
    public Position Position;
    public EdgesLength Inset;

    // Grid
    public GridTemplate? GridColumns, GridRows;
    public short ColSpan, RowSpan;    // 0 = 1, -1 = full

    // Biến đổi (transform-origin: tâm)
    public Length TranslateX, TranslateY;
    public float ScaleX, ScaleY;      // scale-50 đặt cả hai; scale-x-*/scale-y-* từng trục
    public float Rotate;              // độ (rotate-45, -rotate-12, rotate-[17deg])
    public float SkewX, SkewY;        // độ (skew-x-12, -skew-y-6)
    public Length OriginX, OriginY;   // transform-origin, mặc định 50% 50%

    // Chuyển động
    public TransitionProps Transition;
    public float DurationMs, DelayMs; // 0 = mặc định 150ms
    public Easing Easing;
    public AnimationKind Animation;

    // Vẽ
    public Rgba Background;
    public GradientStyle Gradient;     // background-image: bg-linear-* / bg-radial + from/via/to (vẽ trên Background)
    public Edges Border;               // độ rộng viền từng cạnh (px)
    public Rgba? BorderColorValue;     // null = currentColor (mặc định của Tailwind v4)
    public Rgba? BorderTopColor, BorderRightColor, BorderBottomColor, BorderLeftColor; // màu riêng từng cạnh
    public BorderStyle BorderStyle;    // border-dashed, border-dotted…
    /// <summary>Độ rộng viền thực dùng cho layout (border-none → 0, như CSS).</summary>
    public readonly Edges BorderBox => BorderStyle == BorderStyle.None ? default : Border;
    public float DivideX, DivideY;     // divide-x / divide-y: đường kẻ giữa các con
    public Rgba? DivideColor;

    // Outline: vẽ bên ngoài border box, cách một khoảng offset, theo bo góc
    public float OutlineWidth, OutlineOffset;
    public Rgba? OutlineColor;         // null = currentColor
    public BorderStyle OutlineStyle;
    public Corners CornerRadii;
    public float Opacity;
    public ShadowPreset Shadow;
    public BoxShadow CustomShadow;
    public Rgba? ShadowColor;
    public float RingWidth;
    public Rgba? RingColor;          // null = currentColor (mặc định v4)
    public float RingOffsetWidth;
    public Rgba? RingOffsetColor;    // null = trắng
    public CursorKind Cursor;

    // Thanh cuộn của phần tử overflow-*-auto (lớp mở rộng của GotX, không có trong Tailwind)
    public ScrollbarMode ScrollbarMode; // Always (mặc định) | Hover | Scroll | None
    public float ScrollbarWidth;        // bề rộng rãnh (px); 0 = thanh nổi mảnh mặc định
    public Rgba? ScrollbarColor;        // màu con trượt; null = màu mặc định

    // Text (kế thừa xuống con nếu có giá trị)
    public Rgba? TextColorValue;
    public Length? FontSizeValue;
    public Length? LineHeightValue;
    public FontWeight? FontWeightValue;
    public TextAlign? TextAlignValue;
    public bool? NoWrapValue;
    public bool? EllipsisValue;
    public FontFamily? FontFamilyValue;
    public bool? ItalicValue;
    public TextDecoration? DecorationValue;
    public TextCase? CaseValue;
    public float? TrackingValue;      // letter-spacing theo em (tracking-wide = 0.025)
    public float? TrackingPxValue;    // tracking-[2px]: letter-spacing tuyệt đối (thắng TrackingValue)
    public byte? LineClampValue;      // line-clamp-3; 0 = line-clamp-none
    public TextWrapStyle? WrapStyleValue;
    public bool? TabularNumsValue;    // tabular-nums (OpenType tnum), proportional-nums tắt
    public float? UnderlineOffsetValue; // underline-offset-4 (px); NaN = auto
    public Rgba? DecorationColorValue;
    public float? DecorationThicknessValue; // decoration-2 (px); 0 = auto

    // Tỉ lệ khung (aspect-video = 16/9); 0 = auto
    public float AspectRatio;
    // Vị trí grid theo đường kẻ (col-start-2, row-end-[-1]): 1-based, âm = đếm từ cuối, 0 = auto
    public short ColStart, ColEnd, RowStart, RowEnd;
    public bool GridFlowColumn, GridFlowDense; // grid-flow-col / grid-flow-dense

    // Ảnh / path
    public ObjectFit ObjectFit;

    /// <summary>Mặc định giống CSS: flex-shrink 1, align stretch, opacity 1.</summary>
    public static readonly Style Default = new()
    {
        FlexShrinkValue = 1f, Opacity = 1f, ScaleX = 1f, ScaleY = 1f,
        OriginX = Length.Fraction(0.5f), OriginY = Length.Fraction(0.5f),
    };
}

/// <summary>
/// Gradient nền theo kiểu Tailwind v4: hướng (<c>bg-linear-to-r</c>, <c>bg-linear-45</c>, <c>bg-radial</c>) tách khỏi các
/// điểm dừng (<c>from-*</c>, <c>via-*</c>, <c>to-*</c>, <c>from-10%</c>) – giống các biến --tw-gradient-*. Màu chưa đặt = #0000.
/// </summary>
public struct GradientStyle
{
    public GradientKind Kind;
    public float Angle;               // độ, CSS: 0 = lên trên, 90 = sang phải
    public sbyte CornerX, CornerY;    // to-tr = (+1, -1); khác 0 thì Angle bị bỏ qua (hướng phụ thuộc tỉ lệ hộp)
    public bool Oklab;                // "in oklab" (mặc định của bg-linear-*) hay sRGB (bg-linear-[…deg])
    public Rgba? From, Via, To;       // Via = null → chỉ hai điểm dừng
    public Length? FromPos, ViaPos, ToPos; // null = 0%, 50%, 100%
}

/// <summary>Truy cập Style của một element (handle trỏ vào arena).</summary>
public interface IStyled
{
    ref Style Style { get; }
}

/// <summary>Sửa Style tại chỗ, dùng cho <c>.Hover(static (ref Style s) =&gt; s.Bg(...))</c>.</summary>
public delegate void StyleRefiner(ref Style style);

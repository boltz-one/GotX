namespace GotX;

public enum LengthKind : byte { Auto, Px, Rems, Fraction }

/// <summary>Độ dài kiểu CSS: auto, px, rem, hoặc phần trăm của cha (Fraction 0..1).</summary>
public readonly record struct Length(LengthKind Kind, float Value)
{
    /// <summary>Phần cộng thêm (px) cho dạng calc(100% - 2rem): chỉ dùng với Fraction.</summary>
    public float Offset { get; init; }

    public static readonly Length Auto = default;
    public static readonly Length Full = Fraction(1f);
    public static Length Px(float v) => new(LengthKind.Px, v);
    public static Length Rems(float v) => new(LengthKind.Rems, v);
    public static Length Fraction(float v) => new(LengthKind.Fraction, v);

    /// <summary>Số float được hiểu là pixel: <c>.W(240)</c>.</summary>
    public static implicit operator Length(float px) => Px(px);

    public bool IsAuto => Kind == LengthKind.Auto;
    public Length Negate() => Kind == LengthKind.Auto ? this : new(Kind, -Value) { Offset = -Offset };

    /// <summary>Quy ra pixel; null nếu là auto hoặc phụ thuộc kích thước cha chưa biết (NaN).</summary>
    public float? Resolve(float parent, float rem) => Kind switch
    {
        LengthKind.Px => Value,
        LengthKind.Rems => Value * rem,
        LengthKind.Fraction => float.IsNaN(parent) ? null : parent * Value + Offset,
        _ => null,
    };
}

public record struct EdgesLength(Length Top, Length Right, Length Bottom, Length Left)
{
    public static EdgesLength All(Length v) => new(v, v, v, v);

    public readonly Edges Resolve(float parentWidth, float rem) => new(
        Top.Resolve(parentWidth, rem) ?? 0, Right.Resolve(parentWidth, rem) ?? 0,
        Bottom.Resolve(parentWidth, rem) ?? 0, Left.Resolve(parentWidth, rem) ?? 0);
}


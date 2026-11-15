using GotX;

namespace ShadcnShots;

/// <summary>A matched pair measured relative to its container pair: the container's own Δx/Δy removed.</summary>
public readonly record struct ResidualPair(LayoutPair Pair, LayoutPair Container, float Dx, float Dy)
{
    public float Delta => Pair.IsText
        ? MathF.Max(MathF.Abs(Dx), MathF.Max(MathF.Abs(Dy), MathF.Abs(Pair.Dw)))
        : MathF.Max(MathF.Max(MathF.Abs(Dx), MathF.Abs(Dy)), MathF.Max(MathF.Abs(Pair.Dw), MathF.Abs(Pair.Dh)));
}

/// <summary>An element whose vertical offset differs by more than 2 px from the element directly above it.</summary>
public readonly record struct OffsetJump(LayoutPair Pair, LayoutPair? Above, float Jump);

/// <summary>
/// Separates real drift from inherited offsets:
/// <list type="bullet">
/// <item><b>Residuals</b>: every pair inside a matched container (reference <c>data-slot</c> card / sidebar / sidebar-inset
/// box pair whose rect contains the element's centre; the innermost wins) minus that container's Δx/Δy;</item>
/// <item><b>Jumps</b>: walking top to bottom, the elements whose Δy differs by &gt; 2 px from the nearest matched element above
/// them that overlaps horizontally — where a vertical offset is introduced, so porters can fix offsets in order from the top.</item>
/// </list>
/// Δy of a text pair is its vertical-centre delta, of a box its top delta.
/// </summary>
public sealed class LayoutAnalysis
{
    static readonly HashSet<string> ContainerSlots = ["card", "sidebar", "sidebar-inset"];
    public const float JumpThreshold = 2;

    public List<ResidualPair> Residuals { get; } = new();
    public List<OffsetJump> Jumps { get; } = new();
    public int Containers { get; }

    public LayoutAnalysis(List<LayoutPair> pairs)
    {
        var containers = pairs.Where(p => !p.IsText && p.Ref.Slot is { } s && ContainerSlots.Contains(s)).ToList();
        Containers = containers.Count;
        foreach (var p in pairs)
        {
            var c = RefBox(p);
            float cx = c.X + c.Width / 2, cy = c.Y + c.Height / 2;
            LayoutPair? best = null;
            float bestArea = float.MaxValue;
            foreach (var k in containers)
            {
                if (ReferenceEquals(k.Ref, p.Ref)) continue;
                var r = k.Ref.Rect;
                float area = r.Width * r.Height;
                if (cx >= r.X && cx <= r.Right && cy >= r.Y && cy <= r.Bottom && area < bestArea) { best = k; bestArea = area; }
            }
            if (best is { } box) Residuals.Add(new ResidualPair(p, box, p.Dx - box.Dx, p.Dy - box.Dy));
        }

        var ordered = pairs.OrderBy(p => RefBox(p).Y).ThenBy(p => RefBox(p).X).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            var p = ordered[i];
            var b = RefBox(p);
            // Reference element: whichever edge is nearest above b's top — the bottom of a matched element above it
            // (horizontal overlap) or the top of the innermost matched box enclosing it (first element in a card → the card).
            LayoutPair? above = null;
            float edge = float.MinValue;
            for (int j = 0; j < ordered.Count; j++)
            {
                if (j == i) continue;
                var q = ordered[j];
                var a = RefBox(q);
                bool overlaps = a.X < b.Right && a.Right > b.X;
                if (overlaps && a.Bottom <= b.Y + 1 && a.Bottom > edge) { above = q; edge = a.Bottom; }
                bool encloses = !q.IsText && a.X <= b.X + 0.5f && a.Y <= b.Y + 0.5f && a.Right >= b.Right - 0.5f && a.Bottom >= b.Bottom - 0.5f
                    && a.Width * a.Height > b.Width * b.Height;
                if (encloses && a.Y > edge) { above = q; edge = a.Y; }
            }
            float jump = above is { } up ? p.Dy - up.Dy : p.Dy;
            if (MathF.Abs(jump) > JumpThreshold) Jumps.Add(new OffsetJump(p, above, jump));
        }
    }

    public (float Median, float P90, int Count) ResidualStats => Stats(Residuals.Select(r => r.Delta));

    public static Bounds RefBox(LayoutPair p) => p.IsText ? p.Ref.TextRect : p.Ref.Rect;

    public static (float Median, float P90, int Count) Stats(IEnumerable<float> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        if (sorted.Count == 0) return (0, 0, 0);
        float Q(float q) => sorted[Math.Clamp((int)MathF.Round(q * (sorted.Count - 1)), 0, sorted.Count - 1)];
        return (Q(0.5f), Q(0.9f), sorted.Count);
    }
}

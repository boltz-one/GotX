using GotX;

namespace ShadcnShots;

/// <summary>A reference element paired with a GotX node. Δ in CSS px (GotX − reference).</summary>
public readonly record struct LayoutPair(LayoutItem Ref, LayoutItem GotX, bool IsText, float Dx, float Dy, float Dw, float Dh)
{
    /// <summary>Text runs: max of |Δx|, |Δ vertical centre|, |Δw| (line-box heights differ by definition between DOM Range and GotX).
    /// Boxes: max of |Δx|, |Δy|, |Δw|, |Δh|.</summary>
    public float Delta => IsText
        ? MathF.Max(MathF.Abs(Dx), MathF.Max(MathF.Abs(Dy), MathF.Abs(Dw)))
        : MathF.Max(MathF.Max(MathF.Abs(Dx), MathF.Abs(Dy)), MathF.Max(MathF.Abs(Dw), MathF.Abs(Dh)));
}

public sealed class LayoutMatch
{
    public required List<LayoutPair> Pairs { get; init; }
    public int RefTexts { get; init; }
    public int RefBoxes { get; init; }
    public int MatchedTexts { get; init; }
    public int MatchedBoxes { get; init; }
    public float Median { get; init; }
    public float P90 { get; init; }
    public float Max { get; init; }
    /// <summary>Matched pairs within 2 px.</summary>
    public int Within2 { get; init; }

    public IEnumerable<LayoutPair> Worst(int count) => Pairs.OrderByDescending(p => p.Delta).Take(count);
}

/// <summary>
/// Pairs reference elements with GotX nodes by normalized text: text runs with text runs (DOM own text ↔ GotX text node),
/// boxes with boxes (data-slot element innerText ↔ GotX node with background/border/interaction and the same subtree text).
/// Duplicated texts are paired greedily by nearest position.
/// </summary>
public static class LayoutMatcher
{
    public static LayoutMatch Match(List<LayoutItem> reference, List<LayoutItem> gotx)
    {
        var pairs = new List<LayoutPair>();
        var refTexts = reference.Where(r => r.Own && r.Norm.Length > 0).ToList();
        var refBoxes = reference.Where(r => r.Box && r.Norm.Length > 0).ToList();
        int texts = Pair(refTexts, gotx.Where(g => g.Own).ToList(), isText: true, pairs);
        int boxes = Pair(refBoxes, gotx.Where(g => g.Box).ToList(), isText: false, pairs);
        var deltas = pairs.Select(p => p.Delta).OrderBy(d => d).ToList();
        return new LayoutMatch
        {
            Pairs = pairs, RefTexts = refTexts.Count, RefBoxes = refBoxes.Count, MatchedTexts = texts, MatchedBoxes = boxes,
            Median = Quantile(deltas, 0.5f), P90 = Quantile(deltas, 0.9f), Max = deltas.Count > 0 ? deltas[^1] : 0,
            Within2 = deltas.Count(d => d <= 2),
        };
    }

    static int Pair(List<LayoutItem> refs, List<LayoutItem> mine, bool isText, List<LayoutPair> output)
    {
        var byText = new Dictionary<string, List<int>>();
        for (int i = 0; i < mine.Count; i++)
        {
            string key = mine[i].Norm;
            if (key.Length == 0) continue;
            if (!byText.TryGetValue(key, out var list)) byText[key] = list = new List<int>();
            list.Add(i);
        }
        var candidates = new List<(float Dist, int R, int M)>();
        for (int r = 0; r < refs.Count; r++)
        {
            string key = refs[r].Norm;
            // The reference truncates text to 60 chars: compare GotX text by the same prefix.
            IEnumerable<int> hits = byText.TryGetValue(key, out var exact) ? exact
                : key.Length >= 59 ? mine.Select((m, i) => (m, i)).Where(t => t.m.Norm.StartsWith(key, StringComparison.Ordinal)).Select(t => t.i)
                : [];
            foreach (int m in hits)
            {
                var a = Box(refs[r], isText); var b = Box(mine[m], isText);
                float dx = a.X - b.X, dy = (a.Y + a.Height / 2) - (b.Y + b.Height / 2);
                candidates.Add((MathF.Sqrt(dx * dx + dy * dy), r, m));
            }
        }
        candidates.Sort(static (p, q) => p.Dist.CompareTo(q.Dist));
        var usedR = new bool[refs.Count];
        var usedM = new bool[mine.Count];
        int count = 0;
        foreach (var (_, r, m) in candidates)
        {
            if (usedR[r] || usedM[m]) continue;
            usedR[r] = usedM[m] = true;
            var a = Box(refs[r], isText); var b = Box(mine[m], isText);
            float dy = isText ? (b.Y + b.Height / 2) - (a.Y + a.Height / 2) : b.Y - a.Y;
            output.Add(new LayoutPair(refs[r], mine[m], isText, b.X - a.X, dy, b.Width - a.Width, b.Height - a.Height));
            count++;
        }
        return count;
    }

    static Bounds Box(LayoutItem i, bool isText) => isText ? i.TextRect : i.Rect;

    static float Quantile(List<float> sorted, float q) =>
        sorted.Count == 0 ? 0 : sorted[Math.Clamp((int)MathF.Round(q * (sorted.Count - 1)), 0, sorted.Count - 1)];
}

using System.Globalization;
using System.Text;

namespace ShadcnShots;

/// <summary>Per-page README lists from <see cref="LayoutAnalysis"/>: worst container-relative residuals and offset jumps.</summary>
public static class LayoutReport
{
    const int Rows = 12;

    public static void Append(StringBuilder sb, LayoutMatch l)
    {
        var a = l.Analysis;
        var inv = CultureInfo.InvariantCulture;
        if (a.Residuals.Count > 0)
        {
            sb.Append(inv, $"\n**Worst residuals inside containers** ({a.Containers} matched containers; container Δ removed)\n\n");
            sb.Append("| Kind | Text | Container | Container Δx, Δy | Residual Δx | Residual Δy | Δw | Δh |\n|---|---|---|---|---|---|---|---|\n");
            foreach (var r in a.Residuals.OrderByDescending(r => r.Delta).Take(Rows))
            {
                var p = r.Pair;
                sb.Append(inv, $"| {Kind(p)} | {ShotReport.Esc(p.Ref.Text)} | {ShotReport.Esc(Short(r.Container.Ref.Text))} | "
                    + $"{ShotReport.F(r.Container.Dx)}, {ShotReport.F(r.Container.Dy)} | {ShotReport.F(r.Dx)} | {ShotReport.F(r.Dy)} | "
                    + $"{ShotReport.F(p.Dw)} | {ShotReport.F(p.Dh)} |\n");
            }
        }
        if (a.Jumps.Count > 0)
        {
            sb.Append(inv, $"\n**Offset jumps, top → bottom** (first {Math.Min(Rows, a.Jumps.Count)} of {a.Jumps.Count}; Δy vs the element directly above or the enclosing box)\n\n");
            sb.Append("| Ref y | Kind | Text | Compared with | Its Δy | Δy | Jump |\n|---|---|---|---|---|---|---|\n");
            foreach (var j in a.Jumps.Take(Rows))
            {
                var p = j.Pair;
                string above = j.Above is { } up ? ShotReport.Esc(Short(up.Ref.Text)) : "(page top)";
                string upDy = j.Above is { } u ? ShotReport.F(u.Dy) : "0";
                sb.Append(inv, $"| {ShotReport.F(LayoutAnalysis.RefBox(p).Y)} | {Kind(p)} | {ShotReport.Esc(p.Ref.Text)} | {above} | "
                    + $"{upDy} | {ShotReport.F(p.Dy)} | {ShotReport.F(j.Jump)} |\n");
            }
        }
    }

    static string Kind(LayoutPair p) => p.IsText ? "text" : "box " + (p.Ref.Slot ?? p.Ref.Tag);
    static string Short(string? s) => s is null ? "" : s.Length > 30 ? s[..30] + "…" : s;
}

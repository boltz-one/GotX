using GotX.Raster;

namespace GotX.Test;

public sealed class DoctorOptions
{
    public Size[] Sizes { get; set; } = [new(1280, 800), new(800, 600), new(420, 760)];
    public bool[] Dark { get; set; } = [false, true];
    /// <summary>Scale đầu tiên chạy đủ mọi luật; các scale sau chỉ chạy <c>subpixel</c> (luật duy nhất phụ thuộc scale).</summary>
    public float[] Scales { get; set; } = [1, 1.25f, 1.5f, 2];
    public LintOptions Lint { get; set; } = new();
    /// <summary>Thư mục ghi ảnh đã khoanh vùng; null = không ghi ảnh.</summary>
    public string? OutDir { get; set; }
    /// <summary>Tiền tố tên file ảnh.</summary>
    public string Name { get; set; } = "doctor";
    /// <summary>Số frame tối đa chờ giao diện đứng yên trước khi soi.</summary>
    public int SettleFrames { get; set; } = 60;
    /// <summary>Thao tác đưa giao diện về trạng thái cần soi (mở hộp thoại, cuộn…) – chạy sau khi mở ở từng cấu hình.</summary>
    public Action<UiTest>? Prepare { get; set; }
}

/// <summary>Báo cáo của <see cref="UiDoctor"/>: các vấn đề đã gộp trùng qua mọi cấu hình, nặng trước.</summary>
public sealed class DoctorReport
{
    /// <summary>Mỗi vấn đề một phần tử; <see cref="LintIssue.Config"/> ghi các cấu hình gặp, <see cref="LintIssue.Artifact"/> là ảnh khoanh vùng.</summary>
    public List<LintIssue> Issues { get; } = new();
    public int Configs { get; internal set; }
    public List<string> Images { get; } = new();

    public int Count(Severity severity) => Issues.FindAll(i => i.Severity == severity).Count;

    /// <summary>Báo cáo văn bản đã xếp hạng, tối đa <paramref name="max"/> vấn đề.</summary>
    public string ToText(int max = 40)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"UiDoctor: {Configs} cấu hình, {Issues.Count} vấn đề ({Count(Severity.Error)} error, {Count(Severity.Warning)} warning, {Count(Severity.Info)} info)\n");
        for (int i = 0; i < Math.Min(max, Issues.Count); i++)
        {
            var issue = Issues[i];
            sb.Append($"{i + 1,3}. {issue.Severity.ToString().ToLowerInvariant(),-7} {issue.Rule,-13} {issue.Path} {Fmt.B(issue.Bounds)}{(issue.Count > 1 ? $" ×{issue.Count}" : "")}\n");
            sb.Append($"       {issue.Message}\n       [{issue.Config}]{(issue.Artifact is null ? "" : "  " + issue.Artifact)}\n");
        }
        if (Issues.Count > max) sb.Append($"… và {Issues.Count - max} vấn đề nữa\n");
        return sb.ToString();
    }
}

/// <summary>
/// "Chỉ vào một view là soi": mở view ở ma trận kích thước × sáng/tối × scale, chạy lint ở từng cấu hình, gộp các vấn đề
/// trùng nhau (cùng luật, cùng phần tử) và xếp hạng theo mức nặng rồi theo số cấu hình gặp phải.
/// </summary>
public static class UiDoctor
{
    sealed class Finding
    {
        public required LintIssue First;
        public readonly List<string> Configs = new();
        public int MaxCount, CountHere;
        public string? Image;
        public int ImageLabel;
    }

    public static DoctorReport Run(Func<App, View> root, DoctorOptions? options = null)
    {
        var o = options ?? new DoctorOptions();
        var report = new DoctorReport();
        var findings = new Dictionary<string, Finding>(StringComparer.Ordinal);
        var order = new List<Finding>();
        var subpixelOnly = new LintOptions { Tolerance = o.Lint.Tolerance }.Only("subpixel");
        foreach (var size in o.Sizes)
            foreach (bool dark in o.Dark)
                for (int s = 0; s < o.Scales.Length; s++)
                {
                    float scale = o.Scales[s];
                    string config = $"{Fmt.N(size.Width)}×{Fmt.N(size.Height)} {(dark ? "dark" : "light")} @{Fmt.N(scale)}";
                    var t = new UiTest(o.Name);
                    t.Open(root, size, scale, dark);
                    o.Prepare?.Invoke(t);
                    t.Settle(o.SettleFrames);
                    report.Configs++;
                    // Vị trí lẻ ở scale phân số là chuyện của cả cây (Core không ép lưới pixel): chỉ báo ở scale nguyên,
                    // còn scale phân số được tóm lại thành một dòng bên dưới.
                    var issues = t.Lint(s == 0 ? o.Lint : subpixelOnly);
                    if (scale != MathF.Round(scale)) issues = SummarizeFractional(t, issues, scale);
                    var marks = new List<Mark>();
                    var fresh = new List<Finding>();
                    foreach (var f in order) f.CountHere = 0;
                    foreach (var issue in issues)
                    {
                        if (!findings.TryGetValue(issue.Key, out var f))
                        {
                            findings[issue.Key] = f = new Finding { First = issue };
                            order.Add(f);
                            fresh.Add(f);
                            f.ImageLabel = fresh.Count;
                            marks.Add(new Mark(issue.Bounds, Color(issue.Severity), fresh.Count));
                        }
                        if (f.CountHere++ == 0) f.Configs.Add(config);
                        f.MaxCount = Math.Max(f.MaxCount, f.CountHere);
                    }
                    if (fresh.Count == 0 || o.OutDir is null) continue;
                    // Ảnh của cấu hình ĐẦU TIÊN gặp mỗi vấn đề; số trên khung khớp với "#n" ghi trong báo cáo.
                    Directory.CreateDirectory(o.OutDir);
                    string png = Path.Combine(o.OutDir, $"{o.Name}-{Fmt.N(size.Width)}x{Fmt.N(size.Height)}-{(dark ? "dark" : "light")}-{Fmt.N(scale)}x.png");
                    PngWriter.Write(png, Annotator.Annotate(t.Render(), scale, marks));
                    report.Images.Add(png);
                    foreach (var f in fresh) f.Image = png;
                }

        order.Sort((a, b) =>
        {
            int c = b.First.Severity.CompareTo(a.First.Severity);
            if (c == 0) c = b.Configs.Count.CompareTo(a.Configs.Count);
            if (c == 0) c = string.CompareOrdinal(a.First.Rule, b.First.Rule);
            return c != 0 ? c : string.CompareOrdinal(a.First.Path, b.First.Path);
        });
        foreach (var f in order)
        {
            var issue = f.First;
            issue.Count = f.MaxCount;
            issue.Config = f.Configs.Count == report.Configs ? $"mọi cấu hình ({report.Configs})"
                : f.Configs.Count <= 3 ? string.Join("; ", f.Configs) : $"{f.Configs[0]}; {f.Configs[1]}; … ({f.Configs.Count}/{report.Configs} cấu hình)";
            issue.Artifact = f.Image is null ? null : $"{f.Image} #{f.ImageLabel}";
            report.Issues.Add(issue);
        }
        return report;
    }

    static uint Color(Severity s) => s == Severity.Error ? Annotator.Red : s == Severity.Warning ? Annotator.Orange : Annotator.Blue;

    /// <summary>Ở scale phân số, gộp mọi vấn đề subpixel thành một dòng gắn vào node gốc (kèm vài ví dụ).</summary>
    static List<LintIssue> SummarizeFractional(UiTest t, List<LintIssue> issues, float scale)
    {
        var sub = issues.FindAll(i => i.Rule == "subpixel");
        if (sub.Count == 0) return issues;
        var rest = issues.FindAll(i => i.Rule != "subpixel");
        var rootNode = t.Tree.Nodes[0];
        rest.Add(new LintIssue
        {
            Rule = "subpixel", Severity = Severity.Info, Path = t.Tree.Path(rootNode, 0) + " (scale phân số)", Bounds = rootNode.Screen,
            Message = $"ở scale {Fmt.N(scale)} có {sub.Count} chỗ vị trí/kích thước rơi vào pixel lẻ (Core không ép bố cục vào lưới pixel thiết bị) → mép nền/viền nhoè; vd. {sub[0].Path} {Fmt.B(sub[0].Bounds)}",
        });
        return rest;
    }
}

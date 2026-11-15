using GotX.Raster;

namespace GotX.Test;

/// <summary>Lint và so ảnh chuẩn (visual regression) trên trạng thái hiện tại của test.</summary>
public sealed partial class UiTest
{
    /// <summary>Chạy <see cref="UiLint"/> trên cây hiện tại (tự dựng ảnh nếu luật tương phản đang bật).</summary>
    public List<LintIssue> Lint(LintOptions? options = null) =>
        UiLint.Run(Tree, UiLint.NeedsCanvas(options) ? Render() : null, options);

    /// <summary>Biến một vấn đề lint thành lỗi của test, khoanh phần tử (đỏ) và phần tử liên quan (cam) trên ảnh.</summary>
    public Failure FailIssue(LintIssue issue, string prefix = "")
    {
        string message = $"{prefix}{issue.Rule} ({issue.Severity.ToString().ToLowerInvariant()}): {issue.Message}" + (issue.Config is null ? "" : $"  [{issue.Config}]");
        if (issue.Artifact is { } ready)
        {
            // Vấn đề đến từ UiDoctor: ảnh của đúng cấu hình phát hiện đã có sẵn, trạng thái hiện tại của test có thể là cấu hình khác.
            var f = new Failure { Message = message, Path = issue.Path, Bounds = issue.Bounds, Trace = [] };
            f.Artifacts.Add(ready);
            Failures.Add(f);
            return f;
        }
        var marks = new List<Mark> { new(issue.Bounds, Annotator.Red) };
        if (issue.Related is { } related) marks.Add(new Mark(related, Annotator.Orange));
        return Record(message, issue.Path, issue.Bounds, marks, issue.NodeIndex >= 0 ? [issue.NodeIndex] : []);
    }

    /// <summary>
    /// So ảnh hiện tại với ảnh chuẩn <c>__snapshots__/&lt;name&gt;.png</c>. Chưa có ảnh chuẩn → tạo và ghi chú "created".
    /// Khác → ghi <c>.actual.png</c> + <c>.diff.png</c> vào thư mục ra, gom pixel khác thành vùng và chỉ ra PHẦN TỬ chứa
    /// từng vùng. <paramref name="tolerance"/>: chênh lệch mỗi kênh được bỏ qua; <paramref name="maxDiffPixels"/>: số pixel khác cho phép.
    /// </summary>
    public bool Snapshot(string name, int tolerance = 2, int maxDiffPixels = 0)
    {
        string dir = Options.SnapshotDir ?? throw new InvalidOperationException("Chưa cấu hình TestOptions.SnapshotDir.");
        string file = Path.Combine(dir, name + ".png");
        var canvas = Render();
        if (!File.Exists(file))
        {
            Directory.CreateDirectory(dir);
            PngWriter.Write(file, canvas);
            Notes.Add($"snapshot created: {file}");
            return true;
        }
        var expected = PngDecoder.Decode(File.ReadAllBytes(file), out int w, out int h);
        if (w != canvas.Width || h != canvas.Height)
        {
            if (Options.UpdateSnapshots) return Rewrite(file, canvas);
            var f = Fail($"Snapshot \"{name}\": kích thước ảnh {canvas.Width}×{canvas.Height} khác ảnh chuẩn {w}×{h}");
            f.Artifacts.Add(file);
            return false;
        }
        var diff = ImageDiff.Compare(canvas, expected, tolerance);
        if (diff.DiffPixels <= maxDiffPixels) return true;
        if (Options.UpdateSnapshots) return Rewrite(file, canvas);

        var failure = new Failure
        {
            Message = $"Snapshot \"{name}\": {diff.DiffPixels} pixel khác ảnh chuẩn (cho phép {maxDiffPixels}, lệch kênh lớn nhất {diff.MaxDelta}, ngưỡng {tolerance}) trong {diff.Regions.Count} vùng",
            Trace = Trace.Tail(30),
        };
        var marks = new List<Mark>();
        int shown = 0;
        foreach (var region in diff.Regions)
        {
            var logical = region.Area.Scale(1 / Scale);
            if (shown < 12) marks.Add(new Mark(logical, Annotator.Magenta, shown + 1));
            if (++shown > 8) continue;
            var owner = Tree.DeepestContaining(logical) ?? Tree.TopAt(Fmt.Center(logical));
            failure.Details.Add($"vùng {shown}: {Fmt.B(logical)} {region.Pixels} px → {(owner is null ? "(ngoài mọi phần tử)" : Tree.Describe(owner))}");
        }
        if (diff.Regions.Count > 8) failure.Details.Add($"… và {diff.Regions.Count - 8} vùng nữa");
        if (Options.OutDir is not null)
        {
            string actual = ArtifactPath(name + ".actual.png"), diffPng = ArtifactPath(name + ".diff.png");
            PngWriter.Write(actual, canvas);
            PngWriter.Write(diffPng, Annotator.Annotate(diff.Image, Scale, marks));
            failure.Artifacts.Add(diffPng);
            failure.Artifacts.Add(actual);
        }
        failure.Artifacts.Add(file);
        Failures.Add(failure);
        return false;
    }

    bool Rewrite(string file, Canvas canvas)
    {
        PngWriter.Write(file, canvas);
        Notes.Add($"snapshot updated: {file}");
        return true;
    }
}

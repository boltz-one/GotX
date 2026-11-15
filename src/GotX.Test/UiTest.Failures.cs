using GotX.Raster;

namespace GotX.Test;

/// <summary>Ghi nhận lỗi kèm bằng chứng. Một assertion hỏng không dừng test: lỗi được gom lại để runner in cả loạt.</summary>
public sealed partial class UiTest
{
    public bool Passed => Failures.Count == 0;

    /// <summary>
    /// Ghi một lỗi. <paramref name="node"/> là phần tử chính (khoanh đỏ trên ảnh, đánh dấu trong bản in cây),
    /// <paramref name="related"/> là các phần tử liên quan (khoanh cam).
    /// </summary>
    public Failure Fail(string message, UiNode? node = null, params UiNode[] related)
    {
        var marks = new List<Mark>();
        if (node is not null) marks.Add(new Mark(node.Screen, Annotator.Red));
        foreach (var r in related) marks.Add(new Mark(r.Screen, Annotator.Orange));
        var indices = new List<int>();
        if (node is not null) indices.Add(node.Index);
        foreach (var r in related) indices.Add(r.Index);
        return Record(message, node is null || _tree is null ? null : _tree.Path(node), node?.Screen, marks, indices);
    }

    /// <summary>Ghi một lỗi gắn với một vùng trên màn hình (logical px) thay vì một phần tử.</summary>
    public Failure FailAt(string message, Bounds area, string? path = null) =>
        Record(message, path, area, [new Mark(area, Annotator.Magenta)], []);

    internal Failure Record(string message, string? path, Bounds? bounds, IReadOnlyList<Mark> marks, IReadOnlyCollection<int> markNodes)
    {
        var failure = new Failure { Message = message, Path = path, Bounds = bounds, Trace = Trace.Tail(30) };
        Failures.Add(failure);
        AttachArtifacts(failure, marks, markNodes);
        return failure;
    }

    /// <summary>Exception thoát khỏi thân test: giữ nguyên stack để agent thấy dòng mã gây lỗi.</summary>
    public Failure FailException(Exception ex)
    {
        var failure = new Failure
        {
            Message = $"ném {ex.GetType().Name}: {ex.Message}",
            Stack = ex.StackTrace,
            Trace = Trace.Tail(30),
        };
        Failures.Add(failure);
        AttachArtifacts(failure, [], []);
        return failure;
    }

    /// <summary>
    /// Kèm ảnh có khoanh vùng + bản in cây của trạng thái hiện tại. Chỉ làm cho vài lỗi đầu của mỗi test, và không bao giờ
    /// để việc ghi bằng chứng gây thêm lỗi (lúc này app có thể đang ở trạng thái hỏng).
    /// </summary>
    void AttachArtifacts(Failure failure, IReadOnlyList<Mark> marks, IReadOnlyCollection<int> markNodes)
    {
        if (Options.OutDir is null || _window is null || _artifacts >= Options.MaxFailureArtifacts) return;
        int n = ++_artifacts;
        try
        {
            var canvas = Render();
            if (marks.Count > 0) canvas = Annotator.Annotate(canvas, Scale, marks);
            string png = ArtifactPath($"fail{n}.png");
            PngWriter.Write(png, canvas);
            failure.Artifacts.Add(png);
            string txt = ArtifactPath($"fail{n}.tree.txt");
            File.WriteAllText(txt, TreeDump.Dump(Tree, new DumpOptions { Mark = markNodes }));
            failure.Artifacts.Add(txt);
        }
        catch (Exception ex)
        {
            failure.Details.Add($"(không ghi được bằng chứng: {ex.GetType().Name}: {ex.Message})");
        }
    }
}

namespace GotX.Test;

/// <summary>Cây ngữ nghĩa (vai trò, tên, trạng thái) của trạng thái hiện tại – thứ công cụ hỗ trợ nhìn thấy.</summary>
public sealed partial class UiTest
{
    /// <summary>Ảnh chụp ngữ nghĩa của cùng trạng thái với <see cref="Tree"/> (dựng một lần rồi nhớ tới frame kế).</summary>
    public SemanticsTree Semantics => Tree.Semantics ?? throw new InvalidOperationException("Không dựng được cây ngữ nghĩa.");

    SemanticsTree BuildSemantics()
    {
        bool paused = Trace.Paused;
        Trace.Paused = true;
        try { return Window.Semantics(Size, Scale); }
        finally { Trace.Paused = paused; App.Flush(); }
    }
}

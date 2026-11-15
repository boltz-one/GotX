namespace GotX.Components;

/// <summary>View bọc một hàm dựng: dùng làm nội dung overlay hoặc nơi cần một View mà không muốn tạo lớp riêng.</summary>
public sealed class FragmentView : View
{
    readonly Func<Window, Div> _build;
    public FragmentView(Func<Window, Div> build) => _build = build;
    public override Div Render(Window w) => _build(w);
}

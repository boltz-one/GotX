namespace GotX.Components;

public static partial class Ui
{
    /// <summary>Khối giữ chỗ nhấp nháy; kích thước/bo góc do người gọi nối thêm (<c>.Class("h-4 w-32")</c>).</summary>
    public static Div Skeleton(Window w) => w.Div().Class(ComponentStyles.S.Skeleton);
}

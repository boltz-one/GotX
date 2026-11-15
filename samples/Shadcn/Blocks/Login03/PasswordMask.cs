using GotX;
using GotX.Components;

/// <summary>
/// Che mật khẩu như <c>type="password"</c>. Ô nhập của Core luôn vẽ đúng chuỗi nó giữ (chưa có chế độ che), nên ô chỉ
/// giữ các dấu chấm còn giá trị thật nằm ở đây; nhờ vậy Ctrl+C cũng chỉ chép ra dấu chấm, giống trình duyệt.
/// </summary>
sealed class PasswordMask
{
    const char Dot = '•';

    /// <summary>Mật khẩu thật.</summary>
    public string Value { get; private set; } = "";

    /// <summary>
    /// Gọi sau mỗi lần ô đổi nội dung. Mọi dấu chấm giống hệt nhau nên không thể so chuỗi để biết sửa ở đâu; thay vào
    /// đó dựa vào con trỏ: ngay sau một lần sửa, con trỏ đứng ở cuối đoạn vừa chèn (hoặc tại chỗ vừa xoá), nên phần
    /// trước đoạn chèn và phần sau con trỏ là các ký tự cũ còn giữ nguyên.
    /// Đọc chuỗi và con trỏ hiện tại của ô (không dùng chuỗi trong event) vì event tới qua hàng đợi, có thể đã cũ.
    /// </summary>
    public void Sync(Input input)
    {
        string shown = input.Text;
        var box = input.Box;
        int caret = Math.Min(box.Caret, shown.Length), start = caret;
        while (start > 0 && shown[start - 1] != Dot) start--;
        int head = Math.Min(start, Value.Length);
        int tail = Math.Min(shown.Length - caret, Value.Length - head);
        Value = string.Concat(Value.AsSpan(0, head), shown.AsSpan(start, caret - start), Value.AsSpan(Value.Length - tail));
        if (start == caret && shown.Length == Value.Length) return; // chỉ xoá (hoặc event do chính lần gán bên dưới): ô đã toàn dấu chấm

        // Gán Text đưa con trỏ về cuối; lùi lại đúng số ký tự đứng sau để gõ giữa chuỗi không bị nhảy con trỏ.
        input.Text = new string(Dot, Value.Length);
        for (int i = 0; i < tail; i++) box.HandleKey(Key.Left, Modifiers.None);
    }
}

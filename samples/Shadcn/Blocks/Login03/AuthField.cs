using GotX.Components;

/// <summary>Kiểu ô nhập của form, theo thuộc tính <c>type</c> của thẻ input trong bản gốc.</summary>
public enum AuthFieldKind : byte { Text, Email, Password }

/// <summary>
/// Một ô của form đăng nhập/đăng ký: ô nhập, giá trị thật (mật khẩu được che), và lỗi đang hiện. Kiểm tra theo đúng
/// những gì HTML của bản gốc khai báo: mọi ô đều <c>required</c>, ô email thêm luật của <c>type="email"</c>.
/// </summary>
public sealed class AuthField
{
    public const string RequiredMessage = "Please fill out this field.";
    public const string EmailMessage = "Please enter a valid email address.";

    bool _flagged;

    internal AuthField(string id, string label, AuthFieldKind kind, Input input)
    {
        Id = id; Label = label; Kind = kind; Input = input;
        // type="password": ô tự che bằng dấu chấm (Ctrl+C cũng chỉ chép ra dấu chấm, như trình duyệt).
        if (kind == AuthFieldKind.Password) input.Box.Password = true;
        // Khoá dựng một lần ở đây để Render không ghép chuỗi.
        Key = "auth-" + id; LabelKey = Key + "-label"; ErrorKey = Key + "-error";
    }

    /// <summary>Id của thẻ input trong bản gốc: <c>email</c>, <c>password</c>, <c>name</c>, <c>confirm-password</c>.</summary>
    public string Id { get; }
    public string Label { get; }
    public AuthFieldKind Kind { get; }
    public Input Input { get; }

    /// <summary>Khoá của khung ô nhập, của nhãn và của dòng lỗi trong cây (<c>auth-&lt;id&gt;</c>, <c>…-label</c>, <c>…-error</c>).</summary>
    public string Key { get; }
    public string LabelKey { get; }
    public string ErrorKey { get; }

    /// <summary>Thông báo lỗi đang hiện dưới ô; null = hợp lệ hoặc chưa gửi.</summary>
    public string? Error { get; private set; }

    /// <summary>Giá trị gửi đi: mật khẩu giữ nguyên, ô khác bỏ khoảng trắng hai đầu.</summary>
    public string Value => Kind == AuthFieldKind.Password ? Input.Text : Input.Text.Trim();

    internal string? Check()
    {
        string value = Value;
        if (value.Length == 0) return RequiredMessage;
        return Kind == AuthFieldKind.Email && !IsEmail(value) ? EmailMessage : null;
    }

    internal void SetError(string? error)
    {
        Error = error;
        bool invalid = error is not null;
        // Gán Invalid luôn làm ô render lại, nên chỉ gán khi thật sự đổi.
        if (_flagged != invalid) Input.Invalid = invalid;
        _flagged = invalid;
    }

    /// <summary>Luật tối thiểu của <c>type="email"</c>: đúng một '@', hai phía không rỗng, không có khoảng trắng.</summary>
    static bool IsEmail(string s)
    {
        int at = s.IndexOf('@');
        return at > 0 && at < s.Length - 1 && s.IndexOf('@', at + 1) < 0 && !s.Contains(' ');
    }
}

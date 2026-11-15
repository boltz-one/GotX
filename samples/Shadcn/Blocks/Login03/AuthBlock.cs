using GotX;
using GotX.Components;

/// <summary>Các nút/liên kết không gửi form (bản gốc là <c>&lt;a href="#"&gt;</c> và nút <c>type="button"</c>).</summary>
public enum AuthAction : byte { Home, Apple, Google, Meta, ForgotPassword, SignUp, SignIn, Terms, Privacy }

/// <summary>
/// Phần chung của các block đăng nhập/đăng ký (login-03/04, signup-03/04): các ô nhập, kiểm tra kiểu HTML
/// (<c>required</c>, <c>type="email"</c>) và kết quả gửi form. Lớp con khai báo danh sách ô và lo bố cục; các mảnh giao
/// diện dùng chung nằm ở AuthBlock.Parts.cs.
/// </summary>
public abstract partial class AuthBlock : View
{
    /// <summary>Form hợp lệ đã được gửi.</summary>
    public readonly record struct Submitted(string Email);

    /// <summary>Một nút mạng xã hội hoặc liên kết được bấm.</summary>
    public readonly record struct ActionInvoked(AuthAction Action);

    /// <summary>Khai báo một ô: id của thẻ input trong bản gốc, nhãn, kiểu, placeholder.</summary>
    protected readonly record struct FieldSpec(string Id, string Label, AuthFieldKind Kind, string Placeholder = "");

    AuthField[] _fields = [];
    string? _status;

    /// <summary>Các ô của form theo thứ tự trong bản gốc.</summary>
    protected abstract FieldSpec[] Spec { get; }

    /// <summary>Chữ trên nút gửi.</summary>
    protected abstract string SubmitLabel { get; }

    /// <summary>Phần đầu dòng trạng thái sau khi gửi thành công (theo sau là email).</summary>
    protected abstract string StatusPrefix { get; }

    public IReadOnlyList<AuthField> Fields => _fields;

    /// <summary>Email của lần gửi hợp lệ gần nhất; null khi chưa gửi hoặc lần gửi cuối bị từ chối.</summary>
    public string? SubmittedEmail { get; private set; }

    /// <summary>Nút/liên kết phụ được bấm gần nhất.</summary>
    public AuthAction? LastAction { get; private set; }

    /// <summary>Ô có id cho trước (<c>email</c>, <c>password</c>…).</summary>
    public AuthField Field(string id)
    {
        foreach (var f in _fields) if (f.Id == id) return f;
        throw new ArgumentException($"Form không có ô '{id}'.", nameof(id));
    }

    protected override void OnCreated()
    {
        var spec = Spec;
        _fields = new AuthField[spec.Length];
        for (int i = 0; i < spec.Length; i++)
        {
            var field = _fields[i] = new AuthField(spec[i].Id, spec[i].Label, spec[i].Kind, App.New(new Input { Placeholder = spec[i].Placeholder }));
            App.Subscribe<TextEntry.TextChanged>(field.Input, _ => OnTextChanged(field));
            // Enter trong ô nào cũng gửi form, như submit ngầm định của <form>.
            App.Subscribe<TextEntry.Submitted>(field.Input, _ => Submit());
        }
    }

    void OnTextChanged(AuthField field)
    {
        // TextChanged tới sau mỗi phím: chỉ khi ô đang mang lỗi mới phải render lại cả block (để gỡ dòng lỗi).
        if (field.Error is null) return;
        field.SetError(null);
        Notify();
    }

    /// <summary>
    /// Gửi form. Trình duyệt chặn submit và chỉ vào ô sai đầu tiên bằng bong bóng validation; ở đây không có bong bóng
    /// nên mọi ô sai đều hiện lỗi bên dưới, và ô sai đầu tiên nhận focus. Trả về true khi form hợp lệ.
    /// </summary>
    public bool Submit()
    {
        AuthField? firstInvalid = null;
        foreach (var f in _fields)
        {
            f.SetError(f.Check());
            if (f.Error is not null) firstInvalid ??= f;
        }
        bool ok = firstInvalid is null;
        string email = Field("email").Value;
        SubmittedEmail = ok ? email : null;
        // Ghép chuỗi một lần lúc gửi, không ghép trong Render.
        _status = ok ? StatusPrefix + email : null;
        firstInvalid?.Input.Focus();
        Notify();
        if (ok) Emit(new Submitted(email));
        return ok;
    }

    void Act(AuthAction action)
    {
        LastAction = action;
        Emit(new ActionInvoked(action));
    }
}

using GotX;
using GotX.Components;

/// <summary>
/// Form đăng ký của signup-03 và signup-04: nút "Create Account", mật khẩu nhập hai lần. Bản gốc chỉ khai báo
/// <c>required</c> (không có minlength, không so hai mật khẩu) nên ở đây cũng chỉ kiểm tra chừng đó.
/// </summary>
public abstract class SignupBlock : AuthBlock
{
    protected static readonly FieldSpec Email = new("email", "Email", AuthFieldKind.Email, "m@example.com");
    protected static readonly FieldSpec Password = new("password", "Password", AuthFieldKind.Password);
    protected static readonly FieldSpec ConfirmPassword = new("confirm-password", "Confirm Password", AuthFieldKind.Password);

    protected override string SubmitLabel => "Create Account";
    protected override string StatusPrefix => "Account created for ";

    /// <summary>Hai ô mật khẩu cạnh nhau, chung một dòng mô tả.</summary>
    protected Div PasswordPair(Window w) => Ui.Field(w)
        .Child(w.Div().Class("grid w-full grid-cols-2 gap-4").Child(FieldView(w, "password")).Child(FieldView(w, "confirm-password")))
        .Child(Ui.FieldDescription(w, "Must be at least 8 characters long."));

    protected Div SignInLine(Window w) => SwitchLine(w, "Already have an account?", AuthAction.SignIn, "Sign in");
}

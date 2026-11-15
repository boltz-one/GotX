using GotX;

/// <summary>Form đăng nhập của login-03 và login-04: email + mật khẩu, nút "Login".</summary>
public abstract class LoginBlock : AuthBlock
{
    static readonly FieldSpec[] LoginFields =
    [
        new("email", "Email", AuthFieldKind.Email, "m@example.com"), new("password", "Password", AuthFieldKind.Password),
    ];

    protected override FieldSpec[] Spec => LoginFields;
    protected override string SubmitLabel => "Login";
    protected override string StatusPrefix => "Signed in as ";

    protected Div SignUpLine(Window w) => SwitchLine(w, "Don't have an account?", AuthAction.SignUp, "Sign up");
}

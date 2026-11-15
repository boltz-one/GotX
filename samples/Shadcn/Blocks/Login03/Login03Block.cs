using GotX;
using GotX.Components;

/// <summary>
/// Block login-03 của shadcn/ui: trang nền muted, logo "Acme Inc." và một card đăng nhập (nút Apple/Google, email +
/// mật khẩu). Nguồn: apps/v4/registry/new-york-v4/blocks/login-03 (page.tsx + components/login-form.tsx).
/// </summary>
public sealed class Login03Block : LoginBlock
{
    public static readonly SitePage Page = new("login-03", "Login 03", static app => app.New(new Login03Block()), FullBleed: true, Style: ComponentStyle.NewYorkV4);

    public override Div Render(Window w)
    {
        var form = Ui.FieldGroup(w)
            .Child(Ui.Field(w)
                .Child(Social(w, AuthAction.Apple, BlockIcons.Apple, "Login with Apple"))
                .Child(Social(w, AuthAction.Google, BlockIcons.Google, "Login with Google")))
            .Child(Ui.FieldSeparator(w, "Or continue with").Class("-my-2") /* TODO(style): FieldSeparator chưa có trong bảng style (-my-2 của v4) */)
            .Child(FieldView(w, "email"))
            .Child(FieldView(w, "password", forgotLink: true))
            .Child(SubmitField(w).Child(SignUpLine(w)));

        var card = Ui.Card(w).Key("auth-card")
            .Child(Ui.CardHeader(w).Class("text-center")
                .Child(Ui.CardTitle(w, "Welcome back").Class("text-xl"))
                .Child(Ui.CardDescription(w, "Login with your Apple or Google account")))
            .Child(Ui.CardContent(w).Child(form));

        return PageFrame(w, "login-03", w.Div().Class(ColumnSm + " flex flex-col gap-6")
            .Child(Logo(w))
            .Child(w.Div().Class("flex flex-col gap-6").Child(card).Child(TermsLine(w, narrow: true))));
    }
}

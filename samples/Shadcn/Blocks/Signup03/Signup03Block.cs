using GotX;
using GotX.Components;

/// <summary>
/// Block signup-03 của shadcn/ui: trang nền muted, logo "Acme Inc." và card tạo tài khoản (họ tên, email, mật khẩu ×2).
/// Nguồn: apps/v4/registry/new-york-v4/blocks/signup-03 (page.tsx + components/signup-form.tsx).
/// </summary>
public sealed class Signup03Block : SignupBlock
{
    public static readonly SitePage Page = new("signup-03", "Signup 03", static app => app.New(new Signup03Block()), FullBleed: true, Style: ComponentStyle.NewYorkV4);

    static readonly FieldSpec[] FieldSpecs = [new("name", "Full Name", AuthFieldKind.Text, "John Doe"), Email, Password, ConfirmPassword];

    protected override FieldSpec[] Spec => FieldSpecs;

    public override Div Render(Window w)
    {
        var form = Ui.FieldGroup(w)
            .Child(FieldView(w, "name"))
            .Child(FieldView(w, "email"))
            .Child(PasswordPair(w))
            .Child(SubmitField(w).Child(SignInLine(w)));

        var card = Ui.Card(w).Key("auth-card")
            .Child(Ui.CardHeader(w).Class("text-center")
                .Child(Ui.CardTitle(w, "Create your account").Class("text-xl"))
                .Child(Ui.CardDescription(w, "Enter your email below to create your account")))
            .Child(Ui.CardContent(w).Child(form));

        return PageFrame(w, "signup-03", w.Div().Class(ColumnSm + " flex flex-col gap-6")
            .Child(Logo(w))
            .Child(w.Div().Class("flex flex-col gap-6").Child(card).Child(TermsLine(w, narrow: true))));
    }
}

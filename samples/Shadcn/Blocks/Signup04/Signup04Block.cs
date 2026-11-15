using GotX;
using GotX.Components;

/// <summary>
/// Block signup-04 của shadcn/ui: card hai cột – form tạo tài khoản bên trái, ảnh bên phải (ẩn dưới <c>md</c>).
/// Nguồn: apps/v4/registry/new-york-v4/blocks/signup-04 (page.tsx + components/signup-form.tsx).
/// </summary>
public sealed class Signup04Block : SignupBlock
{
    public static readonly SitePage Page = new("signup-04", "Signup 04", static app => app.New(new Signup04Block()), FullBleed: true);

    static readonly FieldSpec[] FieldSpecs = [Email, Password, ConfirmPassword];

    protected override FieldSpec[] Spec => FieldSpecs;

    public override Div Render(Window w)
    {
        var form = Ui.FieldGroup(w)
            .Child(w.Div().Class("flex flex-col items-center gap-2 text-center")
                .Child(w.Div().Class("text-2xl font-bold").Child("Create your account"))
                .Child(w.Div().Class("text-sm text-muted-foreground").Child("Enter your email below to create your account")))
            .Child(FieldView(w, "email", description: "We'll use this to contact you. We will not share your email with anyone else."))
            .Child(PasswordPair(w))
            .Child(SubmitField(w))
            .Child(Ui.FieldSeparator(w, "Or continue with"))
            .Child(SocialRow(w))
            .Child(SignInLine(w));

        var card = Ui.Card(w).Key("auth-card").Class("p-0")
            .Child(Ui.CardContent(w).Class("grid p-0 md:grid-cols-2")
                .Child(w.Div().Key("auth-form").Class("p-6 md:p-8").Child(form))
                .Child(PlaceholderImage.Panel(w)));

        // max-w-sm md:max-w-4xl: 24rem, từ md trở lên 56rem.
        return PageFrame(w, "signup-04", w.Div().Class(ColumnSm + " md:w-[896px]")
            .Child(w.Div().Class("flex flex-col gap-6").Child(card).Child(TermsLine(w))));
    }
}

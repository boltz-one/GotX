using GotX;
using GotX.Components;

/// <summary>
/// Block login-04 của shadcn/ui: card hai cột – form bên trái, ảnh bên phải (ẩn dưới <c>md</c>).
/// Nguồn: apps/v4/registry/new-york-v4/blocks/login-04 (page.tsx + components/login-form.tsx).
/// </summary>
public sealed class Login04Block : LoginBlock
{
    public static readonly SitePage Page = new("login-04", "Login 04", static app => app.New(new Login04Block()), FullBleed: true, Style: ComponentStyle.NewYorkV4);

    public override Div Render(Window w)
    {
        var form = Ui.FieldGroup(w)
            .Child(w.Div().Class("flex flex-col items-center gap-2 text-center")
                .Child(w.Div().Class("text-2xl font-bold").Child("Welcome back"))
                .Child(w.Div().Class("text-balance text-muted-foreground").Child("Login to your Acme Inc account")))
            .Child(FieldView(w, "email"))
            .Child(FieldView(w, "password", forgotLink: true, forgotOffset: 2))
            .Child(SubmitField(w))
            .Child(Ui.FieldSeparator(w, "Or continue with").Class("-my-2") /* TODO(style): FieldSeparator chưa có trong bảng style (-my-2 của v4) */)
            .Child(SocialRow(w))
            .Child(SignUpLine(w));

        var card = Ui.Card(w).Key("auth-card").Class("overflow-hidden p-0")
            .Child(Ui.CardContent(w).Class("grid p-0 md:grid-cols-2")
                .Child(w.Div().Key("auth-form").Class("p-6 md:p-8").Child(form))
                .Child(PlaceholderImage.Panel(w)));

        // max-w-sm md:max-w-4xl: 24rem, từ md trở lên 56rem.
        return PageFrame(w, "login-04", w.Div().Class(ColumnSm + " md:w-[896px]")
            .Child(w.Div().Class("flex flex-col gap-6").Child(card).Child(TermsLine(w, narrow: false))));
    }
}

using GotX;
using GotX.Components;

/// <summary>
/// Thẻ "Account Access" (account-access.tsx): email, mật khẩu, nút cập nhật và dòng "Danger Zone".
/// Ô mật khẩu che ký tự bằng <c>TextInput.Password</c> của ô nhập bên trong <see cref="Input"/>.
/// </summary>
public sealed class AccountAccessCard : View
{
    /// <summary>Người dùng bấm Update Security.</summary>
    public readonly record struct SecurityUpdated(string Email, string Password);

    /// <summary>Người dùng bấm một liên kết của thẻ ("forgot" hoặc "danger").</summary>
    public readonly record struct LinkOpened(string Link);

    Input _email = null!, _password = null!;

    public Input Email => _email;
    public Input Password => _password;

    protected override void OnCreated()
    {
        _email = App.New(new Input { Placeholder = "artist@studio.inc" });
        _password = App.New(new Input { Placeholder = "••••••••••••••••••••••••" });
        _password.Box.Password = true; // Input chưa có thuộc tính riêng: đặt trên TextInput bên trong (có sau App.New)
    }

    void Update() => Emit(new SecurityUpdated(_email.Text, _password.Text));

    public override Div Render(Window w) =>
        HomeUi.Card(w).Key("access-card")
            .Child(HomeUi.Header(w)
                .Child(Ui.CardTitle(w, "Account Access"))
                .Child(Ui.CardDescription(w, "Update your credentials or re-authenticate.")))
            .Child(HomeUi.Content(w).Child(HomeUi.FieldGroup(w)
                .Child(HomeUi.Field(w)
                    .Child(Ui.FieldLabel(w, "Email Address", _email))
                    .Child(w.Div().Key("email-address").Child(w.View(_email))))
                .Child(HomeUi.Field(w)
                    .Child(w.Div().Class("flex items-center justify-between")
                        .Child(Ui.FieldLabel(w, "Current Password", _password))
                        .Child(w.Div().Class(HomeUi.Caps).Class("cursor-pointer rounded-sm hover:text-foreground " + HomeUi.Ring)
                            .Key("forgot-password").Focusable()
                            .OnClick(this, static (c, _) => c.Emit(new LinkOpened("forgot"))).Child("Forgot?")))
                    .Child(w.Div().Key("current-password").Child(w.View(_password))))))
            .Child(HomeUi.Footer(w).Class("flex-col gap-4")
                .Child(Ui.Button(w).Class("w-full").Key("update-security").OnClick(this, static (c, _) => c.Update())
                    .Child(Icon.Lock(w)).Child("Update Security"))
                .Child(HomeUi.LinkItem(w, "danger-zone").OnClick(this, static (c, _) => c.Emit(new LinkOpened("danger")))
                    .Child(HomeUi.Media(w, Icon.AlertCircle(w).Class("text-destructive")))
                    .Child(Ui.ItemContent(w).Class("min-w-0")
                        .Child(Ui.ItemTitle(w, "Danger Zone"))
                        .Child(Ui.ItemDescription(w, "Archive account and remove catalog").Class("truncate")))
                    .Child(Icon.ChevronRight(w))));
}

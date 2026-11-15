using GotX;
using GotX.Components;

/// <summary>Các mảnh giao diện mà các block đăng nhập/đăng ký dùng chung (cùng chữ, cùng hành vi, khác chỗ đặt).</summary>
public abstract partial class AuthBlock
{
    // Chỉ số = (int)AuthAction. Khoá dựng sẵn để Render không ghép chuỗi.
    static readonly string[] ActionKeys =
    [
        "auth-home", "auth-apple", "auth-google", "auth-meta", "auth-forgot-password", "auth-sign-up", "auth-sign-in", "auth-terms", "auth-privacy",
    ];

    const string FocusRing = " rounded-sm cursor-pointer focus-visible:ring-3 focus-visible:ring-ring/50";

    // Liên kết trong dòng mô tả ([&>a]:underline [&>a:hover]:text-primary của FieldDescription).
    const string DescriptionLink = "underline hover:text-primary" + FocusRing;

    // "Forgot your password?": chỉ gạch chân khi rê chuột. underline-offset-* chưa có trong Core nên bỏ.
    const string ForgotLink = "text-sm font-normal text-foreground hover:underline" + FocusRing;

    // Dòng chữ có liên kết ở giữa: Core chưa có chữ inline lẫn phần tử, nên mỗi đoạn là một ô của hàng flex tự xuống dòng.
    const string InlineText = "flex flex-wrap justify-center gap-x-1 text-center text-sm leading-normal font-normal text-muted-foreground";

    /// <summary>
    /// Cột nội dung <c>w-full max-w-sm</c>. Core chưa kẹp được bề rộng phần trăm bằng max-w, và một ô flex có bề rộng cố
    /// định bị co lại thì vẫn đo chiều cao theo bề rộng chưa co (chữ xuống dòng sai). Vì vậy chọn bề rộng theo breakpoint:
    /// hết chiều ngang dưới sm, 24rem từ sm trở lên – chỉ khác bản gốc ở khoảng 432–640 px (cột rộng hơn 24rem).
    /// </summary>
    protected const string ColumnSm = "w-full sm:w-96";

    Indexed<AuthBlock>[]? _actionRefs;

    /// <summary>
    /// Khung trang của bản gốc: <c>flex min-h-svh items-center justify-center bg-muted p-6 md:p-10</c>. "svh" ở đây là
    /// ô trang của shell: gốc chiếm hết ô và tự cuộn dọc; lớp trong lấp đầy ô khi nội dung thấp (để căn giữa) nhưng
    /// không co dưới chiều cao nội dung (flex-auto shrink-0 thay cho min-h-full mà Core chưa tính theo phần trăm).
    /// </summary>
    protected static Div PageFrame(Window w, string key, Div column) =>
        w.Div().Key(key).Class("flex size-full flex-col overflow-y-auto bg-muted")
            .Child(w.Div().Class("flex flex-auto shrink-0 items-center justify-center p-6 md:p-10").Child(column));

    /// <summary>Gắn hành vi của một nút/liên kết phụ: focus được bằng Tab, bấm thì ghi lại và phát <see cref="ActionInvoked"/>.</summary>
    protected Div Wire(Div element, AuthAction action)
    {
        var refs = Indexed<AuthBlock>.Ensure(ref _actionRefs, this, ActionKeys.Length);
        return element.Key(ActionKeys[(int)action]).Focusable()
            .OnClick(refs[(int)action], static (r, _) => r.Owner.Act((AuthAction)r.Index));
    }

    Div Link(Window w, AuthAction action, string text, string classes = DescriptionLink) =>
        Wire(w.Div().Class(classes).Child(text), action);

    /// <summary>Logo "Acme Inc." phía trên card (liên kết về trang chủ trong bản gốc).</summary>
    protected Div Logo(Window w) =>
        Wire(w.Div().Class("flex items-center gap-2 self-center font-medium" + FocusRing), AuthAction.Home)
            .Child(w.Div().Class("flex size-6 items-center justify-center rounded-md bg-primary text-primary-foreground")
                .Child(w.Path(BlockIcons.GalleryVerticalEnd).Class("size-4")))
            .Child("Acme Inc.");

    /// <summary>Nút mạng xã hội; <paramref name="label"/> = null là nút chỉ có logo (chữ <c>sr-only</c> của bản gốc không có chỗ hiện).</summary>
    protected Div Social(Window w, AuthAction action, VectorPath logo, string? label = null)
    {
        var button = Wire(Ui.Button(w, ButtonVariant.Outline), action).Child(w.Path(logo).Class("size-4 shrink-0"));
        return label is null ? button : button.Child(label);
    }

    /// <summary>Ba nút Apple / Google / Meta chỉ có logo của các block hai cột.</summary>
    protected Div SocialRow(Window w) => Ui.Field(w).Class("grid grid-cols-3 gap-4")
        .Child(Social(w, AuthAction.Apple, BlockIcons.Apple))
        .Child(Social(w, AuthAction.Google, BlockIcons.Google))
        .Child(Social(w, AuthAction.Meta, BlockIcons.Meta));

    /// <summary>
    /// Field của một ô: nhãn (bấm thì ô nhận focus), ô nhập, mô tả nếu có, và dòng lỗi khi ô đang sai.
    /// <paramref name="forgotLink"/> đặt "Forgot your password?" cùng hàng với nhãn như ô mật khẩu của form đăng nhập.
    /// </summary>
    protected Div FieldView(Window w, string id, bool forgotLink = false, string? description = null)
    {
        var f = Field(id);
        var label = Ui.FieldLabel(w, f.Label, f.Input).Key(f.LabelKey);
        if (forgotLink)
            label = w.Div().Class("flex items-center")
                .Child(label)
                .Child(w.Div().Class("flex-1")) // thay cho ml-auto của liên kết
                .Child(Link(w, AuthAction.ForgotPassword, "Forgot your password?", ForgotLink));
        var field = Ui.Field(w, invalid: f.Error is not null).Child(label).Child(w.Div().Key(f.Key).Child(w.View(f.Input)));
        if (description is not null) field.Child(Ui.FieldDescription(w, description));
        return f.Error is { } error ? field.Child(Ui.FieldError(w, error).Key(f.ErrorKey)) : field;
    }

    /// <summary>Field chứa nút gửi; sau khi gửi thành công có thêm dòng trạng thái (bản gốc không có – thay cho việc điều hướng).</summary>
    protected Div SubmitField(Window w)
    {
        var field = Ui.Field(w).Child(Ui.Button(w, SubmitLabel).Key("auth-submit").OnClick(this, static (b, _) => b.Submit()));
        return _status is { } status ? field.Child(w.Div().Class(InlineText).Key("auth-status").Child(status)) : field;
    }

    /// <summary>Dòng chuyển qua lại giữa đăng nhập và đăng ký: câu hỏi + liên kết.</summary>
    protected Div SwitchLine(Window w, string question, AuthAction action, string linkText) =>
        w.Div().Class(InlineText).Child(question).Child(Link(w, action, linkText));

    protected Div TermsLine(Window w) => w.Div().Class(InlineText).Class("px-6")
        .Child("By clicking continue, you agree to our")
        .Child(Link(w, AuthAction.Terms, "Terms of Service"))
        .Child("and")
        .Child(Link(w, AuthAction.Privacy, "Privacy Policy"))
        .Child(w.Div().Class("-ml-1").Child(".")); // dấu chấm dính liền liên kết: bù lại khoảng gap-x-1
}

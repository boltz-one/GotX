using GotX;
using GotX.Components;

/// <summary>Các mảnh giao diện mà các block đăng nhập/đăng ký dùng chung (cùng chữ, cùng hành vi, khác chỗ đặt). Class theo new-york-v4 (<see cref="V4"/>).</summary>
public abstract partial class AuthBlock
{
    // Chỉ số = (int)AuthAction. Khoá dựng sẵn để Render không ghép chuỗi.
    static readonly string[] ActionKeys =
    [
        "auth-home", "auth-apple", "auth-google", "auth-meta", "auth-forgot-password", "auth-sign-up", "auth-sign-in", "auth-terms", "auth-privacy",
    ];

    // Thẻ <a> của bản gốc không có class focus riêng: trình duyệt vẽ outline auto màu outline-ring/50 (lớp base của shadcn).
    const string LinkFocus = " cursor-pointer rounded-xs focus-visible:outline-2 focus-visible:outline-ring/50";

    // Liên kết trong FieldDescription: [&>a]:underline [&>a]:underline-offset-4 [&>a:hover]:text-primary.
    const string DescriptionLink = "underline underline-offset-4" + LinkFocus;
    const string DescriptionLinkHover = "underline underline-offset-4 text-primary" + LinkFocus;

    // Khoảng trắng giữa các từ của dòng chữ có liên kết (advance của dấu cách Geist 14px).
    const string Words = "flex flex-wrap gap-x-[3.5px] text-sm leading-normal font-normal text-muted-foreground";

    /// <summary>
    /// Cột nội dung <c>w-full max-w-sm</c>. Core chưa kẹp được bề rộng phần trăm bằng max-w, và một ô flex có bề rộng cố
    /// định bị co lại thì vẫn đo chiều cao theo bề rộng chưa co (chữ xuống dòng sai). Vì vậy chọn bề rộng theo breakpoint:
    /// hết chiều ngang dưới sm, 24rem từ sm trở lên – chỉ khác bản gốc ở khoảng 432–640 px (cột rộng hơn 24rem).
    /// </summary>
    protected const string ColumnSm = "w-full sm:w-96";

    Indexed<AuthBlock>[]? _actionRefs;
    AuthAction? _hovered;

    /// <summary>
    /// Khung trang của bản gốc: <c>flex min-h-svh items-center justify-center bg-muted p-6 md:p-10</c>. "svh" ở đây là
    /// ô trang của shell: gốc chiếm hết ô và tự cuộn dọc; lớp trong lấp đầy ô khi nội dung thấp (để căn giữa) nhưng
    /// không co dưới chiều cao nội dung (flex-auto shrink-0 thay cho min-h-full mà Core chưa tính theo phần trăm).
    /// </summary>
    protected static Div PageFrame(Window w, string key, Div column) =>
        w.Div().Key(key).Class("flex size-full flex-col overflow-y-auto bg-muted")
            .Child(w.Div().Class("flex flex-auto shrink-0 items-center justify-center p-6 md:p-10").Child(column));

    Indexed<AuthBlock>[] Refs => Indexed<AuthBlock>.Ensure(ref _actionRefs, this, ActionKeys.Length);

    /// <summary>Gắn hành vi của một nút/liên kết phụ: focus được bằng Tab, bấm thì ghi lại và phát <see cref="ActionInvoked"/>.</summary>
    protected Div Wire(Div element, AuthAction action) =>
        Clickable(element, action).Key(ActionKeys[(int)action]).Focusable();

    Div Clickable(Div element, AuthAction action) =>
        element.OnClick(Refs[(int)action], static (r, _) => r.Owner.Act((AuthAction)r.Index));

    void SetHovered(AuthAction action, bool on)
    {
        AuthAction? next = on ? action : _hovered == action ? null : _hovered;
        if (next == _hovered) return;
        _hovered = next;
        Notify();
    }

    /// <summary>
    /// Đoạn chữ có liên kết ở giữa (<c>FieldDescription</c> chứa <c>&lt;a&gt;</c>). Core chưa có chữ inline lẫn phần tử nên
    /// mỗi mảnh là một ô của hàng flex tự xuống dòng, cách nhau đúng một dấu cách. Liên kết bị trình duyệt ngắt dòng ở giữa
    /// ("Terms of | Service") được truyền thành nhiều mảnh liền nhau cùng một <see cref="AuthAction"/>: các mảnh sáng cùng
    /// lúc khi rê chuột (state <see cref="_hovered"/>), chỉ mảnh đầu nhận Tab. Mảnh "." dính liền mảnh trước.
    /// </summary>
    protected Div Inline(Window w, string classes, ReadOnlySpan<(string Text, AuthAction? Link)> parts)
    {
        var row = w.Div().Class(Words).Class(classes);
        AuthAction? previous = null;
        foreach (var (text, link) in parts)
        {
            var piece = w.Div();
            if (text == ".") piece.Class("-ml-[3.5px]");
            if (link is { } a)
            {
                piece.Class(_hovered == a ? DescriptionLinkHover : DescriptionLink)
                    .OnHover(Refs[(int)a], static (r, on) => r.Owner.SetHovered((AuthAction)r.Index, on));
                piece = previous == a ? Clickable(piece, a) : Wire(piece, a);
            }
            row.Child(piece.Child(text));
            previous = link;
        }
        return row;
    }
    /// <summary>Logo "Acme Inc." phía trên card (liên kết về trang chủ trong bản gốc).</summary>
    protected Div Logo(Window w) =>
        Wire(w.Div().Class("flex items-center gap-2 self-center font-medium" + LinkFocus), AuthAction.Home)
            .Child(w.Div().Class("flex size-6 items-center justify-center rounded-md bg-primary text-primary-foreground")
                .Child(w.Path(BlockIcons.GalleryVerticalEnd).Class("size-4")))
            .Child("Acme Inc.");

    /// <summary>Nút mạng xã hội; <paramref name="label"/> = null là nút chỉ có logo (chữ <c>sr-only</c> của bản gốc không có chỗ hiện).</summary>
    protected Div Social(Window w, AuthAction action, VectorPath logo, string? label = null)
    {
        var button = Wire(Ui.Button(w, ButtonVariant.Outline), action).AriaLabel(label ?? action.ToString()).Child(w.Path(logo).Class("size-4 shrink-0"));
        return label is null ? button : button.Child(label);
    }

    /// <summary>Ba nút Apple / Google / Meta chỉ có logo của các block hai cột.</summary>
    protected Div SocialRow(Window w) => w.Div().Class("grid w-full grid-cols-3 gap-4")
        .Child(Social(w, AuthAction.Apple, BlockIcons.Apple))
        .Child(Social(w, AuthAction.Google, BlockIcons.Google))
        .Child(Social(w, AuthAction.Meta, BlockIcons.Meta));

    /// <summary>
    /// Field của một ô: nhãn (bấm thì ô nhận focus), ô nhập, mô tả nếu có, và dòng lỗi khi ô đang sai.
    /// <paramref name="forgotLink"/> đặt "Forgot your password?" cùng hàng với nhãn như ô mật khẩu của form đăng nhập
    /// (<paramref name="forgotOffset"/>: underline-offset-4 ở login-03, -2 ở login-04).
    /// </summary>
    protected Div FieldView(Window w, string id, bool forgotLink = false, string? description = null, int forgotOffset = 4)
    {
        var f = Field(id);
        var label = Ui.FieldLabel(w, f.Label).OnClick(f.Input, static (c, _) => c.Focus()).Key(f.LabelKey);
        if (forgotLink)
            label = w.Div().Class("flex items-center")
                .Child(label)
                .Child(w.Div().Class("flex-1")) // thay cho ml-auto của liên kết
                .Child(Wire(w.Div().Class(forgotOffset == 2 ? ForgotLink2 : ForgotLink4).Child("Forgot your password?"), AuthAction.ForgotPassword));
        var field = Ui.Field(w, invalid: f.Error is not null).Child(label).Child(w.Div().Key(f.Key).Child(w.View(f.Input)));
        if (description is not null) field.Child(Ui.FieldDescription(w, description));
        return f.Error is { } error ? field.Child(w.Div().Class("text-sm font-normal text-destructive").Key(f.ErrorKey).Child(error)) : field;
    }

    // "Forgot your password?": ml-auto text-sm underline-offset-{4|2} hover:underline; màu chữ thừa hưởng (foreground).
    const string ForgotLink4 = "text-sm text-card-foreground underline-offset-4 hover:underline" + LinkFocus;
    const string ForgotLink2 = "text-sm text-card-foreground underline-offset-2 hover:underline" + LinkFocus;

    /// <summary>Field chứa nút gửi; sau khi gửi thành công có thêm dòng trạng thái (bản gốc không có – thay cho việc điều hướng).</summary>
    protected Div SubmitField(Window w)
    {
        var field = Ui.Field(w).Child(Ui.Button(w).Key("auth-submit").OnClick(this, static (b, _) => b.Submit()).Child(SubmitLabel));
        return _status is { } status ? field.Child(w.Div().Class("text-sm leading-normal font-normal text-muted-foreground text-center").Key("auth-status").Child(status)) : field;
    }

    /// <summary>Dòng chuyển qua lại giữa đăng nhập và đăng ký: câu hỏi + liên kết (FieldDescription text-center).</summary>
    protected Div SwitchLine(Window w, string question, AuthAction action, string linkText) =>
        Inline(w, "justify-center text-center", [(question, null), (linkText, action)]);

    /// <summary>
    /// "By clicking continue, you agree to our Terms of Service and Privacy Policy." (FieldDescription px-6 text-center).
    /// <paramref name="narrow"/>: cột 24rem (login-03 / signup-03), nơi trình duyệt ngắt dòng giữa "Terms of | Service".
    /// </summary>
    protected Div TermsLine(Window w, bool narrow) => narrow
        ? Inline(w, "justify-center px-6 text-center",
        [
            ("By clicking continue, you agree to our", null), ("Terms of", AuthAction.Terms), ("Service", AuthAction.Terms), ("and", null),
            ("Privacy Policy", AuthAction.Privacy), (".", null),
        ])
        : Inline(w, "justify-center px-6 text-center",
        [
            ("By clicking continue, you agree to our", null), ("Terms of Service", AuthAction.Terms), ("and", null),
            ("Privacy Policy", AuthAction.Privacy), (".", null),
        ]);
}

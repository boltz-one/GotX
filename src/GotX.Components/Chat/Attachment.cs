namespace GotX.Components;

/// <summary>Vòng đời của tệp đính kèm: chưa gửi (viền gạch), đang tải lên, đang xử lý, lỗi, xong.</summary>
public enum AttachmentState : byte { Idle, Uploading, Processing, Error, Done }

/// <summary>Cỡ thẻ đính kèm.</summary>
public enum AttachmentSize : byte { Default, Sm, Xs }

/// <summary>Ngang (chip một dòng) hoặc dọc (thẻ vuông, ô media ở trên).</summary>
public enum AttachmentOrientation : byte { Horizontal, Vertical }

/// <summary>
/// Thẻ tệp đính kèm: ô media (icon/đuôi tệp hoặc vòng quay khi đang tải), tên, mô tả (dung lượng / trạng thái /
/// lỗi) và nút gỡ. Là View vì nút gỡ phát event có kiểu <see cref="RemoveRequested"/>; danh sách chứa thẻ tự quyết
/// có gỡ thật hay không. Nút gỡ là điểm dừng Tab (Enter/Space gỡ); tệp ảnh có thể kèm <see cref="Preview"/>.
/// </summary>
public sealed class Attachment : View
{
    /// <summary>Người dùng bấm nút gỡ.</summary>
    public readonly record struct RemoveRequested(string Name);

    AttachmentState _state = AttachmentState.Done;
    string? _removeKey;

    /// <summary>
    /// Ảnh xem trước (AttachmentMedia variant="image"): lấp ô media thay cho icon tệp, mờ đi trong lúc tải lên.
    /// null = hiện icon. Tạo <see cref="ImageSource"/> một lần rồi giữ lại.
    /// </summary>
    public ImageSource? Preview { get; set; }

    /// <summary>Tên tệp, hiện cắt bớt bằng dấu "…" khi quá dài.</summary>
    public string Name { get; set; } = "";

    /// <summary>Dung lượng (byte); âm = không biết, không hiện.</summary>
    public long Bytes { get; set; } = -1;

    /// <summary>Dòng mô tả tự đặt (ghi đè dung lượng/trạng thái), ví dụ thông báo lỗi.</summary>
    public string? Description { get; set; }

    /// <summary>Cỡ thẻ (chỉ áp cho hướng ngang).</summary>
    public AttachmentSize Size { get; set; }
    /// <summary>Hướng thẻ: chip ngang hay thẻ dọc.</summary>
    public AttachmentOrientation Orientation { get; set; }

    /// <summary>Có nút gỡ hay không.</summary>
    public bool Removable { get; set; } = true;

    /// <summary>Trạng thái tải lên; đổi thì vẽ lại (vòng quay, viền lỗi, viền gạch).</summary>
    public AttachmentState State
    {
        get => _state;
        set { if (_state == value) return; _state = value; Notify(); }
    }

    /// <summary>Phát <see cref="RemoveRequested"/>; gọi được từ code (ví dụ phím tắt) như khi bấm nút.</summary>
    public void RequestRemove() => Emit(new RemoveRequested(Name));

    /// <summary>Dung lượng dạng người đọc: 512 B, 12 KB, 3.4 MB.</summary>
    public static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):0.#} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):0.#} GB",
    };

    // Chuỗi hiển thị và class nhớ theo đầu vào: thẻ đang tải có vòng quay nên render mỗi frame, không được dựng chuỗi mỗi lần.
    string _subtitle = "", _extension = "FILE", _frame = "", _media = "";
    (string? Description, AttachmentState State, long Bytes, string Name) _textFor = (null, (AttachmentState)255, 0, "");
    (AttachmentState State, AttachmentSize Size, AttachmentOrientation Orientation) _classFor = ((AttachmentState)255, 0, 0);

    void EnsureText()
    {
        var key = (Description, _state, Bytes, Name);
        if (key == _textFor) return;
        _textFor = key;
        int dot = Name.LastIndexOf('.');
        _extension = dot >= 0 && dot < Name.Length - 1 ? Name[(dot + 1)..].ToUpperInvariant() : "FILE";
        _subtitle = Description ?? _state switch
        {
            AttachmentState.Uploading => "Đang tải lên…",
            AttachmentState.Processing => "Đang xử lý…",
            AttachmentState.Error => "Tải lên thất bại",
            _ => Bytes >= 0 ? FormatBytes(Bytes) : _extension,
        };
    }

    /// <summary>Vẽ thẻ: class ghép theo state/cỡ/hướng (số tổ hợp nhỏ), ghép lại chỉ khi một trong ba đổi.</summary>
    public override Div Render(Window w)
    {
        bool vertical = Orientation == AttachmentOrientation.Vertical, error = _state == AttachmentState.Error;
        EnsureText();
        if (_classFor != (_state, Size, Orientation)) BuildClasses(vertical, error);
        string frame = _frame, media = _media;
        return Body(w, vertical, error, frame, media);
    }

    void BuildClasses(bool vertical, bool error)
    {
        _classFor = (_state, Size, Orientation);
        _frame = "relative flex min-w-0 shrink-0 rounded-xl border bg-card text-card-foreground transition-colors " +
            (error ? "border-destructive/30 " : _state == AttachmentState.Idle ? "border-dashed border-border " : "border-border ") +
            (vertical ? "w-32 flex-col p-1.5 gap-2 text-xs" : Size switch
            {
                AttachmentSize.Sm => "min-w-40 max-w-full items-center gap-2.5 px-2 py-1.5 text-xs",
                AttachmentSize.Xs => "min-w-40 max-w-full items-center gap-1.5 rounded-lg px-1.5 py-1 text-xs",
                _ => "min-w-40 max-w-full items-center gap-2 px-2.5 py-2 text-sm",
            });
        _media = "relative flex shrink-0 items-center justify-center overflow-hidden bg-muted " +
            (error ? "bg-destructive/10 text-destructive " : "text-foreground ") +
            (vertical ? "w-full h-20 rounded-lg" : Size switch
            {
                AttachmentSize.Sm => "size-8 rounded-lg", AttachmentSize.Xs => "size-7 rounded-md", _ => "size-10 rounded-lg",
            });
    }

    Div Body(Window w, bool vertical, bool error, string frame, string media)
    {
        bool busy = _state is AttachmentState.Uploading or AttachmentState.Processing;
        // Cỡ icon theo thẻ như [&_svg]:size-4 / dọc size-6 / xs size-3.5 của bản gốc.
        string iconSize = vertical ? "size-6" : Size == AttachmentSize.Xs ? "size-3.5" : "size-4";
        var mediaBox = w.Div().Class(media);
        if (Preview is { } preview && !error)
            mediaBox.Child(w.Image(preview).Class(busy ? "size-full object-cover opacity-60" : "size-full object-cover"));
        // Có ảnh thì vòng quay nổi giữa ảnh (lớp absolute phủ ô media); không thì nó là nội dung duy nhất của ô.
        if (busy && Preview is not null && !error)
            mediaBox.Child(w.Div().Class("absolute inset-0 flex items-center justify-center").Child(Icon.Spinner(w).Class(iconSize)));
        else if (busy) mediaBox.Child(Icon.Spinner(w).Class(iconSize));
        else if (error) mediaBox.Child(Icon.AlertCircle(w).Class(iconSize));
        else if (Preview is null) mediaBox.Child(Icon.File(w).Class(iconSize));
        if (vertical && !error && !busy && Preview is null)
            mediaBox.Child(w.Div().Class("absolute bottom-1 left-1 rounded-sm bg-background px-1 text-[10px] font-medium text-muted-foreground").Child(_extension));

        var content = w.Div().Class(vertical ? "min-w-0 max-w-full px-1 pb-0.5 leading-tight" : "min-w-0 max-w-full flex-1 leading-tight")
            .Child(w.Div().Class(busy ? "truncate font-medium animate-pulse" : "truncate font-medium").Child(Name))
            .Child(w.Div().Class(error ? "mt-0.5 truncate text-xs text-destructive/80" : "mt-0.5 truncate text-xs text-muted-foreground")
                .Child(_subtitle));

        var card = w.Div().Class(frame).Child(mediaBox).Child(content);
        if (Removable)
        {
            _removeKey ??= "gotx-attachment-remove-" + Id.Value;
            // focus-within:ring-1 của bản gốc: nút gỡ đang focus (kể cả do bấm chuột) thì cả thẻ có vòng mảnh.
            if (w.IsFocused(_removeKey)) card.Class("ring-1 ring-ring/50");
            var remove = Ui.Button(w, ButtonVariant.Ghost, ButtonSize.IconXs).Key(_removeKey)
                .OnClick(this, static (a, _) => a.RequestRemove())
                .Child(Icon.X(w).Class("size-3"));
            // Dọc: nút nổi ở góc ô media như bản gốc (actions absolute top/right); ngang: nằm cuối hàng.
            card.Child(vertical
                ? w.Div().Class("absolute top-2.5 right-2.5 rounded-md bg-background").Child(remove)
                : w.Div().Class("relative flex shrink-0 items-center").Child(remove));
        }
        return card;
    }
}

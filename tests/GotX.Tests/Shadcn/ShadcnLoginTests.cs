using GotX;
using GotX.Test;

/// <summary>
/// Test các block đăng nhập/đăng ký của samples/Shadcn (login-03/04, signup-03/04): lint sáng/tối ở hai cỡ cửa sổ, bố cục
/// trong ô trang, hiệu năng; phần hành vi form nằm ở ShadcnLoginTests.Behaviour.cs.
/// </summary>
static partial class ShadcnLoginTests
{
    /// <summary>
    /// Ô trang của SiteShell cho trang FullBleed, không có hai thanh phía trên. Khác shell hiện tại một chỗ: node View
    /// mang <c>size-full</c>; thiếu nó thì node View chỉ cao bằng nội dung và <c>h-full</c> của block không có gì để bám
    /// (block không lấp đầy ô, không tự cuộn được).
    /// </summary>
    sealed class PageSlot(AuthBlock block) : View
    {
        public readonly AuthBlock Block = block;
        protected override void OnCreated() => App.New(Block);
        public override Div Render(Window w) => w.Div().Class("size-full flex flex-col bg-background text-foreground")
            .Child(w.Div().Key("site-page").Class("flex-1 min-h-0 overflow-hidden").Child(w.View(Block).Class("size-full")));
    }

    /// <summary>Mô tả một block cho các test dùng chung: nút/liên kết phụ nó có (theo thứ tự Tab) và chữ dòng trạng thái.</summary>
    sealed record Spec(string Id, Func<AuthBlock> Create, string StatusPrefix, bool TwoColumn, AuthAction[] Actions)
    {
        public bool IsLogin => Id.StartsWith("login", StringComparison.Ordinal);
    }

    static readonly Spec[] Blocks =
    [
        new("login-03", static () => new Login03Block(), "Signed in as ", false,
            [AuthAction.Home, AuthAction.Apple, AuthAction.Google, AuthAction.ForgotPassword, AuthAction.SignUp, AuthAction.Terms, AuthAction.Privacy]),
        new("login-04", static () => new Login04Block(), "Signed in as ", true,
            [AuthAction.ForgotPassword, AuthAction.Apple, AuthAction.Google, AuthAction.Meta, AuthAction.SignUp, AuthAction.Terms, AuthAction.Privacy]),
        new("signup-03", static () => new Signup03Block(), "Account created for ", false,
            [AuthAction.Home, AuthAction.SignIn, AuthAction.Terms, AuthAction.Privacy]),
        new("signup-04", static () => new Signup04Block(), "Account created for ", true,
            [AuthAction.Apple, AuthAction.Google, AuthAction.Meta, AuthAction.SignIn, AuthAction.Terms, AuthAction.Privacy]),
    ];

    static readonly Size[] Sizes = [new(1440, 900), new(1024, 768)];

    public static void Register(TestSuite suite)
    {
        foreach (var spec in Blocks)
        {
            RegisterLint(suite, spec);
            RegisterLayout(suite, spec);
            RegisterBehaviour(suite, spec);
            RegisterPerf(suite, spec);
        }
    }

    static AuthBlock Open(UiTest t, Spec spec, Size? size = null, bool dark = false) =>
        t.Open(new PageSlot(spec.Create()), size ?? Sizes[1], dark: dark).Block;

    /// <summary>Giá trị hợp lệ để điền vào một ô.</summary>
    static string Sample(AuthField f) => f.Kind switch
    {
        AuthFieldKind.Email => "m@example.com", AuthFieldKind.Password => "hunter22", _ => "Ada Lovelace",
    };

    static void Fill(UiTest t, AuthBlock block)
    {
        foreach (var f in block.Fields) { t.Click(f.Key); t.Type(Sample(f)); }
    }

    // Dòng điều khoản là text-muted-foreground trên bg-muted như bản gốc: 4.35:1 theo token màu của shadcn, dưới ngưỡng
    // "info" mặc định 4.5. Hạ đúng ngưỡng info đó; ngưỡng warning (3:1) và mọi luật khác giữ nguyên.
    static LintOptions Lint() => new() { ContrastInfo = 4.3f };

    static void ExpectClean(UiTest t, DoctorReport report, string what)
    {
        if (t.Options.Verbose || report.Issues.Count > 0) t.Notes.Add(report.ToText(60).TrimEnd());
        t.Assert.True(report.Issues.Count == 0, $"{what}: {report.Issues.Count} vấn đề lint trong {report.Configs} cấu hình (mong đợi 0)");
    }

    static void RegisterLint(TestSuite suite, Spec spec)
    {
        // Trạng thái ban đầu và trạng thái đang báo lỗi (thêm các dòng FieldError, ô viền đỏ).
        suite.Add($"shadcn/{spec.Id}/lint", t =>
        {
            ExpectClean(t, UiDoctor.Run(app => app.New(new PageSlot(spec.Create())),
                new DoctorOptions { Sizes = Sizes, Lint = Lint(), OutDir = t.Options.OutDir, Name = spec.Id }), "ban đầu");
            ExpectClean(t, UiDoctor.Run(app => app.New(new PageSlot(spec.Create())), new DoctorOptions
            {
                Sizes = Sizes, Lint = Lint(), OutDir = t.Options.OutDir, Name = spec.Id + "-invalid", Prepare = static p => p.Click("auth-submit"),
            }), "đang báo lỗi");
        });

        // Qua SiteShell thật (bấm nút chọn block trên thanh của shell), trạng thái ban đầu.
        suite.Add($"shadcn/{spec.Id}/lint-in-shell", t =>
        {
            ExpectClean(t, UiDoctor.Run(static app => app.New(new SiteShell()), new DoctorOptions
            {
                Sizes = Sizes, Scales = [1], Lint = Lint(), OutDir = t.Options.OutDir, Name = spec.Id + "-shell",
                Prepare = p => { p.Click("nav-blocks"); p.Click("block-" + spec.Id); p.Assert.Exists(spec.Id); },
            }), "trong SiteShell");
        });
    }

    static void RegisterLayout(TestSuite suite, Spec spec)
    {
        suite.Add($"shadcn/{spec.Id}/fills-page-slot-and-scrolls", t =>
        {
            Open(t, spec);
            var slot = t.Box("site-page");
            t.Assert.True(t.Box(spec.Id) == slot, $"gốc block {t.Box(spec.Id)} phải trùng ô trang {slot}");
            var card = t.Box("auth-card");
            t.Assert.Near(card.X + card.Width / 2, slot.X + slot.Width / 2, 0.5f, "card căn giữa ngang");
            t.Assert.Near(card.Width, spec.TwoColumn ? 896 : 384, 0.5f, "bề rộng card (max-w-4xl / max-w-sm)");
            if (spec.TwoColumn)
            {
                // Từ md trở lên: hai cột bằng nhau, cột ảnh cao bằng cột form.
                var form = t.Box("auth-form");
                var image = t.Box("auth-image");
                t.Assert.Near(image.Width, form.Width, 0.5f, "hai cột rộng bằng nhau");
                t.Assert.Near(image.Height, form.Height, 0.5f, "cột ảnh cao bằng cột form");
            }

            // Hẹp hơn sm: cột chiếm hết chiều ngang (trừ p-6), chữ xuống dòng theo đúng bề rộng đó – không gì bị cắt.
            Open(t, spec, new Size(360, 900));
            t.Assert.Near(t.Box("auth-card").Width, 360 - 48, 0.5f, "dưới sm: card rộng bằng ô trừ đệm");
            t.Assert.NoLint(Lint());

            // Ô trang thấp và hẹp hơn md: một cột 24rem, cột ảnh ẩn, nội dung cao hơn ô nên phải cuộn được tới cuối.
            Open(t, spec, new Size(700, 420));
            t.Assert.Near(t.Box("auth-card").Width, 384, 0.5f, "dưới md: card rộng 24rem");
            if (spec.TwoColumn) t.Assert.True(t.Find("auth-image") is not { Visible.IsEmpty: false }, "dưới md: cột ảnh ẩn");
            t.Assert.True(t.Get("auth-privacy").Visible.IsEmpty, "trước khi cuộn: dòng điều khoản nằm dưới mép ô");
            t.Scroll(spec.Id, 2000);
            t.Assert.Visible("auth-privacy");
            t.Assert.NoLint(Lint());
        });
    }

    static void RegisterPerf(TestSuite suite, Spec spec)
    {
        suite.Add($"shadcn/{spec.Id}/perf", t =>
        {
            var block = Open(t, spec);
            t.Perf.ExpectIdle();
            t.Click("auth-email");
            t.Type("m@example.co");
            t.Settle(); // hiệu ứng vòng focus (transition 150 ms) phải xong, nếu không nó tự làm ô render mỗi frame
            // Gõ một ký tự làm bẩn TextInput và các view bọc nó (Input → block → PageSlot): 4 view. Input của component
            // cần 2 frame cho một phím (đo với một Input trần cũng vậy), nên trần là 8; các ô khác và mọi thứ còn lại phát lại.
            t.Perf.ExpectRenders(() => t.Type("m"), max: 8, min: 1);
            t.Click("auth-password");
            t.Type("hunter2");
            t.Settle();
            // Ô mật khẩu đổi nội dung hai lần cho một phím (ký tự gõ → dấu chấm che) nhưng không tốn thêm frame nào.
            t.Perf.ExpectRenders(() => t.Type("2"), max: 8, min: 1);
            t.Assert.Equal(block.Field("password").Value, "hunter22", "mật khẩu sau khi gõ");
            t.Perf.ExpectIdle();
            t.Perf.ExpectReplayMatchesFresh();

            // Điền nốt các ô còn lại rồi gửi: trạng thái thành công cũng phải đứng yên và phát lại đúng.
            foreach (var f in block.Fields)
                if (f.Value.Length == 0) { t.Click(f.Key); t.Type(Sample(f)); }
            t.Press(Key.Enter);
            t.Assert.Equal(block.SubmittedEmail, "m@example.com", "SubmittedEmail sau Enter");
            t.Perf.ExpectIdle();
            t.Perf.ExpectReplayMatchesFresh();
        });
    }
}

using GotX;
using GotX.Test;

/// <summary>Luật về nội dung (tràn, cắt, tương phản), fixture sạch, tuỳ chọn lint, UiDoctor và baseline.</summary>
static class LintContentTests
{
    const string Long = "Một dòng chữ dài không thể nào vừa trong chiếc hộp hẹp này";

    public static void Register(TestSuite suite)
    {
        suite.Add("lint/clean-fixture-is-silent", t =>
        {
            t.Open(new CleanView(), new Size(640, 480));
            Kit.ExpectClean(t, t.Lint(), "CleanView sáng");
            t.Scroll("list", 120);
            Kit.ExpectClean(t, t.Lint(), "CleanView sau khi cuộn danh sách");
            t.Open(new CleanView(), new Size(640, 480), scale: 2);
            Kit.ExpectClean(t, t.Lint(), "CleanView @2x");
        });

        suite.Add("lint/overflow", t =>
        {
            var issues = Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Key("narrow").Class("w-40 h-10 flex items-center bg-slate-100")
                .Child(Fx.Box(w, "wide", "w-56 h-6 shrink-0 bg-sky-500"))));
            var hit = Kit.ExpectOnly(t, issues, "overflow", "div#wide");
            t.Assert.True(hit?.Message.Contains("64px") == true && hit.Message.Contains("phải"), $"tràn 64px về bên phải: {hit?.Message}");
            // Cha cắt hoặc cuộn thì không còn là tràn.
            Kit.ExpectClean(t, Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Class("w-40 h-10 flex items-center overflow-x-auto bg-slate-100")
                .Child(Fx.Box(w, "wide", "w-56 h-6 shrink-0 bg-sky-500")))), "cha cuộn ngang");
        });

        suite.Add("lint/text-overflow-spills", t =>
        {
            var issues = Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Key("label").Class("w-24 whitespace-nowrap text-sm").Child(Long)));
            var hit = Kit.ExpectOnly(t, issues, "text-overflow", "div#label > text");
            t.Assert.True(hit?.Message.Contains("đè lên phần bên cạnh") == true, $"chữ tràn ra ngoài, không bị cắt: {hit?.Message}");
            // Cùng nội dung nhưng có truncate thì đúng.
            Kit.ExpectClean(t, Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Key("label").Class("w-24 truncate text-sm").Child(Long))), "truncate");
            // Chữ tự xuống dòng trong hộp không giới hạn chiều cao cũng đúng.
            Kit.ExpectClean(t, Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Key("label").Class("w-24 text-sm").Child(Long))), "tự xuống dòng");
        });

        suite.Add("lint/text-overflow-clipped-text", t =>
        {
            // Chữ bị cắt: cha overflow-hidden, chữ không xuống dòng và không có ellipsis → mất chữ mà không có dấu hiệu.
            var issues = Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Key("cut").Class("w-24 overflow-hidden whitespace-nowrap text-sm").Child(Long)));
            var hit = Kit.ExpectOnly(t, issues, "text-overflow", "div#cut > text");
            t.Assert.True(hit?.Message.Contains("bị cắt") == true, $"nêu rõ là bị cắt: {hit?.Message}");
            // Chữ nhiều dòng trong hộp cao cố định: tràn theo chiều dọc.
            issues = Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Key("fixed").Class("w-24 h-6 text-sm").Child(Long)));
            hit = Kit.ExpectOnly(t, issues, "text-overflow", "div#fixed > text");
            t.Assert.True(hit?.Message.Contains("dưới") == true, $"tràn xuống dưới: {hit?.Message}");
        });

        suite.Add("lint/clipped", t =>
        {
            var issues = Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Key("frame").Class("w-20 h-8 flex overflow-hidden bg-slate-100")
                .Child(w.Div().Key("btn").Class("w-32 h-8 shrink-0 px-2 flex items-center bg-slate-900 text-white text-sm").OnClick(t, static (_, _) => { }).Child("OK"))));
            var hit = Kit.ExpectOnly(t, issues, "clipped", "div#btn");
            t.Assert.True(hit?.Message.Contains("48×0") == true && hit.Message.Contains("div#frame"), $"mất 48px, thủ phạm là #frame: {hit?.Message}");
            // Bị che trong vùng cuộn là chuyện bình thường.
            Kit.ExpectClean(t, Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Key("frame").Class("w-20 h-8 flex overflow-x-auto bg-slate-100")
                .Child(w.Div().Key("btn").Class("w-32 h-8 shrink-0 px-2 flex items-center bg-slate-900 text-white text-sm").OnClick(t, static (_, _) => { }).Child("OK")))), "vùng cuộn");
        });

        suite.Add("lint/low-contrast", t =>
        {
            var issues = Kit.Lint(t, w => Fx.Page(w)
                .Child(w.Div().Key("faint").Class("text-sm text-slate-300").Child("Chữ xám nhạt trên nền trắng"))
                .Child(w.Div().Key("strong").Class("text-sm text-slate-900").Child("Chữ đậm màu đọc rõ")));
            var hit = Kit.ExpectOnly(t, issues, "low-contrast", "div#faint > text", severity: Severity.Warning);
            t.Assert.True(hit?.Message.Contains("nền #FFFFFF") == true, $"nền lấy từ ảnh: {hit?.Message}");
            // Nền thật đến từ tổ tiên: cùng màu chữ trắng, trên nền tối thì đạt, trên nền vàng nhạt thì không.
            issues = Kit.Lint(t, w => Fx.Page(w)
                .Child(w.Div().Class("p-2 bg-slate-900").Child(w.Div().Key("good").Class("text-sm text-white").Child("Trắng trên nền tối")))
                .Child(w.Div().Class("p-2 bg-yellow-200").Child(w.Div().Key("bad").Class("text-sm text-white").Child("Trắng trên nền vàng"))));
            Kit.ExpectOnly(t, issues, "low-contrast", "div#bad > text", severity: Severity.Warning);
            // 3 ≤ tỉ lệ < 4.5 chỉ là info; chữ của phần tử disabled được miễn.
            issues = Kit.Lint(t, w => Fx.Page(w)
                .Child(w.Div().Key("mid").Class("text-sm text-sky-600").Child("Xanh vừa trên nền trắng"))
                .Child(w.Div().Key("off").Disabled().Class("text-sm text-slate-300").Child("Nút bị vô hiệu")));
            Kit.ExpectOnly(t, issues, "low-contrast", "div#mid > text", severity: Severity.Info);
        });

        suite.Add("lint/options-suppress-and-disable", t =>
        {
            Func<Window, Div> view = w => Fx.Page(w)
                .Child(w.Div().Key("tiny").Class("size-3 bg-rose-500").OnClick(t, static (_, _) => { }))
                .Child(w.Div().Key("rule").Class("w-40 h-0 border-t border-slate-400"));
            t.Assert.Equal(Kit.Lint(t, view).Count, 2, "không tuỳ chọn: 2 vấn đề");
            Kit.ExpectOnly(t, Kit.Lint(t, view, options: new LintOptions().Suppress("small-target", "tiny")), "zero-size", "div#rule");
            Kit.ExpectOnly(t, Kit.Lint(t, view, options: new LintOptions().Suppress("*", "border-t*slate-400")), "small-target", "div#tiny");
            Kit.ExpectOnly(t, Kit.Lint(t, view, options: new LintOptions().Disable("zero-size")), "small-target", "div#tiny");
            Kit.ExpectOnly(t, Kit.Lint(t, view, options: new LintOptions().Only("zero-size")), "zero-size", "div#rule");
            Kit.ExpectClean(t, Kit.Lint(t, view, options: new LintOptions { MinTarget = 12 }.Disable("zero-size")), "ngưỡng vùng bấm 12px");
        });

        suite.Add("lint/doctor-matrix", t =>
        {
            string dir = Kit.TempDir("doctor");
            var options = new DoctorOptions { Sizes = [new Size(640, 400), new Size(400, 600)], OutDir = dir, Name = "fx" };
            // Nút size-3: nhỏ ở mọi cấu hình; hộp 13px không còn lệch lưới ở scale phân số vì Core bắt mép vào pixel.
            var report = UiDoctor.Run(app => app.New(new Fx(w => Fx.Page(w)
                .Child(w.Div().Key("tiny").Class("size-3 bg-rose-500").OnClick(t, static (_, _) => { }))
                .Child(Fx.Box(w, "odd", "w-[13px] h-8 bg-sky-500")))), options);
            t.Assert.Equal(report.Configs, 16, "2 kích thước × sáng/tối × 4 scale");
            t.Assert.Equal(report.Issues.Count, 1, "gộp trùng qua các cấu hình: " + report.ToText());
            if (report.Issues.Count != 1) return;
            var tiny = report.Issues[0];
            t.Assert.True(tiny.Rule == "small-target" && tiny.Path.Contains("div#tiny") && tiny.Config!.Contains("4/16"), $"small-target xếp trước (warning), gặp ở 4 cấu hình scale 1: {tiny.Config}");

            t.Assert.True(tiny.Artifact is { } a && File.Exists(a[..a.LastIndexOf(" #", StringComparison.Ordinal)]), $"có ảnh khoanh vùng: {tiny.Artifact}");
            t.Assert.True(report.ToText().Contains("1. warning small-target"), "báo cáo có đánh số và xếp hạng");
            var clean = UiDoctor.Run(app => app.New(new CleanView()), new DoctorOptions { Sizes = [new Size(640, 480)], Scales = [1, 2] });
            t.Assert.Equal(clean.Issues.Count, 0, "CleanView sạch ở scale nguyên: " + clean.ToText());
        });

        suite.Add("lint/baseline-roundtrip", t =>
        {
            string file = Path.Combine(Kit.TempDir("baseline"), "lint-baseline.txt");
            static LintIssue Issue(string rule, string path) => new() { Rule = rule, Severity = Severity.Warning, Message = "số đo 12px", Path = path };
            var known = new List<LintIssue> { Issue("overflow", "V > div#a .w-40 h-10…"), Issue("small-target", "V >> div[2] > div[1]"), Issue("small-target", "V >> div[2] > div[1]") };
            var baseline = new LintBaseline(file);
            baseline.Replace("page", known);
            baseline.Save();
            var loaded = LintBaseline.Load(file);
            t.Assert.Equal(loaded.Count("page"), 2, "hai khoá (một khoá ×2)");
            var (fresh, gone) = loaded.Compare("page", known);
            t.Assert.True(fresh.Count == 0 && gone == 0, "đúng như baseline thì không có gì mới");
            known.Add(Issue("clipped", "V > div#new"));
            known.Add(Issue("small-target", "V >> div[2] > div[1]")); // phần tử thứ ba cùng đường dẫn
            (fresh, gone) = loaded.Compare("page", known);
            t.Assert.True(fresh.Count == 2 && fresh[0].Rule == "clipped" && fresh[1].Rule == "small-target", "chỉ vấn đề mới (và lần xuất hiện vượt số đã biết) bị báo");
            (fresh, gone) = loaded.Compare("page", [known[0]]);
            t.Assert.True(fresh.Count == 0 && gone == 2, "vấn đề đã sửa được đếm là 'đã hết'");
            t.Assert.True(File.ReadAllText(file).Contains("warning | small-target  | ×2 | V >> div[2] > div[1]  ## số đo 12px"), "file baseline người đọc được");
        });
    }
}

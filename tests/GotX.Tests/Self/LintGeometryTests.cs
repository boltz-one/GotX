using GotX;
using GotX.Test;

/// <summary>Mỗi luật lint về hình học / tương tác: một fixture cài đúng một lỗi, luật phải bắt đúng nó và chỉ nó.</summary>
static class LintGeometryTests
{
    public static void Register(TestSuite suite)
    {
        suite.Add("lint/misaligned-row-1px", t =>
        {
            var issues = Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Class("flex items-start gap-2")
                .Child(Fx.Box(w, "a")).Child(Fx.Box(w, "off", "w-16 h-8 mt-px bg-slate-300")).Child(Fx.Box(w, "c"))));
            var hit = Kit.ExpectOnly(t, issues, "misaligned", "div#off");
            t.Assert.True(hit?.Message.Contains("lệch 1px") == true && hit.Message.Contains("cùng hàng"), $"thông báo nêu độ lệch và hướng: {hit?.Message}");
        });

        suite.Add("lint/misaligned-column-2px", t =>
        {
            var issues = Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Class("flex flex-col items-start gap-2")
                .Child(Fx.Box(w, "a")).Child(Fx.Box(w, "b")).Child(Fx.Box(w, "off", "w-16 h-8 ml-0.5 bg-slate-300"))));
            var hit = Kit.ExpectOnly(t, issues, "misaligned", "div#off");
            t.Assert.True(hit?.Message.Contains("lệch 2px") == true && hit.Message.Contains("cùng cột"), $"thông báo: {hit?.Message}");
        });

        suite.Add("lint/misaligned-ignores-intentional", t =>
        {
            // Căn giữa các phần tử khác chiều cao (đỉnh/đáy lệch nhưng tâm khớp), lệch hẳn 8 px, và khối cao cạnh icon nhỏ: đều là cố ý.
            var issues = Kit.Lint(t, w => Fx.Page(w)
                .Child(w.Div().Class("flex items-center gap-2").Child(Fx.Box(w, "tall", "w-16 h-10 bg-slate-300")).Child(Fx.Box(w, "short", "w-16 h-7 bg-slate-300"))
                    .Child(w.Div().Class("text-xs").Child("nhỏ")).Child(w.Div().Class("text-lg").Child("Lớn")))
                .Child(w.Div().Class("flex items-start gap-2").Child(Fx.Box(w, "p")).Child(Fx.Box(w, "q", "w-16 h-8 mt-2 bg-slate-300")))
                .Child(w.Div().Class("flex items-start gap-2").Child(Fx.Box(w, "block", "w-40 h-24 bg-slate-300")).Child(Fx.Box(w, "icon", "size-5 mt-0.5 bg-slate-500"))));
            Kit.ExpectClean(t, issues, "bố cục cố ý");
        });

        suite.Add("lint/overlap", t =>
        {
            var issues = Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Class("flex items-center")
                .Child(Fx.Box(w, "a", "w-24 h-8 bg-slate-300")).Child(Fx.Box(w, "over", "w-24 h-8 -translate-x-3 bg-sky-500"))));
            var hit = Kit.ExpectOnly(t, issues, "overlap", "div#over");
            t.Assert.True(hit?.Message.Contains("12×32") == true, $"vùng chồng 12×32: {hit?.Message}");
            // Margin âm là cách xếp đè có chủ ý (nhóm avatar): không báo.
            Kit.ExpectClean(t, Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Class("flex items-center")
                .Child(Fx.Box(w, "a", "size-8 rounded-full bg-slate-300")).Child(Fx.Box(w, "b", "size-8 -ml-2 rounded-full bg-sky-500")))), "margin âm");
        });

        suite.Add("lint/subpixel", t =>
        {
            // Toạ độ lẻ do layout (margin 10.5px) được Core bắt vào lưới pixel khi vẽ → không còn là lỗi.
            Kit.ExpectClean(t, Kit.Lint(t, w => Fx.Page(w).Child(Fx.Box(w, "snapped", "ml-[10.5px] w-20 h-8 bg-sky-500"))), "toạ độ lẻ do layout");
            // Phần tử bị phóng (scale) lúc đứng yên thì không được bắt vào lưới: mép nằm giữa hai pixel → nhoè.
            var issues = Kit.Lint(t, w => Fx.Page(w).Child(Fx.Box(w, "blur", "ml-4 w-20 h-8 scale-[1.04] bg-sky-500")));
            var hit = Kit.ExpectOnly(t, issues, "subpixel", "div#blur");
            t.Assert.True(hit?.Message.Contains("top=") == true && hit.Message.Contains("@1x"), $"nêu mép lệch theo pixel thiết bị: {hit?.Message}");
        });

        suite.Add("lint/subpixel-fractional-scale", t =>
        {
            // 13 px × 1.5 = 19.5 pixel thiết bị: Core làm tròn từng mép nên hộp vẫn sắc nét ở DPI 150% – mọi scale đều sạch.
            Func<Window, Div> view = w => Fx.Page(w).Child(Fx.Box(w, "odd", "w-[13px] h-8 bg-sky-500")).Child(Fx.Box(w, "even", "w-16 h-8 bg-sky-500"));
            Kit.ExpectClean(t, Kit.Lint(t, view, scale: 1), "scale 1");
            Kit.ExpectClean(t, Kit.Lint(t, view, scale: 1.5f), "scale 1.5");
            Kit.ExpectClean(t, Kit.Lint(t, view, scale: 2), "scale 2");
        });

        suite.Add("lint/zero-size", t =>
        {
            var issues = Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Key("rule").Class("w-40 h-0 border-t border-slate-400")));
            Kit.ExpectOnly(t, issues, "zero-size", "div#rule", severity: Severity.Warning);
            issues = Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Class("w-40 h-2 flex bg-slate-200").Child(w.Div().Key("bar").Class("w-0 h-2 bg-sky-500"))));
            Kit.ExpectOnly(t, issues, "zero-size", "div#bar", severity: Severity.Info);
            // Đường kẻ viết đúng (cao bằng độ dày nét) thì sạch.
            Kit.ExpectClean(t, Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Class("w-40 h-px bg-slate-400"))), "h-px");
        });

        suite.Add("lint/offscreen", t =>
        {
            var issues = Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Class("relative w-40 h-10 bg-slate-100")
                .Child(w.Div().Key("lost").Class("absolute left-[2000px] top-0 w-10 h-10 bg-rose-500").Child(w.Div().Class("size-4 bg-white")))));
            Kit.ExpectOnly(t, issues, "offscreen", "div#lost"); // con của nó cũng ngoài cửa sổ nhưng chỉ báo node ngoài cùng
            // Nội dung nằm ngoài khung nhìn của một vùng cuộn thì tới được bằng cách cuộn: không báo.
            Kit.ExpectClean(t, Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Class("w-40 h-20 overflow-y-auto")
                .Child(w.Div().Class("h-[3000px] w-10 bg-slate-200")).Child(Fx.Box(w, "far")))), "trong vùng cuộn");
        });

        suite.Add("lint/small-target", t =>
        {
            var issues = Kit.Lint(t, w => Fx.Page(w)
                .Child(w.Div().Key("tiny").Class("size-3 bg-rose-500").OnClick(t, static (_, _) => { }))
                .Child(w.Div().Key("ok").Class("size-4 bg-slate-500").OnClick(t, static (_, _) => { })));
            var hit = Kit.ExpectOnly(t, issues, "small-target", "div#tiny");
            t.Assert.True(hit?.Message.Contains("12×12") == true, $"nêu kích thước: {hit?.Message}");
        });

        suite.Add("lint/occluded", t =>
        {
            var issues = Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Class("relative w-40 h-10")
                .Child(w.Div().Key("under").Class("w-40 h-10 bg-slate-300").OnClick(t, static (_, _) => { }))
                .Child(w.Div().Key("cover").Class("absolute inset-0").OnClick(t, static (_, _) => { }))));
            var hit = Kit.ExpectOnly(t, issues, "occluded", "div#under");
            t.Assert.True(hit?.Message.Contains("div#cover") == true, $"nêu phần tử che: {hit?.Message}");
            // Nút con nằm trong một vùng bấm lớn hơn (hàng bấm được có nút xoá) thì không ai che ai.
            Kit.ExpectClean(t, Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Key("row").Class("w-60 h-10 flex items-center justify-end px-2 bg-slate-100").OnClick(t, static (_, _) => { })
                .Child(w.Div().Key("del").Class("size-6 bg-rose-500").OnClick(t, static (_, _) => { })))), "nút con trong hàng bấm được");
        });

        suite.Add("lint/duplicate-key", t =>
        {
            var issues = Kit.Lint(t, w => Fx.Page(w).Child(Fx.Box(w, "dup")).Child(Fx.Box(w, "dup")).Child(Fx.Box(w, "uniq")));
            Kit.ExpectOnly(t, issues, "duplicate-key", "div#dup", severity: Severity.Error);
            // Hai view khác nhau dùng cùng khoá bên trong thì hợp lệ.
            t.Open(app =>
            {
                var a = app.New(new CounterView());
                var b = app.New(new CounterView());
                return app.New(new Fx(w => Fx.Page(w).Child(w.View(a)).Child(w.View(b))));
            });
            Kit.ExpectClean(t, t.Lint(), "cùng khoá ở hai view");
        });

        suite.Add("lint/unknown-class", t =>
        {
            var issues = Kit.Lint(t, w => Fx.Page(w).Child(w.Div().Key("bad").Class("w-16 h-8 bg-slate-300 bogus-utility md:nope-4")));
            var hit = Kit.ExpectOnly(t, issues, "unknown-class", "div#bad", severity: Severity.Error);
            t.Assert.True(hit?.Message.Contains("bogus-utility") == true && hit.Message.Contains("md:nope-4"), $"liệt kê token lạ: {hit?.Message}");
        });
    }
}

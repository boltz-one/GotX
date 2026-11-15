using GotX;
using GotX.Components;

/// <summary>Phần đầu trang (thông báo, tiêu đề, mô tả, hai nút) và dòng chân trang – đều tĩnh.</summary>
static class HomeHero
{
    const string Link = "cursor-pointer rounded-sm font-medium underline " + HomeUi.Ring;

    /// <summary>page.tsx + page-header.tsx + announcement.tsx.</summary>
    public static Div Hero(Window w) =>
        w.Div().Class("flex flex-col items-center gap-2 px-6 py-8 text-center md:py-16 lg:py-20 xl:gap-4").Key("home-hero")
            // Announcement là một Badge bọc link: bấm được và là điểm dừng Tab.
            .Child(Ui.Badge(w, BadgeVariant.Secondary).Class("cursor-pointer bg-muted").Key("announcement").Focusable()
                .Child("Trace, Code Block and AI Elements")
                .Child(Icon.ArrowRight(w).Class("size-3")))
            // max-w-5xl thay cho max-w-4xl: Segoe UI rộng hơn Geist, 4xl sẽ đẩy chữ cuối xuống dòng ở cỡ 5xl.
            // lg:leading-[1.1] viết sau xl:text-5xl: Core áp class theo thứ tự viết, text-5xl đứng sau sẽ đặt lại line-height.
            .Child(w.Div().Class("max-w-5xl text-3xl leading-tight font-semibold tracking-tight text-primary xl:text-5xl xl:tracking-tighter lg:leading-[1.1]").Key("hero-title")
                .Child("The Foundation for your Design System"))
            // Core chưa có text-balance: max-w-2xl thay cho max-w-4xl để hai dòng mô tả dài gần bằng nhau như bản gốc.
            .Child(w.Div().Class("max-w-2xl text-base text-foreground sm:text-lg").Key("hero-description")
                .Child("Composable, accessible components with thoughtful defaults. Build your own component library with code you can customize, extend, and make your own."))
            .Child(w.Div().Class("flex w-full items-center justify-center gap-2 pt-2")
                .Child(Ui.Button(w, "Get Started").Key("get-started"))
                .Child(Ui.Button(w, "View Components", ButtonVariant.Secondary).Key("view-components")));

    /// <summary>site-footer.tsx. Chữ thường và link xen kẽ: mỗi mẩu là một phần tử trong hàng flex-wrap, gap-1 ≈ một dấu cách.</summary>
    public static Div Footer(Window w) =>
        w.Div().Class("flex h-14 flex-wrap items-center justify-center gap-1 px-4 text-xs text-muted-foreground sm:text-sm xl:px-6").Key("home-footer")
            .Child("Built by")
            .Child(w.Div().Class(Link).Key("footer-shadcn").Focusable().Child("shadcn"))
            .Child("at")
            .Child(w.Div().Class("flex").Child(w.Div().Class(Link).Key("footer-vercel").Focusable().Child("Vercel")).Child("."))
            .Child("The source code is available on")
            .Child(w.Div().Class("flex").Child(w.Div().Class(Link).Key("footer-github").Focusable().Child("GitHub")).Child("."));
}

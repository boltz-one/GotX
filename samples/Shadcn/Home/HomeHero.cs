using GotX;
using GotX.Components;

/// <summary>
/// Phần đầu trang (thông báo, tiêu đề, mô tả, hai nút) và dòng chân trang – đều tĩnh. Nằm ngoài lớp theme-neutral của
/// khối demo nên dùng màu theme chung của site: primary/foreground là đen tuyền ở chế độ sáng (neutral là #171717/#0a0a0a).
/// </summary>
static class HomeHero
{
    const string Link = "cursor-pointer rounded-sm font-medium underline underline-offset-4 " + HomeUi.Ring;

    // announcement.tsx: Badge variant secondary của new-york-v4 (không có h-5: cao 22px) bọc link, nền muted.
    const string Announcement =
        "inline-flex shrink-0 items-center justify-center gap-1 overflow-hidden rounded-full border border-transparent bg-muted " +
        "px-2 py-0.5 text-xs font-medium whitespace-nowrap text-secondary-foreground cursor-pointer transition-colors " +
        "hover:bg-secondary/90 focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50";

    /// <summary>page.tsx + page-header.tsx + announcement.tsx.</summary>
    public static Div Hero(Window w) =>
        // px-8 = container (px-2) + px-6 của PageHeader; pb của trang: md 32px, lg 48px.
        w.Div().Class("flex flex-col items-center gap-2 px-8 py-8 text-center md:py-16 md:pb-8 lg:py-20 lg:pb-12 xl:gap-4").Key("home-hero")
            .Child(w.Div().Class("flex")
                .Child(w.Div().Class(Announcement).Key("announcement").Focusable()
                    .Child("Trace, Code Block and AI Elements")
                    .Child(Icon.ArrowRight(w).Class("size-3"))))
            // leading-tighter (1.2); lg:leading-[1.1] viết sau xl:text-5xl vì Core áp class theo thứ tự viết.
            .Child(w.Div().Class("max-w-4xl text-3xl leading-[1.2] font-semibold tracking-tight text-balance text-black " +
                    "xl:text-5xl xl:tracking-tighter lg:leading-[1.1] dark:text-primary").Key("hero-title")
                .Child("The Foundation for your Design System"))
            .Child(w.Div().Class("max-w-4xl text-base text-balance text-black sm:text-lg dark:text-foreground").Key("hero-description")
                .Child("Composable, accessible components with thoughtful defaults. Build your own component library with code you can customize, extend, and make your own."))
            .Child(w.Div().Class("flex w-full items-center justify-center gap-2 pt-2")
                // Nút của radix-luma (h-9, bo 4xl); Get Started đặt h-[35px] như page.tsx. Hover secondary =
                // color-mix(secondary, foreground 5%) với foreground đen của site.
                .Child(Ui.Button(w, "Get Started").Class("h-[35px] rounded-4xl bg-black hover:bg-black/80 dark:bg-primary dark:hover:bg-primary/80").Key("get-started"))
                .Child(Ui.Button(w, "View Components", ButtonVariant.Secondary).Class("h-9 rounded-4xl hover:bg-[#e5e5e5] dark:hover:bg-[#2f2f2f]").Key("view-components")));

    /// <summary>site-footer.tsx. Chữ thường và link xen kẽ: mỗi mẩu là một phần tử trong hàng flex-wrap, gap = bề rộng dấu cách của Geist (0.25em).</summary>
    public static Div Footer(Window w) =>
        w.Div().Class("flex h-14 items-center px-4 xl:h-24 xl:px-6").Key("home-footer")
            .Child(w.Div().Class("flex w-full flex-wrap items-center justify-center gap-x-[3px] px-1 text-xs leading-loose text-muted-foreground sm:gap-x-[3.5px] sm:text-sm")
                .Child("Built by")
                .Child(w.Div().Class(Link).Key("footer-shadcn").Focusable().Child("shadcn"))
                .Child("at")
                .Child(w.Div().Class("flex").Child(w.Div().Class(Link).Key("footer-vercel").Focusable().Child("Vercel")).Child("."))
                .Child("The source code is available on")
                .Child(w.Div().Class("flex").Child(w.Div().Class(Link).Key("footer-github").Focusable().Child("GitHub")).Child(".")));
}

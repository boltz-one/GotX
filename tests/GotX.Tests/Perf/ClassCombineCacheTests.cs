using GotX;
using GotX.Test;

/// <summary>
/// <c>Tailwind.Combine</c> (ghép hai lần <c>.Class()</c> có variant trạng thái) dùng hai thế hệ như cache class: chạm ngưỡng thì
/// thế hệ hiện tại thành cũ, cặp đang dùng được chuyển lên chứ không bị ghép lại. Trước đây xoá hết ở 4096.
/// </summary>
static class ClassCombineCacheTests
{
    /// <summary>Một cặp cố định (dùng mọi frame) + <c>Churn</c> cặp class sinh động chỉ dùng một lần mỗi frame.</summary>
    sealed class Host : View
    {
        public int Frame, Churn = 20;
        public override Div Render(Window w)
        {
            var root = w.Div().Class("flex flex-col");
            root.Child(w.Div().Class("hover:bg-red-500").Class("hover:text-white").Child("live"));
            for (int i = 0; i < Churn; i++)
                root.Child(w.Div().Class("hover:w-[" + (Frame * 100 + i) + "px]").Class("hover:h-[" + (Frame * 100 + i) + "px]"));
            return root;
        }
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("perf/cache/class-combine-two-generations", t =>
        {
            int saved = Tailwind.MaxCachedClassLists;
            Tailwind.MaxCachedClassLists = 64; // 20 cặp mỗi frame: đổi thế hệ cứ ~3 frame
            try
            {
                var host = new Host();
                t.Open(host);
                t.Frames(2);
                long before = PerfCounters.CombineBuild;
                const int frames = 60;
                for (int i = 0; i < frames; i++)
                {
                    host.Frame++;
                    host.Notify();
                    t.Frames(1);
                }
                long built = PerfCounters.CombineBuild - before;
                // Dung sai 10: slot tham chiếu chuỗi của Tailwind.Get có thể bị đè bởi test khác trong cùng tiến trình (ClassList biên dịch lại = khoá cặp mới); logic cũ (xoá hết) cho +20.
                // Mọi cặp sinh động là mới (host.Churn mỗi frame); cặp cố định không được ghép lại dù qua nhiều lần đổi thế hệ.
                // Frame của harness render lại mọi view nên số lần Render ≥ số frame: đếm theo số cặp sinh động thật sự khác nhau.
                long unique = (long)frames * host.Churn;
                t.Assert.True(built >= unique, $"cặp sinh động đều ghép mới ({built} ≥ {unique})");
                t.Assert.True(built <= unique + 10, $"cặp cố định không bị ghép lại sau đổi thế hệ: {built} lần ghép cho {unique} cặp mới");
            }
            finally { Tailwind.MaxCachedClassLists = saved; }
        });
    }
}

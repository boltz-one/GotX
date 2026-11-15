using GotX;
using GotX.Test;

/// <summary>box-shadow ngoài chỉ được vẽ NGOÀI hộp viền (CSS): nền trong suốt / mờ không để bóng lộ ra bên trong.</summary>
static class ShadowExclusionTests
{
    /// <summary>Màu c với độ mờ alpha trên nền trắng.</summary>
    static Rgba Tint(Rgba c, float alpha) => new(c.R * alpha + (1 - alpha), c.G * alpha + (1 - alpha), c.B * alpha + (1 - alpha), 1);

    static View Sample(string classes) => new Fx(w => Fx.Page(w).Child(w.Div().Key("box").Class(classes)));

    public static void Register(TestSuite suite)
    {
        suite.Add("core/shadow/transparent-background-stays-clean-inside", t =>
        {
            t.Open(_ => Sample("w-40 h-9 rounded-md border border-slate-300 bg-transparent shadow-md"));
            t.Assert.Pixel(new Point(104, 42), Rgba.White, 1);
            t.Assert.Pixel(new Point(32, 57), Rgba.White, 1);          // sát đáy hộp, trong viền
            var below = t.PixelAt(new Point(104, 61.5f));                // ngay dưới mép dưới (24 + 36 = 60): bóng vẫn còn
            t.Assert.True(below.R < 0.985f, $"bóng vẫn hiện dưới hộp (R={below.R:0.000})");
        });

        suite.Add("core/shadow/opaque-background-unchanged-and-translucent-tint-correct", t =>
        {
            t.Open(_ => Sample("w-40 h-9 rounded-md bg-white shadow-md"));
            t.Assert.Pixel(new Point(104, 42), Rgba.White, 1);
            t.Open(_ => Sample("w-40 h-9 rounded-md bg-red-500/30 shadow-md"));
            // 30 % red-500 trên trắng, không bị bóng làm tối
            t.Assert.Pixel(new Point(104, 42), Tint(Theme.Colors["red-500"], 0.3f), 3);
        });

        suite.Add("core/shadow/exclusion-follows-view-replay-and-scroll", t =>
        {
            t.Open(_ => new Fx(w => w.Div().Class("size-full bg-white p-6").Child(
                w.Div().Key("scroll").Class("h-40 overflow-y-auto").Child(w.Div().Class("h-10").Child("x"))
                    .Child(w.Div().Key("box").Class("w-40 h-9 rounded-md border border-slate-300 bg-transparent shadow-md"))
                    .Child(w.Div().Class("h-96")))));
            t.Scroll("scroll", 16);
            t.Settle();
            var box = t.Get("box").Screen;
            t.Assert.Pixel(new Point(box.X + box.Width / 2, box.Y + box.Height / 2), Rgba.White, 1);
            t.Assert.True(t.PixelAt(new Point(box.X + box.Width / 2, box.Bottom + 1.5f)).R < 0.985f, "bóng dưới hộp sau khi cuộn");
        });

        suite.Add("core/shadow/exclusion-on-rotated-box", t =>
        {
            t.Open(_ => Sample("w-40 h-9 rounded-md bg-transparent shadow-lg rotate-6"));
            t.Assert.Pixel(new Point(104, 42), Rgba.White, 1);
        });
    }
}

using GotX;
using GotX.Test;

/// <summary>Hẹn giờ của App dưới đồng hồ ảo: timeout, interval, huỷ, thứ tự, và việc callback làm view vẽ lại.</summary>
static class TimerTests
{
    public static void Register(TestSuite suite)
    {
        suite.Add("core/timers/timeout-fires-once", t =>
        {
            t.Open(new CounterHost());
            int fired = 0;
            t.App.SetTimeout(0.5, () => fired++);
            t.Assert.Equal(t.App.PendingTimers, 1, "một hẹn giờ đang chờ");
            t.Advance(0.4);
            t.Assert.Equal(fired, 0, "chưa tới hạn");
            t.Advance(0.2);
            t.Assert.Equal(fired, 1, "tới hạn thì chạy");
            t.Advance(2);
            t.Assert.True(fired == 1 && t.App.PendingTimers == 0, "timeout chỉ chạy một lần rồi tự gỡ");
        });

        suite.Add("core/timers/interval-repeats-until-disposed", t =>
        {
            t.Open(new CounterHost());
            int ticks = 0;
            var interval = t.App.SetInterval(0.1, () => ticks++);
            t.Advance(1.0);
            t.Assert.True(ticks is >= 9 and <= 10, $"khoảng 10 lần trong 1 giây với bước frame 16 ms (thực tế {ticks})");
            int at = ticks;
            interval.Dispose();
            t.Advance(1.0);
            t.Assert.True(ticks == at && t.App.PendingTimers == 0, "Dispose thì dừng hẳn");
            // Đồng hồ nhảy xa (app bị treo): interval không gọi dồn các chu kỳ đã lỡ.
            int late = 0;
            using var slow = t.App.SetInterval(0.1, () => late++);
            t.Step(5);
            t.Assert.Equal(late, 1, "trễ 50 chu kỳ vẫn chỉ gọi một lần");
        });

        suite.Add("core/timers/cancel-before-due", t =>
        {
            t.Open(new CounterHost());
            int fired = 0;
            var a = t.App.SetTimeout(0.2, () => fired++);
            t.App.SetTimeout(0.2, () => fired += 10);
            a.Dispose();
            t.Assert.Equal(t.App.PendingTimers, 1, "hẹn giờ đã huỷ không còn được tính");
            t.Advance(0.3);
            t.Assert.Equal(fired, 10, "chỉ cái chưa huỷ chạy");
        });

        suite.Add("core/timers/ordering-by-due-time", t =>
        {
            t.Open(new CounterHost());
            var log = new List<string>();
            t.App.SetTimeout(0.30, () => log.Add("c"));
            t.App.SetTimeout(0.10, () => log.Add("a"));
            t.App.SetTimeout(0.20, () => log.Add("b"));
            t.App.SetTimeout(0, () => log.Add("0"));
            t.Step(1); // cả bốn cùng tới hạn trong một lần RunTimers
            t.Assert.Equal(string.Join("", log), "0abc", "cùng tới hạn thì chạy theo thứ tự thời hạn, không theo thứ tự đăng ký");
            // Hẹn giờ đặt bên trong callback chỉ chạy ở lượt sau.
            log.Clear();
            t.App.SetTimeout(0.01, () => { log.Add("ngoài"); t.App.SetTimeout(0, () => log.Add("trong")); });
            t.Step();
            t.Assert.Equal(string.Join(",", log), "ngoài", "callback lồng chưa chạy trong cùng lượt");
            t.Step();
            t.Assert.Equal(string.Join(",", log), "ngoài,trong", "…mà ở lượt kế tiếp");
        });

        suite.Add("core/timers/callback-notify-redraws-only-that-view", t =>
        {
            var host = t.Open(new CounterHost());
            t.App.SetTimeout(0.05, () => { host.Counter.Count = 42; host.Counter.Notify(); });
            int renders = t.Perf.CountRenders(() => t.Advance(0.1));
            t.Assert.Text("c-value", "Giá trị: 42");
            t.Assert.True(renders == 2, $"callback Notify → effect được xử lý, chỉ bộ đếm + view cha render ({renders})");
        });
    }
}

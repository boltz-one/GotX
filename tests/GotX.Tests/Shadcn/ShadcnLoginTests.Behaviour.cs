using GotX;
using GotX.Test;

/// <summary>Phần hành vi form của <see cref="ShadcnLoginTests"/> (tách file để mỗi file dưới 200 dòng).</summary>
static partial class ShadcnLoginTests
{
    static string KeyOf(AuthAction action) => action switch
    {
        AuthAction.Home => "auth-home", AuthAction.Apple => "auth-apple", AuthAction.Google => "auth-google", AuthAction.Meta => "auth-meta",
        AuthAction.ForgotPassword => "auth-forgot-password", AuthAction.SignUp => "auth-sign-up", AuthAction.SignIn => "auth-sign-in",
        AuthAction.Terms => "auth-terms", _ => "auth-privacy",
    };

    static void RegisterBehaviour(TestSuite suite, Spec spec)
    {
        suite.Add($"shadcn/{spec.Id}/empty-submit-shows-required-errors", t =>
        {
            var block = Open(t, spec);
            foreach (var f in block.Fields) t.Assert.NotExists(f.ErrorKey);
            t.Click("auth-submit");
            foreach (var f in block.Fields)
            {
                t.Assert.Text(f.ErrorKey, AuthField.RequiredMessage);
                t.Assert.True(f.Input.Invalid, $"ô {f.Id} được đánh dấu Invalid");
            }
            t.Assert.True(block.Fields[0].Input.IsFocused, "ô sai đầu tiên nhận focus");
            t.Assert.True(block.SubmittedEmail is null, "form trống không được gửi");
            t.Assert.NotExists("auth-status");
            t.Assert.NoLint(Lint());

            // Email sai dạng: thông báo riêng; gõ vào ô thì lỗi của ô đó tự mất, các ô khác giữ nguyên.
            var email = block.Field("email");
            var password = block.Field("password");
            t.Click(email.Key);
            t.Type("not-an-email");
            t.Assert.NotExists(email.ErrorKey);
            t.Assert.True(!email.Input.Invalid && password.Input.Invalid, "gõ vào email chỉ xoá lỗi của email");
            t.Press(Key.Enter);
            t.Assert.Text(email.ErrorKey, AuthField.EmailMessage);
            t.Assert.Text(password.ErrorKey, AuthField.RequiredMessage);
        });

        suite.Add($"shadcn/{spec.Id}/enter-submits", t =>
        {
            var block = Open(t, spec);
            string? submitted = null;
            t.App.Subscribe<AuthBlock.Submitted>(block, e => submitted = e.Email);
            // Điền mọi ô trừ ô cuối rồi Enter: bị chặn, chỉ ô cuối báo lỗi và nhận focus (như trình duyệt).
            var last = block.Fields[^1];
            foreach (var f in block.Fields)
                if (f != last) { t.Click(f.Key); t.Type(Sample(f)); }
            t.Press(Key.Enter);
            foreach (var f in block.Fields)
                if (f != last) t.Assert.NotExists(f.ErrorKey);
            t.Assert.Text(last.ErrorKey, AuthField.RequiredMessage);
            t.Assert.True(last.Input.IsFocused, $"focus chuyển sang ô {last.Id} còn trống");
            t.Assert.True(submitted is null, "chưa phát Submitted");

            t.Type(Sample(last));
            t.Assert.Equal(block.Field("password").Value, "hunter22", "giá trị mật khẩu thật");
            t.Assert.True(block.Field("password").Input.Box.Password, "ô mật khẩu là type=password (TextInput tự che bằng dấu chấm)");
            t.Press(Key.Enter);
            t.Assert.Equal(block.SubmittedEmail, "m@example.com", "SubmittedEmail");
            t.Assert.Equal(submitted, "m@example.com", "event Submitted");
            t.Assert.Text("auth-status", spec.StatusPrefix + "m@example.com");
            t.Assert.NotExists(last.ErrorKey);
            t.Assert.NoLint(Lint());
        });

        suite.Add($"shadcn/{spec.Id}/submit-button-submits", t =>
        {
            var block = Open(t, spec);
            Fill(t, block);
            t.Click("auth-submit");
            t.Assert.Equal(block.SubmittedEmail, "m@example.com", "SubmittedEmail sau khi bấm nút gửi");
            t.Assert.Text("auth-status", spec.StatusPrefix + "m@example.com");
            foreach (var f in block.Fields) t.Assert.True(!f.Input.Invalid, $"ô {f.Id} không bị đánh dấu Invalid");
        });

        suite.Add($"shadcn/{spec.Id}/password-mask-edits-in-the-middle", t =>
        {
            var password = Open(t, spec).Field("password");
            t.Click(password.Key);
            t.Type("abcd");
            t.Press(Key.Left);
            t.Press(Key.Left);
            t.Type("XY");
            t.Assert.Equal(password.Value, "abXYcd", "chèn giữa chuỗi");
            t.Press(Key.Backspace);
            t.Assert.Equal(password.Value, "abXcd", "xoá giữa chuỗi");
            t.Press(Key.A, Modifiers.Control);
            t.Type("z");
            t.Assert.Equal(password.Value, "z", "gõ đè lên vùng chọn");
            t.Assert.True(password.Input.Box.Password && password.Input.Text == "z", "ô che bằng dấu chấm, giữ giá trị thật");
        });

        suite.Add($"shadcn/{spec.Id}/labels-and-tab-order", t =>
        {
            var block = Open(t, spec);
            for (int i = block.Fields.Count - 1; i >= 0; i--)
            {
                t.Click(block.Fields[i].LabelKey);
                t.Assert.True(block.Fields[i].Input.IsFocused, $"bấm nhãn {block.Fields[i].Label} → ô {block.Fields[i].Id} nhận focus");
            }
            // Từ ô đầu tiên, Tab đi qua các ô theo thứ tự DOM của bản gốc rồi tới nút gửi; ở form đăng nhập liên kết
            // "Forgot your password?" đứng trước ô mật khẩu.
            for (int i = 1; i < block.Fields.Count; i++)
            {
                t.Press(Key.Tab);
                if (spec.IsLogin && block.Fields[i].Id == "password")
                {
                    t.Assert.Focused("auth-forgot-password");
                    t.Press(Key.Tab);
                }
                t.Assert.True(block.Fields[i].Input.IsFocused, $"Tab tới ô {block.Fields[i].Id}");
            }
            t.Press(Key.Tab);
            t.Assert.Focused("auth-submit");
            // Enter trên nút đang focus cũng gửi form (form trống → báo lỗi, focus về ô đầu).
            t.Press(Key.Enter);
            t.Assert.Exists(block.Fields[0].ErrorKey);
            t.Assert.True(block.Fields[0].Input.IsFocused, "gửi form trống đưa focus về ô đầu tiên");
        });

        suite.Add($"shadcn/{spec.Id}/social-and-links", t =>
        {
            var block = Open(t, spec);
            var seen = new List<AuthAction>();
            t.App.Subscribe<AuthBlock.ActionInvoked>(block, e => seen.Add(e.Action));
            t.Assert.True(block.LastAction is null, "chưa bấm gì");
            foreach (var action in spec.Actions)
            {
                t.Click(KeyOf(action));
                t.Assert.Equal(block.LastAction, action, "LastAction sau khi bấm " + KeyOf(action));
            }
            t.Assert.Equal(string.Join(",", seen), string.Join(",", spec.Actions), "chuỗi ActionInvoked");
            // Dùng được bằng bàn phím: Enter trên liên kết đang focus phát lại hành động.
            t.Press(Key.Enter);
            t.Assert.Equal(seen.Count, spec.Actions.Length + 1, "Enter trên liên kết đang focus");
            t.Assert.True(block.SubmittedEmail is null && block.Fields[0].Error is null, "nút phụ không gửi form");

            // Thứ tự Tab của các nút phụ đúng thứ tự khai báo (thứ tự DOM): đi hết một vòng Tab và ghi lại.
            var order = new List<AuthAction>();
            t.Window.Blur();
            for (int i = 0; i < spec.Actions.Length + block.Fields.Count + 1; i++)
            {
                t.Press(Key.Tab);
                foreach (var action in spec.Actions)
                    if (t.Window.IsFocused(KeyOf(action))) order.Add(action);
            }
            t.Assert.Equal(string.Join(",", order), string.Join(",", spec.Actions), "thứ tự Tab của nút/liên kết phụ");
        });
    }
}

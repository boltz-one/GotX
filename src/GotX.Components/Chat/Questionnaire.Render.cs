namespace GotX.Components;

// Phần vẽ và bàn phím của Questionnaire, tách khỏi phần state để mỗi file dưới 200 dòng.
public sealed partial class Questionnaire
{
    const string ChoiceBase =
        "relative flex min-h-11 cursor-pointer items-start gap-2.5 rounded-lg border px-3 py-2.5 text-sm transition-colors " +
        "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 ";
    const string ChoiceOn = ChoiceBase + "border-primary/40 bg-muted";
    const string ChoiceOff = ChoiceBase + "border-input bg-transparent hover:bg-muted/50 dark:bg-input/20";

    const string IndicatorBase = "relative flex size-4 shrink-0 translate-y-0.5 items-center justify-center border ";
    const string IndicatorOn = IndicatorBase + "border-primary bg-primary text-primary-foreground";
    const string IndicatorOff = IndicatorBase + "border-input dark:bg-input/30";

    // [đã chọn, nhiều lựa chọn] → class dựng sẵn: radio tròn, checkbox bo 4px.
    static readonly string[] Indicators =
    [
        IndicatorOff + " rounded-full", IndicatorOff + " rounded-[4px]", IndicatorOn + " rounded-full", IndicatorOn + " rounded-[4px]",
    ];

    RovingFocus? _roving;
    Window? _window;
    string? _prevKey, _skipKey, _nextKey;
    string[] _answerKeys = [];

    /// <summary>
    /// Phím trên lựa chọn đang focus. Mũi tên/Home/End chuyển focus (câu chọn một thì chọn luôn, như nhóm radio);
    /// Space chọn/bỏ chọn (Core gọi OnClick); Enter là "xác nhận": chọn lựa chọn đang focus nếu câu chưa có câu trả
    /// lời rồi sang câu sau / gửi, để trả lời cả bộ câu hỏi không cần rời bàn phím.
    /// </summary>
    bool OnChoiceKey(int index, KeyEvent e)
    {
        if (_window is not { } w || _roving is not { } roving || _submitted) return false;
        var q = _questions[_step];
        int count = q.Choices.Count;
        if (e.Key == Key.Tab)
        {
            // Câu chọn một là một điểm dừng Tab (nhóm radio); câu chọn nhiều thì mỗi checkbox là một điểm dừng.
            if (!q.Multiple) roving.LeaveOnTab(w, count, e.Shift);
            return false;
        }
        if (e.Key == Key.Enter)
        {
            if (!q.Multiple || !HasAnswer) { if (!_answers[_step].Contains(index)) Choose(index); }
            Next();
            // Câu mới dùng lại cùng bộ khoá: đưa focus về lựa chọn đã chọn trước đó của câu ấy (hoặc lựa chọn đầu).
            if (!_submitted) roving.Focus(w, _answers[_step].Count > 0 ? _answers[_step][0] : 0);
            return true;
        }
        int next = RovingFocus.Step(e.Key, index, count);
        if (next < 0) return false;
        if (!q.Multiple) Choose(next);
        roving.Focus(w, next);
        return true;
    }

    string _progress = "";
    (int Step, int Count) _progressFor = (-1, -1);

    // Dòng tiến độ chỉ dựng lại khi sang câu khác, không phải mỗi lần render (focus, hover cũng làm view render lại).
    string ProgressText()
    {
        var key = (_step, _questions.Count);
        if (key != _progressFor) { _progressFor = key; _progress = $"Câu {_step + 1} / {_questions.Count}"; }
        return _progress;
    }

    /// <summary>Vẽ câu hiện tại (tiến độ, tiêu đề, lựa chọn, nút) hoặc bản tóm tắt sau khi gửi.</summary>
    public override Div Render(Window w)
    {
        _window = w;
        var root = w.Div().Class("flex w-full min-w-0 flex-col gap-4");
        if (_questions.Count == 0) return root;
        if (_submitted) return root.Child(RenderSummary(w));

        var q = _questions[_step];
        root.Child(w.Div().Class("text-xs font-medium text-muted-foreground").Child(ProgressText()));

        var item = w.Div().Class("flex min-w-0 flex-col gap-4");
        var heading = w.Div().Class("flex flex-col gap-1")
            .Child(w.Div().Class("text-base leading-snug font-medium").Child(q.Title));
        if (q.Description is { } d) heading.Child(w.Div().Class("text-sm text-muted-foreground").Child(d));
        item.Child(heading);

        var refs = Indexed<Questionnaire>.Ensure(ref _refs, this, q.Choices.Count);
        var roving = _roving ??= new RovingFocus($"gotx-questionnaire-{Id.Value}-choice");
        var picked = _answers[_step];
        if (!q.Multiple) roving.Sync(w, q.Choices.Count, picked.Count > 0 ? picked[0] : 0);
        var choices = w.Div().Class("grid min-w-0 gap-2");
        for (int i = 0; i < q.Choices.Count; i++)
        {
            bool on = picked.Contains(i);
            var choice = q.Choices[i];
            var indicator = w.Div().Class(Indicators[(on ? 2 : 0) + (q.Multiple ? 1 : 0)]);
            if (on) indicator.Child(q.Multiple ? Icon.Check(w).Class("size-3.5") : w.Div().Class("size-2 rounded-full bg-primary-foreground"));
            var label = w.Div().Class("flex min-w-0 flex-1 flex-col gap-0.5 leading-snug").Child(choice.Label);
            if (choice.Description is { } cd) label.Child(w.Div().Class("text-muted-foreground").Child(cd));
            choices.Child(w.Div().Class(on ? ChoiceOn : ChoiceOff)
                .Key(roving.Key(i)).Focusable()
                .OnKey(refs[i], static (r, e) => r.Owner.OnChoiceKey(r.Index, e))
                .OnClick(refs[i], static (r, _) => r.Owner.Choose(r.Index))
                .Child(indicator).Child(label));
        }
        item.Child(choices);
        root.Child(item);

        _prevKey ??= $"gotx-questionnaire-{Id.Value}-prev";
        _skipKey ??= $"gotx-questionnaire-{Id.Value}-skip";
        _nextKey ??= $"gotx-questionnaire-{Id.Value}-next";
        var actions = w.Div().Class("flex min-h-8 w-full items-center gap-2")
            .Child(Ui.Button(w, PreviousLabel, ButtonVariant.Outline).Key(_prevKey)
                .Disabled(_step == 0).OnClick(this, static (s, _) => s.Previous()))
            .Child(w.Div().Class("flex-1"));
        if (AllowSkip)
            actions.Child(Ui.Button(w, SkipLabel, ButtonVariant.Outline).Key(_skipKey)
                .OnClick(this, static (s, _) => s.Skip()));
        actions.Child(Ui.Button(w, IsLast ? SubmitLabel : NextLabel).Key(_nextKey)
            .Disabled(!HasAnswer).OnClick(this, static (s, _) => s.Next()));
        return root.Child(actions);
    }

    /// <summary>Sau khi gửi: tóm tắt câu trả lời để người dùng thấy những gì đã gửi.</summary>
    Div RenderSummary(Window w)
    {
        if (_answerKeys.Length != _questions.Count)
        {
            _answerKeys = new string[_questions.Count];
            for (int i = 0; i < _answerKeys.Length; i++) _answerKeys[i] = $"gotx-questionnaire-{Id.Value}-answer-{i}";
        }
        var list = w.Div().Class("flex flex-col gap-3")
            .Child(w.Div().Class("text-sm font-medium").Child("Đã gửi câu trả lời"));
        for (int i = 0; i < _questions.Count; i++)
        {
            string answer = _answers[i].Count == 0 ? "(bỏ qua)" : string.Join(", ", AnswerLabels(i));
            list.Child(w.Div().Class("flex flex-col gap-0.5 text-sm")
                .Child(w.Div().Class("text-muted-foreground").Child(_questions[i].Title))
                .Child(w.Div().Key(_answerKeys[i]).Child(answer)));
        }
        return list;
    }
}

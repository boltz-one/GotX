namespace GotX.Components;

/// <summary>Một lựa chọn của câu hỏi: nhãn và dòng giải thích mờ (tuỳ chọn).</summary>
public sealed record QuestionChoice(string Label, string? Description = null);

/// <summary>Một câu hỏi: chọn một (radio) hoặc chọn nhiều (<paramref name="Multiple"/>, checkbox).</summary>
public sealed record Question(string Title, IReadOnlyList<QuestionChoice> Choices, bool Multiple = false, string? Description = null);

/// <summary>
/// Bộ câu hỏi nhiều bước mà agent hỏi người dùng: mỗi lần một câu, có Quay lại / Bỏ qua / Tiếp, câu cuối là Gửi.
/// Câu trả lời giữ nguyên khi quay lại. Gửi xong phát <see cref="Submitted"/> với chỉ số các lựa chọn của từng câu.
/// Bàn phím: mũi tên chuyển giữa các lựa chọn, Space chọn, Enter xác nhận rồi sang câu sau (xem <c>OnChoiceKey</c>).
/// Chưa có câu trả lời dạng ô nhập chữ và phím tắt số cho từng lựa chọn.
/// </summary>
public sealed partial class Questionnaire : View
{
    /// <summary>Đã chuyển sang câu thứ <c>Step</c> (0-based).</summary>
    public readonly record struct StepChanged(int Step);

    /// <summary>Người dùng bấm Gửi; <c>Answers[i]</c> là chỉ số các lựa chọn của câu i (rỗng = bỏ qua).</summary>
    public readonly record struct Submitted(IReadOnlyList<IReadOnlyList<int>> Answers);

    IReadOnlyList<Question> _questions = [];
    List<int>[] _answers = [];
    int _step;
    bool _submitted;
    Indexed<Questionnaire>[]? _refs;

    /// <summary>Danh sách câu hỏi; gán lại thì xoá câu trả lời và về câu đầu.</summary>
    public IReadOnlyList<Question> Questions
    {
        get => _questions;
        set
        {
            _questions = value;
            _answers = new List<int>[value.Count];
            for (int i = 0; i < _answers.Length; i++) _answers[i] = new List<int>();
            _step = 0;
            _submitted = false;
            Notify();
        }
    }

    /// <summary>Cho phép bỏ qua câu không trả lời.</summary>
    public bool AllowSkip { get; set; } = true;

    /// <summary>Nhãn nút quay lại.</summary>
    public string PreviousLabel { get; set; } = "Quay lại";
    /// <summary>Nhãn nút bỏ qua.</summary>
    public string SkipLabel { get; set; } = "Bỏ qua";
    /// <summary>Nhãn nút sang câu sau.</summary>
    public string NextLabel { get; set; } = "Tiếp";
    /// <summary>Nhãn nút gửi ở câu cuối.</summary>
    public string SubmitLabel { get; set; } = "Gửi";

    /// <summary>Chỉ số câu đang hiện (0-based).</summary>
    public int Step => _step;
    /// <summary>Đã gửi: không đổi câu trả lời được nữa.</summary>
    public bool IsSubmitted => _submitted;
    /// <summary>Đang ở câu cuối (nút Tiếp thành Gửi).</summary>
    public bool IsLast => _step == _questions.Count - 1;

    /// <summary>Câu trả lời hiện tại (chỉ số lựa chọn, theo thứ tự bấm) của từng câu.</summary>
    public IReadOnlyList<IReadOnlyList<int>> Answers => _answers;

    /// <summary>Nhãn các lựa chọn đã chọn ở câu <paramref name="question"/>.</summary>
    public IEnumerable<string> AnswerLabels(int question) => _answers[question].Select(i => _questions[question].Choices[i].Label);

    bool HasAnswer => _step < _answers.Length && _answers[_step].Count > 0;

    /// <summary>Bấm vào lựa chọn <paramref name="index"/> của câu hiện tại: radio thay thế, checkbox bật/tắt.</summary>
    public void Choose(int index)
    {
        if (_submitted || _step >= _questions.Count) return;
        var picked = _answers[_step];
        if (_questions[_step].Multiple)
        {
            if (!picked.Remove(index)) picked.Add(index);
        }
        else
        {
            picked.Clear();
            picked.Add(index);
        }
        Notify();
    }

    /// <summary>Về câu trước; câu trả lời đã chọn được giữ nguyên.</summary>
    public void Previous() { if (!_submitted && _step > 0) GoTo(_step - 1); }

    /// <summary>Sang câu sau (cần đã chọn), hoặc gửi nếu đang ở câu cuối.</summary>
    public void Next()
    {
        if (_submitted || !HasAnswer) return;
        if (IsLast) Submit(); else GoTo(_step + 1);
    }

    /// <summary>Bỏ qua câu hiện tại: xoá lựa chọn của nó rồi đi tiếp (hoặc gửi).</summary>
    public void Skip()
    {
        if (_submitted || !AllowSkip || _step >= _questions.Count) return;
        _answers[_step].Clear();
        if (IsLast) Submit(); else GoTo(_step + 1);
    }

    /// <summary>Quay về câu đầu, xoá mọi câu trả lời.</summary>
    public void Reset() => Questions = _questions;

    void GoTo(int step)
    {
        _step = step;
        Notify();
        Emit(new StepChanged(step));
    }

    void Submit()
    {
        _submitted = true;
        Notify();
        var copy = new IReadOnlyList<int>[_answers.Length];
        for (int i = 0; i < copy.Length; i++) copy[i] = _answers[i].ToArray();
        Emit(new Submitted(copy));
    }
}

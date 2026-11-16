namespace GotX;

/// <summary>
/// Khai báo ngữ nghĩa trên Div (kiểu ARIA). Các hàm này không làm gì cho tới khi có người cần cây ngữ nghĩa
/// (công cụ hỗ trợ, test): lúc đó Window dựng một frame riêng có bật ghi. Frame thường không tốn thêm bộ nhớ hay CPU.
/// </summary>
public readonly partial struct Div
{
    /// <summary>Vai trò (button, checkbox, tab…). Không đặt: phần tử focus được tự nhận là button / nhóm.</summary>
    public Div Role(SemanticRole role) { if (_tree.SemanticsOn) _tree.SemOf(_id).Role = role; return this; }

    /// <summary>Tên truy cập (aria-label). Không đặt thì lấy từ chữ bên trong với các vai trò đặt tên theo nội dung.</summary>
    public Div AriaLabel(string? label) { if (_tree.SemanticsOn && label is not null) _tree.SemOf(_id).Name = label; return this; }

    /// <summary>Mô tả thêm (aria-description).</summary>
    public Div AriaDescription(string? text) { if (_tree.SemanticsOn && text is not null) _tree.SemOf(_id).Description = text; return this; }

    /// <summary>Mô tả lấy từ chữ của phần tử có <c>.Key(key)</c> (aria-describedby).</summary>
    public Div AriaDescribedBy(string key) { if (_tree.SemanticsOn) _tree.SemOf(_id).DescribedBy = _tree.KeyId(key); return this; }

    /// <summary>Tên lấy từ chữ của phần tử có <c>.Key(key)</c> (aria-labelledby).</summary>
    public Div AriaLabelledBy(string key) { if (_tree.SemanticsOn) _tree.SemOf(_id).LabelledBy = _tree.KeyId(key); return this; }

    /// <summary>Bật (<paramref name="on"/> = true) hoặc tắt các cờ trạng thái.</summary>
    public Div AriaState(SemanticState state, bool on = true)
    {
        if (!_tree.SemanticsOn) return this;
        ref var d = ref _tree.SemOf(_id);
        if (on) d.States |= state; else d.States &= ~state;
        if ((state & SemanticState.Checked) != 0) d.States |= SemanticState.Checkable;
        if ((state & SemanticState.Expanded) != 0) d.States |= SemanticState.Expandable;
        if ((state & SemanticState.Selected) != 0) d.States |= SemanticState.Selectable;
        return this;
    }

    /// <summary>Giá trị dạng chữ (aria-valuetext) – ô nhập tự lấy giá trị từ TextInput.</summary>
    public Div AriaValue(string? value) { if (_tree.SemanticsOn && value is not null) _tree.SemOf(_id).Value = value; return this; }

    /// <summary>Giá trị số trong khoảng (slider, progressbar).</summary>
    public Div AriaRange(float min, float max, float now)
    {
        if (!_tree.SemanticsOn) return this;
        ref var d = ref _tree.SemOf(_id);
        d.Min = min; d.Max = max; d.Now = now; d.HasRange = true;
        return this;
    }
}

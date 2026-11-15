namespace GotX.Components;

/// <summary>
/// State cho handler của phần tử thứ <see cref="Index"/> trong một danh sách. OnClick nhận state + lambda static
/// (không capture), nên mỗi phần tử cần một object mang chỉ số của nó; các object này được tạo một lần rồi dùng lại:
/// <code>
/// var refs = Indexed&lt;Tabs&gt;.Ensure(ref _refs, this, Items.Count);
/// item.OnClick(refs[i], static (r, _) =&gt; r.Owner.Select(r.Index));
/// </code>
/// </summary>
public sealed class Indexed<T> where T : class
{
    public T Owner { get; }
    public int Index { get; }

    Indexed(T owner, int index) { Owner = owner; Index = index; }

    /// <summary>Trả về mảng có ít nhất <paramref name="count"/> phần tử, chỉ cấp phát khi cần nhiều hơn lần trước.</summary>
    public static Indexed<T>[] Ensure(ref Indexed<T>[]? cache, T owner, int count)
    {
        if (cache is not null && cache.Length >= count) return cache;
        var next = new Indexed<T>[count];
        for (int i = 0; i < count; i++) next[i] = cache is not null && i < cache.Length ? cache[i] : new Indexed<T>(owner, i);
        return cache = next;
    }
}

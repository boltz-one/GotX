namespace GotX.Components;

/// <summary>
/// Chỉ mục chiều cao của danh sách dài: tổng cộng dồn và "hàng nào ở độ cao y" đều O(log n) (cây Fenwick), đổi chiều cao
/// một hàng cũng O(log n). Khởi tạo bằng chiều cao ước lượng cho mọi hàng; hàng đo được thì <see cref="Set"/>.
/// </summary>
internal sealed class HeightIndex
{
    readonly double[] _tree;   // Fenwick 1-based
    readonly float[] _height;
    readonly int _count, _top; // _top = luỹ thừa 2 lớn nhất ≤ count (bước nhảy đầu khi tìm theo y)

    public HeightIndex(int count, float estimate)
    {
        _count = count;
        _height = new float[count];
        _tree = new double[count + 1];
        Array.Fill(_height, estimate);
        // Mọi hàng cùng chiều cao: nút i phủ (i & -i) hàng, không cần vòng cộng dồn.
        for (int i = 1; i <= count; i++) _tree[i] = (double)estimate * (i & -i);
        _top = count == 0 ? 0 : 1 << (31 - int.LeadingZeroCount(count));
    }

    public int Count => _count;
    public float this[int index] => _height[index];
    public double Total => Offset(_count);

    /// <summary>Độ cao của mép trên hàng <paramref name="index"/> (index = Count → tổng).</summary>
    public double Offset(int index)
    {
        double sum = 0;
        for (int i = index; i > 0; i -= i & -i) sum += _tree[i];
        return sum;
    }

    /// <summary>Đặt chiều cao hàng đo được; trả false nếu không đổi đáng kể (&lt; 0,5 px).</summary>
    public bool Set(int index, float height)
    {
        float delta = height - _height[index];
        if (MathF.Abs(delta) < 0.5f) return false;
        _height[index] = height;
        for (int i = index + 1; i <= _count; i += i & -i) _tree[i] += delta;
        return true;
    }

    /// <summary>Hàng chứa độ cao <paramref name="y"/> (kẹp trong [0, Count-1]).</summary>
    public int IndexAt(double y)
    {
        if (_count == 0) return 0;
        int pos = 0;
        for (int step = _top; step > 0; step >>= 1)
        {
            int next = pos + step;
            if (next <= _count && _tree[next] <= y) { pos = next; y -= _tree[next]; }
        }
        return Math.Min(pos, _count - 1);
    }
}

using System.Globalization;
using System.Text;

namespace GotX.Test;

/// <summary>
/// Bộ ghi JSON tối thiểu viết tay: không dùng serializer/reflection để thư viện giữ được AOT-compatible.
/// Tự chèn dấu phẩy; người gọi chịu trách nhiệm gọi Begin/End cân nhau.
/// </summary>
public sealed class JsonWriter
{
    readonly StringBuilder _sb = new();
    readonly List<bool> _first = new(); // mỗi tầng: phần tử kế tiếp có phải phần tử đầu
    bool _afterName;

    void Sep()
    {
        if (_afterName) { _afterName = false; return; }
        if (_first.Count == 0) return;
        if (!_first[^1]) _sb.Append(',');
        _first[^1] = false;
    }

    public JsonWriter BeginObject() { Sep(); _sb.Append('{'); _first.Add(true); return this; }
    public JsonWriter EndObject() { _sb.Append('}'); _first.RemoveAt(_first.Count - 1); return this; }
    public JsonWriter BeginArray() { Sep(); _sb.Append('['); _first.Add(true); return this; }
    public JsonWriter EndArray() { _sb.Append(']'); _first.RemoveAt(_first.Count - 1); return this; }

    public JsonWriter Name(string name)
    {
        Sep();
        Quote(name);
        _sb.Append(':');
        _afterName = true;
        return this;
    }

    public JsonWriter Value(string? s)
    {
        Sep();
        if (s is null) _sb.Append("null"); else Quote(s);
        return this;
    }

    public JsonWriter Value(bool b) { Sep(); _sb.Append(b ? "true" : "false"); return this; }
    public JsonWriter Value(int n) { Sep(); _sb.Append(n.ToString(CultureInfo.InvariantCulture)); return this; }
    public JsonWriter Value(long n) { Sep(); _sb.Append(n.ToString(CultureInfo.InvariantCulture)); return this; }

    public JsonWriter Value(double d)
    {
        Sep();
        _sb.Append(double.IsFinite(d) ? Math.Round(d, 3).ToString("0.###", CultureInfo.InvariantCulture) : "null");
        return this;
    }

    public JsonWriter Bounds(Bounds b) =>
        BeginArray().Value(b.X).Value(b.Y).Value(b.Width).Value(b.Height).EndArray();

    public JsonWriter Strings(IEnumerable<string> items)
    {
        BeginArray();
        foreach (string s in items) Value(s);
        return EndArray();
    }

    void Quote(string s)
    {
        _sb.Append('"');
        foreach (char c in s)
            switch (c)
            {
                case '"': _sb.Append("\\\""); break;
                case '\\': _sb.Append("\\\\"); break;
                case '\n': _sb.Append("\\n"); break;
                case '\r': _sb.Append("\\r"); break;
                case '\t': _sb.Append("\\t"); break;
                default:
                    if (c < 0x20) _sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else _sb.Append(c);
                    break;
            }
        _sb.Append('"');
    }

    public override string ToString() => _sb.ToString();
}

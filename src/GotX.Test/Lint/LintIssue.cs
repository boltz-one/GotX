namespace GotX.Test;

public enum Severity { Info, Warning, Error }

/// <summary>Một vấn đề bố cục/hiển thị do <see cref="UiLint"/> phát hiện.</summary>
public sealed class LintIssue
{
    public required string Rule { get; init; }
    public required Severity Severity { get; init; }
    /// <summary>Mô tả có số đo cụ thể (px, tỉ lệ tương phản…).</summary>
    public required string Message { get; init; }
    /// <summary>Đường dẫn phần tử (xem <see cref="UiTree.Path"/>).</summary>
    public required string Path { get; init; }
    /// <summary>Hộp trên màn hình của phần tử (logical px).</summary>
    public Bounds Bounds { get; init; }
    /// <summary>Hộp của phần tử liên quan (anh em bị so, phần tử che, tổ tiên cắt), nếu có.</summary>
    public Bounds? Related { get; init; }
    public int NodeIndex { get; init; } = -1;
    /// <summary>Số phần tử có cùng đường dẫn cùng mắc vấn đề này (UiDoctor gộp; các dòng danh sách không có khoá riêng).</summary>
    public int Count { get; set; } = 1;
    /// <summary>Cấu hình phát hiện ra (UiDoctor điền): <c>800×600 dark @1.5</c>.</summary>
    public string? Config { get; set; }
    /// <summary>Ảnh đã khoanh vùng vấn đề này (UiDoctor điền).</summary>
    public string? Artifact { get; set; }

    /// <summary>Khoá để gộp trùng và so với baseline: cùng luật trên cùng phần tử là cùng một vấn đề, dù số đo đổi theo kích thước cửa sổ.</summary>
    public string Key => Rule + " | " + Path;

    public override string ToString() => $"{Severity.ToString().ToLowerInvariant(),-7} {Rule,-13} {Path} {Fmt.B(Bounds)}: {Message}";
}

/// <summary>Tuỳ chọn lint: bật/tắt luật, bỏ qua theo mẫu, các ngưỡng.</summary>
public sealed class LintOptions
{
    readonly List<(string Rule, string Pattern)> _suppress = new();
    readonly HashSet<string> _disabled = new();
    HashSet<string>? _only;

    /// <summary>Vùng bấm nhỏ hơn cạnh này (logical px) bị báo <c>small-target</c>.</summary>
    public float MinTarget { get; set; } = 16;
    /// <summary>Độ lệch tối đa (px) còn bị coi là "định căn mà lệch" của <c>misaligned</c>; lệch hơn thế được coi là cố ý.</summary>
    public float AlignMax { get; set; } = 2;
    /// <summary>Phần tràn/cắt nhỏ hơn mức này (px) được bỏ qua.</summary>
    public float Tolerance { get; set; } = 0.5f;
    /// <summary>Tỉ lệ tương phản WCAG: dưới <see cref="ContrastWarn"/> là warning, dưới <see cref="ContrastInfo"/> là info.</summary>
    public float ContrastWarn { get; set; } = 3f;
    public float ContrastInfo { get; set; } = 4.5f;

    /// <summary>Tắt hẳn các luật.</summary>
    public LintOptions Disable(params string[] rules) { foreach (string r in rules) _disabled.Add(r); return this; }

    /// <summary>Chỉ chạy các luật này.</summary>
    public LintOptions Only(params string[] rules) { _only = new HashSet<string>(rules); return this; }

    /// <summary>
    /// Bỏ qua luật <paramref name="rule"/> (<c>"*"</c> = mọi luật) trên phần tử có khoá, chuỗi class hoặc đường dẫn khớp
    /// <paramref name="pattern"/>. Mẫu là chuỗi con; <c>*</c> khớp đoạn bất kỳ (<c>"badge*absolute"</c>).
    /// </summary>
    public LintOptions Suppress(string rule, string pattern) { _suppress.Add((rule, pattern)); return this; }

    public bool Enabled(string rule) => !_disabled.Contains(rule) && (_only is null || _only.Contains(rule));

    internal bool Suppressed(string rule, UiNode node, string path)
    {
        foreach (var (r, pattern) in _suppress)
        {
            if (r != "*" && r != rule) continue;
            if (Glob(node.Key, pattern) || Glob(node.Classes, pattern) || Glob(path, pattern)) return true;
        }
        return false;
    }

    static bool Glob(string? text, string pattern)
    {
        if (text is null) return false;
        int at = 0;
        foreach (string part in pattern.Split('*', StringSplitOptions.RemoveEmptyEntries))
        {
            int found = text.IndexOf(part, at, StringComparison.Ordinal);
            if (found < 0) return false;
            at = found + part.Length;
        }
        return true;
    }
}

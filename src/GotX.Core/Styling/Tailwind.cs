using System.Collections.Concurrent;
using System.Globalization;

namespace GotX;

/// <summary>
/// Biên dịch chuỗi class Tailwind (v4) thành lệnh gán Style.
/// Cú pháp: <c>[variant:]*[-]utility[-value][/modifier][!]</c>, ví dụ
/// <c>md:hover:bg-indigo-600/90</c>, <c>-mt-2</c>, <c>w-[320px]</c>, <c>text-sm/6</c>, <c>p-4!</c>.
/// </summary>
public static partial class Tailwind
{
    static readonly ConcurrentDictionary<string, ClassList> s_cache = new(StringComparer.Ordinal);

    /// <summary>Gọi khi gặp class không hỗ trợ: (chuỗi class, token).</summary>
    public static Action<string, string>? OnUnknownClass { get; set; }

    /// <summary>
    /// Giới hạn số chuỗi class được cache. Chuỗi class ghép động (nội suy giá trị thay đổi liên tục)
    /// sẽ không làm cache phình mãi: chạm ngưỡng thì xoá hết và biên dịch lại khi cần.
    /// </summary>
    public static int MaxCachedClassLists { get; set; } = 4096;

    public static ClassList Get(string classes)
    {
        if (s_cache.TryGetValue(classes, out var cached)) return cached;
        if (s_cache.Count >= MaxCachedClassLists) s_cache.Clear();
        long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        var parsed = s_cache.GetOrAdd(classes, static c => Parse(c));
        PerfCounters.ClassMiss++;
        PerfCounters.ClassMissTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
        return parsed;
    }

    static readonly ConcurrentDictionary<(ClassList, ClassList), ClassList> s_combined = new();

    /// <summary>
    /// Ghép hai danh sách class đã biên dịch (thứ tự áp dụng: <paramref name="first"/> rồi <paramref name="second"/>).
    /// Kết quả được nhớ theo cặp nên mỗi frame chỉ tốn một lần tra từ điển.
    /// </summary>
    internal static ClassList Combine(ClassList first, ClassList second)
    {
        if (s_combined.TryGetValue((first, second), out var hit)) return hit;
        if (s_combined.Count >= MaxCachedClassLists) s_combined.Clear();
        return s_combined.GetOrAdd((first, second), static pair =>
            new ClassList([.. pair.Item1.Ops, .. pair.Item2.Ops], pair.Item1.IsGroup || pair.Item2.IsGroup, [.. pair.Item1.Unknown, .. pair.Item2.Unknown]));
    }

    /// <summary>Tính Style cuối cùng của một chuỗi class trong một ngữ cảnh/trạng thái (dùng cho test, công cụ).</summary>
    public static Style Resolve(string classes, VariantContext ctx, StateVariant state = StateVariant.None)
    {
        var s = Style.Default;
        var cl = Get(classes);
        cl.ApplyBase(ref s, ctx);
        if (state != StateVariant.None) cl.ApplyState(ref s, state, ctx);
        return s;
    }

    public static ClassList Parse(string classes)
    {
        var ops = new List<UtilityOp>();
        var unknown = new List<string>();
        bool group = false;
        foreach (var token in classes.Split((char[])[' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
        {
            int before = ops.Count;
            if (!ParseToken(token, ops, ref group))
            {
                ops.RemoveRange(before, ops.Count - before);
                unknown.Add(token);
                OnUnknownClass?.Invoke(classes, token);
            }
        }
        return new ClassList(ops.ToArray(), group, unknown.ToArray());
    }

    // ====================== Token ======================

    static bool ParseToken(string token, List<UtilityOp> ops, ref bool group)
    {
        t_dark = null;
        var t = new UtilityOp { MaxRem = float.PositiveInfinity };
        int depth = 0, start = 0;
        string utility = token;
        for (int i = 0; i < token.Length; i++)
        {
            char c = token[i];
            if (c == '[' || c == '(') depth++;
            else if (c == ']' || c == ')') depth--;
            else if (c == ':' && depth == 0)
            {
                if (!ApplyVariant(token[start..i], ref t)) return false;
                start = i + 1;
            }
        }
        utility = token[start..];

        if (utility.EndsWith('!')) { t.Important = true; utility = utility[..^1]; }
        else if (utility.StartsWith('!')) { t.Important = true; utility = utility[1..]; } // cú pháp v3
        bool neg = false;
        if (utility.StartsWith('-')) { neg = true; utility = utility[1..]; }

        if (utility == "group") { group = true; return start == 0; }
        return Utility(utility, neg, t, ops);
    }

    static bool ApplyVariant(string v, ref UtilityOp t)
    {
        switch (v)
        {
            case "hover": t.State |= StateVariant.Hover; return true;
            case "active": t.State |= StateVariant.Active; return true;
            case "focus": t.State |= StateVariant.Focus; return true;
            case "focus-visible": t.State |= StateVariant.FocusVisible; return true;
            case "group-hover": t.State |= StateVariant.GroupHover; return true;
            case "disabled": t.State |= StateVariant.Disabled; return true;
            case "dark": t.Dark = true; return true;
        }
        if (Theme.Breakpoints.TryGetValue(v, out float bp)) { t.MinRem = MathF.Max(t.MinRem, bp); return true; }
        if (v.StartsWith("max-") && Theme.Breakpoints.TryGetValue(v[4..], out bp)) { t.MaxRem = MathF.Min(t.MaxRem, bp); return true; }
        // min-[1900px]: / max-[640px]: breakpoint tuỳ ý (px quy ra rem theo 16px như media query của trình duyệt)
        if ((v.StartsWith("min-[") || v.StartsWith("max-[")) && v.EndsWith(']') && TryLength(v[5..^1], out var bl)
            && bl.Kind is LengthKind.Px or LengthKind.Rems)
        {
            float rem = bl.Kind == LengthKind.Px ? bl.Value / 16f : bl.Value;
            if (v[1] == 'i') t.MinRem = MathF.Max(t.MinRem, rem); else t.MaxRem = MathF.Min(t.MaxRem, rem);
            Theme.AddCustomBreakpoint(rem);
            return true;
        }
        return false; // group-focus, peer-*, @container, aria-*, data-*… chưa hỗ trợ
    }
}

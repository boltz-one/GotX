using System.Globalization;

/// <summary>
/// Dữ liệu "Total Visitors" của chart-area-interactive.tsx: 91 ngày 2024-04-01 … 2024-06-30, lượt desktop và mobile, chép
/// nguyên mảng <c>chartData</c> của bản gốc. Khoảng thời gian lọc như bản gốc: ngày ≥ 2024-06-30 trừ 90 / 30 / 7 ngày.
/// </summary>
public static class DashboardChartData
{
    public static readonly DateOnly First = new(2024, 4, 1), Reference = new(2024, 6, 30);

    public static readonly float[] Desktop =
    [
        222, 97, 167, 242, 373, 301, 245, 409, 59, 261, 327, 292, 342, 137, 120, 138, 446, 364, 243, 89, 137, 224, 138, 387, 215, 75, 383,
        122, 315, 454, 165, 293, 247, 385, 481, 498, 388, 149, 227, 293, 335, 197, 197, 448, 473, 338, 499, 315, 235, 177, 82, 81, 252, 294,
        201, 213, 420, 233, 78, 340, 178, 178, 470, 103, 439, 88, 294, 323, 385, 438, 155, 92, 492, 81, 426, 307, 371, 475, 107, 341, 408,
        169, 317, 480, 132, 141, 434, 448, 149, 103, 446,
    ];

    public static readonly float[] Mobile =
    [
        150, 180, 120, 260, 290, 340, 180, 320, 110, 190, 350, 210, 380, 220, 170, 190, 360, 410, 180, 150, 200, 170, 230, 290, 250, 130, 420,
        180, 240, 380, 220, 310, 190, 420, 390, 520, 300, 210, 180, 330, 270, 240, 160, 490, 380, 400, 420, 350, 180, 230, 140, 120, 290, 220,
        250, 170, 460, 190, 130, 280, 230, 200, 410, 160, 380, 140, 250, 370, 320, 480, 200, 150, 420, 130, 380, 350, 310, 520, 170, 290, 450,
        210, 270, 530, 180, 190, 380, 490, 200, 160, 400,
    ];

    /// <summary>Nhãn ngày "Apr 7" (toLocaleDateString en-US, month short + day numeric) của từng điểm.</summary>
    public static readonly string[] Labels = BuildLabels();

    /// <summary>Giá trị của ToggleGroup / Select: "90d", "30d", "7d" ("" = không nút nào bật, lọc như 90 ngày).</summary>
    public static readonly string[] RangeValues = ["90d", "30d", "7d"];
    public static readonly string[] RangeLabels = ["Last 3 months", "Last 30 days", "Last 7 days"];

    /// <summary>Chỉ số điểm đầu tiên được giữ cho một giá trị khoảng thời gian.</summary>
    public static int StartIndex(string range)
    {
        int days = range == "30d" ? 30 : range == "7d" ? 7 : 90;
        var start = Reference.AddDays(-days);
        return start.DayNumber - First.DayNumber;
    }

    static string[] BuildLabels()
    {
        var labels = new string[Desktop.Length];
        for (int i = 0; i < labels.Length; i++) labels[i] = First.AddDays(i).ToString("MMM d", CultureInfo.InvariantCulture);
        return labels;
    }
}

using GotX;

/// <summary>Danh sách cuộn bên trong một thẻ (overflow-y-auto lồng trong trang cuộn).</summary>
public sealed class InboxView : View
{
    static readonly string[] Names = ["Thu Huyền", "Minh Anh", "Quốc Bảo", "Lan Phương", "Đức Huy", "Ngọc Hà", "Tuấn Kiệt", "Mai Chi"];
    static readonly string[] Subjects =
    [
        "Báo cáo tuần: tiến độ engine giao diện", "Lịch họp thiết kế hệ thống", "Re: Đề xuất dùng D3D11 cho bản Windows",
        "Phản hồi về ô nhập liệu tiếng Việt", "Kế hoạch port sang macOS và Metal", "Cập nhật bảng màu thương hiệu",
    ];
    static readonly string[] Colors = ["bg-indigo-500", "bg-rose-500", "bg-emerald-500", "bg-amber-500", "bg-sky-500", "bg-violet-500"];
    int _selected = -1;
    readonly Row[] _rows = Enumerable.Range(0, 30).Select(i => new Row(i)).ToArray();

    /// <summary>Mỗi dòng giữ chuỗi riêng (tạo một lần) để Render không cấp phát.</summary>
    sealed class Row(int i)
    {
        public readonly int Index = i;
        public readonly string Initial = Names[i % Names.Length][..1];
        public readonly string Name = Names[i % Names.Length];
        public readonly string Subject = Subjects[i % Subjects.Length];
        public readonly string Time = $"{8 + i % 10}:{i * 7 % 60:00}";
        public readonly string AvatarClass =
            $"size-8 shrink-0 rounded-full flex items-center justify-center text-xs font-semibold text-white {Colors[i % Colors.Length]}";
        public InboxView? Owner;
    }

    void Select(int i) { _selected = i; Notify(); }

    public override Div Render(Window w)
    {
        var list = w.Div().Class("flex flex-col gap-1 h-72 overflow-y-auto -mx-2 px-2").Key("inbox-list");
        foreach (var r in _rows)
        {
            r.Owner = this;
            list.Child(
                w.Div().Class(r.Index == _selected ? RowSelected : RowNormal)
                    .OnClick(r, static (row, _) => row.Owner!.Select(row.Index))
                    .Child(w.Div().Class(r.AvatarClass).Child(r.Initial))
                    .Child(w.Div().Class("flex-1 min-w-0")
                        .Child(w.Div().Class("text-sm font-medium truncate").Child(r.Name))
                        .Child(w.Div().Class("text-xs text-slate-500 truncate").Child(r.Subject)))
                    .Child(w.Div().Class("text-xs text-slate-400").Child(r.Time)));
        }
        return w.Div().Class("flex flex-col gap-3 p-5 rounded-xl border shadow-md bg-white border-slate-200 dark:bg-slate-900 dark:border-slate-800")
            .Child(w.Div().Class("flex items-center justify-between")
                .Child(w.Div().Class("text-lg font-semibold").Child("Hộp thư (cuộn lồng)"))
                .Child(w.Div().Class("px-2 py-0.5 rounded-full text-xs font-semibold bg-rose-100 text-rose-700").Child("30")))
            .Child(list);
    }

    const string RowNormal = "flex items-center gap-3 px-2 py-2 rounded-lg cursor-pointer transition-colors duration-150 hover:bg-slate-100 dark:hover:bg-slate-800";
    const string RowSelected = "flex items-center gap-3 px-2 py-2 rounded-lg cursor-pointer bg-indigo-50 text-indigo-900 dark:bg-indigo-950 dark:text-indigo-100";
}

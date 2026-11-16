namespace GotX;

/// <summary>Vai trò ngữ nghĩa (ARIA) của một phần tử – thứ công cụ hỗ trợ (đọc màn hình…) nhìn thấy thay cho pixel.</summary>
public enum SemanticRole : byte
{
    None, Button, Checkbox, Radio, Switch, Textbox, Slider, Tab, TabList, TabPanel, Menu, MenuItem, Dialog, Alert, Status,
    Link, List, ListItem, Table, Row, Cell, Heading, Image, ProgressBar, ComboBox, Group, Text, Tooltip, Separator,
}

/// <summary>Cờ trạng thái ngữ nghĩa (aria-checked, aria-selected, aria-expanded…).</summary>
[Flags]
public enum SemanticState : ushort
{
    None = 0, Checked = 1, Selected = 2, Expanded = 4, Disabled = 8, ReadOnly = 16, Focused = 32, Invalid = 64,
    Required = 128, Busy = 256, Password = 512, Multiline = 1024, Modal = 2048, Mixed = 4096,
    // Cờ "có khai báo": phân biệt "chưa chọn / đang đóng" với "không có khái niệm đó" (AriaState tự đặt khi gặp cờ tương ứng).
    Checkable = 8192, Expandable = 16384, Selectable = 32768,
}

/// <summary>Việc công cụ hỗ trợ có thể yêu cầu trên một phần tử (xem <see cref="Window.PerformSemanticAction"/>).</summary>
public enum SemanticAction : byte { Invoke, Focus, SetValue, SetRange }

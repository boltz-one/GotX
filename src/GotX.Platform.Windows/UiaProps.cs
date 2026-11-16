using static GotX.Platform.Windows.Win32;

namespace GotX.Platform.Windows;

/// <summary>Ánh xạ ngữ nghĩa GotX → thuộc tính và pattern của UI Automation.</summary>
internal static class UiaProps
{
    public const int Name = 30005, ControlType = 30003, HasKeyboardFocus = 30008, IsKeyboardFocusable = 30009, IsEnabled = 30010,
        AutomationId = 30011, ClassName = 30012, HelpText = 30013, IsControlElement = 30016, IsContentElement = 30017, IsPassword = 30019,
        IsOffscreen = 30022, FrameworkId = 30024, LiveSetting = 30135, FullDescription = 30159, ProcessId = 30002,
        ValueValue = 30045, RangeValueValue = 30047, ExpandCollapseState = 30070, IsSelected = 30079, ToggleState = 30086;
    public const int PatternInvoke = 10000, PatternValue = 10002, PatternRangeValue = 10003, PatternExpandCollapse = 10005,
        PatternSelectionItem = 10010, PatternToggle = 10015;

    public static int ControlTypeOf(SemanticRole r) => r switch
    {
        SemanticRole.Button or SemanticRole.Switch => 50000, SemanticRole.Checkbox => 50002, SemanticRole.ComboBox => 50003,
        SemanticRole.Textbox => 50004, SemanticRole.Link => 50005, SemanticRole.Image => 50006, SemanticRole.ListItem => 50007,
        SemanticRole.List => 50008, SemanticRole.Menu => 50009, SemanticRole.MenuItem => 50011, SemanticRole.ProgressBar => 50012,
        SemanticRole.Radio => 50013, SemanticRole.Slider => 50015, SemanticRole.TabList => 50018, SemanticRole.Tab => 50019,
        SemanticRole.Text or SemanticRole.Heading or SemanticRole.Alert or SemanticRole.Status or SemanticRole.Cell => 50020,
        SemanticRole.Tooltip => 50022, SemanticRole.Group => 50026, SemanticRole.Dialog => 50032, SemanticRole.TabPanel => 50033,
        SemanticRole.Table => 50036, SemanticRole.Row => 50029, SemanticRole.Separator => 50038, _ => 50026,
    };

    public static bool HasToggle(UiaNode n) => n.Role is SemanticRole.Checkbox or SemanticRole.Switch
        || (n.Has(SemanticState.Checkable) && n.Role is not (SemanticRole.Radio or SemanticRole.Tab));
    public static bool HasInvoke(UiaNode n) => n.Role is SemanticRole.Button or SemanticRole.Link or SemanticRole.MenuItem
        && !HasToggle(n) && !n.Has(SemanticState.Expandable);
    public static bool HasExpand(UiaNode n) => n.Role == SemanticRole.ComboBox || n.Has(SemanticState.Expandable);
    public static bool HasSelection(UiaNode n) => n.Role is SemanticRole.Radio or SemanticRole.Tab || n.Has(SemanticState.Selectable);
    public static bool HasValue(UiaNode n) => n.Role is SemanticRole.Textbox or SemanticRole.ComboBox;

    public static bool IsSelectedNow(UiaNode n) => n.Role == SemanticRole.Radio ? n.Has(SemanticState.Checked) : n.Has(SemanticState.Selected);
    public static int ToggleStateOf(UiaNode n) => n.Has(SemanticState.Mixed) ? 2 : n.Has(SemanticState.Checked) ? 1 : 0;
    public static int ExpandStateOf(UiaNode n) => n.Has(SemanticState.Expanded) ? 1 : 0; // 0 = collapsed, 1 = expanded

    /// <summary>Giá trị thuộc tính; vt = 0 (VT_EMPTY) nếu không hỗ trợ (UIA dùng mặc định).</summary>
    public static VARIANT Get(UiaNode n, int id) => id switch
    {
        Name => VARIANT.Str(n.Name ?? ""),
        ControlType => VARIANT.Int(ControlTypeOf(n.Role)),
        HasKeyboardFocus => VARIANT.Bool(n.Has(SemanticState.Focused)),
        IsKeyboardFocusable => VARIANT.Bool(n.Focusable),
        IsEnabled => VARIANT.Bool(n.Enabled),
        AutomationId => VARIANT.Str(n.Key ?? ""),
        ClassName or FrameworkId => VARIANT.Str("GotX"),
        HelpText or FullDescription => n.Description is { } d ? VARIANT.Str(d) : default,
        IsControlElement or IsContentElement => VARIANT.Bool(true),
        IsPassword => VARIANT.Bool(n.Has(SemanticState.Password)),
        IsOffscreen => VARIANT.Bool(n.Visible.IsEmpty),
        LiveSetting => n.Role switch { SemanticRole.Alert => VARIANT.Int(2), SemanticRole.Status => VARIANT.Int(1), _ => default },
        ToggleState => VARIANT.Int(ToggleStateOf(n)),
        ExpandCollapseState => VARIANT.Int(ExpandStateOf(n)),
        IsSelected => VARIANT.Bool(IsSelectedNow(n)),
        ValueValue => VARIANT.Str(n.Value ?? ""),
        RangeValueValue => VARIANT.Double(n.Now),
        _ => default,
    };
}

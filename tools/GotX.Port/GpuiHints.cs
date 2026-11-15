using System.Text.RegularExpressions;

namespace GotX.Port;

/// <summary>
/// GPUI behaviour/structure call sites that cannot become a class string, with the GotX mapping. GotX side (OnClick/OnHover/OnDrag/OnKey/
/// OnScroll/Key/Focusable/OpenOverlay/Notify/Emit/App.Observe...) is from docs/components.md section 3 and the gotx-ui skill; the GPUI
/// method names are from memory of the GPUI API (not all verified against source).
/// </summary>
public static class GpuiHints
{
    static readonly Dictionary<string, string> Calls = new()
    {
        ["on_click"] = ".OnClick(state, static (s, e) => ...) (GPUI fires child AND parent on_click for one click: no stop_propagation after listeners, verified div.rs) ; cx.listener(|this, ev, window, cx| ..) -> static lambda with the view as `s`, call s.Notify() instead of cx.notify()",
        ["on_mouse_down"] = "left -> .OnClick (fires on release) or .OnDrag Phase.Start; right/middle -> .OnAuxClick (e.Button, e.Position)",
        ["on_mouse_up"] = ".OnDrag Phase.End (or .OnClick)",
        ["on_mouse_move"] = ".OnDrag Phase.Move while pressed, or .OnHover(state, static (s, inside) => ..) for enter/leave; no free mouse-move callback",
        ["on_mouse_down_out"] = "overlay option DismissOnOutsideClick (OverlayOptions)",
        ["on_mouse_up_out"] = "overlay option DismissOnOutsideClick (OverlayOptions)",
        ["on_hover"] = ".OnHover(state, static (s, inside) => ..) (inside: bool)",
        ["on_scroll_wheel"] = ".OnScroll(state, static (s, info) => ..) reports scroll position (info.Offset/Max), not wheel deltas; give the overflow-y-auto element a .Key",
        ["on_key_down"] = ".Focusable().OnKey(state, static (s, e) => bool) on the focused element",
        ["on_key_up"] = ".OnKey(..) (check whether key-up events are delivered)",
        ["on_action"] = "no action system in GotX: bind the keystroke with .OnKey on a .Focusable() element and call the same method",
        ["key_context"] = "no key contexts: drop",
        ["track_focus"] = ".Focusable().Key(\"k\"); move focus with window.FocusKey(\"k\") (FocusHandle -> string key)",
        ["tab_index"] = ".Focusable(): Tab order is draw order (no tab_index)",
        ["tab_stop"] = ".Focusable()",
        ["track_scroll"] = ".Key(\"k\") on the overflow-*-auto element; window.ScrollTo(\"k\", x, y) / GetScroll(\"k\") replace ScrollHandle",
        ["tooltip"] = "GotX.Components.Tooltip (hover delay handled by the component)",
        ["occlude"] = "no occlude: block input below with a Modal overlay or a handler on the element",
        ["block_mouse_except_scroll"] = "no equivalent: Modal overlay or a handler on the element",
        ["debug_selector"] = ".Key(\"..\") (queries in tests find elements by key)",
        ["with_animation"] = "no animation API: transition-* + state, animate-spin/pulse/ping/bounce, or App.SetInterval + Notify()",
        ["on_drag"] = "no drag-and-drop between elements: .OnDrag handles pointer gestures inside one element",
        ["on_drop"] = "no drag-and-drop between elements",
        ["drag_over"] = "no drag-and-drop between elements",
        ["context_menu"] = "GotX.Components.ContextMenu (right click via .OnAuxClick)",
        ["cursor"] = "cursor(CursorStyle::X): cursor-pointer / cursor-text / cursor-default only",
        ["when_else"] = "choose classes with a C# condition (cond ? \"..\" : \"..\")",
    };

    static readonly Dictionary<string, string> Roots = new()
    {
        ["uniform_list"] = "no virtualised lists in GotX: render rows in Render (overflow-y-auto + .Key); for big data compute the visible range from .OnScroll info.Offset yourself",
        ["list"] = "no virtualised lists in GotX (see uniform_list)",
        ["svg"] = "svg().path(\"icons/x.svg\") -> w.Path(VectorPath.Icon(\"<d of the svg file>\")).Class(\"size-4\") (static readonly VectorPath); colour = text colour",
        ["img"] = "img(..) -> w.Image(ImageSource.FromPngFile(path)) (PNG only; static readonly ImageSource) + object-cover/rounded-*",
        ["canvas"] = "canvas(prepaint, paint)/paint_quad: no custom paint API: compose Divs, VectorPath.Polyline for chart lines, or add a Core primitive",
        ["deferred"] = "deferred(..)/anchored(): w.OpenOverlay(view, new OverlayOptions { Anchor = \"key\", Placement = .., Modal = .. }); anchor element needs .Anchor(\"key\")",
        ["anchored"] = "anchored(): w.OpenOverlay with Anchor/AnchorRect + Placement",
        ["Button"] = "ui::Button::new(id, label) -> Ui.Button(w, \"label\", ButtonVariant.., ButtonSize..).OnClick(..)",
        ["IconButton"] = "ui::IconButton -> Ui.Button(w, ButtonVariant.Ghost, ButtonSize.Icon) with an icon child",
        ["Label"] = "ui::Label::new(\"..\") -> w.Div().Class(\"text-sm\").Child(\"..\")",
        ["Icon"] = "ui::Icon::new(IconName::X) -> Icon.X (core icon set) or w.Path(VectorPath.Icon(..))",
        ["Tooltip"] = "Tooltip::text(..) -> GotX.Components.Tooltip",
        ["h_flex"] = "",
        ["v_flex"] = "",
    };

    // Chains rooted at cx / window.
    static readonly Dictionary<string, string> Cx = new()
    {
        ["notify"] = "Notify()",
        ["emit"] = "Emit(new MyEvent(..)) (readonly record struct; listeners: App.Subscribe<MyEvent>(entity, e => ..))",
        ["observe"] = "App.Observe(entity, () => ..)",
        ["subscribe"] = "App.Subscribe<TEvent>(entity, e => ..)",
        ["new"] = "App.New(new X(..)) (entity creation; child entities in OnCreated)",
        ["spawn"] = "App.Post(action) to run on the UI thread; heavy work on your own Task/thread",
        ["spawn_in"] = "App.Post(action) / Task + App.Post",
        ["defer"] = "App.Post(action)",
        ["focus_handle"] = "string key: .Focusable().Key(\"k\") + window.FocusKey(\"k\")",
        ["focus"] = "window.FocusKey(\"k\") / window.Focus(view)",
        ["global"] = "static state or an App-level singleton field",
        ["set_global"] = "static state or an App-level singleton field",
        ["refresh"] = "Notify()",
        ["dispatch_action"] = "call the handler method directly (no action system)",
        ["open_window"] = "platform layer: IPlatform.OpenWindow",
        ["background_executor"] = "Task/thread; deliver results with App.Post",
        ["foreground_executor"] = "App.Post",
    };

    public static string? ForCall(string name, string arg)
    {
        if (name == "id") return arg.Length > 0 && arg[0] == '"' ? $".Key({arg})" : $".Key(<expr>) from id({Trim(arg)}): use an interpolated string for list rows, e.g. .Key($\"row-{{i}}\")";
        return Calls.TryGetValue(name, out string? h) && h.Length > 0 ? h : null;
    }

    public static string? ForRoot(string rootName) => Roots.TryGetValue(rootName, out string? h) && h.Length > 0 ? h : null;
    public static string? ForCx(string method) => Cx.TryGetValue(method, out string? h) ? h : null;
    public static bool IsElementRoot(string root) => root is "div" or "h_flex" or "v_flex" or "svg" or "img" or "canvas" or "uniform_list" or "list" or "deferred" or "anchored" or "h_group" or "v_group";

    public static string? ImpliedClasses(string root) => root switch
    {
        "h_flex" => "flex flex-row items-center",
        "v_flex" => "flex flex-col",
        _ => null,
    };

    /// <summary>File-level patterns (not part of a chain): (regex over the comment-free source, hint).</summary>
    public static readonly (Regex Rx, string Hint)[] Globals =
    [
        (new(@"impl\s+Render\s+for\s+(\w+)"), "impl Render for X -> `sealed class X : View` with `public override Div Render(Window w)`; fields = state, call Notify() after changes"),
        (new(@"impl\s+(?:gpui::)?EventEmitter<\s*(\w+)\s*>\s+for"), "EventEmitter<E> -> Emit(new E(..)) from the view; define E as a readonly record struct nested in the view"),
        (new(@"impl\s+(?:gpui::)?Focusable\s+for"), "Focusable impl -> drop; use .Focusable().Key(..) on the element"),
        (new(@"\bactions!\s*\("), "actions!(..) / KeyBinding: no action system in GotX; handle keys with .OnKey on a focused element"),
        (new(@"\bKeyBinding\b"), "KeyBinding: no key-binding table; .OnKey on a focused element"),
        (new(@"\bFocusHandle\b"), "FocusHandle -> string key + .Focusable().Key(\"k\") and window.FocusKey(\"k\")"),
        (new(@"\bEntity<\s*(\w+)\s*>"), "Entity<T> -> T (a View/entity created with App.New(new T())); keep the reference in a field"),
        (new(@"\bSharedString\b"), "SharedString -> string"),
        (new(@"\bpaint_quad\b|\bPaintQuad\b"), "paint_quad: no custom paint API (compose Divs / VectorPath)"),
        (new(@"\bScrollHandle\b|\bListState\b|\bUniformListScrollHandle\b"), "ScrollHandle/ListState -> .Key(\"k\") on the scroll element; window.ScrollTo/GetScroll; no virtualised lists"),
        (new(@"\bContext<\s*(?:Self|\w+)\s*>"), "Context<T> / cx parameter -> gone: state lives in the view, App is `App`, Notify()/Emit() are View methods"),
        (new(@"\bWindowContext\b|\bViewContext\b"), "WindowContext/ViewContext -> Window w (Render) + View methods"),
        (new(@"\bElementId\b"), "ElementId -> string passed to .Key(..)"),
        (new(@"\bHsla\b"), "Hsla -> a Tailwind colour token/arbitrary [#hex]; runtime colours via Theme.SetColor(name, light, dark)"),
    ];

    static string Trim(string s) => s.Length > 40 ? s[..37] + "..." : s;
}

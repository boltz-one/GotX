# Accessibility

Semantics are data, not pixels: nothing here is painted, and with no assistive tool attached the cost is zero.

## Model (`src/GotX.Core/Semantics`)

- `Div.Role(SemanticRole)`, `.AriaLabel`, `.AriaDescription`, `.AriaDescribedBy(key)`, `.AriaLabelledBy(key)`, `.AriaState(SemanticState, on)`, `.AriaValue`, `.AriaRange(min,max,now)`.
- Roles: button, checkbox, radio, switch, textbox, slider, tab/tablist/tabpanel, menu/menuitem, dialog, alert/status, link, list/listitem, table/row/cell, heading, image, progressbar, combobox, group, text, tooltip, separator.
- States: checked, selected, expanded, disabled, readonly, focused, invalid, required, busy (+ password, multiline, modal, mixed).
- Focusable nodes with no role are exposed implicitly (button if clickable). Textboxes read value/caret from their `TextInput`.
- Name falls back to the node's descendant text for button/checkbox/radio/switch/link/tab/menuitem/heading/cell/listitem/alert/status; textbox falls back to its placeholder.
- Components set roles, names and states by default (`GotX.Components`). Icon-only buttons you build yourself need `.AriaLabel("...")`; stateful components expose `AriaLabel` (Slider, Switch, Checkbox, Toggle, Select, Combobox, Input, Textarea).

## Zero cost when unused

`Div.Role/Aria*` are no-ops unless `ElementTree.SemanticsOn`. Only `Window.Semantics(size, scale)` turns it on: it draws one extra uncached frame and fills a reused `SemanticsTree` (nodes, lists, one `StringBuilder`). `Node` gets one `int Sem` that fits in existing padding (size unchanged, asserted by `core/semantics/zero-cost-when-unused`).

## Windows UI Automation (`src/GotX.Platform.Windows/Uia*.cs`)

Hand-written COM (vtables of `[UnmanagedCallersOnly]` functions, no reflection/interop codegen; AOT friendly). Created lazily on the first `WM_GETOBJECT(UiaRootObjectId)`.

- Provider: `IRawElementProviderSimple` + `Fragment` + `FragmentRoot`; properties Name, ControlType, IsEnabled, HasKeyboardFocus, IsKeyboardFocusable, AutomationId (= `.Key`), BoundingRectangle (screen px, DPI aware), IsOffscreen, HelpText, LiveSetting.
- Patterns: Invoke, Toggle, Value, ExpandCollapse, SelectionItem, RangeValue. Actions replay real input on the UI thread (click, focus, set text), so every handler runs as for a user.
- Events (only while `UiaClientsAreListening()`): focus changed, property changed (name, enabled, toggle, expand, selection, value, range), live region changed (new/changed alert/status). Snapshot rebuilds are throttled (~120 ms) and compared with the previous one.
- UIA calls providers from its own threads: they read an immutable `UiaSnapshot` copy, never the reused `SemanticsTree`.

## Tests

- `core/semantics/*` (tests/GotX.Tests/Core/Semantics*Tests.cs): roles/names/states per component, actions, cost.
- Lint rule `unnamed-control` (warning, `Suppress` supported): a Tab-reachable control with no accessible name.
- `core/semantics/uia-real-window`: real Win32 window + `IUIAutomation` client, asserts tree, properties, rects, patterns and events. Skips with an explicit note when no desktop/UIA is available.

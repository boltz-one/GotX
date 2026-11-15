# gotx-port tooling

Run from the repo root. `dotnet` is not on PATH on this machine: use `"C:\Program Files\dotnet\dotnet.exe"`. Outputs below are real runs.
`GotX.Port` = `dotnet run --project tools/GotX.Port --` (build once, then call the exe under the artifacts dir to skip the build step).

## GotX.Port commands

### `classes <file|dir|-> [--json]`: Tailwind audit
Every class string (HTML `class`, JSX `className`, `cn/cva/clsx/twMerge`, Svelte/Vue `class`/`:class`, C# `.Class("..")`) is split into
tokens and each token goes through `Tailwind.Parse`. Unsupported tokens are grouped by advice, with count and first line. Exit code is always 0.
```
$ GotX.Port classes mini.html
classes: 1 file(s), 2 class strings, 10 tokens (10 unique), 4 unique unsupported
== mini.html  (2 strings, 10 tokens, 4 unsupported)
  -> no auto margins: center with justify-center / items-center on the parent; push apart with justify-between or a flex-1 spacer Div before the item
     mx-auto x1 :1
  -> no z-index: paint order is tree order (later siblings and absolute children on top); true layering = w.OpenOverlay(...)
     z-10 x1 :1
  -> peer-*: no sibling state. Compute the state in C# and pick classes (_checked ? ".." : "..")
     peer-checked:p-2 x1 :1
  -> visually-hidden text: omit the element (or use `hidden`)
     sr-only x1 :2
  ~ w-fit x1 :1  (approximated) fit/min/max-content behave like auto
variants (state/theme): dark=1 hover=1
variants (other): peer-checked=1
```
`~` lines are accepted by the parser but approximated, read off the Core mapping: `items-baseline` (= start), `inline*` (= block/flex/grid), `space-x/y` (= gap), `w/h/min/max` with `fit|min|max` (= auto), `overflow-clip` / `overflow-scroll`, `text-pretty`. `=` lines are valid Tailwind that GotX ignores by design (`antialiased`, `transform`, `will-change-*`); they are not counted as unsupported. Variants are classified by the parser itself (`variants (UNSUPPORTED)` = aria-/data-/peer-...). `self-baseline` and `backdrop-*` are rejected by the parser, so they are unsupported, not approximated. In C# files the `const string` / `static readonly string[]` / `string X =>` initialisers are scanned too (a literal counts when at least 60 % of its tokens are recognisable Tailwind); example: the `PortedView.cs.txt` template yields 10 class strings (2 via `.Class(..)`, the rest from its const tables). On a real page
(shadcn dashboard-01, 5108 tokens) it reports 235 unique unsupported tokens in well under a second.

### `html <file|-> [--ns Name] [--view Class] [-o out.cs]`: HTML to a GotX C# skeleton
```
$ GotX.Port html mini.html --ns Demo --view MiniView
...
public sealed class MiniView : View
{
    public override Div Render(Window w) =>
        w.Div().Class("mx-auto flex gap-2 p-4 hover:bg-muted dark:bg-card z-10 peer-checked:p-2 w-fit") // UNSUPPORTED: mx-auto, z-10, peer-checked:p-2
            .Child(w.Div().Class("sr-only").Child("x") // UNSUPPORTED: sr-only
            );
}
```
Larger golden example: `tests/GotX.Tests/Port/fixtures/card.html` -> `card.expected.cs.txt` (inputs, svg to `VectorPath.Icon`, img, button stubs,
inline-text TODOs, UA-default classes). A 259 KB / 88-line `--dump-dom` of the shadcn dashboard converts to 1125 lines in 1.6 s.
The skeleton compiles only after you resolve the TODOs (`ImageSource` fields are `null!` placeholders, inputs become empty Divs plus a comment).

### `gpui <file.rs|--sample|--sample-source> [--json]`: GPUI chains to Tailwind
```
$ GotX.Port gpui --sample
gpui: <sample>: 8 chains (4 with classes), 4 file-level hints
line 16: v_flex() -> "flex flex-col size-full gap-2 p-4 bg-[#1e1e1e]"
    note: v_flex() is Zed's ui helper = div() + flex flex-col
    hint: .id("counter"): .Key("counter")
line 23: div() -> "flex items-center justify-between px-3 py-1.5 w-1/2 h-[32px] rounded-md border border-border bg-card text-[#4073bf]/80 text-sm font-bold cursor-pointer overflow-y-auto hover:bg-[#ffffff]/10 hover:text-white"
    when self.active: "bg-accent opacity-50"
    hint: .on_click(cx.listener(..)): .OnClick(state, static (s, e) => ...) ; ...
    hint: .track_focus(&self.focus): .Focusable().Key("k"); move focus with window.FocusKey("k") ...
line 51: uniform_list(..) -> "flex-1"
    hint: uniform_list(..): no virtualised lists in GotX: ...
line 52: div() -> "mx-auto"
    UNSUPPORTED mx-auto -> no auto margins: ...
line 13: hint: impl Render for X -> `sealed class X : View` with `public override Div Render(Window w)`; ...
```
(abridged; run `--sample-source` to see the input.) Also run on real Zed files (`crates/gpui/examples/hello_world.rs`, `input.rs`, `button_like.rs`).

### `diff <ref.png> <gotx.png> [out.png]`
```
$ GotX.Port diff ported-800-light.png ported-800-dark.png diff.png
ref 800x300  gotx 800x300  same size
differing: 100.00% @tol0 (239992 px), 99.80% @tol24 (239514 px), max channel delta 245
top regions (tol24, x,y WxH, px):
  1. [0,0 800x300] 239514 px
diff image: diff.png (dimmed reference; magenta channel delta > 24, amber 1-24, blue only in one image)
```
Different sizes: only the overlapping top-left corner is compared; the mismatch has its own lines and is **not** folded into the percentages:
```
$ GotX.Port diff ported-800-light.png align-600-light.png
ref 800x300  gotx 600x120  SIZE MISMATCH (gotx is -200 wide, -180 tall)
compared: ONLY the overlapping top-left 600x120 corner; the extra area (168000 px, blue in the diff image) is NOT in the percentages below
differing (overlap only): 5.27% @tol0 (3791 px), 3.31% @tol24 (2386 px), max channel delta 245
```

## Scripts (Node 26, headless Edge, no npm packages)

### `capture-html.mjs <url|file.html> --out <dir> [--widths 1440,1024,640] [--themes light,dark] [--height 900] [--full] [--selector css] [--name n]`
```
$ node .claude/skills/gotx-port/scripts/capture-html.mjs align.html --out cap2 --widths 600 --themes light --selector "#box" --name align
align-600-light ... 600x108, 4 elements
```
Writes `<name>-<w>-<theme>.png` + `.json` (layout map of every visible element: tag, id, data-slot, text <= 60, rect, font/colour/border/
radius/padding/gap/display/flex/box-shadow/text-align). `--selector` clips the screenshot and makes rects relative to that element (use it when the
GotX view is a component, not a page). Local files load via `file://`; pages with the Tailwind CDN script wait for the generated CSS (that
script needs network). Theme: `localStorage.theme`, `prefers-color-scheme` and the `dark` class are all set (tools/ShadcnRef/prep.mjs).

### `compare-layout.mjs <ref.json> <gotx.json> [--top 15] [--tol 2] [--position-only]`
GotX side comes from `templates/ShotProgram.cs.txt` (`<name>-<w>-<theme>.tree.json` = `TreeDump.ToJson(new UiTree(snapshot))`; in a test:
`File.WriteAllText("gotx.json", TreeDump.ToJson(t.Tree))`) or from samples/Shadcn LayoutMapFile (`{elements}` format). Both are accepted.
Text is compared by **position only**: GotX text nodes carry the container box, so the glyph start x is derived from the container with the
browser text-align (left / centre / right; from the GotX item `textAlign` if present, else from where the browser run sits in its element),
y from the first line centre; width is never compared for text. Boxes keep size (`--position-only` drops w/h). Ids pair with keys when
`key == id`, `key == <tag>-<id>`, or the key ends with the id.
```
$ node compare-layout.mjs cap2/align-600-light.json shots2/align-600-light.tree.json
ref 600x108 (4 items)  gotx 600x120 (10 items)
matched 4: 3/3 texts, 1/1 boxes, 0 by key
delta px: median 0.5  p90 0.5  max 0.5  within 2px: 4/4
worst 4 (kind  delta  text  ref -> gotx; text: glyph x / line centre y, box: [x,y wxh]):
  text     0.5  "left aligned text"  ref x=16 y=16 -> d=(0,0.5) align=left
  text     0.5  "right aligned text"  ref x=474.8 y=44 -> d=(0,0.5) align=right
  text     0.5  "centered text"  ref x=258.3 y=72 -> d=(0,0.5) align=center
  box        0  "left aligned text right aligned text"  [0,0 600x108] -> [0,0 600x108]  d=(0,0,0,0)
```
(Before this change the right-aligned line read dx -263 and the left one dw 810 against the stretched container.)

### `measure.ps1`: build, shot, diff, layout in one go
```
pwsh .claude/skills/gotx-port/scripts/measure.ps1 -Project <shot.csproj> -Ref <capture prefix> [-View Type] [-Width 1440] [-Height 900]
     [-Theme light|dark] [-Full] [-Fonts dir] [-Out measure-out] [-ArtifactsPath dir] [-Top 15] [-PositionOnly]
```
`-Ref` = prefix of a `capture-html.mjs` result (`cap2\align-600-light` means `.png` + `.json`); `-Project` is a project built from `ShotProgram.cs.txt`.
```
$ pwsh measure.ps1 -Project Shot.csproj -Ref cap2\align-600-light -View AlignDemo -Width 600 -Height 120 -Out measure-out -Top 4
== build Shot.csproj
    0 Warning(s)
    0 Error(s)
== shot
...\measure-out\gotx-600-light.png  600x120
== diff
ref 600x108  gotx 600x120  SIZE MISMATCH (gotx is 0 wide, +12 tall)
compared: ONLY the overlapping top-left 600x108 corner; the extra area (7200 px, blue in the diff image) is NOT in the percentages below
differing (overlap only): 3.11% @tol0 (2014 px), 2.87% @tol24 (1862 px), max channel delta 245
top regions (tol24, x,y WxH, px):
  1. [475,49 109x15] 737 px
== layout
ref 600x108 (4 items)  gotx 600x120 (10 items)
matched 4: 3/3 texts, 1/1 boxes, 0 by key
delta px: median 0.5  p90 0.5  max 0.5  within 2px: 4/4
```
Exit codes of the shot step: 1 = unknown Tailwind classes (listed, images still written, the script warns), 2 = failure (stops).

## Templates (`../templates/*.txt`, compile-checked against the repo with TreatWarningsAsErrors)
- `PortedView.cs.txt`: view skeleton (state, static handlers, keys, constant class sets, `Ui.Button`, `VectorPath`, an `Input` entity keyed on
  `w.View(_name)` and a `BoxClassOverride` with constant `flex flex-col justify-center` box classes).
- `PortTests.cs.txt`: lint at 2 widths x light/dark (`UiDoctor`), behaviour asserts (`t.Click("name"); t.Type(..)` through the key on `w.View(entry)`, value read from
  the entity), layout asserts, `Snapshot`. Verified: 4 tests pass.
- `ShotProgram.cs.txt`: headless PNG + tree JSON at a size/theme; `--view <Type>` (reflection, or `Hooks.Create`), `--fonts <dir>`, `--full`; a `test`
  command runs the GotX.Test suite from `Hooks.RegisterTests` (`#if PORTTESTS`); theme setup is the `Hooks.InstallTheme()` hook (not hard-coded);
  Segoe UI Semibold is also registered at `Medium` (Chrome on Windows draws font-medium with it); unknown classes on stderr (exit 1).
  ```
  $ Shot <out-dir> --view AlignDemo --width 600 --height 120 --theme light --name align
  ...\shots2\align-600-light.png  600x120
  $ Shot test --out out
  GotX.Tests: 4 passed, 0 failed, 3 notes (--verbose) in 0.67 s
  ```
To use: add the `.txt` files to a project that references `GotX.Components` + `GotX.Test` (the check project used
`<Compile Include="..\PortedView.cs.txt" Link="PortedView.cs" />` and `<DefineConstants>PORTTESTS</DefineConstants>`).

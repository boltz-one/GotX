# Stress – `tools/GotX.Stress`

Headless (no windows), deterministic stress scenarios: software renderer, virtual clock, Geist fonts (+ system fallbacks for emoji/RTL if present), fixed RNG seeds.
Complements `GotX.Perf` (scripted page benches) and `GotX.Footprint` (real-window process meter): this one pushes size, churn and pathological input.

```
dotnet run -c Release --project tools/GotX.Stress                        # all scenarios, table + ranked hot spots
dotnet run -c Release --project tools/GotX.Stress -- --only churn,text   # subset
dotnet run -c Release --project tools/GotX.Stress -- --check             # compare with stress-baseline.json (exit 1 on regression)
dotnet run -c Release --project tools/GotX.Stress -- --update-baseline   # median of 3 runs, merged into stress-baseline.json
dotnet run -c Release --project tools/GotX.Stress -- --soak 10           # + soak: leak slope of ws / private / GC heap (off by default)
```

Flags: `--only a,b` `--repeat N` `--baseline file` `--out results.json` `--list`. Always use Release (Debug prints a warning; numbers are meaningless).

## Scenarios

| name | load |
|---|---|
| `tree-10k` / `tree-100k` | 100 / 1000 child views of ~100 nodes in a scrolling column: hover zigzag, 150 wheel notches down, 50 up |
| `vlist-100k` / `vlist-1m` | `VirtualList` 100k / 1,000,000 rows (fixed `h-[640px]`: with no definite height it renders every row): wheel, click, PageDown, Down, End, Home |
| `churn` | 400 leaf views, 1-8 random `Notify` per frame, a burst of all 400 every 100 frames |
| `input` | 4000 seeded mouse move/down/up, wheel, key, text events on a mixed page (one frame per event) |
| `resize-dpi` | sinusoidal size sweep ~320-1600 wide, DPI 1 / 1.25 / 1.5 / 2 every 25 frames |
| `text` | 20k-char wrapped line, 5000-char unbreakable word, emoji ZWJ/flags, Arabic, Hebrew, Vietnamese precomposed/decomposed/stacked marks; new width every frame; typing into a 400-line multiline input |
| `multiwindow` | 4 windows on one `App` (different DPI), state change + input + draw of all windows per round |
| `gpu-loss` | skipped: the device-loss inject seam is `internal` to `GotX.Platform.Windows` (see below) |
| `--soak N` | 3 windows driven unthrottled for N minutes, roots replaced every 1500 frames, forced GC + sample every ~N/24 min |

## Metrics

Per frame (input dispatch + timers + `Draw` + `Flush` + software raster, wall clock): p50 / p95 / p99 / max ms, mean layout / paint / raster / other ms
(layout+paint from `Window.LastFrame`, raster timed by the rig, other = remainder), allocated B/frame, gen0/1/2 collections, working-set and private-bytes growth
(end minus start; start after a forced GC), GC heap growth, rendered / replayed views per frame, nodes, primitives.

`--check` fails a metric when `value > baseline * rel + abs` (`Harness/Baseline.cs`, generous for wall-clock tails). `stress-baseline.json` is machine-specific; regenerate on a quiet machine.
Soak results are never gated; read the printed slope (least-squares after the first quarter of samples).

## Baseline (this machine, Release, 16 cpu, median of 3; tree-100k once)

| scenario | p50 | p95 | p99 | max ms | B/frame | views/frame |
|---|---|---|---|---|---|---|
| tree-10k (9k nodes) | 3.3 | 3.8 | 4.8 | 8.3 | 1.6k | 1.5 |
| tree-100k (90k nodes) | 16.3 | 21.1 | 48.1 | 48.1 | 45k | 3.6 |
| vlist-100k / 1m | 2.9 / 2.8 | 3.8 / 3.0 | 4.3 / 3.0 | 4.3 / 3.0 | ~10k / 0.6k | 1.4 |
| churn (400 views) | 4.5 | 5.4 | 7.7 | 12.4 | 1.7k | 9.2 |
| input (4000 events) | 6.2 | 7.9 | 10.1 | 28.9 | 48k | 1.0 |
| resize-dpi | 4.9 | 12.6 | 15.0 | 19.5 | 1.05M | 25 |
| text | 6.7 | 12.5 | 14.3 | 17.3 | 104k | 0.9 |
| multiwindow (4 win-frames) | 3.9 | 5.6 | 6.6 | 7.9 | 14k | 2.1 |

Before the Core view-culling change landed (earlier run, same tool): tree-10k p50 6.5 ms / 44 views per frame, tree-100k p50 442 ms / 739 views per frame, 1.4 MB/frame (every scroll frame re-rendered nearly every view).
Software raster is a ~2.5-5 ms floor per 1280x800 frame in most scenarios (full-frame repaint, no damage rect), so it tops most hot-spot rows.
Soak (churn+input+text, 5 min): working set +0.21 MB/min, private +0.06 MB/min, GC heap +275 KB/min (weak creep, needs a longer run), text fallback pages flat.

## Known gaps (need Core / platform changes)

- GPU device loss: add `<InternalsVisibleTo Include="GotX.Stress" />` to `GotX.Platform.Windows` (as for `GotX.Perf`), then drive `D3D11Renderer.TryCreateOffscreen(warp: true)` with `InjectHr/InjectCount` under load.
- Arena sizes (scene / tree / layout buffers) have no public counters; soak uses GC heap after a full collection as proxy.

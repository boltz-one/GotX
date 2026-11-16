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
Env: `STRESS_FULL_RASTER=1` whole-frame raster every frame (A/B against the default damage raster), `STRESS_ALLOC=1` per-scenario allocation sample by type, `STRESS_ALLOC_TRACE=<bytes>` print every frame allocating more than that, `STRESS_TRACE=1` slow frames.

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
| `gpu-loss` | mixed page on a D3D11/WARP offscreen renderer; DEVICE_REMOVED / RESET / HUNG / DRIVER_INTERNAL_ERROR injected every 25 frames (internal `InjectHr` seam, visible via `InternalsVisibleTo`); every loss must recover (`lossErrors` = 0, gated) |
| `--soak N` | 3 windows driven unthrottled for N minutes, roots replaced every 1500 frames, forced GC + sample every ~N/24 min |

## Metrics

Per frame (input dispatch + timers + `Draw` + `Flush` + software raster, wall clock): p50 / p95 / p99 / max ms, mean layout / paint / raster / other ms
(layout+paint from `Window.LastFrame`, raster timed by the rig, other = remainder), allocated B/frame, gen0/1/2 collections, working-set and private-bytes growth
(end minus start; start after a forced GC), GC heap growth, rendered / replayed views per frame, nodes, primitives.

Software raster goes through the same path as `Win32Window`: `SceneDamage` + `SoftwareRenderer.RenderDamaged` into a retained canvas (frames with nothing changed cost 0). Extra columns: `cover` = mean fraction of pixels re-rastered, `full%` / `empty%` = frames whose damage was full-frame / empty. Scenarios that scroll or resize repaint most of the frame by nature (`cover` ~0.8-1.0).

`--check` fails a metric when `value > baseline * rel + abs` (`Harness/Baseline.cs`, generous for wall-clock tails). `stress-baseline.json` is machine-specific; regenerate on a quiet machine.
Soak results are never gated; read the printed slope (least-squares after the first quarter of samples).

## Baseline (this machine, Release, 16 cpu, median of 3; tree-100k once)

Before = whole-frame software raster every frame (the old rig) on the unmodified renderer; after = damage raster + renderer fast paths (see `docs/performance.md` §7).

| scenario | p50 ms | raster ms/frame | B/frame | gen2 | ws / private growth MB | views/frame |
|---|---|---|---|---|---|---|
| tree-10k (9k nodes) | 3.3 -> 2.0 | 2.59 -> 0.95 | 3.7k -> 3.7k | 0 -> 0 | 6.1 / 0.9 -> 7 / 1 | 2.8 |
| tree-100k (90k nodes) | 21.5 -> 17-19 | 11.2 -> 9.0 | 50k -> 50k | 0 -> 0 | 60 / 0 -> 67 / 0 | 5.9 |
| vlist-100k / 1m | 2.9 / 3.0 -> 0.73 / 0.71 | 2.74 / 2.77 -> 0.40 / 0.37 | 10k / 0.6k -> 11k / 1.4k | 0 -> 0 | 0 -> 0 | 1.4 |
| churn (400 views) | 4.6 -> 0.86 | 4.15 -> 0.60 | 1.8k -> 2.5k | 0 -> 0 | 2.2 / 0.4 -> 1.3 / 0.8 | 9.2 |
| input (4000 events) | 6.4 -> 5.2 | 4.70 -> 3.22 | 48k -> 48k | 0 -> 0 | 3.4 / 3.4 -> 3 / 3 | 1.0 |
| resize-dpi | 5.8 -> 3.8 | 4.72 -> 2.4 | 1.08M -> 180k | 7 -> 0 | 104 / 105 -> 29 / 32 | 14.8 |
| text | 5.8-7 -> 6.8 | 1.84 -> 1.7 | 112k -> 30k | 0 -> 0 | 29 / 17.6 -> 12 / 0.1 | 0.9 |
| multiwindow (4 win-frames) | 4.0 -> 0.50 | 2.98 -> 0.68 | 15k -> 19k | 0 -> 0 | 0 -> 0 | 2.2 |
| gpu-loss (WARP) | new: 6.8 | GPU 5.3 | 54k | 2 | 5 / 3 | 0.9 |

B/frame differences up to ~1.5 KB/frame in short scenarios are the one-time growth of the damage / hash arrays; tree-100k's 29 measured frames include a 10 MB growth that is not repeated (identical to the old 50k with arrays pre-sized).
Soak (30 min, 3 windows, roots replaced every 1500 frames): ws +0.39 MB/min, private +0.57 MB/min, GC heap +367 KB/min by the fit but a staircase (73 -> 76 -> 78 -> 83 MB at 5 / 10.5 / 20 min) that is flat after minute 20 (heap -1 KB/min, private 0.00 MB/min); details in `docs/performance.md` §7. Earlier 5 min run (churn+input+text): working set +0.21 MB/min, private +0.06 MB/min, GC heap +275 KB/min.

## Known gaps

- Arena sizes (scene / tree / layout buffers) have no public counters; soak uses GC heap after a full collection as proxy.
- Raster of scroll-heavy pages is near full-frame by nature (cover ~0.8): a scrolled quad / glyph still has to be blended; the remaining lever is cheaper per-pixel blending (glyph mask blit) or a scroll-blit (copy the retained canvas up/down) in the renderer.
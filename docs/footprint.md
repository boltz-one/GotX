# Footprint - CPU / RAM / GPU budgets

Goal: GPUI-level capability with a very small footprint. `tools/GotX.Footprint` measures it on a real Win32 window so work is judged by numbers
(sibling of `tools/GotX.Perf`, which measures frame time headless). Measurement-only: it references the samples, changes nothing in `src/`.

## 1. Tool

```
dotnet build tools/GotX.Footprint -c Release
dotnet run -c Release --project tools/GotX.Footprint -- run [--scenarios hello,home,input,vlist,spin,dialog,resize] [--renderer d3d|software|both]
        [--repeat 3] [--seconds 10] [--out dir] [--shots] [--check] [--update-baseline]
        [--mode aot --host <published GotX.Footprint.exe> --exe-size hello=<Hello.exe>,home=<Shadcn.exe>]
dotnet run -c Release --project tools/GotX.Footprint -- baseline out/footprint-<mode>.json     # merge a finished run into the baseline
dotnet publish tools/GotX.Footprint -c Release -r win-x64 -p:PublishAot=true   # AOT host (works here: VS BuildTools present, 5.1 MB exe)
```

- `run` starts one fresh child (`host <scenario>`) per run: a real window, D3D11 or `WindowsPlatform(preferGpu:false)`. 3 s warm-up (input script runs), then 10 s measured.
  Input: PostMessage mouse/wheel/key + `SetWindowPos` (same helpers as GotX.Perf). Process metrics are read from outside (TotalProcessorTime / wall / cores,
  WorkingSet64 and PrivateMemorySize64 sampled every 250 ms: start/peak/end, thread and handle counts, `GetGuiResources`). Inside the child, one stdin thread answers
  counter requests: frames (window asked to draw) / presented (scene handed to renderer), GC gen0/1/2, `GC.GetTotalAllocatedBytes`, heap, DXGI `QueryVideoMemoryInfo`
  (per-process, so it must run in the child), first-present time since process start. Leak check: `GC.GetTotalMemory(true)` before/after + private bytes growth.
- Scenarios: `hello` idle label; `home` shadcn home (SiteShell) idle; `input` focused blinking `Input`; `vlist` 100k-row `VirtualList` wheel-scrolled; `spin` `Ui.Spinner` (animate-spin);
  `dialog` open (click) + Escape x200 (`cycles` metric confirms 200 opens); `resize` shadcn home, 200 `SetWindowPos` steps (900-1500 x 600-900).
- Output: `tools/GotX.Footprint/out/` (gitignored by the existing `out/` rule): `footprint-<mode>.json` (medians + every run) and `.md`. `--shots` saves one PNG per scenario (PrintWindow).
- `footprint-baseline.json` (committed, median of 3, this machine, JIT + AOT entries keyed `scenario/renderer/mode`). `--check` exits 1 when `value > baseline * rel + abs`
  (`Baseline.Tolerance`: CPU x2 + 1 pt, startup x1.8 + 150 ms, alloc/frame x1.3 + 256 B, WS/private x1.25 + 8 MB, threads +3, handles x1.2 + 20, GDI/USER +5, exe x1.05).
  Re-record after changing machine or after an intended win (`--update-baseline`).

## 2. Baseline (median of 3, Release, 16 logical cores, Windows 11, .NET 11, machine without a real GPU)

Columns: CPU = process CPU / wall / cores. WS = working set start/peak/end. Leak = live heap growth after full GC over the run (KB) / private-bytes growth (MB).
`B/frame` = allocated bytes in window / frames drawn. GPU MB reads 0.0 because the adapter is Microsoft Basic Display Adapter (see limits).
frames == presented in every scenario: each drawn frame is presented, nothing is skipped.

### JIT (default `dotnet`, tiered)

#### d3d (jit)

| scenario | CPU % | WS start/peak/end MB | private end MB | heap MB | gen0/1/2 | B/frame | KB/s | frames / presented | threads | handles | GDI/USER | first frame ms | GPU MB | leak heap KB / priv MB |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| hello | 0.00 | 53.4 / 53.5 / 53.5 | 18.6 | 3.4 | 0/0/0 | 0 | 0.1 | 0 / 0 | 20 | 375 | 9/16 | 310 | 0.0 | 2 / 0.0 |
| home | 0.00 | 88.3 / 88.4 / 88.4 | 47.0 | 11.4 | 0/0/0 | 0 | 0.1 | 0 / 0 | 22 | 355 | 9/14 | 525 | 0.0 | 2 / 0.0 |
| input | 0.20 | 63.8 / 64.1 / 60.1 | 20.3 | 3.7 | 0/0/0 | 99 | 0.2 | 19 / 19 | 28 | 369 | 9/16 | 307 | 0.0 | 2 / -4.1 |
| vlist | 0.08 | 68.5 / 71.3 / 71.3 | 29.7 | 7.6 | 0/0/0 | 3932 | 127.1 | 322 / 322 | 25 | 388 | 10/17 | 348 | 0.0 | 661 / 1.9 |
| spin | 0.04 | 63.7 / 64.5 / 64.5 | 24.6 | 2.5 | 0/0/0 | 3 | 0.1 | 321 / 321 | 26 | 384 | 9/17 | 287 | 0.0 | 2 / 0.3 |
| dialog | 0.24 | 65.6 / 67.3 / 67.3 | 27.3 | 3.9 | 0/0/0 | 290 | 9.1 | 432 / 432 | 25 | 366 | 10/14 | 313 | 0.0 | 27 / 2.0 |
| resize | 15.75 | 92.3 / 110.3 / 103.6 | 59.7 | 13.0 | 0/0/0 | 3001 | 78.4 | 299 / 299 | 26 | 383 | 9/16 | 539 | 0.0 | 428 / 10.4 |

#### software (jit)

| scenario | CPU % | WS start/peak/end MB | private end MB | heap MB | gen0/1/2 | B/frame | KB/s | frames / presented | threads | handles | GDI/USER | first frame ms | GPU MB | leak heap KB / priv MB |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| hello | 0.00 | 47.4 / 47.5 / 47.4 | 17.1 | 5.9 | 0/0/0 | 0 | 0.1 | 0 / 0 | 9 | 315 | 9/14 | 281 | n/a | 2 / 0.0 |
| home | 1.20 | 91.0 / 91.3 / 91.0 | 54.5 | 16.6 | 0/0/0 | 1584 | 6.2 | 21 / 22 | 13 | 332 | 9/15 | 648 | n/a | 16 / 0.0 |
| input | 0.13 | 53.9 / 54.1 / 50.5 | 17.7 | 6.2 | 0/0/0 | 100 | 0.2 | 19 / 19 | 13 | 331 | 9/16 | 294 | n/a | 2 / -3.9 |
| vlist | 2.66 | 57.5 / 58.0 / 58.0 | 24.4 | 10.1 | 0/0/0 | 1594 | 133.2 | 856 / 856 | 9 | 315 | 9/15 | 296 | n/a | 668 / 0.6 |
| spin | 0.90 | 51.2 / 51.2 / 51.2 | 20.3 | 5.0 | 0/0/0 | 1 | 0.1 | 615 / 615 | 9 | 314 | 9/15 | 247 | n/a | 2 / -0.0 |
| dialog | 1.80 | 55.5 / 56.4 / 56.4 | 22.6 | 6.3 | 0/0/0 | 212 | 9.3 | 600 / 600 | 9 | 315 | 9/14 | 278 | n/a | 28 / -0.0 |
| resize | 5.63 | 86.5 / 92.5 / 89.2 | 57.3 | 17.5 | 0/0/0 | 4022 | 66.6 | 222 / 222 | 10 | 318 | 10/14 | 630 | n/a | 428 / 0.6 |

### Native AOT (footprint host; exe size is the published sample: Hello 2.7 MB, Shadcn 4.3 MB, host with Shadcn+Components 4.9 MB)

#### d3d (aot)

| scenario | CPU % | WS start/peak/end MB | private end MB | heap MB | gen0/1/2 | B/frame | KB/s | frames / presented | threads | handles | GDI/USER | first frame ms | GPU MB | leak heap KB / priv MB |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| hello | 0.00 | 33.3 / 33.3 / 33.3 | 13.8 | 3.3 | 0/0/0 | 0 | 0.1 | 0 / 0 | 17 | 283 | 9/14 | 86 | 0.0 | 1 / 0.0 |
| home | 0.00 | 66.2 / 66.2 / 66.2 | 42.4 | 11.2 | 0/0/0 | 0 | 0.1 | 0 / 0 | 23 | 284 | 9/14 | 188 | 0.0 | 1 / 0.0 |
| input | 0.04 | 37.4 / 37.8 / 37.7 | 15.4 | 3.6 | 0/0/0 | 424 | 0.8 | 19 / 19 | 22 | 341 | 9/27 | 164 | 0.0 | 7 / 0.1 |
| vlist | 20.20 | 41.4 / 58.1 / 57.6 | 35.0 | 7.5 | 0/0/0 | 1616 | 131.8 | 837 / 837 | 22 | 410 | 9/15 | 86 | 0.0 | 663 / 13.3 |
| spin | 4.41 | 36.7 / 36.9 / 36.9 | 14.5 | 2.4 | 0/0/0 | 1 | 0.1 | 632 / 632 | 23 | 286 | 9/15 | 76 | 0.0 | 1 / 0.8 |
| dialog | 0.45 | 38.2 / 39.3 / 39.3 | 17.0 | 3.7 | 0/0/0 | 292 | 8.7 | 427 / 426 | 23 | 293 | 9/14 | 93 | 0.0 | 27 / 1.8 |
| resize | 13.00 | 66.9 / 72.2 / 66.4 | 44.3 | 12.9 | 0/0/0 | 3123 | 72.6 | 287 / 287 | 23 | 289 | 9/14 | 144 | 0.0 | 427 / 0.0 |

#### software (aot)

| scenario | CPU % | WS start/peak/end MB | private end MB | heap MB | gen0/1/2 | B/frame | KB/s | frames / presented | threads | handles | GDI/USER | first frame ms | GPU MB | leak heap KB / priv MB |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| hello | 0.00 | 24.8 / 24.8 / 24.8 | 10.3 | 5.7 | 0/0/0 | 0 | 0.1 | 0 / 0 | 6 | 224 | 9/12 | 132 | n/a | 3 / 0.0 |
| home | 0.00 | 56.0 / 56.0 / 56.0 | 40.1 | 15.7 | 0/0/0 | 0 | 0.1 | 0 / 0 | 7 | 225 | 9/12 | 197 | n/a | 1 / 0.0 |
| input | 0.02 | 25.4 / 25.4 / 25.4 | 11.0 | 6.0 | 0/0/0 | 91 | 0.2 | 19 / 19 | 7 | 229 | 9/13 | 72 | n/a | 5 / 0.0 |
| vlist | 3.04 | 29.0 / 29.4 / 29.3 | 14.3 | 9.9 | 0/0/0 | 1651 | 130.1 | 808 / 808 | 6 | 224 | 9/13 | 73 | n/a | 654 / 0.7 |
| spin | 0.72 | 24.4 / 27.2 / 27.2 | 10.0 | 4.8 | 0/0/0 | 1 | 0.1 | 607 / 608 | 13 | 272 | 9/15 | 61 | n/a | 1 / 0.6 |
| dialog | 1.97 | 26.7 / 26.8 / 26.8 | 12.1 | 6.2 | 0/0/0 | 209 | 9.2 | 600 / 600 | 6 | 224 | 9/12 | 61 | n/a | 27 / 0.0 |
| resize | 5.51 | 52.1 / 56.9 / 52.4 | 41.7 | 17.3 | 0/0/0 | 3899 | 55.6 | 229 / 229 | 7 | 225 | 10/12 | 205 | n/a | 427 / 0.0 |

## 3. Proposed budgets

| Metric | Budget | Now | Status |
|---|---|---|---|
| Idle CPU (hello, home, 10 s) | < 0.5 % | 0.00 % (home software JIT 1.2 % in the median, see gap 7) | met |
| Blinking input CPU | < 0.5 % | 0.04-0.20 % | met |
| Hello working set | < 40 MB | AOT d3d 33 / software 25; JIT d3d 53 / software 47 | met with AOT only |
| Shadcn home working set | < 80 MB | AOT 56-66; JIT 88-91 | met with AOT only |
| Steady-state alloc | 0 B/frame | hello/home 0, spin 1-3, input 91-530, dialog 209-292, vlist 1.6-3.9 KB, resize 3.1-4.0 KB | gap |
| GC (10 s, any scenario) | gen2 = 0 | 0/0/0 everywhere | met (budget is large, not zero-alloc) |
| Leak, 200 dialog cycles | heap +0 KB (noise < 64), private < +1 MB | heap +27 KB; private +0-2.0 MB (d3d) | borderline (d3d) |
| Leak, 200 resizes | heap < +100 KB, private < +2 MB | heap +428 KB; private +10.4 MB (d3d JIT), 0.0-0.6 elsewhere | gap, unstable |
| Threads (hello) | <= 10 software, <= 20 d3d | JIT 9 / 20; AOT 6 / 17 (includes 1 harness thread) | met |
| First present | AOT < 150 ms hello, < 250 ms home; JIT < 400 / 700 | AOT 86-132 / 188-197; JIT 281-310 / 525-648 | met (median) |
| Exe (AOT) | hello <= 3 MB, shadcn <= 5 MB | 2.7 / 4.3 | met |
| Resize drag CPU | < 5 % | 5.5 % software, 13-16 % d3d (WARP) | gap |

## 4. Gaps, largest first

1. **Working set.** Hello 53 MB (JIT d3d) vs 33 MB AOT; home 88-91 vs 56-66. Private bytes are only 14-19 MB (hello) / 40-55 MB (home), so the rest is image-backed (runtime, JIT, DLLs, D3D). On home, managed heap is 11-17 MB for a static page; D3D adds 6-9 MB and 11-14 threads over software.
2. **Allocation is not zero in steady state.** Blinking input 91-530 B/frame (caret toggles 2x/s, so a few allocations per toggle), dialog 210-290 B/frame, vlist ~130 KB/s while scrolling (1.6-3.9 KB/frame), resize 3-4 KB/frame. Harmless for GC today (gen0 never ran in 10 s) but it breaks the 0 B rule and costs on small-heap targets.
3. **Resize CPU.** 5.5 % (software) / 13-16 % (d3d) of all cores during the drag, ~4 KB/frame, 220-300 frames/10 s. Matches docs/performance.md (whole tree re-rendered per resize).
4. **Startup.** JIT first present 250-650 ms vs AOT 60-205 ms. Spikes of 5-7.6 s were seen on first launches after a rebuild and once in a `--check` run (outside medians).
5. **Animation raster cost.** Spinner 64 px: software 0.7-0.9 % CPU at ~61 fps = whole 1000x700 frame rasterised per tick; d3d here reaches only 32 fps because the "GPU" is the WARP rasteriser.
6. **D3D resource growth under churn.** Private bytes +10.4 MB (resize) and +2 MB (dialog) in JIT d3d, not seen with software; AOT d3d shows +1.8 MB dialog, 0 resize. Needs a longer run to separate warm-up from leak.
7. **Idle noise.** `home/software/jit` drew 21 frames in 10 s (1.2 % CPU) while every other idle run drew 0. Something time-driven fires in that configuration (suspect the chat-card script / hover timer); not reproduced in AOT.

## 5. Next optimisation wave

| Change | Where | Expected win |
|---|---|---|
| Ship AOT as the default (publish docs/README) and set `TieredPGO=false`, `ConcurrentGarbageCollection=false` in sample csprojs (hypothesis, measure with `--mode`) | samples/*.csproj, README | -20 MB WS, -3 threads, startup -60 % (measured for AOT) |
| Find and remove the per-frame allocations: run `GotX.Perf run-target <t> --alloc-types` on input / dialog / vlist scroll, fix top types (likely caret/blink string or closure, overlay layer rebuild, `VirtualList` row Div + key strings) | Core/Widgets/TextInput.Blink, Overlay, Components/Lists/VirtualList.Render | input 0 B/frame; vlist -80 %; dialog 0 |
| Retained element tree across resize (no full `Render` of every view per step) | Core/App Window.Views/Cache | resize CPU about -50 %, KB/frame -70 % |
| Damage-rect repaint in the software renderer (and clear/redraw only dirty regions); skip Present when the scene hash is unchanged | Raster SoftwareRenderer, Platform.Windows Win32Window.Draw | spinner 0.9 % -> < 0.3 %; blink near 0 |
| Shrink home managed heap: lazy decode of Home images, cap glyph atlas/layout caches, drop unused default font data | samples/Shadcn Home, Core/Text | -5..8 MB private on home |
| Investigate D3D private growth with a 2000-cycle run, check `ResizeBuffers` and atlas/staging buffers are reused | Platform.Windows D3D11Renderer.Resources/Init | removes +2..10 MB drift |
| Find the idle timer on home (software JIT): `GOTX_LOG` + `GotX.Perf report` | Core Window.Pointer/Effects | idle 0 frames |

## 6. Limits and honesty

- One machine, shared with other running agents/builds: CPU % and startup are noisy (CPU +-2x on sub-1 % numbers; vlist/spin AOT d3d 20 % and 4.4 % were outliers vs 0.08 % / 0.04 % in JIT). Medians of 3 hide, not remove, this; tolerances in `--check` are generous for that reason.
- No real GPU: D3D11 falls to the Basic Display / WARP path. GPU MB reads 0, PrintWindow captures of the D3D window are black (software `--shots` work), d3d frame rates are capped by software rasterisation. Re-run on a machine with a GPU before trusting d3d CPU/GPU numbers.
- `vlist` leak column (+650 KB) is the harness's own lazily cached row strings, not the library. `allocPerFrame` counts all process allocations (input handling, harness stdin thread idle) over frames drawn; idle scenarios (0 frames) report 0.
- Harness cost in every number: 1 blocked stdin thread (and 1 poller until first present); console stdio. JIT mode = default tiered settings, not `TieredCompilation=0`.
- Medians are per metric, so a row may not equal any single run. Peak WS is the max of 250 ms samples, not the OS peak.
- Resize via cross-process `SetWindowPos` is a synchronous WM_SIZE step, not a real mouse drag (no live-resize modal loop).

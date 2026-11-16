# shadcn/ui ↔ GotX screenshots

Generated — do not edit by hand. Reference = ui.shadcn.com in headless Edge (see `manifest.json`); GotX = `samples/Shadcn`
rendered headless with the CPU renderer and the Geist fonts from `samples/Shadcn/Assets/fonts`.

- Re-capture references (network): `node tools/ShadcnRef/capture.mjs [--only home,login-03]`
- Regenerate GotX + diff + this file: `dotnet run --project samples/Shadcn -- --screenshots [--only home]`
- Compare only (existing `gotx/`): `dotnet run --project samples/Shadcn -- --compare`

Home is captured full page **without the site header** on both sides (the GotX home view has no site header; the
reference hides `header.sticky`). Blocks: viewport only. Folders: `reference/`, `gotx/`, `diff/` (dimmed reference;
magenta = channel Δ > 24, amber = 1–24, blue = area only one image has), `compare/` (reference | GotX, diff below; halved when wide). `diff/` and `compare/` are not tracked in git: run `--screenshots` to create them.

**Layout match**: reference elements paired with GotX nodes by normalized text (case-insensitive). *Texts*: DOM text runs
(Range rect = union of the rendered line boxes) ↔ GotX text nodes (union of the painted lines: wrapped exactly like the
painter — case, tracking, kerning, text-balance, line-clamp — each line placed by text-align; x = leftmost line,
w = widest line, y/h = first to last line box), Δ = max(|Δx|, |Δ vertical centre|, |Δw|) (line-box heights differ
by definition: Range = font ascent+descent, GotX = line-height).
*Boxes*: `data-slot` elements ↔ GotX nodes with background/border/interaction and the same subtree text,
Δ = max(|Δx|, |Δy|, |Δw|, |Δh|). Δ in CSS px; median / p90 / max over all matched pairs, then texts and boxes separately.
*Residual*: Δ of every pair inside a matched container (reference `data-slot` card / sidebar / sidebar-inset; innermost
wins) after subtracting the container's own Δx/Δy — drift inside the card, without the offset the card inherited.
*Jumps*: elements whose Δy (text: vertical centre, box: top) differs by > 2 px from the nearest matched element above
them (horizontal overlap) or from the matched box enclosing them, whichever edge is nearer — where each vertical offset is introduced; fix them top-down.

| Page | Viewport | Theme | Size ref → GotX | Pixel diff (tol 0 / 24) | Texts matched | Boxes matched | ≤2 px | Median Δ | P90 Δ | Max Δ | Text med / P90 | Box med / P90 | Residual med / P90 (n) | Jumps | Images |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| home | 1440×900 | light | 1440×1829 → 1440×1829 | 10.24 % / 3.25 % | 124/131 | 69/80 | 192/193 | 0.2 | 0.8 | 2.9 | 0.2 / 0.8 | 0.2 / 0.7 | 0 / 0.7 (154) | 1 | [ref](reference/home-1440-light.png) · [gotx](gotx/home-1440-light.png) · [diff](diff/home-1440-light.png) · [compare](compare/home-1440-light.png) |
| home | 1440×900 | dark | 1440×1829 → 1440×1829 | 15.84 % / 3.44 % | 124/131 | 69/80 | 192/193 | 0.2 | 0.8 | 2.9 | 0.2 / 0.8 | 0.2 / 0.7 | 0 / 0.7 (154) | 1 | [ref](reference/home-1440-dark.png) · [gotx](gotx/home-1440-dark.png) · [diff](diff/home-1440-dark.png) · [compare](compare/home-1440-dark.png) |
| home | 1024×900 | light | 1024×1765 → 1024×1765 | 11.88 % / 3.59 % | 96/101 | 57/64 | 153/153 | 0.3 | 0.7 | 1.4 | 0.3 / 0.6 | 0.6 / 0.7 | 0.3 / 0.7 (119) | 0 | [ref](reference/home-1024-light.png) · [gotx](gotx/home-1024-light.png) · [diff](diff/home-1024-light.png) · [compare](compare/home-1024-light.png) |
| home | 1024×900 | dark | 1024×1765 → 1024×1765 | 18.17 % / 3.77 % | 96/101 | 57/64 | 153/153 | 0.3 | 0.7 | 1.4 | 0.3 / 0.6 | 0.6 / 0.7 | 0.3 / 0.7 (119) | 0 | [ref](reference/home-1024-dark.png) · [gotx](gotx/home-1024-dark.png) · [diff](diff/home-1024-dark.png) · [compare](compare/home-1024-dark.png) |
| home | 640×900 | light | 640×1297 → 640×1298 ⚠ | 29.47 % / 2.46 % | 8/9 | 4/4 | 12/12 | 0.5 | 0.9 | 1.4 | 0.5 / 0.6 | 0.5 / 0.9 | 0 / 0 (0) | 0 | [ref](reference/home-640-light.png) · [gotx](gotx/home-640-light.png) · [diff](diff/home-640-light.png) · [compare](compare/home-640-light.png) |
| home | 640×900 | dark | 640×1297 → 640×1298 ⚠ | 21.21 % / 2.48 % | 8/9 | 4/4 | 12/12 | 0.5 | 0.9 | 1.4 | 0.5 / 0.6 | 0.5 / 0.9 | 0 / 0 (0) | 0 | [ref](reference/home-640-dark.png) · [gotx](gotx/home-640-dark.png) · [diff](diff/home-640-dark.png) · [compare](compare/home-640-dark.png) |
| dashboard-01 | 1440×900 | light | 1440×900 → 1440×900 | 27.03 % / 3.19 % | 66/153 | 44/131 | 110/110 | 0.3 | 0.9 | 1.5 | 0.3 / 0.7 | 0.5 / 1 | 0.5 / 1 (24) | 0 | [ref](reference/dashboard-01-1440-light.png) · [gotx](gotx/dashboard-01-1440-light.png) · [diff](diff/dashboard-01-1440-light.png) · [compare](compare/dashboard-01-1440-light.png) |
| dashboard-01 | 1440×900 | dark | 1440×900 → 1440×900 | 17.93 % / 3.57 % | 66/153 | 44/131 | 110/110 | 0.3 | 0.9 | 1.5 | 0.3 / 0.7 | 0.5 / 1 | 0.5 / 1 (24) | 0 | [ref](reference/dashboard-01-1440-dark.png) · [gotx](gotx/dashboard-01-1440-dark.png) · [diff](diff/dashboard-01-1440-dark.png) · [compare](compare/dashboard-01-1440-dark.png) |
| dashboard-01 | 1024×768 | light | 1024×768 → 1024×768 | 34.93 % / 3.03 % | 42/146 | 24/123 | 66/66 | 0 | 0.6 | 1 | 0 / 0.6 | 0 / 0.7 | 0.5 / 1 (24) | 0 | [ref](reference/dashboard-01-1024-light.png) · [gotx](gotx/dashboard-01-1024-light.png) · [diff](diff/dashboard-01-1024-light.png) · [compare](compare/dashboard-01-1024-light.png) |
| dashboard-01 | 1024×768 | dark | 1024×768 → 1024×768 | 19.11 % / 3.16 % | 42/146 | 24/123 | 66/66 | 0 | 0.6 | 1 | 0 / 0.6 | 0 / 0.7 | 0.5 / 1 (24) | 0 | [ref](reference/dashboard-01-1024-dark.png) · [gotx](gotx/dashboard-01-1024-dark.png) · [diff](diff/dashboard-01-1024-dark.png) · [compare](compare/dashboard-01-1024-dark.png) |
| sidebar-07 | 1440×900 | light | 1440×900 → 1440×900 | 0.88 % / 0.65 % | 19/20 | 15/19 | 34/34 | 0 | 0.5 | 2 | 0 / 0.5 | 0 / 0 | 0 / 0 (0) | 0 | [ref](reference/sidebar-07-1440-light.png) · [gotx](gotx/sidebar-07-1440-light.png) · [diff](diff/sidebar-07-1440-light.png) · [compare](compare/sidebar-07-1440-light.png) |
| sidebar-07 | 1440×900 | dark | 1440×900 → 1440×900 | 0.97 % / 0.70 % | 19/20 | 15/19 | 34/34 | 0 | 0.5 | 2 | 0 / 0.5 | 0 / 0 | 0 / 0 (0) | 0 | [ref](reference/sidebar-07-1440-dark.png) · [gotx](gotx/sidebar-07-1440-dark.png) · [diff](diff/sidebar-07-1440-dark.png) · [compare](compare/sidebar-07-1440-dark.png) |
| sidebar-07 | 1024×768 | light | 1024×768 → 1024×768 | 1.44 % / 1.07 % | 19/20 | 15/19 | 34/34 | 0 | 0.5 | 2 | 0 / 0.5 | 0 / 0 | 0 / 0 (0) | 0 | [ref](reference/sidebar-07-1024-light.png) · [gotx](gotx/sidebar-07-1024-light.png) · [diff](diff/sidebar-07-1024-light.png) · [compare](compare/sidebar-07-1024-light.png) |
| sidebar-07 | 1024×768 | dark | 1024×768 → 1024×768 | 1.58 % / 1.15 % | 19/20 | 15/19 | 34/34 | 0 | 0.5 | 2 | 0 / 0.5 | 0 / 0 | 0 / 0 (0) | 0 | [ref](reference/sidebar-07-1024-dark.png) · [gotx](gotx/sidebar-07-1024-dark.png) · [diff](diff/sidebar-07-1024-dark.png) · [compare](compare/sidebar-07-1024-dark.png) |
| sidebar-03 | 1440×900 | light | 1440×900 → 1440×900 | 1.51 % / 1.24 % | 30/36 | 32/42 | 62/62 | 0 | 0 | 2 | 0 / 0 | 0 / 0 | 0 / 0 (0) | 0 | [ref](reference/sidebar-03-1440-light.png) · [gotx](gotx/sidebar-03-1440-light.png) · [diff](diff/sidebar-03-1440-light.png) · [compare](compare/sidebar-03-1440-light.png) |
| sidebar-03 | 1440×900 | dark | 1440×900 → 1440×900 | 1.70 % / 1.34 % | 30/36 | 32/42 | 62/62 | 0 | 0 | 2 | 0 / 0 | 0 / 0 | 0 / 0 (0) | 0 | [ref](reference/sidebar-03-1440-dark.png) · [gotx](gotx/sidebar-03-1440-dark.png) · [diff](diff/sidebar-03-1440-dark.png) · [compare](compare/sidebar-03-1440-dark.png) |
| sidebar-03 | 1024×768 | light | 1024×768 → 1024×768 | 2.13 % / 1.75 % | 26/36 | 28/42 | 54/54 | 0 | 0 | 2 | 0 / 0 | 0 / 0 | 0 / 0 (0) | 0 | [ref](reference/sidebar-03-1024-light.png) · [gotx](gotx/sidebar-03-1024-light.png) · [diff](diff/sidebar-03-1024-light.png) · [compare](compare/sidebar-03-1024-light.png) |
| sidebar-03 | 1024×768 | dark | 1024×768 → 1024×768 | 2.37 % / 1.88 % | 26/36 | 28/42 | 54/54 | 0 | 0 | 2 | 0 / 0 | 0 / 0 | 0 / 0 (0) | 0 | [ref](reference/sidebar-03-1024-dark.png) · [gotx](gotx/sidebar-03-1024-dark.png) · [diff](diff/sidebar-03-1024-dark.png) · [compare](compare/sidebar-03-1024-dark.png) |
| login-03 | 1440×900 | light | 1440×900 → 1440×900 | 2.71 % / 0.85 % | 13/15 | 4/6 | 16/17 | 0.2 | 0.8 | 3.5 | 0.4 / 1.1 | 0.1 / 0.3 | 0.3 / 1 (14) | 0 | [ref](reference/login-03-1440-light.png) · [gotx](gotx/login-03-1440-light.png) · [diff](diff/login-03-1440-light.png) · [compare](compare/login-03-1440-light.png) |
| login-03 | 1440×900 | dark | 1440×900 → 1440×900 | 1.75 % / 0.85 % | 13/15 | 4/6 | 16/17 | 0.2 | 0.8 | 3.5 | 0.4 / 1.1 | 0.1 / 0.3 | 0.3 / 1 (14) | 0 | [ref](reference/login-03-1440-dark.png) · [gotx](gotx/login-03-1440-dark.png) · [diff](diff/login-03-1440-dark.png) · [compare](compare/login-03-1440-dark.png) |
| login-03 | 1024×768 | light | 1024×768 → 1024×768 | 4.47 % / 1.40 % | 13/15 | 4/6 | 16/17 | 0.2 | 0.8 | 3.5 | 0.4 / 1.1 | 0.1 / 0.3 | 0.3 / 1 (14) | 0 | [ref](reference/login-03-1024-light.png) · [gotx](gotx/login-03-1024-light.png) · [diff](diff/login-03-1024-light.png) · [compare](compare/login-03-1024-light.png) |
| login-03 | 1024×768 | dark | 1024×768 → 1024×768 | 2.89 % / 1.41 % | 13/15 | 4/6 | 16/17 | 0.2 | 0.8 | 3.5 | 0.4 / 1.1 | 0.1 / 0.3 | 0.3 / 1 (14) | 0 | [ref](reference/login-03-1024-dark.png) · [gotx](gotx/login-03-1024-dark.png) · [diff](diff/login-03-1024-dark.png) · [compare](compare/login-03-1024-dark.png) |
| login-04 | 1440×900 | light | 1440×900 → 1440×900 | 2.98 % / 0.70 % | 11/15 | 2/7 | 12/13 | 0.5 | 0.9 | 3.5 | 0.5 / 0.9 | 0.4 / 0.8 | 0.8 / 1.3 (10) | 0 | [ref](reference/login-04-1440-light.png) · [gotx](gotx/login-04-1440-light.png) · [diff](diff/login-04-1440-light.png) · [compare](compare/login-04-1440-light.png) |
| login-04 | 1440×900 | dark | 1440×900 → 1440×900 | 1.78 % / 0.68 % | 11/15 | 2/7 | 12/13 | 0.5 | 0.9 | 3.5 | 0.5 / 0.9 | 0.4 / 0.8 | 0.8 / 1.3 (10) | 0 | [ref](reference/login-04-1440-dark.png) · [gotx](gotx/login-04-1440-dark.png) · [diff](diff/login-04-1440-dark.png) · [compare](compare/login-04-1440-dark.png) |
| login-04 | 1024×768 | light | 1024×768 → 1024×768 | 4.91 % / 1.15 % | 11/15 | 2/7 | 12/13 | 0.5 | 0.9 | 3.5 | 0.5 / 0.9 | 0.4 / 0.8 | 0.7 / 1.2 (10) | 0 | [ref](reference/login-04-1024-light.png) · [gotx](gotx/login-04-1024-light.png) · [diff](diff/login-04-1024-light.png) · [compare](compare/login-04-1024-light.png) |
| login-04 | 1024×768 | dark | 1024×768 → 1024×768 | 2.94 % / 1.13 % | 11/15 | 2/7 | 12/13 | 0.5 | 0.9 | 3.5 | 0.5 / 0.9 | 0.4 / 0.8 | 0.7 / 1.2 (10) | 0 | [ref](reference/login-04-1024-dark.png) · [gotx](gotx/login-04-1024-dark.png) · [diff](diff/login-04-1024-dark.png) · [compare](compare/login-04-1024-dark.png) |
| signup-03 | 1440×900 | light | 1440×900 → 1440×900 | 2.53 % / 0.74 % | 12/14 | 2/4 | 13/14 | 0.4 | 1 | 3.5 | 0.6 / 1 | 0.1 / 0.3 | 0.6 / 1.1 (11) | 0 | [ref](reference/signup-03-1440-light.png) · [gotx](gotx/signup-03-1440-light.png) · [diff](diff/signup-03-1440-light.png) · [compare](compare/signup-03-1440-light.png) |
| signup-03 | 1440×900 | dark | 1440×900 → 1440×900 | 1.61 % / 0.77 % | 12/14 | 2/4 | 13/14 | 0.4 | 1 | 3.5 | 0.6 / 1 | 0.1 / 0.3 | 0.6 / 1.1 (11) | 0 | [ref](reference/signup-03-1440-dark.png) · [gotx](gotx/signup-03-1440-dark.png) · [diff](diff/signup-03-1440-dark.png) · [compare](compare/signup-03-1440-dark.png) |
| signup-03 | 1024×768 | light | 1024×768 → 1024×768 | 4.17 % / 1.22 % | 12/14 | 2/4 | 13/14 | 0.4 | 1 | 3.5 | 0.6 / 1 | 0.1 / 0.3 | 0.6 / 1.1 (11) | 0 | [ref](reference/signup-03-1024-light.png) · [gotx](gotx/signup-03-1024-light.png) · [diff](diff/signup-03-1024-light.png) · [compare](compare/signup-03-1024-light.png) |
| signup-03 | 1024×768 | dark | 1024×768 → 1024×768 | 2.66 % / 1.27 % | 12/14 | 2/4 | 13/14 | 0.4 | 1 | 3.5 | 0.6 / 1 | 0.1 / 0.3 | 0.6 / 1.1 (11) | 0 | [ref](reference/signup-03-1024-dark.png) · [gotx](gotx/signup-03-1024-dark.png) · [diff](diff/signup-03-1024-dark.png) · [compare](compare/signup-03-1024-dark.png) |
| signup-04 | 1440×900 | light | 1440×900 → 1440×900 | 3.48 % / 0.97 % | 13/17 | 2/7 | 14/15 | 0.8 | 1.1 | 3.5 | 0.8 / 1.1 | 0.3 / 0.5 | 1 / 1.4 (12) | 0 | [ref](reference/signup-04-1440-light.png) · [gotx](gotx/signup-04-1440-light.png) · [diff](diff/signup-04-1440-light.png) · [compare](compare/signup-04-1440-light.png) |
| signup-04 | 1440×900 | dark | 1440×900 → 1440×900 | 2.29 % / 0.98 % | 13/17 | 2/7 | 14/15 | 0.8 | 1.1 | 3.5 | 0.8 / 1.1 | 0.3 / 0.5 | 1 / 1.4 (12) | 0 | [ref](reference/signup-04-1440-dark.png) · [gotx](gotx/signup-04-1440-dark.png) · [diff](diff/signup-04-1440-dark.png) · [compare](compare/signup-04-1440-dark.png) |
| signup-04 | 1024×768 | light | 1024×768 → 1024×768 | 5.74 % / 1.60 % | 13/17 | 2/7 | 14/15 | 0.8 | 1.1 | 3.5 | 0.8 / 1.1 | 0.3 / 0.5 | 1 / 1.4 (12) | 0 | [ref](reference/signup-04-1024-light.png) · [gotx](gotx/signup-04-1024-light.png) · [diff](diff/signup-04-1024-light.png) · [compare](compare/signup-04-1024-light.png) |
| signup-04 | 1024×768 | dark | 1024×768 → 1024×768 | 3.78 % / 1.61 % | 13/17 | 2/7 | 14/15 | 0.8 | 1.1 | 3.5 | 0.8 / 1.1 | 0.3 / 0.5 | 1 / 1.4 (12) | 0 | [ref](reference/signup-04-1024-dark.png) · [gotx](gotx/signup-04-1024-dark.png) · [diff](diff/signup-04-1024-dark.png) · [compare](compare/signup-04-1024-dark.png) |

## Worst layout offenders

Light theme (layout is theme-independent). Rects are `x,y w×h`; Δ = GotX − reference.

### home 1440×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | $50 (MIN) | 752,1031.6 63.7×18 | 752,1033 63.9×21 | 0 | 2.9 | 0.3 | 3 |
| text | shadcn | 563.4,1771.8 47.7×18 | 562,1771 47.7×20 | -1.4 | 0.2 | -0 | 2 |
| text | Realty Income | 415,1654.6 93.4×18 | 415,1655 93.4×19.3 | 0 | 1.1 | -0 | 1.3 |
| text | Notes | 752,1077.6 39.1×18 | 752,1078 39.1×19.3 | 0 | 1.1 | -0 | 1.3 |
| text | $2500.00 | 931,968.6 111×31 | 931,969 111×32 | 0 | 1 | 0 | 1 |
| text | $10,000 (MAX) | 946.6,1033.6 95.4×18 | 946,1033 95.3×21 | -0.6 | 0.9 | -0.1 | 3 |
| text | Outline | 259.1,401.8 47.8×18 | 260,401 47.8×20 | 0.9 | 0.2 | -0 | 2 |
| box button | Outline | 246.1,394.8 73.8×32 | 247,395 74×32 | 0.9 | 0.2 | 0.2 | 0 |
| text | Secondary | 115.1,596.8 60.3×16 | 116,597 60.3×16 | 0.9 | 0.2 | 0 | 0 |
| box badge | Badge | 44,594.8 54.1×20 | 44,595 55×20 | 0 | 0.2 | 0.9 | 0 |
| box button | Get Started | 592.5,267.3 102.1×35 | 592,267 103×35 | -0.5 | -0.3 | 0.9 | 0 |
| box badge | Secondary | 106.1,594.8 78.3×20 | 107,595 79×20 | 0.9 | 0.2 | 0.7 | 0 |
| text | -$37.46 | 618.9,1058.8 52.1×18 | 618,1058 52.1×20 | -0.9 | 0.2 | -0 | 2 |
| text | Goal Name | 752,484.8 71.9×18 | 752,485 71.9×19.3 | 0 | 0.8 | -0 | 1.3 |
| text | Vanguard | 415,1386.8 63×18 | 415,1387 62.9×19.3 | 0 | 0.8 | -0 | 1.3 |

**Worst residuals inside containers** (14 matched containers; container Δ removed)

| Kind | Text | Container | Container Δx, Δy | Residual Δx | Residual Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | $50 (MIN) | Payout Threshold Set the minim… | 0, -0.3 | 0 | 3.2 | 0.3 | 3 |
| text | Notes | Payout Threshold Set the minim… | 0, -0.3 | 0 | 1.4 | -0 | 1.3 |
| text | $2500.00 | Payout Threshold Set the minim… | 0, -0.3 | 0 | 1.3 | 0 | 1 |
| text | $10,000 (MAX) | Payout Threshold Set the minim… | 0, -0.3 | -0.6 | 1.2 | -0.1 | 3 |
| text | Outline | Button Secondary Outline Badge… | 0, 0.2 | 0.9 | 0 | -0 | 2 |
| box button | Outline | Button Secondary Outline Badge… | 0, 0.2 | 0.9 | 0 | 0.2 | 0 |
| text | Secondary | Button Secondary Outline Badge… | 0, 0.2 | 0.9 | 0 | 0 | 0 |
| box badge | Badge | Button Secondary Outline Badge… | 0, 0.2 | 0 | 0 | 0.9 | 0 |
| box badge | Secondary | Button Secondary Outline Badge… | 0, 0.2 | 0.9 | 0 | 0.7 | 0 |
| text | -$37.46 | Claimable Balance $1,211.29 Pe… | 0, 0.2 | -0.9 | -0 | -0 | 2 |
| text | Realty Income | Q2 Dividend Income Quarterly d… | 0, 0.2 | 0 | 0.9 | -0 | 1.3 |
| text | USD — United States Dollar | Payout Threshold Set the minim… | 0, -0.3 | 0 | 0.8 | -0 | 2 |

**Offset jumps, top → bottom** (first 1 of 1; Δy vs the element directly above or the enclosing box)

| Ref y | Kind | Text | Compared with | Its Δy | Δy | Jump |
|---|---|---|---|---|---|---|
| 1031.6 | text | $50 (MIN) | Minimum Payout Amount | 0.1 | 2.9 | 2.9 |

### home 1024×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | shadcn | 355.4,1728 47.7×18 | 354,1727 47.7×20 | -1.4 | 0 | -0 | 2 |
| text | Apple AAPL | 394.3,1541.5 77.8×18 | 394,1542 77.8×19.3 | -0.3 | 1.1 | 0 | 1.3 |
| text | Recurring card payments | 757.7,1588.5 164.1×18 | 758,1589 164.1×19.3 | 0.3 | 1.1 | 0 | 1.3 |
| text | Outline | 259.1,358 47.8×18 | 260,357 47.8×20 | 0.9 | 0 | -0 | 2 |
| box button | Outline | 246.1,351 73.8×32 | 247,351 74×32 | 0.9 | 0 | 0.2 | 0 |
| text | $273,000 | 232.9,1403 63.4×18 | 232,1402 63.4×20 | -0.9 | 0 | -0 | 2 |
| text | Secondary | 115.1,553 60.3×16 | 116,553 60.3×16 | 0.9 | 0 | 0 | 0 |
| box badge | Badge | 44,551 54.1×20 | 44,551 55×20 | 0 | 0 | 0.9 | 0 |
| box badge | Secondary | 106.1,551 78.3×20 | 107,551 79×20 | 0.9 | 0 | 0.7 | 0 |
| box button | Get Started | 384.5,223.5 102.1×35 | 384,224 103×35 | -0.5 | 0.5 | 0.9 | 0 |
| text | Realty Income | 394.3,1630.8 93.4×18 | 394,1631 93.4×19.3 | -0.3 | 0.9 | -0 | 1.3 |
| box item | Scheduled transfers Set up a transfer to send at a later dat | 710.7,1464.3 269.3×93.3 | 711,1464 269×94 | 0.3 | -0.3 | -0.3 | 0.8 |
| box item | S&P 500 VOO 112 Shares | 377.3,1437.3 269.3×73.3 | 377,1437 270×74 | -0.3 | -0.3 | 0.7 | 0.8 |
| text | 112 Shares | 394.3,1476.5 66.8×18 | 394,1476 67.4×20 | -0.3 | 0.5 | 0.7 | 2 |
| box card | Contribution History Last 6 months of activity Dec Jan Feb M | 357.3,331 309.3×480 | 357,331 310×480 | -0.3 | 0 | 0.7 | 0 |

**Worst residuals inside containers** (11 matched containers; container Δ removed)

| Kind | Text | Container | Container Δx, Δy | Residual Δx | Residual Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Apple AAPL | Q2 Dividend Income Quarterly d… | -0.3, 0 | 0 | 1.1 | 0 | 1.3 |
| text | Outline | Button Secondary Outline Badge… | 0, 0 | 0.9 | 0 | -0 | 2 |
| box button | Outline | Button Secondary Outline Badge… | 0, 0 | 0.9 | 0 | 0.2 | 0 |
| text | $273,000 | Savings Targets Active milesto… | 0, 0 | -0.9 | 0 | -0 | 2 |
| text | Secondary | Button Secondary Outline Badge… | 0, 0 | 0.9 | 0 | 0 | 0 |
| box badge | Badge | Button Secondary Outline Badge… | 0, 0 | 0 | 0 | 0.9 | 0 |
| box badge | Secondary | Button Secondary Outline Badge… | 0, 0 | 0.9 | 0 | 0.7 | 0 |
| text | Realty Income | Q2 Dividend Income Quarterly d… | -0.3, 0 | 0 | 0.9 | -0 | 1.3 |
| box item | S&P 500 VOO 112 Shares | Q2 Dividend Income Quarterly d… | -0.3, 0 | 0 | -0.3 | 0.7 | 0.8 |
| text | 112 Shares | Q2 Dividend Income Quarterly d… | -0.3, 0 | 0 | 0.5 | 0.7 | 2 |
| box item | UPCOMING May 2024 Scheduled | Contribution History Last 6 mo… | -0.3, 0 | 0 | 0 | 0.7 | 0 |
| box button | View Full Report | Contribution History Last 6 mo… | -0.3, 0 | 0 | 0 | 0.7 | 0 |

### home 640×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | shadcn | 163.4,1260.4 47.7×18 | 162,1259 47.7×20 | -1.4 | -0.4 | -0 | 2 |
| box button | Get Started | 192.5,206.5 102.1×35 | 192,207 103×35 | -0.5 | 0.5 | 0.9 | 0 |
| text | Vercel | 230.6,1260.4 41.6×18 | 230,1259 41.6×20 | -0.6 | -0.4 | -0 | 2 |
| text | GitHub | 480.4,1260.4 44.9×18 | 481,1259 44.9×20 | 0.6 | -0.4 | 0 | 2 |
| text | The Foundation for your Design System | 52.7,60 534.5×39 | 53.3,62 534.5×36 | 0.5 | 0.5 | -0.1 | -3 |
| box badge | Trace, Code Block and AI Elements | 205.8,32 228.5×22 | 206,32 229×22 | 0.3 | 0 | 0.5 | 0 |
| text | Trace, Code Block and AI Elements | 214.8,35 194.5×16 | 215.3,35 194.5×16 | 0.5 | 0 | 0 | 0 |
| text | Composable, accessible components with thoughtful defaults.  | 102.3,108 435.4×79 | 102.3,106 435.3×84 | 0 | 0.5 | -0 | 5 |
| text | View Components | 315.6,215 118.8×18 | 316.1,214 118.8×20 | 0.4 | 0 | 0 | 2 |
| box layout | Trace, Code Block and AI Elements The Foundation for your De | 0,0 640×1297.4 | 0,0 640×1297 | 0 | 0 | 0 | -0.4 |
| box button | View Components | 302.6,206 144.8×36 | 303,206 145×36 | 0.4 | 0 | 0.2 | 0 |
| text | Get Started | 205.5,215 76.1×18 | 205.4,214 76.1×20 | -0.1 | 0 | -0 | 2 |

### dashboard-01 1440×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Key Personnel | 550.5,733 92.6×18 | 552,732 92.6×20 | 1.5 | 0 | 0 | 2 |
| box badge | 3 | 512.5,732 20×20 | 514,732 20×20 | 1.5 | 0 | 0 | 0 |
| text | Past Performance | 389.8,733 116.8×18 | 391,732 116.7×20 | 1.2 | 0 | -0 | 2 |
| box tabs-trigger | Outline | 315,727.5 65.8×29 | 315,728 67×29 | 0 | 0.5 | 1.2 | 0 |
| text | 3 | 518.8,734 7.5×16 | 520,734 7.5×16 | 1.2 | 0 | 0 | 0 |
| box tabs-list | Outline Past Performance 3 Key Personnel 2 Focus Documents | 312,724 504.8×36 | 312,724 506×36 | 0 | 0 | 1.2 | 0 |
| text | Documents | 369,20.5 85.7×21 | 369,20 85.7×24 | 0 | 1 | 0 | 3 |
| box badge | +12.5% | 490.5,105 73×22 | 491,105 74×22 | 0.5 | 0 | 1 | 0 |
| box badge | +12.5% | 1032,105 73×22 | 1031,105 74×22 | -1 | 0 | 1 | 0 |
| text | +12.5% | 1057,108 39×16 | 1056,108 39.1×16 | -1 | 0 | 0.1 | 0 |
| text | Last 3 months | 1048,342 93.6×18 | 1047,341 93.6×20 | -0.9 | 0 | 0 | 2 |
| box toggle-group-item | Last 3 months | 1031,333 127.6×36 | 1030,333 128×36 | -0.9 | 0 | 0.4 | 0 |
| text | Focus Documents | 687.1,733 117.7×18 | 688,732 117.7×20 | 0.9 | 0 | 0 | 2 |
| box badge | 2 | 649.1,732 20×20 | 650,732 20×20 | 0.9 | 0 | 0 | 0 |
| box tabs-trigger | Focus Documents | 678.1,727.5 135.7×29 | 679,728 136×29 | 0.9 | 0.5 | 0.3 | 0 |

**Worst residuals inside containers** (4 matched containers; container Δ removed)

| Kind | Text | Container | Container Δx, Δy | Residual Δx | Residual Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| box badge | +12.5% | Total Revenue $1,250.00 +12.5%… | 0, 0 | 0.5 | 0 | 1 | 0 |
| box badge | +12.5% | Active Accounts 45,678 +12.5% … | 0, 0 | -1 | 0 | 1 | 0 |
| text | +12.5% | Active Accounts 45,678 +12.5% … | 0, 0 | -1 | 0 | 0.1 | 0 |
| box badge | +4.5% | Growth Rate 4.5% +4.5% Steady … | 0, 0 | -0.7 | 0 | 0.7 | 0 |
| text | +4.5% | Growth Rate 4.5% +4.5% Steady … | 0, 0 | -0.7 | 0 | -0 | 0 |
| text | -20% | New Customers 1,234 -20% Down … | 0, 0 | -0.6 | 0 | -0 | 0 |
| box badge | -20% | New Customers 1,234 -20% Down … | 0, 0 | -0.6 | 0 | 0.6 | 0 |
| text | $1,250.00 | Total Revenue $1,250.00 +12.5%… | 0, 0 | 0 | 0.5 | 0 | -3 |
| text | 4.5% | Growth Rate 4.5% +4.5% Steady … | 0, 0 | 0 | 0.5 | -0 | -3 |
| text | 1,234 | New Customers 1,234 -20% Down … | 0, 0 | 0 | 0.5 | -0 | -3 |
| text | 45,678 | Active Accounts 45,678 +12.5% … | 0, 0 | 0 | 0.5 | -0 | -3 |
| text | +12.5% | Total Revenue $1,250.00 +12.5%… | 0, 0 | 0.5 | 0 | 0.1 | 0 |

### dashboard-01 1024×768

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Documents | 369,20.5 85.7×21 | 369,20 85.7×24 | 0 | 1 | 0 | 3 |
| box badge | +12.5% | 546,105 73×22 | 545,105 74×22 | -1 | 0 | 1 | 0 |
| box badge | +12.5% | 546,305 73×22 | 545,305 74×22 | -1 | 0 | 1 | 0 |
| text | +12.5% | 571,108 39×16 | 570,108 39.1×16 | -1 | 0 | 0.1 | 0 |
| text | +12.5% | 571,308 39×16 | 570,308 39.1×16 | -1 | 0 | 0.1 | 0 |
| box badge | +4.5% | 898.7,305 68.3×22 | 898,305 69×22 | -0.7 | 0 | 0.7 | 0 |
| text | +4.5% | 923.7,308 34.3×16 | 923,308 34.3×16 | -0.7 | 0 | -0 | 0 |
| text | -20% | 927.6,108 30.4×16 | 927,108 30.4×16 | -0.6 | 0 | -0 | 0 |
| box badge | -20% | 902.6,105 64.4×22 | 902,105 65×22 | -0.6 | 0 | 0.6 | 0 |
| text | $1,250.00 | 337,131 145.5×39 | 337,133 145.5×36 | 0 | 0.5 | 0 | -3 |
| text | 4.5% | 685,331 68.7×39 | 685,333 68.7×36 | 0 | 0.5 | -0 | -3 |
| text | shadcn | 64,710.3 47.7×18 | 64,711 47.7×17.5 | 0 | 0.5 | -0 | -0.5 |
| text | Total Visitors | 337,510 100×21 | 337,513 100×16 | 0 | 0.5 | -0 | -5 |
| text | 1,234 | 685,131 81.6×39 | 685,133 81.6×36 | 0 | 0.5 | -0 | -3 |
| text | 45,678 | 337,331 100.4×39 | 337,333 100.4×36 | 0 | 0.5 | -0 | -3 |

**Worst residuals inside containers** (4 matched containers; container Δ removed)

| Kind | Text | Container | Container Δx, Δy | Residual Δx | Residual Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| box badge | +12.5% | Total Revenue $1,250.00 +12.5%… | 0, 0 | -1 | 0 | 1 | 0 |
| box badge | +12.5% | Active Accounts 45,678 +12.5% … | 0, 0 | -1 | 0 | 1 | 0 |
| text | +12.5% | Total Revenue $1,250.00 +12.5%… | 0, 0 | -1 | 0 | 0.1 | 0 |
| text | +12.5% | Active Accounts 45,678 +12.5% … | 0, 0 | -1 | 0 | 0.1 | 0 |
| box badge | +4.5% | Growth Rate 4.5% +4.5% Steady … | 0, 0 | -0.7 | 0 | 0.7 | 0 |
| text | +4.5% | Growth Rate 4.5% +4.5% Steady … | 0, 0 | -0.7 | 0 | -0 | 0 |
| text | -20% | New Customers 1,234 -20% Down … | 0, 0 | -0.6 | 0 | -0 | 0 |
| box badge | -20% | New Customers 1,234 -20% Down … | 0, 0 | -0.6 | 0 | 0.6 | 0 |
| text | $1,250.00 | Total Revenue $1,250.00 +12.5%… | 0, 0 | 0 | 0.5 | 0 | -3 |
| text | 4.5% | Growth Rate 4.5% +4.5% Steady … | 0, 0 | 0 | 0.5 | -0 | -3 |
| text | 1,234 | New Customers 1,234 -20% Down … | 0, 0 | 0 | 0.5 | -0 | -3 |
| text | 45,678 | Active Accounts 45,678 +12.5% … | 0, 0 | 0 | 0.5 | -0 | -3 |

### sidebar-07 1440×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| box breadcrumb-link | Build Your Application | 321,23 139.5×18 | 321,22 140×20 | 0 | -1 | 0.5 | 2 |
| text | Data Fetching | 494.5,23 89.3×18 | 495,22 89.3×20 | 0.5 | 0 | -0 | 2 |
| text | shadcn | 56,850.3 47.7×18 | 56,851 47.7×17.5 | 0 | 0.5 | -0 | -0.5 |
| text | Acme Inc | 56,14.3 60.8×18 | 56,15 60.8×17.5 | 0 | 0.5 | 0 | -0.5 |
| text | m@example.com | 56,868.8 93.9×16 | 56,869 93.9×16 | 0 | 0.3 | -0 | 0 |
| text | Enterprise | 56,32.8 55.8×16 | 56,33 55.8×16 | 0 | 0.3 | -0 | 0 |
| text | Travel | 40,475 37.8×18 | 40,474 37.8×20 | 0 | 0 | -0 | 2 |
| text | Platform | 16,80 48.3×16 | 16,80 48.3×16 | 0 | 0 | -0 | 0 |
| text | Projects | 16,372 46.6×16 | 16,372 46.5×16 | 0 | 0 | -0 | 0 |
| text | History | 41,143 45.5×18 | 41,142 45.5×20 | 0 | 0 | -0 | 2 |
| text | Models | 40,243 46.9×18 | 40,242 46.9×20 | 0 | 0 | -0 | 2 |
| text | Design Engineering | 40,403 124.1×18 | 40,402 124.1×20 | 0 | 0 | -0 | 2 |
| text | Playground | 40,111 72.7×18 | 40,110 72.7×20 | 0 | 0 | -0 | 2 |
| text | Settings | 41,207 53×18 | 41,206 53×20 | 0 | 0 | -0 | 2 |
| text | Settings | 40,315 53×18 | 40,314 53×20 | 0 | 0 | -0 | 2 |

### sidebar-07 1024×768

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| box breadcrumb-link | Build Your Application | 321,23 139.5×18 | 321,22 140×20 | 0 | -1 | 0.5 | 2 |
| text | Data Fetching | 494.5,23 89.3×18 | 495,22 89.3×20 | 0.5 | 0 | -0 | 2 |
| text | shadcn | 56,718.3 47.7×18 | 56,719 47.7×17.5 | 0 | 0.5 | -0 | -0.5 |
| text | Acme Inc | 56,14.3 60.8×18 | 56,15 60.8×17.5 | 0 | 0.5 | 0 | -0.5 |
| text | m@example.com | 56,736.8 93.9×16 | 56,737 93.9×16 | 0 | 0.3 | -0 | 0 |
| text | Enterprise | 56,32.8 55.8×16 | 56,33 55.8×16 | 0 | 0.3 | -0 | 0 |
| text | Travel | 40,475 37.8×18 | 40,474 37.8×20 | 0 | 0 | -0 | 2 |
| text | Platform | 16,80 48.3×16 | 16,80 48.3×16 | 0 | 0 | -0 | 0 |
| text | Projects | 16,372 46.6×16 | 16,372 46.5×16 | 0 | 0 | -0 | 0 |
| text | History | 41,143 45.5×18 | 41,142 45.5×20 | 0 | 0 | -0 | 2 |
| text | Models | 40,243 46.9×18 | 40,242 46.9×20 | 0 | 0 | -0 | 2 |
| text | Design Engineering | 40,403 124.1×18 | 40,402 124.1×20 | 0 | 0 | -0 | 2 |
| text | Playground | 40,111 72.7×18 | 40,110 72.7×20 | 0 | 0 | -0 | 2 |
| text | Settings | 41,207 53×18 | 41,206 53×20 | 0 | 0 | -0 | 2 |
| text | Settings | 40,315 53×18 | 40,314 53×20 | 0 | 0 | -0 | 2 |

### sidebar-03 1440×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| box breadcrumb-link | Build Your Application | 321,22.5 139.5×18 | 321,22 140×20 | 0 | -0.5 | 0.5 | 2 |
| text | Data Fetching | 494.5,22.5 89.3×18 | 495,22 89.3×20 | 0.5 | 0.5 | -0 | 2 |
| text | Build Your Application | 321,22.5 139.5×18 | 321,22 139.5×20 | 0 | 0.5 | -0 | 2 |
| text | Documentation | 56,15 100.1×18 | 56,17 100.1×14 | 0 | 0 | 0 | -4 |
| text | Upgrading | 41,531 66.4×18 | 41,530 66.4×20 | 0 | 0 | -0 | 2 |
| text | Caching | 41,307 52.8×18 | 41,306 52.8×20 | 0 | 0 | -0 | 2 |
| text | Edge Runtime | 41,791 89.5×18 | 41,790 89.5×20 | 0 | 0 | -0 | 2 |
| text | Styling | 41,339 44.8×18 | 41,338 44.8×20 | 0 | 0 | -0 | 2 |
| text | Installation | 41,111 70.5×18 | 41,110 70.5×20 | 0 | 0 | -0 | 2 |
| text | Optimizing | 41,371 69.7×18 | 41,370 69.7×20 | 0 | 0 | -0 | 2 |
| text | Examples | 41,563 62.2×18 | 41,562 62.2×20 | 0 | 0 | -0 | 2 |
| text | File Conventions | 41,663 105.6×18 | 41,662 105.6×20 | 0 | 0 | -0 | 2 |
| text | Configuring | 41,403 74.6×18 | 41,402 74.6×20 | 0 | 0 | -0 | 2 |
| text | Project Structure | 41,143 109.4×18 | 41,142 109.4×20 | 0 | 0 | -0 | 2 |
| text | CLI | 41,759 20.9×18 | 41,758 20.9×20 | 0 | 0 | -0 | 2 |

### sidebar-03 1024×768

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| box breadcrumb-link | Build Your Application | 321,22.5 139.5×18 | 321,22 140×20 | 0 | -0.5 | 0.5 | 2 |
| text | Data Fetching | 494.5,22.5 89.3×18 | 495,22 89.3×20 | 0.5 | 0.5 | -0 | 2 |
| text | Build Your Application | 321,22.5 139.5×18 | 321,22 139.5×20 | 0 | 0.5 | -0 | 2 |
| text | Documentation | 56,15 100.1×18 | 56,17 100.1×14 | 0 | 0 | 0 | -4 |
| text | Upgrading | 41,531 66.4×18 | 41,530 66.4×20 | 0 | 0 | -0 | 2 |
| text | Caching | 41,307 52.8×18 | 41,306 52.8×20 | 0 | 0 | -0 | 2 |
| text | Styling | 41,339 44.8×18 | 41,338 44.8×20 | 0 | 0 | -0 | 2 |
| text | Installation | 41,111 70.5×18 | 41,110 70.5×20 | 0 | 0 | -0 | 2 |
| text | Optimizing | 41,371 69.7×18 | 41,370 69.7×20 | 0 | 0 | -0 | 2 |
| text | Configuring | 41,403 74.6×18 | 41,402 74.6×20 | 0 | 0 | -0 | 2 |
| text | Examples | 41,563 62.2×18 | 41,562 62.2×20 | 0 | 0 | -0 | 2 |
| text | File Conventions | 41,663 105.6×18 | 41,662 105.6×20 | 0 | 0 | -0 | 2 |
| text | Project Structure | 41,143 109.4×18 | 41,142 109.4×20 | 0 | 0 | -0 | 2 |
| text | CLI | 41,759 20.9×18 | 41,758 20.9×20 | 0 | 0 | -0 | 2 |
| text | v1.0.0 | 56,31 37.1×18 | 56,33 37.1×14 | 0 | 0 | -0 | -4 |

### login-03 1440×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Don't have an account? | 618.9,663.1 153.5×18 | 618.5,662 150×21 | -0.4 | 0.4 | -3.5 | 3 |
| text | Password | 553,518.5 64×18 | 553,519 64×19.3 | 0 | 1.1 | -0 | 1.3 |
| text | Privacy Policy | 714.1,754.1 87.8×18 | 714.2,753 88.6×21 | 0.1 | 0.4 | 0.8 | 3 |
| text | Email | 553,422.9 36×18 | 553,423 36×19.3 | 0 | 0.7 | -0 | 1.3 |
| text | Sign up | 772.4,663.1 48.7×18 | 773.1,662 48.7×21 | 0.7 | 0.4 | 0 | 3 |
| text | Acme Inc. | 699.6,126.9 72.9×21 | 700,126 72.9×24 | 0.4 | 0.6 | 0 | 3 |
| text | Login with Apple | 677.4,287.9 109.3×18 | 677,287 109.3×20 | -0.4 | 0.1 | 0 | 2 |
| box card | Welcome back Login with your Apple or Google account Login w | 528,173.9 384×534.3 | 528,174 384×534 | 0 | 0.1 | 0 | -0.3 |
| text | Or continue with | 667.2,383.9 105.7×18 | 667,383 105.6×20 | -0.2 | 0.1 | -0 | 2 |
| text | Forgot your password? | 740,519.1 147×18 | 740,518 147×20 | -0 | -0.1 | -0 | 2 |
| text | Login | 702,623.1 36×18 | 702,622 36×20 | 0 | -0.1 | -0 | 2 |
| box button | Login | 553,614.1 334×36 | 553,614 334×36 | 0 | -0.1 | 0 | 0 |
| text | Login with your Apple or Google account | 590.2,235.9 259.6×18 | 590.2,235 259.5×20 | 0 | 0.1 | -0 | 2 |
| text | Welcome back | 649.2,199.9 141.5×26 | 649.2,199 141.5×28 | 0 | 0.1 | -0 | 2 |
| text | Login with Google | 673.1,335.9 117.9×18 | 673,335 117.9×20 | -0 | 0.1 | 0 | 2 |

**Worst residuals inside containers** (1 matched containers; container Δ removed)

| Kind | Text | Container | Container Δx, Δy | Residual Δx | Residual Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Don't have an account? | Welcome back Login with your A… | 0, 0.1 | -0.4 | 0.3 | -3.5 | 3 |
| text | Password | Welcome back Login with your A… | 0, 0.1 | 0 | 1 | -0 | 1.3 |
| text | Sign up | Welcome back Login with your A… | 0, 0.1 | 0.7 | 0.3 | 0 | 3 |
| text | Email | Welcome back Login with your A… | 0, 0.1 | 0 | 0.6 | -0 | 1.3 |
| text | Login with Apple | Welcome back Login with your A… | 0, 0.1 | -0.4 | 0 | 0 | 2 |
| text | Forgot your password? | Welcome back Login with your A… | 0, 0.1 | -0 | -0.3 | -0 | 2 |
| text | Login | Welcome back Login with your A… | 0, 0.1 | 0 | -0.3 | -0 | 2 |
| box button | Login | Welcome back Login with your A… | 0, 0.1 | 0 | -0.3 | 0 | 0 |
| text | Or continue with | Welcome back Login with your A… | 0, 0.1 | -0.2 | 0 | -0 | 2 |
| text | Login with Google | Welcome back Login with your A… | 0, 0.1 | -0 | 0 | 0 | 2 |
| text | Welcome back | Welcome back Login with your A… | 0, 0.1 | 0 | 0 | -0 | 2 |
| text | Login with your Apple or Google account | Welcome back Login with your A… | 0, 0.1 | 0 | 0 | -0 | 2 |

### login-03 1024×768

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Don't have an account? | 410.9,597.1 153.5×18 | 410.5,596 150×21 | -0.4 | 0.4 | -3.5 | 3 |
| text | Password | 345,452.5 64×18 | 345,453 64×19.3 | 0 | 1.1 | -0 | 1.3 |
| text | Privacy Policy | 506.1,688.1 87.8×18 | 506.2,687 88.6×21 | 0.1 | 0.4 | 0.8 | 3 |
| text | Email | 345,356.9 36×18 | 345,357 36×19.3 | 0 | 0.7 | 0 | 1.3 |
| text | Sign up | 564.4,597.1 48.7×18 | 565.1,596 48.7×21 | 0.7 | 0.4 | 0 | 3 |
| text | Acme Inc. | 491.6,60.9 72.9×21 | 492,60 72.9×24 | 0.4 | 0.6 | 0 | 3 |
| text | Login with Apple | 469.4,221.9 109.3×18 | 469,221 109.3×20 | -0.4 | 0.1 | 0 | 2 |
| box card | Welcome back Login with your Apple or Google account Login w | 320,107.9 384×534.3 | 320,108 384×534 | 0 | 0.1 | 0 | -0.3 |
| text | Or continue with | 459.2,317.9 105.7×18 | 459,317 105.6×20 | -0.2 | 0.1 | -0 | 2 |
| text | Forgot your password? | 532,453.1 147×18 | 532,452 147×20 | -0 | -0.1 | -0 | 2 |
| text | Login | 494,557.1 36×18 | 494,556 36×20 | 0 | -0.1 | -0 | 2 |
| box button | Login | 345,548.1 334×36 | 345,548 334×36 | 0 | -0.1 | 0 | 0 |
| text | Login with your Apple or Google account | 382.2,169.9 259.6×18 | 382.2,169 259.5×20 | 0 | 0.1 | -0 | 2 |
| text | Welcome back | 441.2,133.9 141.5×26 | 441.2,133 141.5×28 | 0 | 0.1 | -0 | 2 |
| text | Login with Google | 465.1,269.9 117.9×18 | 465,269 117.9×20 | -0 | 0.1 | 0 | 2 |

**Worst residuals inside containers** (1 matched containers; container Δ removed)

| Kind | Text | Container | Container Δx, Δy | Residual Δx | Residual Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Don't have an account? | Welcome back Login with your A… | 0, 0.1 | -0.4 | 0.2 | -3.5 | 3 |
| text | Password | Welcome back Login with your A… | 0, 0.1 | 0 | 1 | -0 | 1.3 |
| text | Sign up | Welcome back Login with your A… | 0, 0.1 | 0.7 | 0.2 | 0 | 3 |
| text | Email | Welcome back Login with your A… | 0, 0.1 | 0 | 0.6 | 0 | 1.3 |
| text | Login with Apple | Welcome back Login with your A… | 0, 0.1 | -0.4 | -0 | 0 | 2 |
| text | Forgot your password? | Welcome back Login with your A… | 0, 0.1 | -0 | -0.3 | -0 | 2 |
| text | Login | Welcome back Login with your A… | 0, 0.1 | 0 | -0.3 | -0 | 2 |
| box button | Login | Welcome back Login with your A… | 0, 0.1 | 0 | -0.3 | 0 | 0 |
| text | Or continue with | Welcome back Login with your A… | 0, 0.1 | -0.2 | -0 | -0 | 2 |
| text | Login with Google | Welcome back Login with your A… | 0, 0.1 | -0 | -0 | 0 | 2 |
| text | Welcome back | Welcome back Login with your A… | 0, 0.1 | 0 | -0 | -0 | 2 |
| text | Login with your Apple or Google account | Welcome back Login with your A… | 0, 0.1 | 0 | -0 | -0 | 2 |

### login-04 1440×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Don't have an account? | 395.4,639.6 153.5×18 | 395.5,639 150×21 | 0.1 | 0.9 | -3.5 | 3 |
| text | Terms of Service | 727.3,717.6 106.4×18 | 727.3,717 106.4×21 | 0 | 0.9 | -0 | 3 |
| text | Sign up | 548.9,639.6 48.7×18 | 549.1,639 48.7×21 | 0.2 | 0.9 | 0 | 3 |
| text | Privacy Policy | 864.7,717.6 87.8×18 | 865.2,717 88.6×21 | 0.5 | 0.9 | 0.8 | 3 |
| box card | Welcome back Login to your Acme Inc account Email Password F | 272,162.4 896×530.3 | 272,162 896×531 | 0 | -0.4 | 0 | 0.8 |
| text | Password | 305,383 64×18 | 305,383 64×19.3 | 0 | 0.6 | -0 | 1.3 |
| text | Welcome back | 409.7,195.4 173.6×31 | 410.2,195 173.5×32 | 0.5 | 0.1 | -0 | 1 |
| text | Login | 478.5,487.6 36×18 | 478,487 36×20 | -0.5 | 0.4 | -0 | 2 |
| text | Forgot your password? | 541,383.6 147×18 | 541,383 147×20 | -0 | 0.4 | -0 | 2 |
| text | Or continue with | 443.7,535.6 105.7×18 | 444,535 105.6×20 | 0.3 | 0.4 | -0 | 2 |
| box button | Login | 305,478.6 383×36 | 305,479 383×36 | 0 | 0.4 | 0 | 0 |
| text | Email | 305,287.4 36×18 | 305,287 36×19.3 | 0 | 0.2 | 0 | 1.3 |
| text | Login to your Acme Inc account | 381,236.4 230.9×21 | 381,235 230.9×24 | 0 | 0.1 | -0 | 3 |

**Worst residuals inside containers** (1 matched containers; container Δ removed)

| Kind | Text | Container | Container Δx, Δy | Residual Δx | Residual Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Don't have an account? | Welcome back Login to your Acm… | 0, -0.4 | 0.1 | 1.3 | -3.5 | 3 |
| text | Sign up | Welcome back Login to your Acm… | 0, -0.4 | 0.2 | 1.3 | 0 | 3 |
| text | Password | Welcome back Login to your Acm… | 0, -0.4 | 0 | 1 | -0 | 1.3 |
| text | Forgot your password? | Welcome back Login to your Acm… | 0, -0.4 | -0 | 0.8 | -0 | 2 |
| text | Or continue with | Welcome back Login to your Acm… | 0, -0.4 | 0.3 | 0.8 | -0 | 2 |
| text | Login | Welcome back Login to your Acm… | 0, -0.4 | -0.5 | 0.8 | -0 | 2 |
| box button | Login | Welcome back Login to your Acm… | 0, -0.4 | 0 | 0.8 | 0 | 0 |
| text | Email | Welcome back Login to your Acm… | 0, -0.4 | 0 | 0.6 | 0 | 1.3 |
| text | Welcome back | Welcome back Login to your Acm… | 0, -0.4 | 0.5 | 0.5 | -0 | 1 |
| text | Login to your Acme Inc account | Welcome back Login to your Acm… | 0, -0.4 | 0 | 0.5 | -0 | 3 |

### login-04 1024×768

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Don't have an account? | 187.4,573.6 153.5×18 | 187.5,573 150×21 | 0.1 | 0.9 | -3.5 | 3 |
| text | Terms of Service | 519.3,651.6 106.4×18 | 519.3,651 106.4×21 | 0 | 0.9 | -0 | 3 |
| text | Sign up | 340.9,573.6 48.7×18 | 341.1,573 48.7×21 | 0.2 | 0.9 | 0 | 3 |
| text | Privacy Policy | 656.7,651.6 87.8×18 | 657.2,651 88.6×21 | 0.5 | 0.9 | 0.8 | 3 |
| box card | Welcome back Login to your Acme Inc account Email Password F | 64,96.4 896×530.3 | 64,96 896×531 | 0 | -0.4 | 0 | 0.8 |
| text | Password | 97,317 64×18 | 97,317 64×19.3 | 0 | 0.6 | -0 | 1.3 |
| text | Welcome back | 201.7,129.4 173.6×31 | 202.2,129 173.5×32 | 0.5 | 0.1 | -0 | 1 |
| text | Login | 270.5,421.6 36×18 | 270,421 36×20 | -0.5 | 0.4 | -0 | 2 |
| text | Forgot your password? | 333,317.6 147×18 | 333,317 147×20 | -0 | 0.4 | -0 | 2 |
| text | Or continue with | 235.7,469.6 105.7×18 | 236,469 105.6×20 | 0.3 | 0.4 | -0 | 2 |
| box button | Login | 97,412.6 383×36 | 97,413 383×36 | 0 | 0.4 | 0 | 0 |
| text | Email | 97,221.4 36×18 | 97,221 36×19.3 | 0 | 0.2 | 0 | 1.3 |
| text | Login to your Acme Inc account | 173,170.4 230.9×21 | 173,169 230.9×24 | 0 | 0.1 | -0 | 3 |

**Worst residuals inside containers** (1 matched containers; container Δ removed)

| Kind | Text | Container | Container Δx, Δy | Residual Δx | Residual Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Don't have an account? | Welcome back Login to your Acm… | 0, -0.4 | 0.1 | 1.2 | -3.5 | 3 |
| text | Sign up | Welcome back Login to your Acm… | 0, -0.4 | 0.2 | 1.2 | 0 | 3 |
| text | Password | Welcome back Login to your Acm… | 0, -0.4 | 0 | 1 | -0 | 1.3 |
| text | Forgot your password? | Welcome back Login to your Acm… | 0, -0.4 | -0 | 0.7 | -0 | 2 |
| text | Or continue with | Welcome back Login to your Acm… | 0, -0.4 | 0.3 | 0.7 | -0 | 2 |
| text | Login | Welcome back Login to your Acm… | 0, -0.4 | -0.5 | 0.7 | -0 | 2 |
| box button | Login | Welcome back Login to your Acm… | 0, -0.4 | 0 | 0.7 | 0 | 0 |
| text | Email | Welcome back Login to your Acm… | 0, -0.4 | 0 | 0.6 | 0 | 1.3 |
| text | Welcome back | Welcome back Login to your Acm… | 0, -0.4 | 0.5 | 0.5 | -0 | 1 |
| text | Login to your Acme Inc account | Welcome back Login to your Acm… | 0, -0.4 | 0 | 0.5 | -0 | 3 |

### signup-03 1440×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Already have an account? | 613.8,654.9 168.4×18 | 614.1,654 164.9×21 | 0.3 | 0.6 | -3.5 | 3 |
| text | Confirm Password | 728,477.6 119.6×18 | 728,478 119.6×19.3 | 0 | 1 | -0 | 1.3 |
| text | Password | 553,477.6 64×18 | 553,478 64×19.3 | 0 | 1 | -0 | 1.3 |
| text | Privacy Policy | 714.1,745.9 87.8×18 | 714.2,745 88.6×21 | 0.1 | 0.6 | 0.8 | 3 |
| text | Must be at least 8 characters long. | 553,557.9 220.2×18 | 553,557 220.2×21 | 0 | 0.6 | -0 | 3 |
| text | Sign in | 782.2,654.9 44×18 | 782,654 44×21 | -0.2 | 0.6 | -0 | 3 |
| text | Full Name | 553,287.1 65.7×18 | 553,287 65.7×19.3 | 0 | 0.5 | -0 | 1.3 |
| text | Acme Inc. | 699.6,135.1 72.9×21 | 700,134 72.9×24 | 0.4 | 0.4 | 0 | 3 |
| box card | Create your account Enter your email below to create your ac | 528,182.1 384×517.8 | 528,182 384×518 | 0 | -0.1 | 0 | 0.3 |
| text | Email | 553,382.4 36×18 | 553,382 36×19.3 | 0 | 0.2 | -0 | 1.3 |
| text | Enter your email below to create your account | 573,244.1 294×18 | 573,243 294×20 | 0 | -0.1 | -0 | 2 |
| text | Create your account | 622.8,208.1 194.4×26 | 622.8,207 194.3×28 | 0 | -0.1 | -0 | 2 |
| text | Create Account | 669.1,614.9 101.8×18 | 669,614 101.8×20 | -0.1 | 0.1 | 0 | 2 |
| box button | Create Account | 553,605.9 334×36 | 553,606 334×36 | 0 | 0.1 | 0 | 0 |

**Worst residuals inside containers** (1 matched containers; container Δ removed)

| Kind | Text | Container | Container Δx, Δy | Residual Δx | Residual Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Already have an account? | Create your account Enter your… | 0, -0.1 | 0.3 | 0.8 | -3.5 | 3 |
| text | Confirm Password | Create your account Enter your… | 0, -0.1 | 0 | 1.1 | -0 | 1.3 |
| text | Password | Create your account Enter your… | 0, -0.1 | 0 | 1.1 | -0 | 1.3 |
| text | Must be at least 8 characters long. | Create your account Enter your… | 0, -0.1 | 0 | 0.8 | -0 | 3 |
| text | Sign in | Create your account Enter your… | 0, -0.1 | -0.2 | 0.8 | -0 | 3 |
| text | Full Name | Create your account Enter your… | 0, -0.1 | 0 | 0.6 | -0 | 1.3 |
| text | Email | Create your account Enter your… | 0, -0.1 | 0 | 0.4 | -0 | 1.3 |
| text | Create Account | Create your account Enter your… | 0, -0.1 | -0.1 | 0.3 | 0 | 2 |
| box button | Create Account | Create your account Enter your… | 0, -0.1 | 0 | 0.3 | 0 | 0 |
| text | Create your account | Create your account Enter your… | 0, -0.1 | 0 | 0 | -0 | 2 |
| text | Enter your email below to create your account | Create your account Enter your… | 0, -0.1 | 0 | 0 | -0 | 2 |

### signup-03 1024×768

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Already have an account? | 405.8,588.9 168.4×18 | 406.1,588 164.9×21 | 0.3 | 0.6 | -3.5 | 3 |
| text | Confirm Password | 520,411.6 119.6×18 | 520,412 119.6×19.3 | 0 | 1 | -0 | 1.3 |
| text | Password | 345,411.6 64×18 | 345,412 64×19.3 | 0 | 1 | -0 | 1.3 |
| text | Privacy Policy | 506.1,679.9 87.8×18 | 506.2,679 88.6×21 | 0.1 | 0.6 | 0.8 | 3 |
| text | Must be at least 8 characters long. | 345,491.9 220.2×18 | 345,491 220.2×21 | 0 | 0.6 | -0 | 3 |
| text | Sign in | 574.2,588.9 44×18 | 574,588 44×21 | -0.2 | 0.6 | -0 | 3 |
| text | Full Name | 345,221.1 65.7×18 | 345,221 65.7×19.3 | 0 | 0.5 | -0 | 1.3 |
| text | Acme Inc. | 491.6,69.1 72.9×21 | 492,68 72.9×24 | 0.4 | 0.4 | 0 | 3 |
| box card | Create your account Enter your email below to create your ac | 320,116.1 384×517.8 | 320,116 384×518 | 0 | -0.1 | 0 | 0.3 |
| text | Email | 345,316.4 36×18 | 345,316 36×19.3 | 0 | 0.2 | 0 | 1.3 |
| text | Enter your email below to create your account | 365,178.1 294×18 | 365,177 294×20 | 0 | -0.1 | -0 | 2 |
| text | Create your account | 414.8,142.1 194.4×26 | 414.8,141 194.3×28 | 0 | -0.1 | -0 | 2 |
| text | Create Account | 461.1,548.9 101.8×18 | 461,548 101.8×20 | -0.1 | 0.1 | 0 | 2 |
| box button | Create Account | 345,539.9 334×36 | 345,540 334×36 | 0 | 0.1 | 0 | 0 |

**Worst residuals inside containers** (1 matched containers; container Δ removed)

| Kind | Text | Container | Container Δx, Δy | Residual Δx | Residual Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Already have an account? | Create your account Enter your… | 0, -0.1 | 0.3 | 0.7 | -3.5 | 3 |
| text | Confirm Password | Create your account Enter your… | 0, -0.1 | 0 | 1.1 | -0 | 1.3 |
| text | Password | Create your account Enter your… | 0, -0.1 | 0 | 1.1 | -0 | 1.3 |
| text | Must be at least 8 characters long. | Create your account Enter your… | 0, -0.1 | 0 | 0.7 | -0 | 3 |
| text | Sign in | Create your account Enter your… | 0, -0.1 | -0.2 | 0.7 | -0 | 3 |
| text | Full Name | Create your account Enter your… | 0, -0.1 | 0 | 0.6 | -0 | 1.3 |
| text | Email | Create your account Enter your… | 0, -0.1 | 0 | 0.4 | 0 | 1.3 |
| text | Create Account | Create your account Enter your… | 0, -0.1 | -0.1 | 0.2 | 0 | 2 |
| box button | Create Account | Create your account Enter your… | 0, -0.1 | 0 | 0.2 | 0 | 0 |
| text | Create your account | Create your account Enter your… | 0, -0.1 | 0 | -0 | -0 | 2 |
| text | Enter your email below to create your account | Create your account Enter your… | 0, -0.1 | 0 | -0 | -0 | 2 |

### signup-04 1440×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Already have an account? | 390.3,680.8 168.4×18 | 390.1,680 164.9×21 | -0.2 | 0.8 | -3.5 | 3 |
| text | Password | 305,391.5 64×18 | 305,392 64×19.3 | 0 | 1.1 | -0 | 1.3 |
| text | Confirm Password | 504.5,391.5 119.6×18 | 505,392 119.6×19.3 | 0.5 | 1.1 | -0 | 1.3 |
| text | We'll use this to contact you. We will not share your email  | 305,322.5 365.1×39 | 305,322 365.1×42 | 0 | 1 | 0 | 3 |
| text | Privacy Policy | 864.7,758.8 87.8×18 | 865.2,758 88.6×21 | 0.5 | 0.8 | 0.8 | 3 |
| text | Terms of Service | 727.3,758.8 106.4×18 | 727.3,758 106.4×21 | 0 | 0.8 | -0 | 3 |
| text | Must be at least 8 characters long. | 305,471.8 220.2×18 | 305,471 220.2×21 | 0 | 0.8 | -0 | 3 |
| text | Sign in | 558.7,680.8 44×18 | 559,680 44×21 | 0.3 | 0.8 | -0 | 3 |
| box card | Create your account Enter your email below to create your ac | 272,121.3 896×612.5 | 272,121 896×613 | 0 | -0.3 | 0 | 0.5 |
| text | Email | 305,242.3 36×18 | 305,242 36×19.3 | 0 | 0.4 | 0 | 1.3 |
| text | Create Account | 445.6,528.8 101.8×18 | 446,528 101.8×20 | 0.4 | 0.3 | 0 | 2 |
| text | Or continue with | 443.7,576.8 105.7×18 | 444,576 105.6×20 | 0.3 | 0.3 | -0 | 2 |
| text | Enter your email below to create your account | 349.5,195.3 294×18 | 349.5,194 294×20 | 0 | -0.3 | -0 | 2 |
| text | Create your account | 377.1,154.3 238.7×31 | 377.1,154 238.7×32 | -0 | 0.3 | 0 | 1 |
| box button | Create Account | 305,519.8 383×36 | 305,520 383×36 | 0 | 0.3 | 0 | 0 |

**Worst residuals inside containers** (1 matched containers; container Δ removed)

| Kind | Text | Container | Container Δx, Δy | Residual Δx | Residual Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Already have an account? | Create your account Enter your… | 0, -0.3 | -0.2 | 1 | -3.5 | 3 |
| text | Password | Create your account Enter your… | 0, -0.3 | 0 | 1.4 | -0 | 1.3 |
| text | Confirm Password | Create your account Enter your… | 0, -0.3 | 0.5 | 1.4 | -0 | 1.3 |
| text | We'll use this to contact you. We will not share your email  | Create your account Enter your… | 0, -0.3 | 0 | 1.3 | 0 | 3 |
| text | Must be at least 8 characters long. | Create your account Enter your… | 0, -0.3 | 0 | 1 | -0 | 3 |
| text | Sign in | Create your account Enter your… | 0, -0.3 | 0.3 | 1 | -0 | 3 |
| text | Email | Create your account Enter your… | 0, -0.3 | 0 | 0.6 | 0 | 1.3 |
| text | Create your account | Create your account Enter your… | 0, -0.3 | -0 | 0.5 | 0 | 1 |
| text | Or continue with | Create your account Enter your… | 0, -0.3 | 0.3 | 0.5 | -0 | 2 |
| text | Create Account | Create your account Enter your… | 0, -0.3 | 0.4 | 0.5 | 0 | 2 |
| box button | Create Account | Create your account Enter your… | 0, -0.3 | 0 | 0.5 | 0 | 0 |
| text | Enter your email below to create your account | Create your account Enter your… | 0, -0.3 | 0 | 0 | -0 | 2 |

### signup-04 1024×768

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Already have an account? | 182.3,614.8 168.4×18 | 182.1,614 164.9×21 | -0.2 | 0.8 | -3.5 | 3 |
| text | Password | 97,325.5 64×18 | 97,326 64×19.3 | 0 | 1.1 | -0 | 1.3 |
| text | Confirm Password | 296.5,325.5 119.6×18 | 297,326 119.6×19.3 | 0.5 | 1.1 | -0 | 1.3 |
| text | We'll use this to contact you. We will not share your email  | 97,256.5 365.1×39 | 97,256 365.1×42 | 0 | 1 | 0 | 3 |
| text | Privacy Policy | 656.7,692.8 87.8×18 | 657.2,692 88.6×21 | 0.5 | 0.8 | 0.8 | 3 |
| text | Terms of Service | 519.3,692.8 106.4×18 | 519.3,692 106.4×21 | 0 | 0.8 | -0 | 3 |
| text | Must be at least 8 characters long. | 97,405.8 220.2×18 | 97,405 220.2×21 | 0 | 0.8 | -0 | 3 |
| text | Sign in | 350.7,614.8 44×18 | 351,614 44×21 | 0.3 | 0.8 | -0 | 3 |
| box card | Create your account Enter your email below to create your ac | 64,55.3 896×612.5 | 64,55 896×613 | 0 | -0.3 | 0 | 0.5 |
| text | Email | 97,176.3 36×18 | 97,176 36×19.3 | 0 | 0.4 | 0 | 1.3 |
| text | Create Account | 237.6,462.8 101.8×18 | 238,462 101.8×20 | 0.4 | 0.3 | 0 | 2 |
| text | Or continue with | 235.7,510.8 105.7×18 | 236,510 105.6×20 | 0.3 | 0.3 | -0 | 2 |
| text | Enter your email below to create your account | 141.5,129.3 294×18 | 141.5,128 294×20 | 0 | -0.3 | -0 | 2 |
| text | Create your account | 169.1,88.3 238.7×31 | 169.1,88 238.7×32 | -0 | 0.3 | 0 | 1 |
| box button | Create Account | 97,453.8 383×36 | 97,454 383×36 | 0 | 0.3 | 0 | 0 |

**Worst residuals inside containers** (1 matched containers; container Δ removed)

| Kind | Text | Container | Container Δx, Δy | Residual Δx | Residual Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Already have an account? | Create your account Enter your… | 0, -0.3 | -0.2 | 1 | -3.5 | 3 |
| text | Password | Create your account Enter your… | 0, -0.3 | 0 | 1.4 | -0 | 1.3 |
| text | Confirm Password | Create your account Enter your… | 0, -0.3 | 0.5 | 1.4 | -0 | 1.3 |
| text | We'll use this to contact you. We will not share your email  | Create your account Enter your… | 0, -0.3 | 0 | 1.3 | 0 | 3 |
| text | Must be at least 8 characters long. | Create your account Enter your… | 0, -0.3 | 0 | 1 | -0 | 3 |
| text | Sign in | Create your account Enter your… | 0, -0.3 | 0.3 | 1 | -0 | 3 |
| text | Email | Create your account Enter your… | 0, -0.3 | 0 | 0.6 | 0 | 1.3 |
| text | Create your account | Create your account Enter your… | 0, -0.3 | -0 | 0.5 | 0 | 1 |
| text | Or continue with | Create your account Enter your… | 0, -0.3 | 0.3 | 0.5 | -0 | 2 |
| text | Create Account | Create your account Enter your… | 0, -0.3 | 0.4 | 0.5 | 0 | 2 |
| box button | Create Account | Create your account Enter your… | 0, -0.3 | 0 | 0.5 | 0 | 0 |
| text | Enter your email below to create your account | Create your account Enter your… | 0, -0.3 | 0 | 0 | -0 | 2 |

## States

Interaction states (hover, keyboard focus, press, click/open, type) from `tools/ShadcnRef/scenarios.json`, one clipped
region per state: the target box(es) and every floating layer that opened (menu, select list, tooltip), inflated by
24 px — or only the modal content when a dialog opened. Images live in `states/{reference,gotx,diff,compare}/<page>-<state>-<theme>.png`
(+ region layout maps `.json`). The region is computed on the reference; GotX is cropped with the SAME rect,
translated by the target's offset — median GotX − reference Δ of the matched elements inside the target (its label),
else the target / modal box when both are about the same size, else the text runs matched around it — rounded to whole px, 0 when ≤ 1 px per axis — so inherited page offsets and sub-pixel
layout snapping do not shift the crops; what remains is the control's own look. When GotX's own targets/overlays reach
> 2 px beyond that rect the row notes "GotX content extends beyond the reference frame". Both sides use real input: Edge via CDP `Input.dispatchMouseEvent` / `dispatchKeyEvent`
(Tab presses, never `element.focus()`, so `:focus-visible` is real) / `insertText`; GotX via `Window.DispatchInput`
(mouse move / down / up, `Key.Tab`, text input) on a virtual clock. Transitions: disabled by CSS on the reference
(end state immediately), run to their end on GotX; after each step both wait 1.2 s (hover: tooltip delay) or 0.4 s.
The reference viewport is the full page height so nothing scrolls; GotX uses the same window size.

- Capture references: `node tools/ShadcnRef/states.mjs [--only home] [--state get-started-hover] [--theme light]`
- GotX + comparison: part of `--screenshots` (`--state a,b` filters, `--no-states` skips)

**Adding scenarios (e.g. a block):** add a `pages[]` entry to `scenarios.json` with `page` (the `SitePage` id), `url`
(`https://ui.shadcn.com/view/new-york-v4/<id>`), `hideHeader: false`, `width`, `height` (the block viewport, e.g.
1440 × 900), `themes`, a `targets` map and `states`. A target has a reference side `ref: { selector, text? | prefix?, index? }`
(CSS selector, preferably `[data-slot=…]`; `text` matches the element's innerText, else aria-label, else placeholder,
case-insensitive) and a GotX side `gotx: { key } | { key, pick: n } | { key, next: n } | { text, index? }` (`pick` = n-th
clickable/focusable node inside the keyed node, `next` = n-th one after it, `text` = text node → nearest
clickable/focusable ancestor). States: `{ "target": "t", "do": ["hover", "focus-key", "press", "click"] }` makes
`t-hover` …; multi-step: `{ "name": "n", "steps": [{ "target": "a", "do": "click" }, { "target": "b", "do": "hover" }] }`;
typing: `{ "target": "t", "do": "type", "text": "…" }`. Then run states.mjs for the page and `--screenshots`; the
fidelity test picks the new states up automatically (first run records their baseline).
| State | Theme | Size ref → GotX | Pixel diff (tol 0 / 24) | Overlays ref / GotX | Layout matched (≤2 px) | Median Δ | Notes | Images |
|---|---|---|---|---|---|---|---|---|
| home / hero-badge-hover | light | 278×70 → 278×70 | 6.6 % / 4.8 % | 0 / 0 | 4 (4) | 0.5 |  | [ref](states/reference/home-hero-badge-hover-light.png) · [gotx](states/gotx/home-hero-badge-hover-light.png) · [compare](states/compare/home-hero-badge-hover-light.png) |
| home / hero-badge-hover | dark | 278×70 → 278×70 | 6.9 % / 5.3 % | 0 / 0 | 4 (4) | 0.5 |  | [ref](states/reference/home-hero-badge-hover-dark.png) · [gotx](states/gotx/home-hero-badge-hover-dark.png) · [compare](states/compare/home-hero-badge-hover-dark.png) |
| home / hero-badge-focus-key | light | 278×70 → 278×70 | 10.5 % / 7.7 % | 0 / 0 | 4 (4) | 0.5 |  | [ref](states/reference/home-hero-badge-focus-key-light.png) · [gotx](states/gotx/home-hero-badge-focus-key-light.png) · [compare](states/compare/home-hero-badge-focus-key-light.png) |
| home / hero-badge-focus-key | dark | 278×70 → 278×70 | 12.6 % / 8.3 % | 0 / 0 | 4 (4) | 0.5 |  | [ref](states/reference/home-hero-badge-focus-key-dark.png) · [gotx](states/gotx/home-hero-badge-focus-key-dark.png) · [compare](states/compare/home-hero-badge-focus-key-dark.png) |
| home / get-started-hover | light | 151×84 → 151×84 | 5.7 % / 4.1 % | 0 / 0 | 5 (5) | 0.4 |  | [ref](states/reference/home-get-started-hover-light.png) · [gotx](states/gotx/home-get-started-hover-light.png) · [compare](states/compare/home-get-started-hover-light.png) |
| home / get-started-hover | dark | 151×84 → 151×84 | 5.8 % / 3.5 % | 0 / 0 | 5 (5) | 0.4 |  | [ref](states/reference/home-get-started-hover-dark.png) · [gotx](states/gotx/home-get-started-hover-dark.png) · [compare](states/compare/home-get-started-hover-dark.png) |
| home / get-started-focus-key | light | 151×84 → 151×84 | 7.9 % / 4.6 % | 0 / 0 | 5 (5) | 0.4 |  | [ref](states/reference/home-get-started-focus-key-light.png) · [gotx](states/gotx/home-get-started-focus-key-light.png) · [compare](states/compare/home-get-started-focus-key-light.png) |
| home / get-started-focus-key | dark | 151×84 → 151×84 | 12.8 % / 4.3 % | 0 / 0 | 5 (5) | 0.4 |  | [ref](states/reference/home-get-started-focus-key-dark.png) · [gotx](states/gotx/home-get-started-focus-key-dark.png) · [compare](states/compare/home-get-started-focus-key-dark.png) |
| home / get-started-press | light | 151×84 → 151×84 | 5.7 % / 4.1 % | 0 / 0 | 5 (5) | 0.4 |  | [ref](states/reference/home-get-started-press-light.png) · [gotx](states/gotx/home-get-started-press-light.png) · [compare](states/compare/home-get-started-press-light.png) |
| home / get-started-press | dark | 151×84 → 151×84 | 5.8 % / 3.5 % | 0 / 0 | 5 (5) | 0.4 |  | [ref](states/reference/home-get-started-press-dark.png) · [gotx](states/gotx/home-get-started-press-dark.png) · [compare](states/compare/home-get-started-press-dark.png) |
| home / view-components-hover | light | 194×85 → 194×85 | 6.2 % / 4.2 % | 0 / 0 | 6 (6) | 0.4 |  | [ref](states/reference/home-view-components-hover-light.png) · [gotx](states/gotx/home-view-components-hover-light.png) · [compare](states/compare/home-view-components-hover-light.png) |
| home / view-components-hover | dark | 194×85 → 194×85 | 6.5 % / 4.5 % | 0 / 0 | 6 (6) | 0.4 |  | [ref](states/reference/home-view-components-hover-dark.png) · [gotx](states/gotx/home-view-components-hover-dark.png) · [compare](states/compare/home-view-components-hover-dark.png) |
| home / view-components-focus-key | light | 194×85 → 194×85 | 8.0 % / 4.8 % | 0 / 0 | 6 (6) | 0.4 |  | [ref](states/reference/home-view-components-focus-key-light.png) · [gotx](states/gotx/home-view-components-focus-key-light.png) · [compare](states/compare/home-view-components-focus-key-light.png) |
| home / view-components-focus-key | dark | 194×85 → 194×85 | 13.3 % / 5.0 % | 0 / 0 | 6 (6) | 0.4 |  | [ref](states/reference/home-view-components-focus-key-dark.png) · [gotx](states/gotx/home-view-components-focus-key-dark.png) · [compare](states/compare/home-view-components-focus-key-dark.png) |
| home / view-components-press | light | 194×85 → 194×85 | 6.2 % / 4.2 % | 0 / 0 | 6 (6) | 0.4 |  | [ref](states/reference/home-view-components-press-light.png) · [gotx](states/gotx/home-view-components-press-light.png) · [compare](states/compare/home-view-components-press-light.png) |
| home / view-components-press | dark | 194×85 → 194×85 | 6.5 % / 4.5 % | 0 / 0 | 6 (6) | 0.4 |  | [ref](states/reference/home-view-components-press-dark.png) · [gotx](states/gotx/home-view-components-press-dark.png) · [compare](states/compare/home-view-components-press-dark.png) |
| home / button-default-hover | light | 138×81 → 138×81 | 8.4 % / 2.7 % | 0 / 0 | 7 (7) | 0.2 |  | [ref](states/reference/home-button-default-hover-light.png) · [gotx](states/gotx/home-button-default-hover-light.png) · [compare](states/compare/home-button-default-hover-light.png) |
| home / button-default-hover | dark | 138×81 → 138×81 | 26.9 % / 2.3 % | 0 / 0 | 7 (7) | 0.2 |  | [ref](states/reference/home-button-default-hover-dark.png) · [gotx](states/gotx/home-button-default-hover-dark.png) · [compare](states/compare/home-button-default-hover-dark.png) |
| home / button-default-focus-key | light | 138×81 → 138×81 | 10.3 % / 2.9 % | 0 / 0 | 7 (7) | 0.2 |  | [ref](states/reference/home-button-default-focus-key-light.png) · [gotx](states/gotx/home-button-default-focus-key-light.png) · [compare](states/compare/home-button-default-focus-key-light.png) |
| home / button-default-focus-key | dark | 138×81 → 138×81 | 10.9 % / 2.6 % | 0 / 0 | 7 (7) | 0.2 |  | [ref](states/reference/home-button-default-focus-key-dark.png) · [gotx](states/gotx/home-button-default-focus-key-dark.png) · [compare](states/compare/home-button-default-focus-key-dark.png) |
| home / button-default-press | light | 138×81 → 138×81 | 8.4 % / 2.7 % | 0 / 0 | 7 (7) | 0.2 |  | [ref](states/reference/home-button-default-press-light.png) · [gotx](states/gotx/home-button-default-press-light.png) · [compare](states/compare/home-button-default-press-light.png) |
| home / button-default-press | dark | 138×81 → 138×81 | 26.9 % / 2.3 % | 0 / 0 | 7 (7) | 0.2 |  | [ref](states/reference/home-button-default-press-dark.png) · [gotx](states/gotx/home-button-default-press-dark.png) · [compare](states/compare/home-button-default-press-dark.png) |
| home / button-secondary-hover | light | 146×81 → 146×81 | 8.7 % / 3.5 % | 0 / 0 | 8 (8) | 0.3 |  | [ref](states/reference/home-button-secondary-hover-light.png) · [gotx](states/gotx/home-button-secondary-hover-light.png) · [compare](states/compare/home-button-secondary-hover-light.png) |
| home / button-secondary-hover | dark | 146×81 → 146×81 | 9.6 % / 3.9 % | 0 / 0 | 8 (8) | 0.3 |  | [ref](states/reference/home-button-secondary-hover-dark.png) · [gotx](states/gotx/home-button-secondary-hover-dark.png) · [compare](states/compare/home-button-secondary-hover-dark.png) |
| home / button-secondary-focus-key | light | 146×81 → 146×81 | 11.0 % / 4.4 % | 0 / 0 | 8 (8) | 0.3 |  | [ref](states/reference/home-button-secondary-focus-key-light.png) · [gotx](states/gotx/home-button-secondary-focus-key-light.png) · [compare](states/compare/home-button-secondary-focus-key-light.png) |
| home / button-secondary-focus-key | dark | 146×81 → 146×81 | 12.0 % / 4.8 % | 0 / 0 | 8 (8) | 0.3 |  | [ref](states/reference/home-button-secondary-focus-key-dark.png) · [gotx](states/gotx/home-button-secondary-focus-key-dark.png) · [compare](states/compare/home-button-secondary-focus-key-dark.png) |
| home / button-secondary-press | light | 146×81 → 146×81 | 8.7 % / 3.5 % | 0 / 0 | 8 (8) | 0.3 |  | [ref](states/reference/home-button-secondary-press-light.png) · [gotx](states/gotx/home-button-secondary-press-light.png) · [compare](states/compare/home-button-secondary-press-light.png) |
| home / button-secondary-press | dark | 146×81 → 146×81 | 9.6 % / 3.9 % | 0 / 0 | 8 (8) | 0.3 |  | [ref](states/reference/home-button-secondary-press-dark.png) · [gotx](states/gotx/home-button-secondary-press-dark.png) · [compare](states/compare/home-button-secondary-press-dark.png) |
| home / button-outline-hover | light | 122×81 → 122×81 | 9.5 % / 3.9 % | 0 / 0 | 7 (7) | 0.3 |  | [ref](states/reference/home-button-outline-hover-light.png) · [gotx](states/gotx/home-button-outline-hover-light.png) · [compare](states/compare/home-button-outline-hover-light.png) |
| home / button-outline-hover | dark | 122×81 → 122×81 | 11.0 % / 4.0 % | 0 / 0 | 7 (7) | 0.3 |  | [ref](states/reference/home-button-outline-hover-dark.png) · [gotx](states/gotx/home-button-outline-hover-dark.png) · [compare](states/compare/home-button-outline-hover-dark.png) |
| home / button-outline-focus-key | light | 122×81 → 122×81 | 11.7 % / 5.4 % | 0 / 0 | 7 (7) | 0.3 |  | [ref](states/reference/home-button-outline-focus-key-light.png) · [gotx](states/gotx/home-button-outline-focus-key-light.png) · [compare](states/compare/home-button-outline-focus-key-light.png) |
| home / button-outline-focus-key | dark | 122×81 → 122×81 | 12.4 % / 5.5 % | 0 / 0 | 7 (7) | 0.3 |  | [ref](states/reference/home-button-outline-focus-key-dark.png) · [gotx](states/gotx/home-button-outline-focus-key-dark.png) · [compare](states/compare/home-button-outline-focus-key-dark.png) |
| home / button-outline-press | light | 122×81 → 122×81 | 9.5 % / 3.9 % | 0 / 0 | 7 (7) | 0.3 |  | [ref](states/reference/home-button-outline-press-light.png) · [gotx](states/gotx/home-button-outline-press-light.png) · [compare](states/compare/home-button-outline-press-light.png) |
| home / button-outline-press | dark | 122×81 → 122×81 | 11.0 % / 4.0 % | 0 / 0 | 7 (7) | 0.3 |  | [ref](states/reference/home-button-outline-press-dark.png) · [gotx](states/gotx/home-button-outline-press-dark.png) · [compare](states/compare/home-button-outline-press-dark.png) |
| home / input-name-hover | light | 312×81 → 312×81 | 2.0 % / 0.7 % | 0 / 0 | 6 (6) | 0.2 |  | [ref](states/reference/home-input-name-hover-light.png) · [gotx](states/gotx/home-input-name-hover-light.png) · [compare](states/compare/home-input-name-hover-light.png) |
| home / input-name-hover | dark | 312×81 → 312×81 | 2.6 % / 0.7 % | 0 / 0 | 6 (6) | 0.2 |  | [ref](states/reference/home-input-name-hover-dark.png) · [gotx](states/gotx/home-input-name-hover-dark.png) · [compare](states/compare/home-input-name-hover-dark.png) |
| home / input-name-focus-key | light | 312×81 → 312×81 | 2.9 % / 0.8 % | 0 / 0 | 6 (6) | 0.2 |  | [ref](states/reference/home-input-name-focus-key-light.png) · [gotx](states/gotx/home-input-name-focus-key-light.png) · [compare](states/compare/home-input-name-focus-key-light.png) |
| home / input-name-focus-key | dark | 312×81 → 312×81 | 3.5 % / 0.9 % | 0 / 0 | 6 (6) | 0.2 |  | [ref](states/reference/home-input-name-focus-key-dark.png) · [gotx](states/gotx/home-input-name-focus-key-dark.png) · [compare](states/compare/home-input-name-focus-key-dark.png) |
| home / input-name-type | light | 312×81 → 312×81 | 4.1 % / 1.9 % | 0 / 0 | 5 (4) | 0.7 |  | [ref](states/reference/home-input-name-type-light.png) · [gotx](states/gotx/home-input-name-type-light.png) · [compare](states/compare/home-input-name-type-light.png) |
| home / input-name-type | dark | 312×81 → 312×81 | 4.8 % / 2.2 % | 0 / 0 | 5 (4) | 0.7 |  | [ref](states/reference/home-input-name-type-dark.png) · [gotx](states/gotx/home-input-name-type-dark.png) · [compare](states/compare/home-input-name-type-dark.png) |
| home / textarea-message-hover | light | 338×113 → 338×113 | 2.4 % / 0.7 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-textarea-message-hover-light.png) · [gotx](states/gotx/home-textarea-message-hover-light.png) · [compare](states/compare/home-textarea-message-hover-light.png) |
| home / textarea-message-hover | dark | 338×113 → 338×113 | 3.1 % / 0.7 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-textarea-message-hover-dark.png) · [gotx](states/gotx/home-textarea-message-hover-dark.png) · [compare](states/compare/home-textarea-message-hover-dark.png) |
| home / textarea-message-focus-key | light | 338×113 → 338×113 | 3.2 % / 0.7 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-textarea-message-focus-key-light.png) · [gotx](states/gotx/home-textarea-message-focus-key-light.png) · [compare](states/compare/home-textarea-message-focus-key-light.png) |
| home / textarea-message-focus-key | dark | 338×113 → 338×113 | 3.9 % / 0.7 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-textarea-message-focus-key-dark.png) · [gotx](states/gotx/home-textarea-message-focus-key-dark.png) · [compare](states/compare/home-textarea-message-focus-key-dark.png) |
| home / radio-first-hover | light | 64×65 → 64×65 | 2.7 % / 0.3 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-radio-first-hover-light.png) · [gotx](states/gotx/home-radio-first-hover-light.png) · [compare](states/compare/home-radio-first-hover-light.png) |
| home / radio-first-hover | dark | 64×65 → 64×65 | 2.7 % / 0.3 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-radio-first-hover-dark.png) · [gotx](states/gotx/home-radio-first-hover-dark.png) · [compare](states/compare/home-radio-first-hover-dark.png) |
| home / radio-first-focus-key | light | 64×65 → 64×65 | 4.9 % / 0.1 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-radio-first-focus-key-light.png) · [gotx](states/gotx/home-radio-first-focus-key-light.png) · [compare](states/compare/home-radio-first-focus-key-light.png) |
| home / radio-first-focus-key | dark | 64×65 → 64×65 | 5.1 % / 0.1 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-radio-first-focus-key-dark.png) · [gotx](states/gotx/home-radio-first-focus-key-dark.png) · [compare](states/compare/home-radio-first-focus-key-dark.png) |
| home / radio-second-click | light | 64×65 → 64×65 | 4.3 % / 0.6 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-radio-second-click-light.png) · [gotx](states/gotx/home-radio-second-click-light.png) · [compare](states/compare/home-radio-second-click-light.png) |
| home / radio-second-click | dark | 64×65 → 64×65 | 4.3 % / 0.6 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-radio-second-click-dark.png) · [gotx](states/gotx/home-radio-second-click-dark.png) · [compare](states/compare/home-radio-second-click-dark.png) |
| home / checkbox-hover | light | 64×65 → 64×65 | 4.8 % / 0.8 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-checkbox-hover-light.png) · [gotx](states/gotx/home-checkbox-hover-light.png) · [compare](states/compare/home-checkbox-hover-light.png) |
| home / checkbox-hover | dark | 64×65 → 64×65 | 4.7 % / 0.7 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-checkbox-hover-dark.png) · [gotx](states/gotx/home-checkbox-hover-dark.png) · [compare](states/compare/home-checkbox-hover-dark.png) |
| home / checkbox-focus-key | light | 64×65 → 64×65 | 6.7 % / 0.6 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-checkbox-focus-key-light.png) · [gotx](states/gotx/home-checkbox-focus-key-light.png) · [compare](states/compare/home-checkbox-focus-key-light.png) |
| home / checkbox-focus-key | dark | 64×65 → 64×65 | 6.7 % / 0.5 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-checkbox-focus-key-dark.png) · [gotx](states/gotx/home-checkbox-focus-key-dark.png) · [compare](states/compare/home-checkbox-focus-key-dark.png) |
| home / checkbox-click | light | 64×65 → 64×65 | 3.8 % / 0.5 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-checkbox-click-light.png) · [gotx](states/gotx/home-checkbox-click-light.png) · [compare](states/compare/home-checkbox-click-light.png) |
| home / checkbox-click | dark | 64×65 → 64×65 | 3.8 % / 0.5 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-checkbox-click-dark.png) · [gotx](states/gotx/home-checkbox-click-dark.png) · [compare](states/compare/home-checkbox-click-dark.png) |
| home / switch-hover | light | 80×69 → 80×69 | 8.2 % / 1.1 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-switch-hover-light.png) · [gotx](states/gotx/home-switch-hover-light.png) · [compare](states/compare/home-switch-hover-light.png) |
| home / switch-hover | dark | 80×69 → 80×69 | 8.9 % / 0.9 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-switch-hover-dark.png) · [gotx](states/gotx/home-switch-hover-dark.png) · [compare](states/compare/home-switch-hover-dark.png) |
| home / switch-focus-key | light | 80×69 → 80×69 | 9.4 % / 0.3 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-switch-focus-key-light.png) · [gotx](states/gotx/home-switch-focus-key-light.png) · [compare](states/compare/home-switch-focus-key-light.png) |
| home / switch-focus-key | dark | 80×69 → 80×69 | 9.9 % / 0.3 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-switch-focus-key-dark.png) · [gotx](states/gotx/home-switch-focus-key-dark.png) · [compare](states/compare/home-switch-focus-key-dark.png) |
| home / switch-click | light | 80×69 → 80×69 | 8.0 % / 0.2 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-switch-click-light.png) · [gotx](states/gotx/home-switch-click-light.png) · [compare](states/compare/home-switch-click-light.png) |
| home / switch-click | dark | 80×69 → 80×69 | 7.8 % / 0.3 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-switch-click-dark.png) · [gotx](states/gotx/home-switch-click-dark.png) · [compare](states/compare/home-switch-click-dark.png) |
| home / alert-dialog-hover | light | 151×81 → 151×81 | 11.4 % / 3.7 % | 0 / 0 | 7 (7) | 0.2 |  | [ref](states/reference/home-alert-dialog-hover-light.png) · [gotx](states/gotx/home-alert-dialog-hover-light.png) · [compare](states/compare/home-alert-dialog-hover-light.png) |
| home / alert-dialog-hover | dark | 151×81 → 151×81 | 13.6 % / 3.9 % | 0 / 0 | 7 (7) | 0.2 |  | [ref](states/reference/home-alert-dialog-hover-dark.png) · [gotx](states/gotx/home-alert-dialog-hover-dark.png) · [compare](states/compare/home-alert-dialog-hover-dark.png) |
| home / alert-dialog-focus-key | light | 151×81 → 151×81 | 12.4 % / 3.9 % | 0 / 0 | 7 (7) | 0.2 |  | [ref](states/reference/home-alert-dialog-focus-key-light.png) · [gotx](states/gotx/home-alert-dialog-focus-key-light.png) · [compare](states/compare/home-alert-dialog-focus-key-light.png) |
| home / alert-dialog-focus-key | dark | 151×81 → 151×81 | 13.8 % / 4.1 % | 0 / 0 | 7 (7) | 0.2 |  | [ref](states/reference/home-alert-dialog-focus-key-dark.png) · [gotx](states/gotx/home-alert-dialog-focus-key-dark.png) · [compare](states/compare/home-alert-dialog-focus-key-dark.png) |
| home / alert-dialog-click | light | 368×227 → 368×227 | 37.4 % / 12.6 % | 1 / 1 | 24 (24) | 0.4 |  | [ref](states/reference/home-alert-dialog-click-light.png) · [gotx](states/gotx/home-alert-dialog-click-light.png) · [compare](states/compare/home-alert-dialog-click-light.png) |
| home / alert-dialog-click | dark | 368×227 → 368×227 | 37.9 % / 12.9 % | 1 / 1 | 24 (24) | 0.4 |  | [ref](states/reference/home-alert-dialog-click-dark.png) · [gotx](states/gotx/home-alert-dialog-click-dark.png) · [compare](states/compare/home-alert-dialog-click-dark.png) |
| home / group-button-hover | light | 161×81 → 161×81 | 9.5 % / 3.8 % | 0 / 0 | 6 (6) | 0.2 |  | [ref](states/reference/home-group-button-hover-light.png) · [gotx](states/gotx/home-group-button-hover-light.png) · [compare](states/compare/home-group-button-hover-light.png) |
| home / group-button-hover | dark | 161×81 → 161×81 | 12.6 % / 4.1 % | 0 / 0 | 6 (6) | 0.2 |  | [ref](states/reference/home-group-button-hover-dark.png) · [gotx](states/gotx/home-group-button-hover-dark.png) · [compare](states/compare/home-group-button-hover-dark.png) |
| home / group-menu-hover | light | 80×81 → 80×81 | 10.0 % / 1.2 % | 0 / 0 | 5 (5) | 0.2 |  | [ref](states/reference/home-group-menu-hover-light.png) · [gotx](states/gotx/home-group-menu-hover-light.png) · [compare](states/compare/home-group-menu-hover-light.png) |
| home / group-menu-hover | dark | 80×81 → 80×81 | 11.1 % / 1.3 % | 0 / 0 | 5 (5) | 0.2 |  | [ref](states/reference/home-group-menu-hover-dark.png) · [gotx](states/gotx/home-group-menu-hover-dark.png) · [compare](states/compare/home-group-menu-hover-dark.png) |
| home / group-menu-focus-key | light | 80×81 → 80×81 | 11.2 % / 1.2 % | 0 / 0 | 5 (5) | 0.2 |  | [ref](states/reference/home-group-menu-focus-key-light.png) · [gotx](states/gotx/home-group-menu-focus-key-light.png) · [compare](states/compare/home-group-menu-focus-key-light.png) |
| home / group-menu-focus-key | dark | 80×81 → 80×81 | 12.9 % / 1.3 % | 0 / 0 | 5 (5) | 0.2 |  | [ref](states/reference/home-group-menu-focus-key-dark.png) · [gotx](states/gotx/home-group-menu-focus-key-dark.png) · [compare](states/compare/home-group-menu-focus-key-dark.png) |
| home / group-menu-click | light | 208×253 → 208×253 | 18.0 % / 5.8 % | 1 / 1 | 13 (13) | 0 |  | [ref](states/reference/home-group-menu-click-light.png) · [gotx](states/gotx/home-group-menu-click-light.png) · [compare](states/compare/home-group-menu-click-light.png) |
| home / group-menu-click | dark | 208×253 → 208×253 | 19.8 % / 6.4 % | 1 / 1 | 13 (13) | 0 |  | [ref](states/reference/home-group-menu-click-dark.png) · [gotx](states/gotx/home-group-menu-click-dark.png) · [compare](states/compare/home-group-menu-click-dark.png) |
| home / sidebar-active-hover | light | 185×81 → 185×81 | 12.0 % / 7.3 % | 0 / 0 | 8 (8) | 0.2 |  | [ref](states/reference/home-sidebar-active-hover-light.png) · [gotx](states/gotx/home-sidebar-active-hover-light.png) · [compare](states/compare/home-sidebar-active-hover-light.png) |
| home / sidebar-active-hover | dark | 185×81 → 185×81 | 13.5 % / 7.7 % | 0 / 0 | 8 (8) | 0.2 |  | [ref](states/reference/home-sidebar-active-hover-dark.png) · [gotx](states/gotx/home-sidebar-active-hover-dark.png) · [compare](states/compare/home-sidebar-active-hover-dark.png) |
| home / sidebar-item-hover | light | 185×81 → 185×81 | 14.1 % / 9.1 % | 0 / 0 | 9 (9) | 0.2 |  | [ref](states/reference/home-sidebar-item-hover-light.png) · [gotx](states/gotx/home-sidebar-item-hover-light.png) · [compare](states/compare/home-sidebar-item-hover-light.png) |
| home / sidebar-item-hover | dark | 185×81 → 185×81 | 16.0 % / 9.9 % | 0 / 0 | 9 (9) | 0.2 |  | [ref](states/reference/home-sidebar-item-hover-dark.png) · [gotx](states/gotx/home-sidebar-item-hover-dark.png) · [compare](states/compare/home-sidebar-item-hover-dark.png) |
| home / sidebar-item-focus-key | light | 185×81 → 185×81 | 16.9 % / 11.1 % | 0 / 0 | 9 (9) | 0.2 |  | [ref](states/reference/home-sidebar-item-focus-key-light.png) · [gotx](states/gotx/home-sidebar-item-focus-key-light.png) · [compare](states/compare/home-sidebar-item-focus-key-light.png) |
| home / sidebar-item-focus-key | dark | 185×81 → 185×81 | 18.5 % / 12.0 % | 0 / 0 | 9 (9) | 0.2 |  | [ref](states/reference/home-sidebar-item-focus-key-dark.png) · [gotx](states/gotx/home-sidebar-item-focus-key-dark.png) · [compare](states/compare/home-sidebar-item-focus-key-dark.png) |
| home / sidebar-item-press | light | 185×81 → 185×81 | 14.1 % / 9.1 % | 0 / 0 | 9 (9) | 0.2 |  | [ref](states/reference/home-sidebar-item-press-light.png) · [gotx](states/gotx/home-sidebar-item-press-light.png) · [compare](states/compare/home-sidebar-item-press-light.png) |
| home / sidebar-item-press | dark | 185×81 → 185×81 | 16.0 % / 9.9 % | 0 / 0 | 9 (9) | 0.2 |  | [ref](states/reference/home-sidebar-item-press-dark.png) · [gotx](states/gotx/home-sidebar-item-press-dark.png) · [compare](states/compare/home-sidebar-item-press-dark.png) |
| home / sidebar-item-click | light | 185×81 → 185×81 | 14.1 % / 9.1 % | 0 / 0 | 9 (9) | 0.2 |  | [ref](states/reference/home-sidebar-item-click-light.png) · [gotx](states/gotx/home-sidebar-item-click-light.png) · [compare](states/compare/home-sidebar-item-click-light.png) |
| home / sidebar-item-click | dark | 185×81 → 185×81 | 16.0 % / 9.9 % | 0 / 0 | 9 (9) | 0.2 |  | [ref](states/reference/home-sidebar-item-click-dark.png) · [gotx](states/gotx/home-sidebar-item-click-dark.png) · [compare](states/compare/home-sidebar-item-click-dark.png) |
| home / select-hover | light | 338×81 → 338×81 | 8.4 % / 5.7 % | 0 / 0 | 6 (6) | 0.3 |  | [ref](states/reference/home-select-hover-light.png) · [gotx](states/gotx/home-select-hover-light.png) · [compare](states/compare/home-select-hover-light.png) |
| home / select-hover | dark | 338×81 → 338×81 | 9.1 % / 6.1 % | 0 / 0 | 6 (6) | 0.3 |  | [ref](states/reference/home-select-hover-dark.png) · [gotx](states/gotx/home-select-hover-dark.png) · [compare](states/compare/home-select-hover-dark.png) |
| home / select-focus-key | light | 338×81 → 338×81 | 9.3 % / 5.8 % | 0 / 0 | 6 (6) | 0.3 |  | [ref](states/reference/home-select-focus-key-light.png) · [gotx](states/gotx/home-select-focus-key-light.png) · [compare](states/compare/home-select-focus-key-light.png) |
| home / select-focus-key | dark | 338×81 → 338×81 | 10.0 % / 6.2 % | 0 / 0 | 6 (6) | 0.3 |  | [ref](states/reference/home-select-focus-key-dark.png) · [gotx](states/gotx/home-select-focus-key-dark.png) · [compare](states/compare/home-select-focus-key-dark.png) |
| home / select-click | light | 339×185 → 339×185 | 17.8 % / 5.5 % | 1 / 1 | 16 (15) | 0.5 |  | [ref](states/reference/home-select-click-light.png) · [gotx](states/gotx/home-select-click-light.png) · [compare](states/compare/home-select-click-light.png) |
| home / select-click | dark | 339×185 → 339×185 | 18.7 % / 6.5 % | 1 / 1 | 16 (15) | 0.5 |  | [ref](states/reference/home-select-click-dark.png) · [gotx](states/gotx/home-select-click-dark.png) · [compare](states/compare/home-select-click-dark.png) |
| home / select-open-item-hover | light | 339×185 → 339×185 | 17.8 % / 5.2 % | 1 / 1 | 16 (15) | 0.5 |  | [ref](states/reference/home-select-open-item-hover-light.png) · [gotx](states/gotx/home-select-open-item-hover-light.png) · [compare](states/compare/home-select-open-item-hover-light.png) |
| home / select-open-item-hover | dark | 339×185 → 339×185 | 18.7 % / 6.3 % | 1 / 1 | 16 (15) | 0.5 |  | [ref](states/reference/home-select-open-item-hover-dark.png) · [gotx](states/gotx/home-select-open-item-hover-dark.png) · [compare](states/compare/home-select-open-item-hover-dark.png) |
| home / breadcrumb-home-hover | light | 86×69 → 86×69 | 8.6 % / 3.0 % | 0 / 0 | 4 (4) | 0.2 |  | [ref](states/reference/home-breadcrumb-home-hover-light.png) · [gotx](states/gotx/home-breadcrumb-home-hover-light.png) · [compare](states/compare/home-breadcrumb-home-hover-light.png) |
| home / breadcrumb-home-hover | dark | 86×69 → 86×69 | 8.9 % / 3.7 % | 0 / 0 | 4 (4) | 0.2 |  | [ref](states/reference/home-breadcrumb-home-hover-dark.png) · [gotx](states/gotx/home-breadcrumb-home-hover-dark.png) · [compare](states/compare/home-breadcrumb-home-hover-dark.png) |
| home / breadcrumb-home-focus-key | light | 86×69 → 86×69 | 13.0 % / 6.0 % | 0 / 0 | 4 (4) | 0.2 |  | [ref](states/reference/home-breadcrumb-home-focus-key-light.png) · [gotx](states/gotx/home-breadcrumb-home-focus-key-light.png) · [compare](states/compare/home-breadcrumb-home-focus-key-light.png) |
| home / breadcrumb-home-focus-key | dark | 86×69 → 86×69 | 15.4 % / 5.7 % | 0 / 0 | 4 (4) | 0.2 |  | [ref](states/reference/home-breadcrumb-home-focus-key-dark.png) · [gotx](states/gotx/home-breadcrumb-home-focus-key-dark.png) · [compare](states/compare/home-breadcrumb-home-focus-key-dark.png) |
| home / payments-item-hover | light | 338×143 → 338×143 | 11.7 % / 4.3 % | 0 / 0 | 9 (9) | 0.3 |  | [ref](states/reference/home-payments-item-hover-light.png) · [gotx](states/gotx/home-payments-item-hover-light.png) · [compare](states/compare/home-payments-item-hover-light.png) |
| home / payments-item-hover | dark | 338×143 → 338×143 | 17.6 % / 4.5 % | 0 / 0 | 9 (9) | 0.3 |  | [ref](states/reference/home-payments-item-hover-dark.png) · [gotx](states/gotx/home-payments-item-hover-dark.png) · [compare](states/compare/home-payments-item-hover-dark.png) |
| home / payments-item-focus-key | light | 338×143 → 338×143 | 14.4 % / 4.4 % | 0 / 0 | 9 (9) | 0.3 |  | [ref](states/reference/home-payments-item-focus-key-light.png) · [gotx](states/gotx/home-payments-item-focus-key-light.png) · [compare](states/compare/home-payments-item-focus-key-light.png) |
| home / payments-item-focus-key | dark | 338×143 → 338×143 | 62.1 % / 4.6 % | 0 / 0 | 9 (9) | 0.3 |  | [ref](states/reference/home-payments-item-focus-key-dark.png) · [gotx](states/gotx/home-payments-item-focus-key-dark.png) · [compare](states/compare/home-payments-item-focus-key-dark.png) |
| home / forgot-link-hover | light | 107×65 → 107×65 | 23.3 % / 4.6 % | 0 / 0 | 4 (4) | 0.3 |  | [ref](states/reference/home-forgot-link-hover-light.png) · [gotx](states/gotx/home-forgot-link-hover-light.png) · [compare](states/compare/home-forgot-link-hover-light.png) |
| home / forgot-link-hover | dark | 107×65 → 107×65 | 23.9 % / 4.5 % | 0 / 0 | 4 (4) | 0.3 |  | [ref](states/reference/home-forgot-link-hover-dark.png) · [gotx](states/gotx/home-forgot-link-hover-dark.png) · [compare](states/compare/home-forgot-link-hover-dark.png) |
| home / update-security-disabled-hover | light | 338×81 → 338×81 | 49.0 % / 1.1 % | 0 / 0 | 6 (6) | 0.3 |  | [ref](states/reference/home-update-security-disabled-hover-light.png) · [gotx](states/gotx/home-update-security-disabled-hover-light.png) · [compare](states/compare/home-update-security-disabled-hover-light.png) |
| home / update-security-disabled-hover | dark | 338×81 → 338×81 | 50.2 % / 1.1 % | 0 / 0 | 6 (6) | 0.3 |  | [ref](states/reference/home-update-security-disabled-hover-dark.png) · [gotx](states/gotx/home-update-security-disabled-hover-dark.png) · [compare](states/compare/home-update-security-disabled-hover-dark.png) |
| home / dismiss-payout-hover | light | 76×77 → 76×77 | 5.9 % / 0.3 % | 0 / 0 | 5 (5) | 0.2 |  | [ref](states/reference/home-dismiss-payout-hover-light.png) · [gotx](states/gotx/home-dismiss-payout-hover-light.png) · [compare](states/compare/home-dismiss-payout-hover-light.png) |
| home / dismiss-payout-hover | dark | 76×77 → 76×77 | 14.6 % / 0.3 % | 0 / 0 | 5 (5) | 0.2 |  | [ref](states/reference/home-dismiss-payout-hover-dark.png) · [gotx](states/gotx/home-dismiss-payout-hover-dark.png) · [compare](states/compare/home-dismiss-payout-hover-dark.png) |
| home / dismiss-payout-focus-key | light | 76×77 → 76×77 | 9.6 % / 0.3 % | 0 / 0 | 5 (5) | 0.2 |  | [ref](states/reference/home-dismiss-payout-focus-key-light.png) · [gotx](states/gotx/home-dismiss-payout-focus-key-light.png) · [compare](states/compare/home-dismiss-payout-focus-key-light.png) |
| home / dismiss-payout-focus-key | dark | 76×77 → 76×77 | 10.9 % / 0.3 % | 0 / 0 | 5 (5) | 0.2 |  | [ref](states/reference/home-dismiss-payout-focus-key-dark.png) · [gotx](states/gotx/home-dismiss-payout-focus-key-dark.png) · [compare](states/compare/home-dismiss-payout-focus-key-dark.png) |
| home / chat-reset-hover | light | 104×118 → 104×118 | 10.7 % / 2.3 % | 1 / 1 | 8 (8) | 0.2 |  | [ref](states/reference/home-chat-reset-hover-light.png) · [gotx](states/gotx/home-chat-reset-hover-light.png) · [compare](states/compare/home-chat-reset-hover-light.png) |
| home / chat-reset-hover | dark | 104×118 → 104×118 | 10.5 % / 1.9 % | 1 / 1 | 8 (8) | 0.2 |  | [ref](states/reference/home-chat-reset-hover-dark.png) · [gotx](states/gotx/home-chat-reset-hover-dark.png) · [compare](states/compare/home-chat-reset-hover-dark.png) |
| home / chat-reset-focus-key | light | 104×118 → 104×118 | 11.6 % / 2.5 % | 1 / 1 | 8 (8) | 0.2 |  | [ref](states/reference/home-chat-reset-focus-key-light.png) · [gotx](states/gotx/home-chat-reset-focus-key-light.png) · [compare](states/compare/home-chat-reset-focus-key-light.png) |
| home / chat-reset-focus-key | dark | 104×118 → 104×118 | 11.8 % / 2.2 % | 1 / 1 | 8 (8) | 0.2 |  | [ref](states/reference/home-chat-reset-focus-key-dark.png) · [gotx](states/gotx/home-chat-reset-focus-key-dark.png) · [compare](states/compare/home-chat-reset-focus-key-dark.png) |
| home / chat-attach-focus-key | light | 80×81 → 80×81 | 11.0 % / 4.4 % | 0 / 0 | 5 (4) | 0.2 |  | [ref](states/reference/home-chat-attach-focus-key-light.png) · [gotx](states/gotx/home-chat-attach-focus-key-light.png) · [compare](states/compare/home-chat-attach-focus-key-light.png) |
| home / chat-attach-focus-key | dark | 80×81 → 80×81 | 14.4 % / 5.1 % | 0 / 0 | 5 (4) | 0.2 |  | [ref](states/reference/home-chat-attach-focus-key-dark.png) · [gotx](states/gotx/home-chat-attach-focus-key-dark.png) · [compare](states/compare/home-chat-attach-focus-key-dark.png) |
| home / chat-attach-click | light | 224×229 → 224×229 | 17.0 % / 4.1 % | 1 / 1 | 11 (10) | 0 |  | [ref](states/reference/home-chat-attach-click-light.png) · [gotx](states/gotx/home-chat-attach-click-light.png) · [compare](states/compare/home-chat-attach-click-light.png) |
| home / chat-attach-click | dark | 224×229 → 224×229 | 17.2 % / 5.5 % | 1 / 1 | 11 (10) | 0 |  | [ref](states/reference/home-chat-attach-click-dark.png) · [gotx](states/gotx/home-chat-attach-click-dark.png) · [compare](states/compare/home-chat-attach-click-dark.png) |
| home / footer-link-hover | light | 97×66 → 97×66 | 9.2 % / 6.0 % | 0 / 0 | 3 (3) | 0.4 |  | [ref](states/reference/home-footer-link-hover-light.png) · [gotx](states/gotx/home-footer-link-hover-light.png) · [compare](states/compare/home-footer-link-hover-light.png) |
| home / footer-link-hover | dark | 97×66 → 97×66 | 9.4 % / 6.4 % | 0 / 0 | 3 (3) | 0.4 |  | [ref](states/reference/home-footer-link-hover-dark.png) · [gotx](states/gotx/home-footer-link-hover-dark.png) · [compare](states/compare/home-footer-link-hover-dark.png) |
| home / payments-menu-hover | light | 77×77 → 77×77 | 3.4 % / 0.0 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-payments-menu-hover-light.png) · [gotx](states/gotx/home-payments-menu-hover-light.png) · [compare](states/compare/home-payments-menu-hover-light.png) |
| home / payments-menu-hover | dark | 77×77 → 77×77 | 17.8 % / 0.0 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-payments-menu-hover-dark.png) · [gotx](states/gotx/home-payments-menu-hover-dark.png) · [compare](states/compare/home-payments-menu-hover-dark.png) |
| home / payments-menu-focus-key | light | 77×77 → 77×77 | 7.2 % / 0.5 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-payments-menu-focus-key-light.png) · [gotx](states/gotx/home-payments-menu-focus-key-light.png) · [compare](states/compare/home-payments-menu-focus-key-light.png) |
| home / payments-menu-focus-key | dark | 77×77 → 77×77 | 14.0 % / 0.6 % | 0 / 0 | 3 (3) | 0.2 |  | [ref](states/reference/home-payments-menu-focus-key-dark.png) · [gotx](states/gotx/home-payments-menu-focus-key-dark.png) · [compare](states/compare/home-payments-menu-focus-key-dark.png) |
| home / payments-menu-click | light | 177×185 → 177×185 | 20.1 % / 5.3 % | 1 / 1 | 11 (11) | 0.1 |  | [ref](states/reference/home-payments-menu-click-light.png) · [gotx](states/gotx/home-payments-menu-click-light.png) · [compare](states/compare/home-payments-menu-click-light.png) |
| home / payments-menu-click | dark | 177×185 → 177×185 | 31.7 % / 5.9 % | 1 / 1 | 11 (11) | 0.1 |  | [ref](states/reference/home-payments-menu-click-dark.png) · [gotx](states/gotx/home-payments-menu-click-dark.png) · [compare](states/compare/home-payments-menu-click-dark.png) |
| home / payments-menu-item-hover | light | 177×185 → 177×185 | 28.8 % / 5.3 % | 1 / 1 | 12 (12) | 0.1 |  | [ref](states/reference/home-payments-menu-item-hover-light.png) · [gotx](states/gotx/home-payments-menu-item-hover-light.png) · [compare](states/compare/home-payments-menu-item-hover-light.png) |
| home / payments-menu-item-hover | dark | 177×185 → 177×185 | 31.1 % / 5.8 % | 1 / 1 | 12 (12) | 0.1 |  | [ref](states/reference/home-payments-menu-item-hover-dark.png) · [gotx](states/gotx/home-payments-menu-item-hover-dark.png) · [compare](states/compare/home-payments-menu-item-hover-dark.png) |
| home / group-menu-item-hover | light | 208×253 → 208×253 | 18.1 % / 5.8 % | 1 / 1 | 14 (14) | 0 |  | [ref](states/reference/home-group-menu-item-hover-light.png) · [gotx](states/gotx/home-group-menu-item-hover-light.png) · [compare](states/compare/home-group-menu-item-hover-light.png) |
| home / group-menu-item-hover | dark | 208×253 → 208×253 | 21.1 % / 6.3 % | 1 / 1 | 14 (14) | 0 |  | [ref](states/reference/home-group-menu-item-hover-dark.png) · [gotx](states/gotx/home-group-menu-item-hover-dark.png) · [compare](states/compare/home-group-menu-item-hover-dark.png) |
| home / alert-dialog-cancel-hover | light | 368×227 → 368×227 | 37.4 % / 12.6 % | 1 / 1 | 24 (24) | 0.4 |  | [ref](states/reference/home-alert-dialog-cancel-hover-light.png) · [gotx](states/gotx/home-alert-dialog-cancel-hover-light.png) · [compare](states/compare/home-alert-dialog-cancel-hover-light.png) |
| home / alert-dialog-cancel-hover | dark | 368×227 → 368×227 | 38.2 % / 12.9 % | 1 / 1 | 24 (24) | 0.4 |  | [ref](states/reference/home-alert-dialog-cancel-hover-dark.png) · [gotx](states/gotx/home-alert-dialog-cancel-hover-dark.png) · [compare](states/compare/home-alert-dialog-cancel-hover-dark.png) |
| home / textarea-message-type | light | 338×113 → 338×113 | 3.2 % / 0.9 % | 0 / 0 | 2 (2) | 0 |  | [ref](states/reference/home-textarea-message-type-light.png) · [gotx](states/gotx/home-textarea-message-type-light.png) · [compare](states/compare/home-textarea-message-type-light.png) |
| home / textarea-message-type | dark | 338×113 → 338×113 | 3.9 % / 1.1 % | 0 / 0 | 2 (2) | 0 |  | [ref](states/reference/home-textarea-message-type-dark.png) · [gotx](states/gotx/home-textarea-message-type-dark.png) · [compare](states/compare/home-textarea-message-type-dark.png) |
| home / dismiss-dividend-hover | light | 76×77 → 76×77 | 6.8 % / 0.9 % | 0 / 0 | 5 (5) | 0.2 |  | [ref](states/reference/home-dismiss-dividend-hover-light.png) · [gotx](states/gotx/home-dismiss-dividend-hover-light.png) · [compare](states/compare/home-dismiss-dividend-hover-light.png) |
| home / dismiss-dividend-hover | dark | 76×77 → 76×77 | 15.7 % / 1.1 % | 0 / 0 | 5 (5) | 0.2 |  | [ref](states/reference/home-dismiss-dividend-hover-dark.png) · [gotx](states/gotx/home-dismiss-dividend-hover-dark.png) · [compare](states/compare/home-dismiss-dividend-hover-dark.png) |
| home / dismiss-dividend-focus-key | light | 76×77 → 76×77 | 10.7 % / 0.9 % | 0 / 0 | 5 (5) | 0.2 |  | [ref](states/reference/home-dismiss-dividend-focus-key-light.png) · [gotx](states/gotx/home-dismiss-dividend-focus-key-light.png) · [compare](states/compare/home-dismiss-dividend-focus-key-light.png) |
| home / dismiss-dividend-focus-key | dark | 76×77 → 76×77 | 12.0 % / 1.1 % | 0 / 0 | 5 (5) | 0.2 |  | [ref](states/reference/home-dismiss-dividend-focus-key-dark.png) · [gotx](states/gotx/home-dismiss-dividend-focus-key-dark.png) · [compare](states/compare/home-dismiss-dividend-focus-key-dark.png) |
| home / view-full-report-hover | light | 338×81 → 338×81 | 8.3 % / 2.4 % | 0 / 0 | 7 (7) | 0.2 |  | [ref](states/reference/home-view-full-report-hover-light.png) · [gotx](states/gotx/home-view-full-report-hover-light.png) · [compare](states/compare/home-view-full-report-hover-light.png) |
| home / view-full-report-hover | dark | 338×81 → 338×81 | 41.7 % / 2.3 % | 0 / 0 | 7 (7) | 0.2 |  | [ref](states/reference/home-view-full-report-hover-dark.png) · [gotx](states/gotx/home-view-full-report-hover-dark.png) · [compare](states/compare/home-view-full-report-hover-dark.png) |
| home / view-full-report-focus-key | light | 338×81 → 338×81 | 9.0 % / 2.5 % | 0 / 0 | 7 (7) | 0.2 |  | [ref](states/reference/home-view-full-report-focus-key-light.png) · [gotx](states/gotx/home-view-full-report-focus-key-light.png) · [compare](states/compare/home-view-full-report-focus-key-light.png) |
| home / view-full-report-focus-key | dark | 338×81 → 338×81 | 14.6 % / 2.4 % | 0 / 0 | 7 (7) | 0.2 |  | [ref](states/reference/home-view-full-report-focus-key-dark.png) · [gotx](states/gotx/home-view-full-report-focus-key-dark.png) · [compare](states/compare/home-view-full-report-focus-key-dark.png) |
| home / view-full-report-press | light | 338×81 → 338×81 | 8.3 % / 2.4 % | 0 / 0 | 7 (7) | 0.2 |  | [ref](states/reference/home-view-full-report-press-light.png) · [gotx](states/gotx/home-view-full-report-press-light.png) · [compare](states/compare/home-view-full-report-press-light.png) |
| home / view-full-report-press | dark | 338×81 → 338×81 | 41.7 % / 2.3 % | 0 / 0 | 7 (7) | 0.2 |  | [ref](states/reference/home-view-full-report-press-dark.png) · [gotx](states/gotx/home-view-full-report-press-dark.png) · [compare](states/compare/home-view-full-report-press-dark.png) |
| home / create-goal-hover | light | 338×81 → 338×81 | 4.8 % / 2.1 % | 0 / 0 | 9 (9) | 0.3 |  | [ref](states/reference/home-create-goal-hover-light.png) · [gotx](states/gotx/home-create-goal-hover-light.png) · [compare](states/compare/home-create-goal-hover-light.png) |
| home / create-goal-hover | dark | 338×81 → 338×81 | 33.8 % / 1.9 % | 0 / 0 | 9 (9) | 0.3 |  | [ref](states/reference/home-create-goal-hover-dark.png) · [gotx](states/gotx/home-create-goal-hover-dark.png) · [compare](states/compare/home-create-goal-hover-dark.png) |
| home / cancel-goal-hover | light | 338×81 → 338×81 | 8.3 % / 2.0 % | 0 / 0 | 7 (7) | 0.3 |  | [ref](states/reference/home-cancel-goal-hover-light.png) · [gotx](states/gotx/home-cancel-goal-hover-light.png) · [compare](states/compare/home-cancel-goal-hover-light.png) |
| home / cancel-goal-hover | dark | 338×81 → 338×81 | 11.3 % / 1.9 % | 0 / 0 | 7 (7) | 0.3 |  | [ref](states/reference/home-cancel-goal-hover-dark.png) · [gotx](states/gotx/home-cancel-goal-hover-dark.png) · [compare](states/compare/home-cancel-goal-hover-dark.png) |
| home / save-threshold-hover | light | 338×81 → 338×81 | 8.1 % / 2.3 % | 0 / 0 | 5 (5) | 0.2 |  | [ref](states/reference/home-save-threshold-hover-light.png) · [gotx](states/gotx/home-save-threshold-hover-light.png) · [compare](states/compare/home-save-threshold-hover-light.png) |
| home / save-threshold-hover | dark | 338×81 → 338×81 | 37.2 % / 1.9 % | 0 / 0 | 5 (5) | 0.2 |  | [ref](states/reference/home-save-threshold-hover-dark.png) · [gotx](states/gotx/home-save-threshold-hover-dark.png) · [compare](states/compare/home-save-threshold-hover-dark.png) |
| home / danger-item-hover | light | 338×122 → 338×122 | 46.2 % / 0.4 % | 0 / 0 | 8 (7) | 0.3 |  | [ref](states/reference/home-danger-item-hover-light.png) · [gotx](states/gotx/home-danger-item-hover-light.png) · [compare](states/compare/home-danger-item-hover-light.png) |
| home / danger-item-hover | dark | 338×122 → 338×122 | 54.4 % / 0.4 % | 0 / 0 | 8 (7) | 0.3 |  | [ref](states/reference/home-danger-item-hover-dark.png) · [gotx](states/gotx/home-danger-item-hover-dark.png) · [compare](states/compare/home-danger-item-hover-dark.png) |
| home / danger-item-focus-key | light | 338×122 → 338×122 | 30.5 % / 0.4 % | 0 / 0 | 8 (7) | 0.3 |  | [ref](states/reference/home-danger-item-focus-key-light.png) · [gotx](states/gotx/home-danger-item-focus-key-light.png) · [compare](states/compare/home-danger-item-focus-key-light.png) |
| home / danger-item-focus-key | dark | 338×122 → 338×122 | 29.2 % / 0.4 % | 0 / 0 | 8 (7) | 0.3 |  | [ref](states/reference/home-danger-item-focus-key-dark.png) · [gotx](states/gotx/home-danger-item-focus-key-dark.png) · [compare](states/compare/home-danger-item-focus-key-dark.png) |
| home / chat-send-hover | light | 80×81 → 80×81 | 6.3 % / 3.1 % | 0 / 0 | 5 (4) | 0.2 |  | [ref](states/reference/home-chat-send-hover-light.png) · [gotx](states/gotx/home-chat-send-hover-light.png) · [compare](states/compare/home-chat-send-hover-light.png) |
| home / chat-send-hover | dark | 80×81 → 80×81 | 6.4 % / 3.6 % | 0 / 0 | 5 (4) | 0.2 |  | [ref](states/reference/home-chat-send-hover-dark.png) · [gotx](states/gotx/home-chat-send-hover-dark.png) · [compare](states/compare/home-chat-send-hover-dark.png) |
| home / chat-send-focus-key | light | 80×81 → 80×81 | 9.5 % / 3.2 % | 0 / 0 | 5 (4) | 0.2 |  | [ref](states/reference/home-chat-send-focus-key-light.png) · [gotx](states/gotx/home-chat-send-focus-key-light.png) · [compare](states/compare/home-chat-send-focus-key-light.png) |
| home / chat-send-focus-key | dark | 80×81 → 80×81 | 12.9 % / 3.6 % | 0 / 0 | 5 (4) | 0.2 |  | [ref](states/reference/home-chat-send-focus-key-dark.png) · [gotx](states/gotx/home-chat-send-focus-key-dark.png) · [compare](states/compare/home-chat-send-focus-key-dark.png) |
| dashboard-01 / brand-hover | light | 296×72 → 296×72 | 12.6 % / 2.5 % | 0 / 0 | 4 (4) | 0 |  | [ref](states/reference/dashboard-01-brand-hover-light.png) · [gotx](states/gotx/dashboard-01-brand-hover-light.png) · [compare](states/compare/dashboard-01-brand-hover-light.png) |
| dashboard-01 / brand-hover | dark | 296×72 → 296×72 | 5.0 % / 2.5 % | 0 / 0 | 4 (4) | 0 |  | [ref](states/reference/dashboard-01-brand-hover-dark.png) · [gotx](states/gotx/dashboard-01-brand-hover-dark.png) · [compare](states/compare/dashboard-01-brand-hover-dark.png) |
| dashboard-01 / brand-focus-key | light | 296×72 → 296×72 | 15.8 % / 5.2 % | 0 / 0 | 4 (4) | 0 |  | [ref](states/reference/dashboard-01-brand-focus-key-light.png) · [gotx](states/gotx/dashboard-01-brand-focus-key-light.png) · [compare](states/compare/dashboard-01-brand-focus-key-light.png) |
| dashboard-01 / brand-focus-key | dark | 296×72 → 296×72 | 12.8 % / 10.5 % | 0 / 0 | 4 (4) | 0 |  | [ref](states/reference/dashboard-01-brand-focus-key-dark.png) · [gotx](states/gotx/dashboard-01-brand-focus-key-dark.png) · [compare](states/compare/dashboard-01-brand-focus-key-dark.png) |
| dashboard-01 / quick-create-hover | light | 256×80 → 256×80 | 35.1 % / 3.4 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/dashboard-01-quick-create-hover-light.png) · [gotx](states/gotx/dashboard-01-quick-create-hover-light.png) · [compare](states/compare/dashboard-01-quick-create-hover-light.png) |
| dashboard-01 / quick-create-hover | dark | 256×80 → 256×80 | 35.0 % / 2.8 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/dashboard-01-quick-create-hover-dark.png) · [gotx](states/gotx/dashboard-01-quick-create-hover-dark.png) · [compare](states/compare/dashboard-01-quick-create-hover-dark.png) |
| dashboard-01 / quick-create-focus-key | light | 256×80 → 256×80 | 35.5 % / 3.4 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/dashboard-01-quick-create-focus-key-light.png) · [gotx](states/gotx/dashboard-01-quick-create-focus-key-light.png) · [compare](states/compare/dashboard-01-quick-create-focus-key-light.png) |
| dashboard-01 / quick-create-focus-key | dark | 256×80 → 256×80 | 10.1 % / 7.7 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/dashboard-01-quick-create-focus-key-dark.png) · [gotx](states/gotx/dashboard-01-quick-create-focus-key-dark.png) · [compare](states/compare/dashboard-01-quick-create-focus-key-dark.png) |
| dashboard-01 / quick-create-press | light | 256×80 → 256×80 | 35.1 % / 3.4 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/dashboard-01-quick-create-press-light.png) · [gotx](states/gotx/dashboard-01-quick-create-press-light.png) · [compare](states/compare/dashboard-01-quick-create-press-light.png) |
| dashboard-01 / quick-create-press | dark | 256×80 → 256×80 | 35.0 % / 2.8 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/dashboard-01-quick-create-press-dark.png) · [gotx](states/gotx/dashboard-01-quick-create-press-dark.png) · [compare](states/compare/dashboard-01-quick-create-press-dark.png) |
| dashboard-01 / inbox-hover | light | 80×80 → 80×80 | 16.6 % / 0.3 % | 0 / 0 | 3 (3) | 0 |  | [ref](states/reference/dashboard-01-inbox-hover-light.png) · [gotx](states/gotx/dashboard-01-inbox-hover-light.png) · [compare](states/compare/dashboard-01-inbox-hover-light.png) |
| dashboard-01 / inbox-hover | dark | 80×80 → 80×80 | 9.4 % / 0.3 % | 0 / 0 | 3 (3) | 0 |  | [ref](states/reference/dashboard-01-inbox-hover-dark.png) · [gotx](states/gotx/dashboard-01-inbox-hover-dark.png) · [compare](states/compare/dashboard-01-inbox-hover-dark.png) |
| dashboard-01 / inbox-focus-key | light | 80×80 → 80×80 | 17.5 % / 0.3 % | 0 / 0 | 3 (3) | 0 |  | [ref](states/reference/dashboard-01-inbox-focus-key-light.png) · [gotx](states/gotx/dashboard-01-inbox-focus-key-light.png) · [compare](states/compare/dashboard-01-inbox-focus-key-light.png) |
| dashboard-01 / inbox-focus-key | dark | 80×80 → 80×80 | 9.3 % / 2.1 % | 0 / 0 | 3 (3) | 0 |  | [ref](states/reference/dashboard-01-inbox-focus-key-dark.png) · [gotx](states/gotx/dashboard-01-inbox-focus-key-dark.png) · [compare](states/compare/dashboard-01-inbox-focus-key-dark.png) |
| dashboard-01 / nav-lifecycle-hover | light | 296×80 → 296×80 | 7.1 % / 4.3 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/dashboard-01-nav-lifecycle-hover-light.png) · [gotx](states/gotx/dashboard-01-nav-lifecycle-hover-light.png) · [compare](states/compare/dashboard-01-nav-lifecycle-hover-light.png) |
| dashboard-01 / nav-lifecycle-hover | dark | 296×80 → 296×80 | 7.3 % / 4.8 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/dashboard-01-nav-lifecycle-hover-dark.png) · [gotx](states/gotx/dashboard-01-nav-lifecycle-hover-dark.png) · [compare](states/compare/dashboard-01-nav-lifecycle-hover-dark.png) |
| dashboard-01 / nav-lifecycle-focus-key | light | 296×80 → 296×80 | 9.9 % / 6.8 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/dashboard-01-nav-lifecycle-focus-key-light.png) · [gotx](states/gotx/dashboard-01-nav-lifecycle-focus-key-light.png) · [compare](states/compare/dashboard-01-nav-lifecycle-focus-key-light.png) |
| dashboard-01 / nav-lifecycle-focus-key | dark | 296×80 → 296×80 | 14.3 % / 11.9 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/dashboard-01-nav-lifecycle-focus-key-dark.png) · [gotx](states/gotx/dashboard-01-nav-lifecycle-focus-key-dark.png) · [compare](states/compare/dashboard-01-nav-lifecycle-focus-key-dark.png) |
| dashboard-01 / nav-lifecycle-press | light | 296×80 → 296×80 | 7.1 % / 4.3 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/dashboard-01-nav-lifecycle-press-light.png) · [gotx](states/gotx/dashboard-01-nav-lifecycle-press-light.png) · [compare](states/compare/dashboard-01-nav-lifecycle-press-light.png) |
| dashboard-01 / nav-lifecycle-press | dark | 296×80 → 296×80 | 7.3 % / 4.8 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/dashboard-01-nav-lifecycle-press-dark.png) · [gotx](states/gotx/dashboard-01-nav-lifecycle-press-dark.png) · [compare](states/compare/dashboard-01-nav-lifecycle-press-dark.png) |
| dashboard-01 / nav-lifecycle-click | light | 296×80 → 296×80 | 7.1 % / 4.3 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/dashboard-01-nav-lifecycle-click-light.png) · [gotx](states/gotx/dashboard-01-nav-lifecycle-click-light.png) · [compare](states/compare/dashboard-01-nav-lifecycle-click-light.png) |
| dashboard-01 / nav-lifecycle-click | dark | 296×80 → 296×80 | 7.3 % / 4.8 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/dashboard-01-nav-lifecycle-click-dark.png) · [gotx](states/gotx/dashboard-01-nav-lifecycle-click-dark.png) · [compare](states/compare/dashboard-01-nav-lifecycle-click-dark.png) |
| dashboard-01 / doc-item-hover | light | 296×80 → 296×80 | 7.1 % / 4.1 % | 0 / 0 | 5 (5) | 0 |  | [ref](states/reference/dashboard-01-doc-item-hover-light.png) · [gotx](states/gotx/dashboard-01-doc-item-hover-light.png) · [compare](states/compare/dashboard-01-doc-item-hover-light.png) |
| dashboard-01 / doc-item-hover | dark | 296×80 → 296×80 | 7.3 % / 4.5 % | 0 / 0 | 5 (5) | 0 |  | [ref](states/reference/dashboard-01-doc-item-hover-dark.png) · [gotx](states/gotx/dashboard-01-doc-item-hover-dark.png) · [compare](states/compare/dashboard-01-doc-item-hover-dark.png) |
| dashboard-01 / doc-action-focus-key | light | 68×68 → 68×68 | 17.6 % / 12.4 % | 0 / 0 | 2 (2) | 0 |  | [ref](states/reference/dashboard-01-doc-action-focus-key-light.png) · [gotx](states/gotx/dashboard-01-doc-action-focus-key-light.png) · [compare](states/compare/dashboard-01-doc-action-focus-key-light.png) |
| dashboard-01 / doc-action-focus-key | dark | 68×68 → 68×68 | 17.6 % / 12.1 % | 0 / 0 | 2 (2) | 0 |  | [ref](states/reference/dashboard-01-doc-action-focus-key-dark.png) · [gotx](states/gotx/dashboard-01-doc-action-focus-key-dark.png) · [compare](states/compare/dashboard-01-doc-action-focus-key-dark.png) |
| dashboard-01 / doc-action-click | light | 200×163 → 200×163 | 13.3 % / 2.8 % | 1 / 1 | 11 (10) | 0 |  | [ref](states/reference/dashboard-01-doc-action-click-light.png) · [gotx](states/gotx/dashboard-01-doc-action-click-light.png) · [compare](states/compare/dashboard-01-doc-action-click-light.png) |
| dashboard-01 / doc-action-click | dark | 200×163 → 200×163 | 15.7 % / 4.2 % | 1 / 1 | 11 (10) | 0 |  | [ref](states/reference/dashboard-01-doc-action-click-dark.png) · [gotx](states/gotx/dashboard-01-doc-action-click-dark.png) · [compare](states/compare/dashboard-01-doc-action-click-dark.png) |
| dashboard-01 / nav-user-hover | light | 296×88 → 296×88 | 8.8 % / 6.5 % | 0 / 0 | 4 (4) | 0.3 |  | [ref](states/reference/dashboard-01-nav-user-hover-light.png) · [gotx](states/gotx/dashboard-01-nav-user-hover-light.png) · [compare](states/compare/dashboard-01-nav-user-hover-light.png) |
| dashboard-01 / nav-user-hover | dark | 296×88 → 296×88 | 9.1 % / 5.6 % | 0 / 0 | 4 (4) | 0.3 |  | [ref](states/reference/dashboard-01-nav-user-hover-dark.png) · [gotx](states/gotx/dashboard-01-nav-user-hover-dark.png) · [compare](states/compare/dashboard-01-nav-user-hover-dark.png) |
| dashboard-01 / nav-user-click | light | 556×241 → 556×241 | 51.1 % / 8.4 % | 1 / 1 | 21 (15) | 0.5 | GotX content extends beyond the reference frame | [ref](states/reference/dashboard-01-nav-user-click-light.png) · [gotx](states/gotx/dashboard-01-nav-user-click-light.png) · [compare](states/compare/dashboard-01-nav-user-click-light.png) |
| dashboard-01 / nav-user-click | dark | 556×241 → 556×241 | 25.7 % / 7.5 % | 1 / 1 | 21 (15) | 0.5 | GotX content extends beyond the reference frame | [ref](states/reference/dashboard-01-nav-user-click-dark.png) · [gotx](states/gotx/dashboard-01-nav-user-click-dark.png) · [compare](states/compare/dashboard-01-nav-user-click-dark.png) |
| dashboard-01 / trigger-hover | light | 76×70 → 76×70 | 7.0 % / 0.2 % | 0 / 0 | 0 (0) | 0 |  | [ref](states/reference/dashboard-01-trigger-hover-light.png) · [gotx](states/gotx/dashboard-01-trigger-hover-light.png) · [compare](states/compare/dashboard-01-trigger-hover-light.png) |
| dashboard-01 / trigger-hover | dark | 76×70 → 76×70 | 21.6 % / 0.2 % | 0 / 0 | 0 (0) | 0 |  | [ref](states/reference/dashboard-01-trigger-hover-dark.png) · [gotx](states/gotx/dashboard-01-trigger-hover-dark.png) · [compare](states/compare/dashboard-01-trigger-hover-dark.png) |
| dashboard-01 / trigger-focus-key | light | 76×70 → 76×70 | 10.6 % / 0.8 % | 0 / 0 | 0 (0) | 0 |  | [ref](states/reference/dashboard-01-trigger-focus-key-light.png) · [gotx](states/gotx/dashboard-01-trigger-focus-key-light.png) · [compare](states/compare/dashboard-01-trigger-focus-key-light.png) |
| dashboard-01 / trigger-focus-key | dark | 76×70 → 76×70 | 13.4 % / 1.3 % | 0 / 0 | 0 (0) | 0 |  | [ref](states/reference/dashboard-01-trigger-focus-key-dark.png) · [gotx](states/gotx/dashboard-01-trigger-focus-key-dark.png) · [compare](states/compare/dashboard-01-trigger-focus-key-dark.png) |
| dashboard-01 / github-hover | light | 117×72 → 117×72 | 6.6 % / 3.9 % | 0 / 0 | 2 (2) | 0.5 |  | [ref](states/reference/dashboard-01-github-hover-light.png) · [gotx](states/gotx/dashboard-01-github-hover-light.png) · [compare](states/compare/dashboard-01-github-hover-light.png) |
| dashboard-01 / github-hover | dark | 117×72 → 117×72 | 30.1 % / 4.0 % | 0 / 0 | 2 (2) | 0.5 |  | [ref](states/reference/dashboard-01-github-hover-dark.png) · [gotx](states/gotx/dashboard-01-github-hover-dark.png) · [compare](states/compare/dashboard-01-github-hover-dark.png) |
| dashboard-01 / github-focus-key | light | 117×72 → 117×72 | 9.6 % / 4.0 % | 0 / 0 | 2 (2) | 0.5 |  | [ref](states/reference/dashboard-01-github-focus-key-light.png) · [gotx](states/gotx/dashboard-01-github-focus-key-light.png) · [compare](states/compare/dashboard-01-github-focus-key-light.png) |
| dashboard-01 / github-focus-key | dark | 117×72 → 117×72 | 12.3 % / 6.0 % | 0 / 0 | 2 (2) | 0.5 |  | [ref](states/reference/dashboard-01-github-focus-key-dark.png) · [gotx](states/gotx/dashboard-01-github-focus-key-dark.png) · [compare](states/compare/dashboard-01-github-focus-key-dark.png) |
| dashboard-01 / toggle-90d-focus-key | light | 177×84 → 177×84 | 11.3 % / 5.8 % | 0 / 0 | 6 (6) | 0.6 |  | [ref](states/reference/dashboard-01-toggle-90d-focus-key-light.png) · [gotx](states/gotx/dashboard-01-toggle-90d-focus-key-light.png) · [compare](states/compare/dashboard-01-toggle-90d-focus-key-light.png) |
| dashboard-01 / toggle-90d-focus-key | dark | 177×84 → 177×84 | 31.6 % / 29.7 % | 0 / 0 | 6 (6) | 0.6 |  | [ref](states/reference/dashboard-01-toggle-90d-focus-key-dark.png) · [gotx](states/gotx/dashboard-01-toggle-90d-focus-key-dark.png) · [compare](states/compare/dashboard-01-toggle-90d-focus-key-dark.png) |
| dashboard-01 / toggle-30d-hover | light | 167×84 → 167×84 | 11.5 % / 4.1 % | 0 / 0 | 6 (6) | 0.5 |  | [ref](states/reference/dashboard-01-toggle-30d-hover-light.png) · [gotx](states/gotx/dashboard-01-toggle-30d-hover-light.png) · [compare](states/compare/dashboard-01-toggle-30d-hover-light.png) |
| dashboard-01 / toggle-30d-hover | dark | 167×84 → 167×84 | 38.1 % / 33.4 % | 0 / 0 | 6 (6) | 0.5 |  | [ref](states/reference/dashboard-01-toggle-30d-hover-dark.png) · [gotx](states/gotx/dashboard-01-toggle-30d-hover-dark.png) · [compare](states/compare/dashboard-01-toggle-30d-hover-dark.png) |
| dashboard-01 / toggle-30d-click | light | 167×84 → 167×84 | 11.5 % / 4.3 % | 0 / 0 | 6 (6) | 0.5 |  | [ref](states/reference/dashboard-01-toggle-30d-click-light.png) · [gotx](states/gotx/dashboard-01-toggle-30d-click-light.png) · [compare](states/compare/dashboard-01-toggle-30d-click-light.png) |
| dashboard-01 / toggle-30d-click | dark | 167×84 → 167×84 | 32.7 % / 28.3 % | 0 / 0 | 6 (6) | 0.5 |  | [ref](states/reference/dashboard-01-toggle-30d-click-dark.png) · [gotx](states/gotx/dashboard-01-toggle-30d-click-dark.png) · [compare](states/compare/dashboard-01-toggle-30d-click-dark.png) |
| dashboard-01 / tab-outline-focus-key | light | 114×78 → 114×78 | 11.5 % / 6.7 % | 0 / 0 | 4 (4) | 1.2 |  | [ref](states/reference/dashboard-01-tab-outline-focus-key-light.png) · [gotx](states/gotx/dashboard-01-tab-outline-focus-key-light.png) · [compare](states/compare/dashboard-01-tab-outline-focus-key-light.png) |
| dashboard-01 / tab-outline-focus-key | dark | 114×78 → 114×78 | 28.2 % / 7.8 % | 0 / 0 | 4 (4) | 1.2 |  | [ref](states/reference/dashboard-01-tab-outline-focus-key-dark.png) · [gotx](states/gotx/dashboard-01-tab-outline-focus-key-dark.png) · [compare](states/compare/dashboard-01-tab-outline-focus-key-dark.png) |
| dashboard-01 / tab-past-hover | light | 210×78 → 210×78 | 8.4 % / 5.3 % | 0 / 0 | 7 (7) | 0.5 |  | [ref](states/reference/dashboard-01-tab-past-hover-light.png) · [gotx](states/gotx/dashboard-01-tab-past-hover-light.png) · [compare](states/compare/dashboard-01-tab-past-hover-light.png) |
| dashboard-01 / tab-past-hover | dark | 210×78 → 210×78 | 12.6 % / 5.4 % | 0 / 0 | 7 (7) | 0.5 |  | [ref](states/reference/dashboard-01-tab-past-hover-dark.png) · [gotx](states/gotx/dashboard-01-tab-past-hover-dark.png) · [compare](states/compare/dashboard-01-tab-past-hover-dark.png) |
| dashboard-01 / tab-past-click | light | 210×78 → 210×78 | 12.3 % / 5.3 % | 0 / 0 | 7 (7) | 0.5 |  | [ref](states/reference/dashboard-01-tab-past-click-light.png) · [gotx](states/gotx/dashboard-01-tab-past-click-light.png) · [compare](states/compare/dashboard-01-tab-past-click-light.png) |
| dashboard-01 / tab-past-click | dark | 210×78 → 210×78 | 34.9 % / 5.2 % | 0 / 0 | 7 (7) | 0.5 |  | [ref](states/reference/dashboard-01-tab-past-click-dark.png) · [gotx](states/gotx/dashboard-01-tab-past-click-dark.png) · [compare](states/compare/dashboard-01-tab-past-click-dark.png) |
| dashboard-01 / columns-hover | light | 246×80 → 246×80 | 10.5 % / 3.8 % | 0 / 0 | 3 (3) | 0.3 |  | [ref](states/reference/dashboard-01-columns-hover-light.png) · [gotx](states/gotx/dashboard-01-columns-hover-light.png) · [compare](states/compare/dashboard-01-columns-hover-light.png) |
| dashboard-01 / columns-hover | dark | 246×80 → 246×80 | 14.9 % / 4.1 % | 0 / 0 | 3 (3) | 0.3 |  | [ref](states/reference/dashboard-01-columns-hover-dark.png) · [gotx](states/gotx/dashboard-01-columns-hover-dark.png) · [compare](states/compare/dashboard-01-columns-hover-dark.png) |
| dashboard-01 / columns-focus-key | light | 246×80 → 246×80 | 14.3 % / 6.2 % | 0 / 0 | 3 (3) | 0.3 |  | [ref](states/reference/dashboard-01-columns-focus-key-light.png) · [gotx](states/gotx/dashboard-01-columns-focus-key-light.png) · [compare](states/compare/dashboard-01-columns-focus-key-light.png) |
| dashboard-01 / columns-focus-key | dark | 246×80 → 246×80 | 38.5 % / 4.2 % | 0 / 0 | 3 (3) | 0.3 |  | [ref](states/reference/dashboard-01-columns-focus-key-dark.png) · [gotx](states/gotx/dashboard-01-columns-focus-key-dark.png) · [compare](states/compare/dashboard-01-columns-focus-key-dark.png) |
| dashboard-01 / columns-click | light | 273×254 → 273×254 | 17.4 % / 5.4 % | 1 / 1 | 22 (17) | 0.5 |  | [ref](states/reference/dashboard-01-columns-click-light.png) · [gotx](states/gotx/dashboard-01-columns-click-light.png) · [compare](states/compare/dashboard-01-columns-click-light.png) |
| dashboard-01 / columns-click | dark | 273×254 → 273×254 | 21.1 % / 5.7 % | 1 / 1 | 22 (17) | 0.5 |  | [ref](states/reference/dashboard-01-columns-click-dark.png) · [gotx](states/gotx/dashboard-01-columns-click-dark.png) · [compare](states/compare/dashboard-01-columns-click-dark.png) |
| dashboard-01 / add-section-hover | light | 172×80 → 172×80 | 10.2 % / 3.4 % | 0 / 0 | 3 (3) | 0.3 |  | [ref](states/reference/dashboard-01-add-section-hover-light.png) · [gotx](states/gotx/dashboard-01-add-section-hover-light.png) · [compare](states/compare/dashboard-01-add-section-hover-light.png) |
| dashboard-01 / add-section-hover | dark | 172×80 → 172×80 | 14.8 % / 3.8 % | 0 / 0 | 3 (3) | 0.3 |  | [ref](states/reference/dashboard-01-add-section-hover-dark.png) · [gotx](states/gotx/dashboard-01-add-section-hover-dark.png) · [compare](states/compare/dashboard-01-add-section-hover-dark.png) |
| dashboard-01 / add-section-focus-key | light | 172×80 → 172×80 | 8.8 % / 3.5 % | 0 / 0 | 3 (3) | 0.3 |  | [ref](states/reference/dashboard-01-add-section-focus-key-light.png) · [gotx](states/gotx/dashboard-01-add-section-focus-key-light.png) · [compare](states/compare/dashboard-01-add-section-focus-key-light.png) |
| dashboard-01 / add-section-focus-key | dark | 172×80 → 172×80 | 36.9 % / 6.0 % | 0 / 0 | 3 (3) | 0.3 |  | [ref](states/reference/dashboard-01-add-section-focus-key-dark.png) · [gotx](states/gotx/dashboard-01-add-section-focus-key-dark.png) · [compare](states/compare/dashboard-01-add-section-focus-key-dark.png) |
| dashboard-01 / add-section-press | light | 172×80 → 172×80 | 10.2 % / 3.4 % | 0 / 0 | 3 (3) | 0.3 |  | [ref](states/reference/dashboard-01-add-section-press-light.png) · [gotx](states/gotx/dashboard-01-add-section-press-light.png) · [compare](states/compare/dashboard-01-add-section-press-light.png) |
| dashboard-01 / add-section-press | dark | 172×80 → 172×80 | 14.8 % / 3.8 % | 0 / 0 | 3 (3) | 0.3 |  | [ref](states/reference/dashboard-01-add-section-press-dark.png) · [gotx](states/gotx/dashboard-01-add-section-press-dark.png) · [compare](states/compare/dashboard-01-add-section-press-dark.png) |
| dashboard-01 / select-all-hover | light | 65×65 → 65×65 | 8.0 % / 3.7 % | 0 / 0 | 3 (3) | 0.5 |  | [ref](states/reference/dashboard-01-select-all-hover-light.png) · [gotx](states/gotx/dashboard-01-select-all-hover-light.png) · [compare](states/compare/dashboard-01-select-all-hover-light.png) |
| dashboard-01 / select-all-hover | dark | 65×65 → 65×65 | 14.4 % / 3.9 % | 0 / 0 | 3 (3) | 0.5 |  | [ref](states/reference/dashboard-01-select-all-hover-dark.png) · [gotx](states/gotx/dashboard-01-select-all-hover-dark.png) · [compare](states/compare/dashboard-01-select-all-hover-dark.png) |
| dashboard-01 / select-all-focus-key | light | 65×65 → 65×65 | 8.0 % / 3.7 % | 0 / 0 | 3 (3) | 0.5 |  | [ref](states/reference/dashboard-01-select-all-focus-key-light.png) · [gotx](states/gotx/dashboard-01-select-all-focus-key-light.png) · [compare](states/compare/dashboard-01-select-all-focus-key-light.png) |
| dashboard-01 / select-all-focus-key | dark | 65×65 → 65×65 | 14.4 % / 3.9 % | 0 / 0 | 3 (3) | 0.5 |  | [ref](states/reference/dashboard-01-select-all-focus-key-dark.png) · [gotx](states/gotx/dashboard-01-select-all-focus-key-dark.png) · [compare](states/compare/dashboard-01-select-all-focus-key-dark.png) |
| dashboard-01 / select-all-click | light | 65×65 → 65×65 | 13.0 % / 2.4 % | 0 / 0 | 3 (3) | 0.5 |  | [ref](states/reference/dashboard-01-select-all-click-light.png) · [gotx](states/gotx/dashboard-01-select-all-click-light.png) · [compare](states/compare/dashboard-01-select-all-click-light.png) |
| dashboard-01 / select-all-click | dark | 65×65 → 65×65 | 10.0 % / 2.3 % | 0 / 0 | 3 (3) | 0.5 |  | [ref](states/reference/dashboard-01-select-all-click-dark.png) · [gotx](states/gotx/dashboard-01-select-all-click-dark.png) · [compare](states/compare/dashboard-01-select-all-click-dark.png) |
| dashboard-01 / row-check-click | light | 65×65 → 65×65 | 17.0 % / 6.5 % | 0 / 0 | 3 (3) | 0.5 |  | [ref](states/reference/dashboard-01-row-check-click-light.png) · [gotx](states/gotx/dashboard-01-row-check-click-light.png) · [compare](states/compare/dashboard-01-row-check-click-light.png) |
| dashboard-01 / row-check-click | dark | 65×65 → 65×65 | 13.3 % / 6.5 % | 0 / 0 | 3 (3) | 0.5 |  | [ref](states/reference/dashboard-01-row-check-click-dark.png) · [gotx](states/gotx/dashboard-01-row-check-click-dark.png) · [compare](states/compare/dashboard-01-row-check-click-dark.png) |
| dashboard-01 / row-link-hover | light | 124×85 → 124×85 | 14.0 % / 7.8 % | 0 / 0 | 5 (5) | 0.5 |  | [ref](states/reference/dashboard-01-row-link-hover-light.png) · [gotx](states/gotx/dashboard-01-row-link-hover-light.png) · [compare](states/compare/dashboard-01-row-link-hover-light.png) |
| dashboard-01 / row-link-hover | dark | 124×85 → 124×85 | 15.9 % / 8.6 % | 0 / 0 | 5 (5) | 0.5 |  | [ref](states/reference/dashboard-01-row-link-hover-dark.png) · [gotx](states/gotx/dashboard-01-row-link-hover-dark.png) · [compare](states/compare/dashboard-01-row-link-hover-dark.png) |
| dashboard-01 / row-link-focus-key | light | 124×85 → 124×85 | 12.5 % / 9.1 % | 0 / 0 | 5 (5) | 0.5 |  | [ref](states/reference/dashboard-01-row-link-focus-key-light.png) · [gotx](states/gotx/dashboard-01-row-link-focus-key-light.png) · [compare](states/compare/dashboard-01-row-link-focus-key-light.png) |
| dashboard-01 / row-link-focus-key | dark | 124×85 → 124×85 | 14.5 % / 9.6 % | 0 / 0 | 5 (5) | 0.5 |  | [ref](states/reference/dashboard-01-row-link-focus-key-dark.png) · [gotx](states/gotx/dashboard-01-row-link-focus-key-dark.png) · [compare](states/compare/dashboard-01-row-link-focus-key-dark.png) |
| dashboard-01 / row-drag-hover | light | 76×77 → 76×77 | 6.5 % / 1.8 % | 0 / 0 | 1 (1) | 0 |  | [ref](states/reference/dashboard-01-row-drag-hover-light.png) · [gotx](states/gotx/dashboard-01-row-drag-hover-light.png) · [compare](states/compare/dashboard-01-row-drag-hover-light.png) |
| dashboard-01 / row-drag-hover | dark | 76×77 → 76×77 | 20.8 % / 2.1 % | 0 / 0 | 1 (1) | 0 |  | [ref](states/reference/dashboard-01-row-drag-hover-dark.png) · [gotx](states/gotx/dashboard-01-row-drag-hover-dark.png) · [compare](states/compare/dashboard-01-row-drag-hover-dark.png) |
| dashboard-01 / row-target-hover | light | 113×81 → 113×81 | 27.7 % / 2.5 % | 0 / 0 | 4 (4) | 0.5 |  | [ref](states/reference/dashboard-01-row-target-hover-light.png) · [gotx](states/gotx/dashboard-01-row-target-hover-light.png) · [compare](states/compare/dashboard-01-row-target-hover-light.png) |
| dashboard-01 / row-target-hover | dark | 113×81 → 113×81 | 8.3 % / 2.7 % | 0 / 0 | 4 (4) | 0.5 |  | [ref](states/reference/dashboard-01-row-target-hover-dark.png) · [gotx](states/gotx/dashboard-01-row-target-hover-dark.png) · [compare](states/compare/dashboard-01-row-target-hover-dark.png) |
| dashboard-01 / row-target-focus-key | light | 113×81 → 113×81 | 6.5 % / 5.0 % | 0 / 0 | 4 (4) | 0.5 |  | [ref](states/reference/dashboard-01-row-target-focus-key-light.png) · [gotx](states/gotx/dashboard-01-row-target-focus-key-light.png) · [compare](states/compare/dashboard-01-row-target-focus-key-light.png) |
| dashboard-01 / row-target-focus-key | dark | 113×81 → 113×81 | 6.5 % / 5.1 % | 0 / 0 | 4 (4) | 0.5 |  | [ref](states/reference/dashboard-01-row-target-focus-key-dark.png) · [gotx](states/gotx/dashboard-01-row-target-focus-key-dark.png) · [compare](states/compare/dashboard-01-row-target-focus-key-dark.png) |
| dashboard-01 / row-menu-hover | light | 81×81 → 81×81 | 6.4 % / 1.6 % | 0 / 0 | 1 (1) | 0 |  | [ref](states/reference/dashboard-01-row-menu-hover-light.png) · [gotx](states/gotx/dashboard-01-row-menu-hover-light.png) · [compare](states/compare/dashboard-01-row-menu-hover-light.png) |
| dashboard-01 / row-menu-hover | dark | 81×81 → 81×81 | 21.0 % / 1.6 % | 0 / 0 | 1 (1) | 0 |  | [ref](states/reference/dashboard-01-row-menu-hover-dark.png) · [gotx](states/gotx/dashboard-01-row-menu-hover-dark.png) · [compare](states/compare/dashboard-01-row-menu-hover-dark.png) |
| dashboard-01 / row-menu-click | light | 176×232 → 176×232 | 13.5 % / 5.9 % | 1 / 1 | 10 (10) | 0.5 |  | [ref](states/reference/dashboard-01-row-menu-click-light.png) · [gotx](states/gotx/dashboard-01-row-menu-click-light.png) · [compare](states/compare/dashboard-01-row-menu-click-light.png) |
| dashboard-01 / row-menu-click | dark | 176×232 → 176×232 | 17.5 % / 5.2 % | 1 / 1 | 10 (10) | 0.5 |  | [ref](states/reference/dashboard-01-row-menu-click-dark.png) · [gotx](states/gotx/dashboard-01-row-menu-click-dark.png) · [compare](states/compare/dashboard-01-row-menu-click-dark.png) |
| dashboard-01 / reviewer-hover | light | 201×81 → 201×81 | 20.0 % / 8.1 % | 0 / 0 | 6 (6) | 0.5 |  | [ref](states/reference/dashboard-01-reviewer-hover-light.png) · [gotx](states/gotx/dashboard-01-reviewer-hover-light.png) · [compare](states/compare/dashboard-01-reviewer-hover-light.png) |
| dashboard-01 / reviewer-hover | dark | 201×81 → 201×81 | 20.0 % / 11.7 % | 0 / 0 | 6 (6) | 0.5 |  | [ref](states/reference/dashboard-01-reviewer-hover-dark.png) · [gotx](states/gotx/dashboard-01-reviewer-hover-dark.png) · [compare](states/compare/dashboard-01-reviewer-hover-dark.png) |
| dashboard-01 / reviewer-click | light | 219×123 → 219×123 | 45.6 % / 12.7 % | 1 / 1 | 12 (8) | 0.5 | GotX content extends beyond the reference frame | [ref](states/reference/dashboard-01-reviewer-click-light.png) · [gotx](states/gotx/dashboard-01-reviewer-click-light.png) · [compare](states/compare/dashboard-01-reviewer-click-light.png) |
| dashboard-01 / reviewer-click | dark | 219×123 → 219×123 | 51.3 % / 14.2 % | 1 / 1 | 12 (8) | 0.5 | GotX content extends beyond the reference frame | [ref](states/reference/dashboard-01-reviewer-click-dark.png) · [gotx](states/gotx/dashboard-01-reviewer-click-dark.png) · [compare](states/compare/dashboard-01-reviewer-click-dark.png) |
| dashboard-01 / next-page-hover | light | 80×81 → 80×81 | 15.0 % / 5.6 % | 0 / 0 | 0 (0) | 0 |  | [ref](states/reference/dashboard-01-next-page-hover-light.png) · [gotx](states/gotx/dashboard-01-next-page-hover-light.png) · [compare](states/compare/dashboard-01-next-page-hover-light.png) |
| dashboard-01 / next-page-hover | dark | 80×81 → 80×81 | 24.6 % / 5.9 % | 0 / 0 | 0 (0) | 0 |  | [ref](states/reference/dashboard-01-next-page-hover-dark.png) · [gotx](states/gotx/dashboard-01-next-page-hover-dark.png) · [compare](states/compare/dashboard-01-next-page-hover-dark.png) |
| dashboard-01 / next-page-focus-key | light | 80×81 → 80×81 | 15.1 % / 6.1 % | 0 / 0 | 0 (0) | 0 |  | [ref](states/reference/dashboard-01-next-page-focus-key-light.png) · [gotx](states/gotx/dashboard-01-next-page-focus-key-light.png) · [compare](states/compare/dashboard-01-next-page-focus-key-light.png) |
| dashboard-01 / next-page-focus-key | dark | 80×81 → 80×81 | 35.9 % / 5.8 % | 0 / 0 | 0 (0) | 0 |  | [ref](states/reference/dashboard-01-next-page-focus-key-dark.png) · [gotx](states/gotx/dashboard-01-next-page-focus-key-dark.png) · [compare](states/compare/dashboard-01-next-page-focus-key-dark.png) |
| sidebar-07 / team-hover | light | 271×80 → 271×80 | 4.8 % / 3.3 % | 0 / 0 | 4 (4) | 0.3 |  | [ref](states/reference/sidebar-07-team-hover-light.png) · [gotx](states/gotx/sidebar-07-team-hover-light.png) · [compare](states/compare/sidebar-07-team-hover-light.png) |
| sidebar-07 / team-hover | dark | 271×80 → 271×80 | 5.4 % / 3.6 % | 0 / 0 | 4 (4) | 0.3 |  | [ref](states/reference/sidebar-07-team-hover-dark.png) · [gotx](states/gotx/sidebar-07-team-hover-dark.png) · [compare](states/compare/sidebar-07-team-hover-dark.png) |
| sidebar-07 / team-focus-key | light | 271×80 → 271×80 | 15.3 % / 11.1 % | 0 / 0 | 4 (4) | 0.3 |  | [ref](states/reference/sidebar-07-team-focus-key-light.png) · [gotx](states/gotx/sidebar-07-team-focus-key-light.png) · [compare](states/compare/sidebar-07-team-focus-key-light.png) |
| sidebar-07 / team-focus-key | dark | 271×80 → 271×80 | 15.7 % / 6.2 % | 0 / 0 | 4 (4) | 0.3 |  | [ref](states/reference/sidebar-07-team-focus-key-dark.png) · [gotx](states/gotx/sidebar-07-team-focus-key-dark.png) · [compare](states/compare/sidebar-07-team-focus-key-dark.png) |
| sidebar-07 / team-click | light | 514×239 → 514×239 | 20.0 % / 5.9 % | 1 / 1 | 27 (20) | 0 |  | [ref](states/reference/sidebar-07-team-click-light.png) · [gotx](states/gotx/sidebar-07-team-click-light.png) · [compare](states/compare/sidebar-07-team-click-light.png) |
| sidebar-07 / team-click | dark | 514×239 → 514×239 | 22.4 % / 5.1 % | 1 / 1 | 27 (20) | 0 |  | [ref](states/reference/sidebar-07-team-click-dark.png) · [gotx](states/gotx/sidebar-07-team-click-dark.png) · [compare](states/compare/sidebar-07-team-click-dark.png) |
| sidebar-07 / models-hover | light | 271×80 → 271×80 | 6.8 % / 5.0 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-07-models-hover-light.png) · [gotx](states/gotx/sidebar-07-models-hover-light.png) · [compare](states/compare/sidebar-07-models-hover-light.png) |
| sidebar-07 / models-hover | dark | 271×80 → 271×80 | 7.4 % / 5.3 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-07-models-hover-dark.png) · [gotx](states/gotx/sidebar-07-models-hover-dark.png) · [compare](states/compare/sidebar-07-models-hover-dark.png) |
| sidebar-07 / models-focus-key | light | 271×80 → 271×80 | 9.8 % / 7.5 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-07-models-focus-key-light.png) · [gotx](states/gotx/sidebar-07-models-focus-key-light.png) · [compare](states/compare/sidebar-07-models-focus-key-light.png) |
| sidebar-07 / models-focus-key | dark | 271×80 → 271×80 | 14.6 % / 12.7 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-07-models-focus-key-dark.png) · [gotx](states/gotx/sidebar-07-models-focus-key-dark.png) · [compare](states/compare/sidebar-07-models-focus-key-dark.png) |
| sidebar-07 / models-click | light | 271×80 → 271×80 | 5.5 % / 4.0 % | 0 / 0 | 9 (9) | 0 |  | [ref](states/reference/sidebar-07-models-click-light.png) · [gotx](states/gotx/sidebar-07-models-click-light.png) · [compare](states/compare/sidebar-07-models-click-light.png) |
| sidebar-07 / models-click | dark | 271×80 → 271×80 | 6.2 % / 4.5 % | 0 / 0 | 9 (9) | 0 |  | [ref](states/reference/sidebar-07-models-click-dark.png) · [gotx](states/gotx/sidebar-07-models-click-dark.png) · [compare](states/compare/sidebar-07-models-click-dark.png) |
| sidebar-07 / history-hover | light | 238×76 → 238×76 | 7.0 % / 5.1 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-07-history-hover-light.png) · [gotx](states/gotx/sidebar-07-history-hover-light.png) · [compare](states/compare/sidebar-07-history-hover-light.png) |
| sidebar-07 / history-hover | dark | 238×76 → 238×76 | 7.2 % / 5.6 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-07-history-hover-dark.png) · [gotx](states/gotx/sidebar-07-history-hover-dark.png) · [compare](states/compare/sidebar-07-history-hover-dark.png) |
| sidebar-07 / history-focus-key | light | 238×76 → 238×76 | 9.7 % / 7.4 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-07-history-focus-key-light.png) · [gotx](states/gotx/sidebar-07-history-focus-key-light.png) · [compare](states/compare/sidebar-07-history-focus-key-light.png) |
| sidebar-07 / history-focus-key | dark | 238×76 → 238×76 | 14.1 % / 12.7 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-07-history-focus-key-dark.png) · [gotx](states/gotx/sidebar-07-history-focus-key-dark.png) · [compare](states/compare/sidebar-07-history-focus-key-dark.png) |
| sidebar-07 / history-press | light | 238×76 → 238×76 | 7.0 % / 5.1 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-07-history-press-light.png) · [gotx](states/gotx/sidebar-07-history-press-light.png) · [compare](states/compare/sidebar-07-history-press-light.png) |
| sidebar-07 / history-press | dark | 238×76 → 238×76 | 7.2 % / 5.6 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-07-history-press-dark.png) · [gotx](states/gotx/sidebar-07-history-press-dark.png) · [compare](states/compare/sidebar-07-history-press-dark.png) |
| sidebar-07 / project-hover | light | 271×80 → 271×80 | 9.5 % / 6.9 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/sidebar-07-project-hover-light.png) · [gotx](states/gotx/sidebar-07-project-hover-light.png) · [compare](states/compare/sidebar-07-project-hover-light.png) |
| sidebar-07 / project-hover | dark | 271×80 → 271×80 | 10.2 % / 7.7 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/sidebar-07-project-hover-dark.png) · [gotx](states/gotx/sidebar-07-project-hover-dark.png) · [compare](states/compare/sidebar-07-project-hover-dark.png) |
| sidebar-07 / project-action-click | light | 264×163 → 264×163 | 25.7 % / 2.0 % | 1 / 1 | 10 (8) | 0 |  | [ref](states/reference/sidebar-07-project-action-click-light.png) · [gotx](states/gotx/sidebar-07-project-action-click-light.png) · [compare](states/compare/sidebar-07-project-action-click-light.png) |
| sidebar-07 / project-action-click | dark | 264×163 → 264×163 | 27.7 % / 3.0 % | 1 / 1 | 10 (8) | 0 |  | [ref](states/reference/sidebar-07-project-action-click-dark.png) · [gotx](states/gotx/sidebar-07-project-action-click-dark.png) · [compare](states/compare/sidebar-07-project-action-click-dark.png) |
| sidebar-07 / user-hover | light | 271×80 → 271×80 | 9.4 % / 8.4 % | 0 / 0 | 3 (3) | 0.3 |  | [ref](states/reference/sidebar-07-user-hover-light.png) · [gotx](states/gotx/sidebar-07-user-hover-light.png) · [compare](states/compare/sidebar-07-user-hover-light.png) |
| sidebar-07 / user-hover | dark | 271×80 → 271×80 | 10.0 % / 8.7 % | 0 / 0 | 3 (3) | 0.3 |  | [ref](states/reference/sidebar-07-user-hover-dark.png) · [gotx](states/gotx/sidebar-07-user-hover-dark.png) · [compare](states/compare/sidebar-07-user-hover-dark.png) |
| sidebar-07 / user-click | light | 514×274 → 514×274 | 26.3 % / 5.4 % | 1 / 1 | 11 (8) | 0.5 | GotX content extends beyond the reference frame | [ref](states/reference/sidebar-07-user-click-light.png) · [gotx](states/gotx/sidebar-07-user-click-light.png) · [compare](states/compare/sidebar-07-user-click-light.png) |
| sidebar-07 / user-click | dark | 514×274 → 514×274 | 26.7 % / 4.8 % | 1 / 1 | 11 (8) | 0.5 | GotX content extends beyond the reference frame | [ref](states/reference/sidebar-07-user-click-dark.png) · [gotx](states/gotx/sidebar-07-user-click-dark.png) · [compare](states/compare/sidebar-07-user-click-dark.png) |
| sidebar-07 / trigger-hover | light | 76×70 → 76×70 | 2.2 % / 0.2 % | 0 / 0 | 2 (2) | 0 |  | [ref](states/reference/sidebar-07-trigger-hover-light.png) · [gotx](states/gotx/sidebar-07-trigger-hover-light.png) · [compare](states/compare/sidebar-07-trigger-hover-light.png) |
| sidebar-07 / trigger-hover | dark | 76×70 → 76×70 | 15.9 % / 0.2 % | 0 / 0 | 2 (2) | 0 |  | [ref](states/reference/sidebar-07-trigger-hover-dark.png) · [gotx](states/gotx/sidebar-07-trigger-hover-dark.png) · [compare](states/compare/sidebar-07-trigger-hover-dark.png) |
| sidebar-07 / trigger-focus-key | light | 76×70 → 76×70 | 5.8 % / 0.8 % | 0 / 0 | 2 (2) | 0 |  | [ref](states/reference/sidebar-07-trigger-focus-key-light.png) · [gotx](states/gotx/sidebar-07-trigger-focus-key-light.png) · [compare](states/compare/sidebar-07-trigger-focus-key-light.png) |
| sidebar-07 / trigger-focus-key | dark | 76×70 → 76×70 | 7.6 % / 1.3 % | 0 / 0 | 2 (2) | 0 |  | [ref](states/reference/sidebar-07-trigger-focus-key-dark.png) · [gotx](states/gotx/sidebar-07-trigger-focus-key-dark.png) · [compare](states/compare/sidebar-07-trigger-focus-key-dark.png) |
| sidebar-07 / trigger-click | light | 76×70 → 76×70 | 5.8 % / 4.7 % | 0 / 0 | 3 (3) | 0.5 |  | [ref](states/reference/sidebar-07-trigger-click-light.png) · [gotx](states/gotx/sidebar-07-trigger-click-light.png) · [compare](states/compare/sidebar-07-trigger-click-light.png) |
| sidebar-07 / trigger-click | dark | 76×70 → 76×70 | 5.9 % / 5.1 % | 0 / 0 | 3 (3) | 0.5 |  | [ref](states/reference/sidebar-07-trigger-click-dark.png) · [gotx](states/gotx/sidebar-07-trigger-click-dark.png) · [compare](states/compare/sidebar-07-trigger-click-dark.png) |
| sidebar-07 / crumb-hover | light | 188×65 → 188×65 | 7.7 % / 6.5 % | 0 / 0 | 2 (2) | 0 |  | [ref](states/reference/sidebar-07-crumb-hover-light.png) · [gotx](states/gotx/sidebar-07-crumb-hover-light.png) · [compare](states/compare/sidebar-07-crumb-hover-light.png) |
| sidebar-07 / crumb-hover | dark | 188×65 → 188×65 | 7.9 % / 7.0 % | 0 / 0 | 2 (2) | 0 |  | [ref](states/reference/sidebar-07-crumb-hover-dark.png) · [gotx](states/gotx/sidebar-07-crumb-hover-dark.png) · [compare](states/compare/sidebar-07-crumb-hover-dark.png) |
| sidebar-03 / brand-hover | light | 271×80 → 271×80 | 5.3 % / 3.8 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/sidebar-03-brand-hover-light.png) · [gotx](states/gotx/sidebar-03-brand-hover-light.png) · [compare](states/compare/sidebar-03-brand-hover-light.png) |
| sidebar-03 / brand-hover | dark | 271×80 → 271×80 | 5.9 % / 4.1 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/sidebar-03-brand-hover-dark.png) · [gotx](states/gotx/sidebar-03-brand-hover-dark.png) · [compare](states/compare/sidebar-03-brand-hover-dark.png) |
| sidebar-03 / brand-focus-key | light | 271×80 → 271×80 | 8.2 % / 6.5 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/sidebar-03-brand-focus-key-light.png) · [gotx](states/gotx/sidebar-03-brand-focus-key-light.png) · [compare](states/compare/sidebar-03-brand-focus-key-light.png) |
| sidebar-03 / brand-focus-key | dark | 271×80 → 271×80 | 13.6 % / 11.9 % | 0 / 0 | 6 (6) | 0 |  | [ref](states/reference/sidebar-03-brand-focus-key-dark.png) · [gotx](states/gotx/sidebar-03-brand-focus-key-dark.png) · [compare](states/compare/sidebar-03-brand-focus-key-dark.png) |
| sidebar-03 / getting-started-hover | light | 271×80 → 271×80 | 6.8 % / 5.6 % | 0 / 0 | 7 (7) | 0 |  | [ref](states/reference/sidebar-03-getting-started-hover-light.png) · [gotx](states/gotx/sidebar-03-getting-started-hover-light.png) · [compare](states/compare/sidebar-03-getting-started-hover-light.png) |
| sidebar-03 / getting-started-hover | dark | 271×80 → 271×80 | 7.5 % / 6.0 % | 0 / 0 | 7 (7) | 0 |  | [ref](states/reference/sidebar-03-getting-started-hover-dark.png) · [gotx](states/gotx/sidebar-03-getting-started-hover-dark.png) · [compare](states/compare/sidebar-03-getting-started-hover-dark.png) |
| sidebar-03 / getting-started-focus-key | light | 271×80 → 271×80 | 9.7 % / 8.1 % | 0 / 0 | 7 (7) | 0 |  | [ref](states/reference/sidebar-03-getting-started-focus-key-light.png) · [gotx](states/gotx/sidebar-03-getting-started-focus-key-light.png) · [compare](states/compare/sidebar-03-getting-started-focus-key-light.png) |
| sidebar-03 / getting-started-focus-key | dark | 271×80 → 271×80 | 14.7 % / 13.4 % | 0 / 0 | 7 (7) | 0 |  | [ref](states/reference/sidebar-03-getting-started-focus-key-dark.png) · [gotx](states/gotx/sidebar-03-getting-started-focus-key-dark.png) · [compare](states/compare/sidebar-03-getting-started-focus-key-dark.png) |
| sidebar-03 / installation-hover | light | 238×76 → 238×76 | 10.9 % / 8.5 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-03-installation-hover-light.png) · [gotx](states/gotx/sidebar-03-installation-hover-light.png) · [compare](states/compare/sidebar-03-installation-hover-light.png) |
| sidebar-03 / installation-hover | dark | 238×76 → 238×76 | 11.1 % / 9.4 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-03-installation-hover-dark.png) · [gotx](states/gotx/sidebar-03-installation-hover-dark.png) · [compare](states/compare/sidebar-03-installation-hover-dark.png) |
| sidebar-03 / installation-focus-key | light | 238×76 → 238×76 | 13.6 % / 10.8 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-03-installation-focus-key-light.png) · [gotx](states/gotx/sidebar-03-installation-focus-key-light.png) · [compare](states/compare/sidebar-03-installation-focus-key-light.png) |
| sidebar-03 / installation-focus-key | dark | 238×76 → 238×76 | 17.9 % / 16.4 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-03-installation-focus-key-dark.png) · [gotx](states/gotx/sidebar-03-installation-focus-key-dark.png) · [compare](states/compare/sidebar-03-installation-focus-key-dark.png) |
| sidebar-03 / installation-press | light | 238×76 → 238×76 | 10.9 % / 8.5 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-03-installation-press-light.png) · [gotx](states/gotx/sidebar-03-installation-press-light.png) · [compare](states/compare/sidebar-03-installation-press-light.png) |
| sidebar-03 / installation-press | dark | 238×76 → 238×76 | 11.1 % / 9.4 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-03-installation-press-dark.png) · [gotx](states/gotx/sidebar-03-installation-press-dark.png) · [compare](states/compare/sidebar-03-installation-press-dark.png) |
| sidebar-03 / installation-click | light | 238×76 → 238×76 | 10.9 % / 8.5 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-03-installation-click-light.png) · [gotx](states/gotx/sidebar-03-installation-click-light.png) · [compare](states/compare/sidebar-03-installation-click-light.png) |
| sidebar-03 / installation-click | dark | 238×76 → 238×76 | 11.1 % / 9.4 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-03-installation-click-dark.png) · [gotx](states/gotx/sidebar-03-installation-click-dark.png) · [compare](states/compare/sidebar-03-installation-click-dark.png) |
| sidebar-03 / data-fetching-hover | light | 238×76 → 238×76 | 7.8 % / 6.3 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-03-data-fetching-hover-light.png) · [gotx](states/gotx/sidebar-03-data-fetching-hover-light.png) · [compare](states/compare/sidebar-03-data-fetching-hover-light.png) |
| sidebar-03 / data-fetching-hover | dark | 238×76 → 238×76 | 8.2 % / 7.0 % | 0 / 0 | 8 (8) | 0 |  | [ref](states/reference/sidebar-03-data-fetching-hover-dark.png) · [gotx](states/gotx/sidebar-03-data-fetching-hover-dark.png) · [compare](states/compare/sidebar-03-data-fetching-hover-dark.png) |
| sidebar-03 / trigger-hover | light | 76×70 → 76×70 | 2.7 % / 0.7 % | 0 / 0 | 2 (2) | 0 |  | [ref](states/reference/sidebar-03-trigger-hover-light.png) · [gotx](states/gotx/sidebar-03-trigger-hover-light.png) · [compare](states/compare/sidebar-03-trigger-hover-light.png) |
| sidebar-03 / trigger-hover | dark | 76×70 → 76×70 | 17.1 % / 0.8 % | 0 / 0 | 2 (2) | 0 |  | [ref](states/reference/sidebar-03-trigger-hover-dark.png) · [gotx](states/gotx/sidebar-03-trigger-hover-dark.png) · [compare](states/compare/sidebar-03-trigger-hover-dark.png) |
| sidebar-03 / trigger-focus-key | light | 76×70 → 76×70 | 6.0 % / 1.1 % | 0 / 0 | 1 (1) | 0 |  | [ref](states/reference/sidebar-03-trigger-focus-key-light.png) · [gotx](states/gotx/sidebar-03-trigger-focus-key-light.png) · [compare](states/compare/sidebar-03-trigger-focus-key-light.png) |
| sidebar-03 / trigger-focus-key | dark | 76×70 → 76×70 | 8.6 % / 1.6 % | 0 / 0 | 1 (1) | 0 |  | [ref](states/reference/sidebar-03-trigger-focus-key-dark.png) · [gotx](states/gotx/sidebar-03-trigger-focus-key-dark.png) · [compare](states/compare/sidebar-03-trigger-focus-key-dark.png) |
| sidebar-03 / trigger-click | light | 76×70 → 76×70 | 11.2 % / 9.8 % | 0 / 0 | 1 (1) | 0.5 |  | [ref](states/reference/sidebar-03-trigger-click-light.png) · [gotx](states/gotx/sidebar-03-trigger-click-light.png) · [compare](states/compare/sidebar-03-trigger-click-light.png) |
| sidebar-03 / trigger-click | dark | 76×70 → 76×70 | 12.7 % / 10.5 % | 0 / 0 | 1 (1) | 0.5 |  | [ref](states/reference/sidebar-03-trigger-click-dark.png) · [gotx](states/gotx/sidebar-03-trigger-click-dark.png) · [compare](states/compare/sidebar-03-trigger-click-dark.png) |
| login-03 / logo-hover | light | 154×73 → 154×73 | 11.0 % / 4.9 % | 0 / 0 | 1 (1) | 0.6 |  | [ref](states/reference/login-03-logo-hover-light.png) · [gotx](states/gotx/login-03-logo-hover-light.png) · [compare](states/compare/login-03-logo-hover-light.png) |
| login-03 / logo-hover | dark | 154×73 → 154×73 | 7.8 % / 4.5 % | 0 / 0 | 1 (1) | 0.6 |  | [ref](states/reference/login-03-logo-hover-dark.png) · [gotx](states/gotx/login-03-logo-hover-dark.png) · [compare](states/compare/login-03-logo-hover-dark.png) |
| login-03 / logo-focus-key | light | 154×73 → 154×73 | 16.2 % / 5.3 % | 0 / 0 | 1 (1) | 0.6 |  | [ref](states/reference/login-03-logo-focus-key-light.png) · [gotx](states/gotx/login-03-logo-focus-key-light.png) · [compare](states/compare/login-03-logo-focus-key-light.png) |
| login-03 / logo-focus-key | dark | 154×73 → 154×73 | 13.0 % / 5.0 % | 0 / 0 | 1 (1) | 0.6 |  | [ref](states/reference/login-03-logo-focus-key-dark.png) · [gotx](states/gotx/login-03-logo-focus-key-dark.png) · [compare](states/compare/login-03-logo-focus-key-dark.png) |
| login-03 / apple-hover | light | 382×85 → 382×85 | 7.3 % / 1.9 % | 0 / 0 | 6 (6) | 0.1 |  | [ref](states/reference/login-03-apple-hover-light.png) · [gotx](states/gotx/login-03-apple-hover-light.png) · [compare](states/compare/login-03-apple-hover-light.png) |
| login-03 / apple-hover | dark | 382×85 → 382×85 | 7.3 % / 2.1 % | 0 / 0 | 6 (6) | 0.1 |  | [ref](states/reference/login-03-apple-hover-dark.png) · [gotx](states/gotx/login-03-apple-hover-dark.png) · [compare](states/compare/login-03-apple-hover-dark.png) |
| login-03 / apple-focus-key | light | 382×85 → 382×85 | 5.7 % / 2.0 % | 0 / 0 | 6 (6) | 0.1 |  | [ref](states/reference/login-03-apple-focus-key-light.png) · [gotx](states/gotx/login-03-apple-focus-key-light.png) · [compare](states/compare/login-03-apple-focus-key-light.png) |
| login-03 / apple-focus-key | dark | 382×85 → 382×85 | 6.3 % / 4.3 % | 0 / 0 | 6 (6) | 0.1 |  | [ref](states/reference/login-03-apple-focus-key-dark.png) · [gotx](states/gotx/login-03-apple-focus-key-dark.png) · [compare](states/compare/login-03-apple-focus-key-dark.png) |
| login-03 / apple-press | light | 382×85 → 382×85 | 7.3 % / 1.9 % | 0 / 0 | 6 (6) | 0.1 |  | [ref](states/reference/login-03-apple-press-light.png) · [gotx](states/gotx/login-03-apple-press-light.png) · [compare](states/compare/login-03-apple-press-light.png) |
| login-03 / apple-press | dark | 382×85 → 382×85 | 7.3 % / 2.1 % | 0 / 0 | 6 (6) | 0.1 |  | [ref](states/reference/login-03-apple-press-dark.png) · [gotx](states/gotx/login-03-apple-press-dark.png) · [compare](states/compare/login-03-apple-press-dark.png) |
| login-03 / forgot-hover | light | 195×69 → 195×69 | 9.5 % / 5.5 % | 0 / 0 | 2 (2) | 0.1 |  | [ref](states/reference/login-03-forgot-hover-light.png) · [gotx](states/gotx/login-03-forgot-hover-light.png) · [compare](states/compare/login-03-forgot-hover-light.png) |
| login-03 / forgot-hover | dark | 195×69 → 195×69 | 8.4 % / 6.0 % | 0 / 0 | 2 (2) | 0.1 |  | [ref](states/reference/login-03-forgot-hover-dark.png) · [gotx](states/gotx/login-03-forgot-hover-dark.png) · [compare](states/compare/login-03-forgot-hover-dark.png) |
| login-03 / forgot-focus-key | light | 195×69 → 195×69 | 8.6 % / 5.5 % | 0 / 0 | 2 (2) | 0.1 |  | [ref](states/reference/login-03-forgot-focus-key-light.png) · [gotx](states/gotx/login-03-forgot-focus-key-light.png) · [compare](states/compare/login-03-forgot-focus-key-light.png) |
| login-03 / forgot-focus-key | dark | 195×69 → 195×69 | 13.8 % / 6.0 % | 0 / 0 | 2 (2) | 0.1 |  | [ref](states/reference/login-03-forgot-focus-key-dark.png) · [gotx](states/gotx/login-03-forgot-focus-key-dark.png) · [compare](states/compare/login-03-forgot-focus-key-dark.png) |
| login-03 / submit-hover | light | 382×85 → 382×85 | 39.9 % / 3.2 % | 0 / 0 | 5 (4) | 0.3 |  | [ref](states/reference/login-03-submit-hover-light.png) · [gotx](states/gotx/login-03-submit-hover-light.png) · [compare](states/compare/login-03-submit-hover-light.png) |
| login-03 / submit-hover | dark | 382×85 → 382×85 | 39.9 % / 3.2 % | 0 / 0 | 5 (4) | 0.3 |  | [ref](states/reference/login-03-submit-hover-dark.png) · [gotx](states/gotx/login-03-submit-hover-dark.png) · [compare](states/compare/login-03-submit-hover-dark.png) |
| login-03 / submit-focus-key | light | 382×85 → 382×85 | 40.1 % / 3.2 % | 0 / 0 | 5 (4) | 0.3 |  | [ref](states/reference/login-03-submit-focus-key-light.png) · [gotx](states/gotx/login-03-submit-focus-key-light.png) · [compare](states/compare/login-03-submit-focus-key-light.png) |
| login-03 / submit-focus-key | dark | 382×85 → 382×85 | 4.2 % / 3.3 % | 0 / 0 | 5 (4) | 0.3 |  | [ref](states/reference/login-03-submit-focus-key-dark.png) · [gotx](states/gotx/login-03-submit-focus-key-dark.png) · [compare](states/compare/login-03-submit-focus-key-dark.png) |
| login-03 / submit-press | light | 382×85 → 382×85 | 39.9 % / 3.2 % | 0 / 0 | 5 (4) | 0.3 |  | [ref](states/reference/login-03-submit-press-light.png) · [gotx](states/gotx/login-03-submit-press-light.png) · [compare](states/compare/login-03-submit-press-light.png) |
| login-03 / submit-press | dark | 382×85 → 382×85 | 39.9 % / 3.2 % | 0 / 0 | 5 (4) | 0.3 |  | [ref](states/reference/login-03-submit-press-dark.png) · [gotx](states/gotx/login-03-submit-press-dark.png) · [compare](states/compare/login-03-submit-press-dark.png) |
| login-03 / sign-up-hover | light | 98×67 → 98×67 | 26.3 % / 8.6 % | 0 / 0 | 4 (3) | 0.7 |  | [ref](states/reference/login-03-sign-up-hover-light.png) · [gotx](states/gotx/login-03-sign-up-hover-light.png) · [compare](states/compare/login-03-sign-up-hover-light.png) |
| login-03 / sign-up-hover | dark | 98×67 → 98×67 | 10.0 % / 9.0 % | 0 / 0 | 4 (3) | 0.7 |  | [ref](states/reference/login-03-sign-up-hover-dark.png) · [gotx](states/gotx/login-03-sign-up-hover-dark.png) · [compare](states/compare/login-03-sign-up-hover-dark.png) |
| login-03 / sign-up-focus-key | light | 98×67 → 98×67 | 31.0 % / 12.1 % | 0 / 0 | 4 (3) | 0.7 |  | [ref](states/reference/login-03-sign-up-focus-key-light.png) · [gotx](states/gotx/login-03-sign-up-focus-key-light.png) · [compare](states/compare/login-03-sign-up-focus-key-light.png) |
| login-03 / sign-up-focus-key | dark | 98×67 → 98×67 | 16.0 % / 12.5 % | 0 / 0 | 4 (3) | 0.7 |  | [ref](states/reference/login-03-sign-up-focus-key-dark.png) · [gotx](states/gotx/login-03-sign-up-focus-key-dark.png) · [compare](states/compare/login-03-sign-up-focus-key-dark.png) |
| login-03 / email-hover | light | 382×85 → 382×85 | 6.5 % / 2.1 % | 0 / 0 | 2 (2) | 0.3 |  | [ref](states/reference/login-03-email-hover-light.png) · [gotx](states/gotx/login-03-email-hover-light.png) · [compare](states/compare/login-03-email-hover-light.png) |
| login-03 / email-hover | dark | 382×85 → 382×85 | 6.5 % / 2.2 % | 0 / 0 | 2 (2) | 0.3 |  | [ref](states/reference/login-03-email-hover-dark.png) · [gotx](states/gotx/login-03-email-hover-dark.png) · [compare](states/compare/login-03-email-hover-dark.png) |
| login-03 / email-focus-key | light | 382×85 → 382×85 | 4.6 % / 2.2 % | 0 / 0 | 2 (2) | 0.3 |  | [ref](states/reference/login-03-email-focus-key-light.png) · [gotx](states/gotx/login-03-email-focus-key-light.png) · [compare](states/compare/login-03-email-focus-key-light.png) |
| login-03 / email-focus-key | dark | 382×85 → 382×85 | 3.5 % / 2.2 % | 0 / 0 | 2 (2) | 0.3 |  | [ref](states/reference/login-03-email-focus-key-dark.png) · [gotx](states/gotx/login-03-email-focus-key-dark.png) · [compare](states/compare/login-03-email-focus-key-dark.png) |
| login-03 / email-type | light | 382×85 → 382×85 | 4.6 % / 2.5 % | 0 / 0 | 3 (3) | 0.3 |  | [ref](states/reference/login-03-email-type-light.png) · [gotx](states/gotx/login-03-email-type-light.png) · [compare](states/compare/login-03-email-type-light.png) |
| login-03 / email-type | dark | 382×85 → 382×85 | 3.5 % / 2.7 % | 0 / 0 | 3 (3) | 0.3 |  | [ref](states/reference/login-03-email-type-dark.png) · [gotx](states/gotx/login-03-email-type-dark.png) · [compare](states/compare/login-03-email-type-dark.png) |
| login-03 / password-type | light | 382×85 → 382×85 | 6.0 % / 3.4 % | 0 / 0 | 4 (4) | 0.3 |  | [ref](states/reference/login-03-password-type-light.png) · [gotx](states/gotx/login-03-password-type-light.png) · [compare](states/compare/login-03-password-type-light.png) |
| login-03 / password-type | dark | 382×85 → 382×85 | 4.8 % / 3.7 % | 0 / 0 | 4 (4) | 0.3 |  | [ref](states/reference/login-03-password-type-dark.png) · [gotx](states/gotx/login-03-password-type-dark.png) · [compare](states/compare/login-03-password-type-dark.png) |
| login-03 / privacy-hover | light | 136×67 → 136×67 | 21.7 % / 18.6 % | 0 / 0 | 1 (1) | 0.8 |  | [ref](states/reference/login-03-privacy-hover-light.png) · [gotx](states/gotx/login-03-privacy-hover-light.png) · [compare](states/compare/login-03-privacy-hover-light.png) |
| login-03 / privacy-hover | dark | 136×67 → 136×67 | 22.1 % / 19.1 % | 0 / 0 | 1 (1) | 0.8 |  | [ref](states/reference/login-03-privacy-hover-dark.png) · [gotx](states/gotx/login-03-privacy-hover-dark.png) · [compare](states/compare/login-03-privacy-hover-dark.png) |
| login-03 / privacy-focus-key | light | 136×67 → 136×67 | 27.9 % / 22.3 % | 0 / 0 | 1 (1) | 0.8 |  | [ref](states/reference/login-03-privacy-focus-key-light.png) · [gotx](states/gotx/login-03-privacy-focus-key-light.png) · [compare](states/compare/login-03-privacy-focus-key-light.png) |
| login-03 / privacy-focus-key | dark | 136×67 → 136×67 | 28.3 % / 22.9 % | 0 / 0 | 1 (1) | 0.8 |  | [ref](states/reference/login-03-privacy-focus-key-dark.png) · [gotx](states/gotx/login-03-privacy-focus-key-dark.png) · [compare](states/compare/login-03-privacy-focus-key-dark.png) |
| login-04 / apple-hover | light | 165×85 → 165×85 | 4.9 % / 0.1 % | 0 / 0 | 2 (2) | 0.4 |  | [ref](states/reference/login-04-apple-hover-light.png) · [gotx](states/gotx/login-04-apple-hover-light.png) · [compare](states/compare/login-04-apple-hover-light.png) |
| login-04 / apple-hover | dark | 165×85 → 165×85 | 4.8 % / 0.1 % | 0 / 0 | 2 (2) | 0.4 |  | [ref](states/reference/login-04-apple-hover-dark.png) · [gotx](states/gotx/login-04-apple-hover-dark.png) · [compare](states/compare/login-04-apple-hover-dark.png) |
| login-04 / apple-focus-key | light | 165×85 → 165×85 | 4.1 % / 0.1 % | 0 / 0 | 2 (2) | 0.4 |  | [ref](states/reference/login-04-apple-focus-key-light.png) · [gotx](states/gotx/login-04-apple-focus-key-light.png) · [compare](states/compare/login-04-apple-focus-key-light.png) |
| login-04 / apple-focus-key | dark | 165×85 → 165×85 | 4.0 % / 2.2 % | 0 / 0 | 2 (2) | 0.4 |  | [ref](states/reference/login-04-apple-focus-key-dark.png) · [gotx](states/gotx/login-04-apple-focus-key-dark.png) · [compare](states/compare/login-04-apple-focus-key-dark.png) |
| login-04 / apple-press | light | 165×85 → 165×85 | 4.9 % / 0.1 % | 0 / 0 | 2 (2) | 0.4 |  | [ref](states/reference/login-04-apple-press-light.png) · [gotx](states/gotx/login-04-apple-press-light.png) · [compare](states/compare/login-04-apple-press-light.png) |
| login-04 / apple-press | dark | 165×85 → 165×85 | 4.8 % / 0.1 % | 0 / 0 | 2 (2) | 0.4 |  | [ref](states/reference/login-04-apple-press-dark.png) · [gotx](states/gotx/login-04-apple-press-dark.png) · [compare](states/compare/login-04-apple-press-dark.png) |
| login-04 / meta-hover | light | 165×85 → 165×85 | 5.2 % / 0.0 % | 0 / 0 | 2 (2) | 0.4 |  | [ref](states/reference/login-04-meta-hover-light.png) · [gotx](states/gotx/login-04-meta-hover-light.png) · [compare](states/compare/login-04-meta-hover-light.png) |
| login-04 / meta-hover | dark | 165×85 → 165×85 | 5.1 % / 0.0 % | 0 / 0 | 2 (2) | 0.4 |  | [ref](states/reference/login-04-meta-hover-dark.png) · [gotx](states/gotx/login-04-meta-hover-dark.png) · [compare](states/compare/login-04-meta-hover-dark.png) |
| login-04 / forgot-hover | light | 195×69 → 195×69 | 9.4 % / 5.6 % | 0 / 0 | 2 (2) | 0.4 |  | [ref](states/reference/login-04-forgot-hover-light.png) · [gotx](states/gotx/login-04-forgot-hover-light.png) · [compare](states/compare/login-04-forgot-hover-light.png) |
| login-04 / forgot-hover | dark | 195×69 → 195×69 | 8.4 % / 6.1 % | 0 / 0 | 2 (2) | 0.4 |  | [ref](states/reference/login-04-forgot-hover-dark.png) · [gotx](states/gotx/login-04-forgot-hover-dark.png) · [compare](states/compare/login-04-forgot-hover-dark.png) |
| login-04 / forgot-focus-key | light | 195×69 → 195×69 | 8.6 % / 5.5 % | 0 / 0 | 2 (2) | 0.4 |  | [ref](states/reference/login-04-forgot-focus-key-light.png) · [gotx](states/gotx/login-04-forgot-focus-key-light.png) · [compare](states/compare/login-04-forgot-focus-key-light.png) |
| login-04 / forgot-focus-key | dark | 195×69 → 195×69 | 13.8 % / 6.0 % | 0 / 0 | 2 (2) | 0.4 |  | [ref](states/reference/login-04-forgot-focus-key-dark.png) · [gotx](states/gotx/login-04-forgot-focus-key-dark.png) · [compare](states/compare/login-04-forgot-focus-key-dark.png) |
| login-04 / submit-hover | light | 431×85 → 431×85 | 37.5 % / 0.6 % | 0 / 0 | 4 (4) | 0.5 |  | [ref](states/reference/login-04-submit-hover-light.png) · [gotx](states/gotx/login-04-submit-hover-light.png) · [compare](states/compare/login-04-submit-hover-light.png) |
| login-04 / submit-hover | dark | 431×85 → 431×85 | 37.5 % / 0.5 % | 0 / 0 | 4 (4) | 0.5 |  | [ref](states/reference/login-04-submit-hover-dark.png) · [gotx](states/gotx/login-04-submit-hover-dark.png) · [compare](states/compare/login-04-submit-hover-dark.png) |
| login-04 / submit-focus-key | light | 431×85 → 431×85 | 37.7 % / 0.6 % | 0 / 0 | 4 (4) | 0.5 |  | [ref](states/reference/login-04-submit-focus-key-light.png) · [gotx](states/gotx/login-04-submit-focus-key-light.png) · [compare](states/compare/login-04-submit-focus-key-light.png) |
| login-04 / submit-focus-key | dark | 431×85 → 431×85 | 1.0 % / 0.5 % | 0 / 0 | 4 (4) | 0.5 |  | [ref](states/reference/login-04-submit-focus-key-dark.png) · [gotx](states/gotx/login-04-submit-focus-key-dark.png) · [compare](states/compare/login-04-submit-focus-key-dark.png) |
| login-04 / submit-press | light | 431×85 → 431×85 | 37.5 % / 0.6 % | 0 / 0 | 4 (4) | 0.5 |  | [ref](states/reference/login-04-submit-press-light.png) · [gotx](states/gotx/login-04-submit-press-light.png) · [compare](states/compare/login-04-submit-press-light.png) |
| login-04 / submit-press | dark | 431×85 → 431×85 | 37.5 % / 0.5 % | 0 / 0 | 4 (4) | 0.5 |  | [ref](states/reference/login-04-submit-press-dark.png) · [gotx](states/gotx/login-04-submit-press-dark.png) · [compare](states/compare/login-04-submit-press-dark.png) |
| login-04 / sign-up-hover | light | 98×67 → 98×67 | 8.3 % / 6.1 % | 0 / 0 | 3 (2) | 0.9 |  | [ref](states/reference/login-04-sign-up-hover-light.png) · [gotx](states/gotx/login-04-sign-up-hover-light.png) · [compare](states/compare/login-04-sign-up-hover-light.png) |
| login-04 / sign-up-hover | dark | 98×67 → 98×67 | 7.6 % / 6.3 % | 0 / 0 | 3 (2) | 0.9 |  | [ref](states/reference/login-04-sign-up-hover-dark.png) · [gotx](states/gotx/login-04-sign-up-hover-dark.png) · [compare](states/compare/login-04-sign-up-hover-dark.png) |
| login-04 / sign-up-focus-key | light | 98×67 → 98×67 | 12.6 % / 9.3 % | 0 / 0 | 3 (2) | 0.9 |  | [ref](states/reference/login-04-sign-up-focus-key-light.png) · [gotx](states/gotx/login-04-sign-up-focus-key-light.png) · [compare](states/compare/login-04-sign-up-focus-key-light.png) |
| login-04 / sign-up-focus-key | dark | 98×67 → 98×67 | 14.6 % / 9.5 % | 0 / 0 | 3 (2) | 0.9 |  | [ref](states/reference/login-04-sign-up-focus-key-dark.png) · [gotx](states/gotx/login-04-sign-up-focus-key-dark.png) · [compare](states/compare/login-04-sign-up-focus-key-dark.png) |
| login-04 / email-hover | light | 431×85 → 431×85 | 6.2 % / 2.0 % | 0 / 0 | 2 (2) | 0.2 |  | [ref](states/reference/login-04-email-hover-light.png) · [gotx](states/gotx/login-04-email-hover-light.png) · [compare](states/compare/login-04-email-hover-light.png) |
| login-04 / email-hover | dark | 431×85 → 431×85 | 6.3 % / 2.1 % | 0 / 0 | 2 (2) | 0.2 |  | [ref](states/reference/login-04-email-hover-dark.png) · [gotx](states/gotx/login-04-email-hover-dark.png) · [compare](states/compare/login-04-email-hover-dark.png) |
| login-04 / email-focus-key | light | 431×85 → 431×85 | 4.3 % / 2.1 % | 0 / 0 | 2 (2) | 0.2 |  | [ref](states/reference/login-04-email-focus-key-light.png) · [gotx](states/gotx/login-04-email-focus-key-light.png) · [compare](states/compare/login-04-email-focus-key-light.png) |
| login-04 / email-focus-key | dark | 431×85 → 431×85 | 3.2 % / 2.1 % | 0 / 0 | 2 (2) | 0.2 |  | [ref](states/reference/login-04-email-focus-key-dark.png) · [gotx](states/gotx/login-04-email-focus-key-dark.png) · [compare](states/compare/login-04-email-focus-key-dark.png) |
| login-04 / email-type | light | 431×85 → 431×85 | 4.3 % / 2.3 % | 0 / 0 | 2 (2) | 0.2 |  | [ref](states/reference/login-04-email-type-light.png) · [gotx](states/gotx/login-04-email-type-light.png) · [compare](states/compare/login-04-email-type-light.png) |
| login-04 / email-type | dark | 431×85 → 431×85 | 3.2 % / 2.5 % | 0 / 0 | 2 (2) | 0.2 |  | [ref](states/reference/login-04-email-type-dark.png) · [gotx](states/gotx/login-04-email-type-dark.png) · [compare](states/compare/login-04-email-type-dark.png) |
| login-04 / password-type | light | 431×85 → 431×85 | 5.9 % / 3.6 % | 0 / 0 | 4 (4) | 0.6 |  | [ref](states/reference/login-04-password-type-light.png) · [gotx](states/gotx/login-04-password-type-light.png) · [compare](states/compare/login-04-password-type-light.png) |
| login-04 / password-type | dark | 431×85 → 431×85 | 4.8 % / 3.8 % | 0 / 0 | 4 (4) | 0.6 |  | [ref](states/reference/login-04-password-type-dark.png) · [gotx](states/gotx/login-04-password-type-dark.png) · [compare](states/compare/login-04-password-type-dark.png) |
| login-04 / privacy-hover | light | 137×67 → 137×67 | 13.4 % / 6.4 % | 0 / 0 | 1 (1) | 0.9 |  | [ref](states/reference/login-04-privacy-hover-light.png) · [gotx](states/gotx/login-04-privacy-hover-light.png) · [compare](states/compare/login-04-privacy-hover-light.png) |
| login-04 / privacy-hover | dark | 137×67 → 137×67 | 12.5 % / 6.5 % | 0 / 0 | 1 (1) | 0.9 |  | [ref](states/reference/login-04-privacy-hover-dark.png) · [gotx](states/gotx/login-04-privacy-hover-dark.png) · [compare](states/compare/login-04-privacy-hover-dark.png) |
| login-04 / privacy-focus-key | light | 137×67 → 137×67 | 20.6 % / 11.0 % | 0 / 0 | 1 (1) | 0.9 |  | [ref](states/reference/login-04-privacy-focus-key-light.png) · [gotx](states/gotx/login-04-privacy-focus-key-light.png) · [compare](states/compare/login-04-privacy-focus-key-light.png) |
| login-04 / privacy-focus-key | dark | 137×67 → 137×67 | 20.7 % / 11.2 % | 0 / 0 | 1 (1) | 0.9 |  | [ref](states/reference/login-04-privacy-focus-key-dark.png) · [gotx](states/gotx/login-04-privacy-focus-key-dark.png) · [compare](states/compare/login-04-privacy-focus-key-dark.png) |
| signup-03 / logo-hover | light | 154×73 → 154×73 | 11.0 % / 4.9 % | 0 / 0 | 2 (2) | 0.3 |  | [ref](states/reference/signup-03-logo-hover-light.png) · [gotx](states/gotx/signup-03-logo-hover-light.png) · [compare](states/compare/signup-03-logo-hover-light.png) |
| signup-03 / logo-hover | dark | 154×73 → 154×73 | 7.8 % / 4.5 % | 0 / 0 | 2 (2) | 0.3 |  | [ref](states/reference/signup-03-logo-hover-dark.png) · [gotx](states/gotx/signup-03-logo-hover-dark.png) · [compare](states/compare/signup-03-logo-hover-dark.png) |
| signup-03 / name-hover | light | 382×85 → 382×85 | 6.0 % / 2.0 % | 0 / 0 | 2 (2) | 0.3 |  | [ref](states/reference/signup-03-name-hover-light.png) · [gotx](states/gotx/signup-03-name-hover-light.png) · [compare](states/compare/signup-03-name-hover-light.png) |
| signup-03 / name-hover | dark | 382×85 → 382×85 | 6.0 % / 2.0 % | 0 / 0 | 2 (2) | 0.3 |  | [ref](states/reference/signup-03-name-hover-dark.png) · [gotx](states/gotx/signup-03-name-hover-dark.png) · [compare](states/compare/signup-03-name-hover-dark.png) |
| signup-03 / name-focus-key | light | 382×85 → 382×85 | 4.1 % / 2.1 % | 0 / 0 | 2 (2) | 0.3 |  | [ref](states/reference/signup-03-name-focus-key-light.png) · [gotx](states/gotx/signup-03-name-focus-key-light.png) · [compare](states/compare/signup-03-name-focus-key-light.png) |
| signup-03 / name-focus-key | dark | 382×85 → 382×85 | 3.0 % / 2.1 % | 0 / 0 | 2 (2) | 0.3 |  | [ref](states/reference/signup-03-name-focus-key-dark.png) · [gotx](states/gotx/signup-03-name-focus-key-dark.png) · [compare](states/compare/signup-03-name-focus-key-dark.png) |
| signup-03 / submit-hover | light | 382×85 → 382×85 | 39.4 % / 3.4 % | 0 / 0 | 5 (4) | 0.3 |  | [ref](states/reference/signup-03-submit-hover-light.png) · [gotx](states/gotx/signup-03-submit-hover-light.png) · [compare](states/compare/signup-03-submit-hover-light.png) |
| signup-03 / submit-hover | dark | 382×85 → 382×85 | 39.4 % / 3.4 % | 0 / 0 | 5 (4) | 0.3 |  | [ref](states/reference/signup-03-submit-hover-dark.png) · [gotx](states/gotx/signup-03-submit-hover-dark.png) · [compare](states/compare/signup-03-submit-hover-dark.png) |
| signup-03 / submit-focus-key | light | 382×85 → 382×85 | 39.6 % / 3.5 % | 0 / 0 | 5 (4) | 0.3 |  | [ref](states/reference/signup-03-submit-focus-key-light.png) · [gotx](states/gotx/signup-03-submit-focus-key-light.png) · [compare](states/compare/signup-03-submit-focus-key-light.png) |
| signup-03 / submit-focus-key | dark | 382×85 → 382×85 | 5.0 % / 3.5 % | 0 / 0 | 5 (4) | 0.3 |  | [ref](states/reference/signup-03-submit-focus-key-dark.png) · [gotx](states/gotx/signup-03-submit-focus-key-dark.png) · [compare](states/compare/signup-03-submit-focus-key-dark.png) |
| signup-03 / submit-press | light | 382×85 → 382×85 | 39.4 % / 3.4 % | 0 / 0 | 5 (4) | 0.3 |  | [ref](states/reference/signup-03-submit-press-light.png) · [gotx](states/gotx/signup-03-submit-press-light.png) · [compare](states/compare/signup-03-submit-press-light.png) |
| signup-03 / submit-press | dark | 382×85 → 382×85 | 39.4 % / 3.4 % | 0 / 0 | 5 (4) | 0.3 |  | [ref](states/reference/signup-03-submit-press-dark.png) · [gotx](states/gotx/signup-03-submit-press-dark.png) · [compare](states/compare/signup-03-submit-press-dark.png) |
| signup-03 / sign-in-hover | light | 93×67 → 93×67 | 25.5 % / 5.5 % | 0 / 0 | 5 (4) | 0.3 |  | [ref](states/reference/signup-03-sign-in-hover-light.png) · [gotx](states/gotx/signup-03-sign-in-hover-light.png) · [compare](states/compare/signup-03-sign-in-hover-light.png) |
| signup-03 / sign-in-hover | dark | 93×67 → 93×67 | 7.0 % / 5.9 % | 0 / 0 | 5 (4) | 0.3 |  | [ref](states/reference/signup-03-sign-in-hover-dark.png) · [gotx](states/gotx/signup-03-sign-in-hover-dark.png) · [compare](states/compare/signup-03-sign-in-hover-dark.png) |
| signup-03 / sign-in-focus-key | light | 93×67 → 93×67 | 29.7 % / 8.4 % | 0 / 0 | 5 (4) | 0.3 |  | [ref](states/reference/signup-03-sign-in-focus-key-light.png) · [gotx](states/gotx/signup-03-sign-in-focus-key-light.png) · [compare](states/compare/signup-03-sign-in-focus-key-light.png) |
| signup-03 / sign-in-focus-key | dark | 93×67 → 93×67 | 13.9 % / 8.9 % | 0 / 0 | 5 (4) | 0.3 |  | [ref](states/reference/signup-03-sign-in-focus-key-dark.png) · [gotx](states/gotx/signup-03-sign-in-focus-key-dark.png) · [compare](states/compare/signup-03-sign-in-focus-key-dark.png) |
| signup-03 / email-hover | light | 382×85 → 382×85 | 6.6 % / 2.3 % | 0 / 0 | 2 (2) | 0.2 |  | [ref](states/reference/signup-03-email-hover-light.png) · [gotx](states/gotx/signup-03-email-hover-light.png) · [compare](states/compare/signup-03-email-hover-light.png) |
| signup-03 / email-hover | dark | 382×85 → 382×85 | 6.6 % / 2.3 % | 0 / 0 | 2 (2) | 0.2 |  | [ref](states/reference/signup-03-email-hover-dark.png) · [gotx](states/gotx/signup-03-email-hover-dark.png) · [compare](states/compare/signup-03-email-hover-dark.png) |
| signup-03 / email-focus-key | light | 382×85 → 382×85 | 4.7 % / 2.4 % | 0 / 0 | 2 (2) | 0.2 |  | [ref](states/reference/signup-03-email-focus-key-light.png) · [gotx](states/gotx/signup-03-email-focus-key-light.png) · [compare](states/compare/signup-03-email-focus-key-light.png) |
| signup-03 / email-focus-key | dark | 382×85 → 382×85 | 3.6 % / 2.4 % | 0 / 0 | 2 (2) | 0.2 |  | [ref](states/reference/signup-03-email-focus-key-dark.png) · [gotx](states/gotx/signup-03-email-focus-key-dark.png) · [compare](states/compare/signup-03-email-focus-key-dark.png) |
| signup-03 / email-type | light | 382×85 → 382×85 | 4.7 % / 2.6 % | 0 / 0 | 3 (3) | 0.3 |  | [ref](states/reference/signup-03-email-type-light.png) · [gotx](states/gotx/signup-03-email-type-light.png) · [compare](states/compare/signup-03-email-type-light.png) |
| signup-03 / email-type | dark | 382×85 → 382×85 | 3.6 % / 2.8 % | 0 / 0 | 3 (3) | 0.3 |  | [ref](states/reference/signup-03-email-type-dark.png) · [gotx](states/gotx/signup-03-email-type-dark.png) · [compare](states/compare/signup-03-email-type-dark.png) |
| signup-03 / password-type | light | 207×85 → 207×85 | 10.8 % / 5.8 % | 0 / 0 | 5 (5) | 0.6 |  | [ref](states/reference/signup-03-password-type-light.png) · [gotx](states/gotx/signup-03-password-type-light.png) · [compare](states/compare/signup-03-password-type-light.png) |
| signup-03 / password-type | dark | 207×85 → 207×85 | 9.7 % / 6.3 % | 0 / 0 | 5 (5) | 0.6 |  | [ref](states/reference/signup-03-password-type-dark.png) · [gotx](states/gotx/signup-03-password-type-dark.png) · [compare](states/compare/signup-03-password-type-dark.png) |
| signup-03 / privacy-hover | light | 136×67 → 136×67 | 18.0 % / 12.7 % | 0 / 0 | 1 (1) | 0.8 |  | [ref](states/reference/signup-03-privacy-hover-light.png) · [gotx](states/gotx/signup-03-privacy-hover-light.png) · [compare](states/compare/signup-03-privacy-hover-light.png) |
| signup-03 / privacy-hover | dark | 136×67 → 136×67 | 17.5 % / 13.1 % | 0 / 0 | 1 (1) | 0.8 |  | [ref](states/reference/signup-03-privacy-hover-dark.png) · [gotx](states/gotx/signup-03-privacy-hover-dark.png) · [compare](states/compare/signup-03-privacy-hover-dark.png) |
| signup-03 / privacy-focus-key | light | 136×67 → 136×67 | 25.1 % / 16.8 % | 0 / 0 | 1 (1) | 0.8 |  | [ref](states/reference/signup-03-privacy-focus-key-light.png) · [gotx](states/gotx/signup-03-privacy-focus-key-light.png) · [compare](states/compare/signup-03-privacy-focus-key-light.png) |
| signup-03 / privacy-focus-key | dark | 136×67 → 136×67 | 25.6 % / 17.6 % | 0 / 0 | 1 (1) | 0.8 |  | [ref](states/reference/signup-03-privacy-focus-key-dark.png) · [gotx](states/gotx/signup-03-privacy-focus-key-dark.png) · [compare](states/compare/signup-03-privacy-focus-key-dark.png) |
| signup-04 / apple-hover | light | 165×85 → 165×85 | 4.9 % / 0.1 % | 0 / 0 | 2 (2) | 0.3 |  | [ref](states/reference/signup-04-apple-hover-light.png) · [gotx](states/gotx/signup-04-apple-hover-light.png) · [compare](states/compare/signup-04-apple-hover-light.png) |
| signup-04 / apple-hover | dark | 165×85 → 165×85 | 4.8 % / 0.1 % | 0 / 0 | 2 (2) | 0.3 |  | [ref](states/reference/signup-04-apple-hover-dark.png) · [gotx](states/gotx/signup-04-apple-hover-dark.png) · [compare](states/compare/signup-04-apple-hover-dark.png) |
| signup-04 / apple-focus-key | light | 165×85 → 165×85 | 4.1 % / 0.1 % | 0 / 0 | 2 (2) | 0.3 |  | [ref](states/reference/signup-04-apple-focus-key-light.png) · [gotx](states/gotx/signup-04-apple-focus-key-light.png) · [compare](states/compare/signup-04-apple-focus-key-light.png) |
| signup-04 / apple-focus-key | dark | 165×85 → 165×85 | 4.0 % / 2.2 % | 0 / 0 | 2 (2) | 0.3 |  | [ref](states/reference/signup-04-apple-focus-key-dark.png) · [gotx](states/gotx/signup-04-apple-focus-key-dark.png) · [compare](states/compare/signup-04-apple-focus-key-dark.png) |
| signup-04 / apple-press | light | 165×85 → 165×85 | 4.9 % / 0.1 % | 0 / 0 | 2 (2) | 0.3 |  | [ref](states/reference/signup-04-apple-press-light.png) · [gotx](states/gotx/signup-04-apple-press-light.png) · [compare](states/compare/signup-04-apple-press-light.png) |
| signup-04 / apple-press | dark | 165×85 → 165×85 | 4.8 % / 0.1 % | 0 / 0 | 2 (2) | 0.3 |  | [ref](states/reference/signup-04-apple-press-dark.png) · [gotx](states/gotx/signup-04-apple-press-dark.png) · [compare](states/compare/signup-04-apple-press-dark.png) |
| signup-04 / submit-hover | light | 431×85 → 431×85 | 37.5 % / 1.5 % | 0 / 0 | 4 (4) | 0.4 |  | [ref](states/reference/signup-04-submit-hover-light.png) · [gotx](states/gotx/signup-04-submit-hover-light.png) · [compare](states/compare/signup-04-submit-hover-light.png) |
| signup-04 / submit-hover | dark | 431×85 → 431×85 | 37.5 % / 1.4 % | 0 / 0 | 4 (4) | 0.4 |  | [ref](states/reference/signup-04-submit-hover-dark.png) · [gotx](states/gotx/signup-04-submit-hover-dark.png) · [compare](states/compare/signup-04-submit-hover-dark.png) |
| signup-04 / submit-focus-key | light | 431×85 → 431×85 | 37.7 % / 1.6 % | 0 / 0 | 4 (4) | 0.4 |  | [ref](states/reference/signup-04-submit-focus-key-light.png) · [gotx](states/gotx/signup-04-submit-focus-key-light.png) · [compare](states/compare/signup-04-submit-focus-key-light.png) |
| signup-04 / submit-focus-key | dark | 431×85 → 431×85 | 2.2 % / 1.4 % | 0 / 0 | 4 (4) | 0.4 |  | [ref](states/reference/signup-04-submit-focus-key-dark.png) · [gotx](states/gotx/signup-04-submit-focus-key-dark.png) · [compare](states/compare/signup-04-submit-focus-key-dark.png) |
| signup-04 / submit-press | light | 431×85 → 431×85 | 37.5 % / 1.5 % | 0 / 0 | 4 (4) | 0.4 |  | [ref](states/reference/signup-04-submit-press-light.png) · [gotx](states/gotx/signup-04-submit-press-light.png) · [compare](states/compare/signup-04-submit-press-light.png) |
| signup-04 / submit-press | dark | 431×85 → 431×85 | 37.5 % / 1.4 % | 0 / 0 | 4 (4) | 0.4 |  | [ref](states/reference/signup-04-submit-press-dark.png) · [gotx](states/gotx/signup-04-submit-press-dark.png) · [compare](states/compare/signup-04-submit-press-dark.png) |
| signup-04 / sign-in-hover | light | 93×67 → 93×67 | 7.6 % / 5.7 % | 0 / 0 | 3 (2) | 0.8 |  | [ref](states/reference/signup-04-sign-in-hover-light.png) · [gotx](states/gotx/signup-04-sign-in-hover-light.png) · [compare](states/compare/signup-04-sign-in-hover-light.png) |
| signup-04 / sign-in-hover | dark | 93×67 → 93×67 | 7.0 % / 5.9 % | 0 / 0 | 3 (2) | 0.8 |  | [ref](states/reference/signup-04-sign-in-hover-dark.png) · [gotx](states/gotx/signup-04-sign-in-hover-dark.png) · [compare](states/compare/signup-04-sign-in-hover-dark.png) |
| signup-04 / sign-in-focus-key | light | 93×67 → 93×67 | 11.8 % / 8.2 % | 0 / 0 | 3 (2) | 0.8 |  | [ref](states/reference/signup-04-sign-in-focus-key-light.png) · [gotx](states/gotx/signup-04-sign-in-focus-key-light.png) · [compare](states/compare/signup-04-sign-in-focus-key-light.png) |
| signup-04 / sign-in-focus-key | dark | 93×67 → 93×67 | 13.9 % / 8.7 % | 0 / 0 | 3 (2) | 0.8 |  | [ref](states/reference/signup-04-sign-in-focus-key-dark.png) · [gotx](states/gotx/signup-04-sign-in-focus-key-dark.png) · [compare](states/compare/signup-04-sign-in-focus-key-dark.png) |
| signup-04 / email-hover | light | 431×85 → 431×85 | 10.4 % / 5.2 % | 0 / 0 | 3 (3) | 0.5 |  | [ref](states/reference/signup-04-email-hover-light.png) · [gotx](states/gotx/signup-04-email-hover-light.png) · [compare](states/compare/signup-04-email-hover-light.png) |
| signup-04 / email-hover | dark | 431×85 → 431×85 | 10.5 % / 5.5 % | 0 / 0 | 3 (3) | 0.5 |  | [ref](states/reference/signup-04-email-hover-dark.png) · [gotx](states/gotx/signup-04-email-hover-dark.png) · [compare](states/compare/signup-04-email-hover-dark.png) |
| signup-04 / email-focus-key | light | 431×85 → 431×85 | 8.4 % / 5.3 % | 0 / 0 | 3 (3) | 0.5 |  | [ref](states/reference/signup-04-email-focus-key-light.png) · [gotx](states/gotx/signup-04-email-focus-key-light.png) · [compare](states/compare/signup-04-email-focus-key-light.png) |
| signup-04 / email-focus-key | dark | 431×85 → 431×85 | 7.4 % / 5.5 % | 0 / 0 | 3 (3) | 0.5 |  | [ref](states/reference/signup-04-email-focus-key-dark.png) · [gotx](states/gotx/signup-04-email-focus-key-dark.png) · [compare](states/compare/signup-04-email-focus-key-dark.png) |
| signup-04 / email-type | light | 431×85 → 431×85 | 8.4 % / 5.4 % | 0 / 0 | 4 (4) | 0.5 |  | [ref](states/reference/signup-04-email-type-light.png) · [gotx](states/gotx/signup-04-email-type-light.png) · [compare](states/compare/signup-04-email-type-light.png) |
| signup-04 / email-type | dark | 431×85 → 431×85 | 7.4 % / 5.7 % | 0 / 0 | 4 (4) | 0.5 |  | [ref](states/reference/signup-04-email-type-dark.png) · [gotx](states/gotx/signup-04-email-type-dark.png) · [compare](states/compare/signup-04-email-type-dark.png) |
| signup-04 / password-type | light | 232×85 → 232×85 | 10.2 % / 5.5 % | 0 / 0 | 5 (5) | 0.8 |  | [ref](states/reference/signup-04-password-type-light.png) · [gotx](states/gotx/signup-04-password-type-light.png) · [compare](states/compare/signup-04-password-type-light.png) |
| signup-04 / password-type | dark | 232×85 → 232×85 | 9.2 % / 6.0 % | 0 / 0 | 5 (5) | 0.8 |  | [ref](states/reference/signup-04-password-type-dark.png) · [gotx](states/gotx/signup-04-password-type-dark.png) · [compare](states/compare/signup-04-password-type-dark.png) |
| signup-04 / privacy-hover | light | 137×67 → 137×67 | 13.4 % / 6.4 % | 0 / 0 | 1 (1) | 0.8 |  | [ref](states/reference/signup-04-privacy-hover-light.png) · [gotx](states/gotx/signup-04-privacy-hover-light.png) · [compare](states/compare/signup-04-privacy-hover-light.png) |
| signup-04 / privacy-hover | dark | 137×67 → 137×67 | 12.5 % / 6.5 % | 0 / 0 | 1 (1) | 0.8 |  | [ref](states/reference/signup-04-privacy-hover-dark.png) · [gotx](states/gotx/signup-04-privacy-hover-dark.png) · [compare](states/compare/signup-04-privacy-hover-dark.png) |
| signup-04 / privacy-focus-key | light | 137×67 → 137×67 | 20.6 % / 11.0 % | 0 / 0 | 1 (1) | 0.8 |  | [ref](states/reference/signup-04-privacy-focus-key-light.png) · [gotx](states/gotx/signup-04-privacy-focus-key-light.png) · [compare](states/compare/signup-04-privacy-focus-key-light.png) |
| signup-04 / privacy-focus-key | dark | 137×67 → 137×67 | 20.7 % / 11.2 % | 0 / 0 | 1 (1) | 0.8 |  | [ref](states/reference/signup-04-privacy-focus-key-dark.png) · [gotx](states/gotx/signup-04-privacy-focus-key-dark.png) · [compare](states/compare/signup-04-privacy-focus-key-dark.png) |

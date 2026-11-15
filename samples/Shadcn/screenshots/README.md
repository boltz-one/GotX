# shadcn/ui ↔ GotX screenshots

Generated — do not edit by hand. Reference = ui.shadcn.com in headless Edge (see `manifest.json`); GotX = `samples/Shadcn`
rendered headless with the CPU renderer and the Geist fonts from `samples/Shadcn/Assets/fonts`.

- Re-capture references (network): `node tools/ShadcnRef/capture.mjs [--only home,login-03]`
- Regenerate GotX + diff + this file: `dotnet run --project samples/Shadcn -- --screenshots [--only home]`
- Compare only (existing `gotx/`): `dotnet run --project samples/Shadcn -- --compare`

Home is captured full page **without the site header** on both sides (the GotX home view has no site header; the
reference hides `header.sticky`). Blocks: viewport only. Folders: `reference/`, `gotx/`, `diff/` (dimmed reference;
magenta = channel Δ > 24, amber = 1–24, blue = area only one image has), `compare/` (reference | GotX, diff below; halved when wide).

**Layout match**: reference elements paired with GotX nodes by normalized text (case-insensitive). *Texts*: DOM text runs
(Range box) ↔ GotX text nodes (advance width placed by text-align), Δ = max(|Δx|, |Δ vertical centre|, |Δw|).
*Boxes*: `data-slot` elements ↔ GotX nodes with background/border/interaction and the same subtree text,
Δ = max(|Δx|, |Δy|, |Δw|, |Δh|). Δ in CSS px; median / p90 / max over all matched pairs.

| Page | Viewport | Theme | Size ref → GotX | Pixel diff (tol 0 / 24) | Texts matched | Boxes matched | ≤2 px | Median Δ | P90 Δ | Max Δ | Images |
|---|---|---|---|---|---|---|---|---|---|---|---|
| home | 1440×900 | light | 1440×1829 → 1440×2310 ⚠ | 54.26 % / 30.08 % | 124/131 | 69/80 | 4/193 | 26.2 | 39.2 | 495 | [ref](reference/home-1440-light.png) · [gotx](gotx/home-1440-light.png) · [diff](diff/home-1440-light.png) · [compare](compare/home-1440-light.png) |
| home | 1440×900 | dark | 1440×1829 → 1440×2310 ⚠ | 57.42 % / 30.07 % | 124/131 | 69/80 | 4/193 | 26.2 | 39.2 | 495 | [ref](reference/home-1440-dark.png) · [gotx](gotx/home-1440-dark.png) · [diff](diff/home-1440-dark.png) · [compare](compare/home-1440-dark.png) |
| home | 1024×900 | light | 1024×1765 → 1024×3217 ⚠ | 69.88 % / 52.18 % | 96/101 | 57/64 | 4/153 | 26 | 1444 | 1452 | [ref](reference/home-1024-light.png) · [gotx](gotx/home-1024-light.png) · [diff](diff/home-1024-light.png) · [compare](compare/home-1024-light.png) |
| home | 1024×900 | dark | 1024×1765 → 1024×3217 ⚠ | 71.30 % / 52.36 % | 96/101 | 57/64 | 4/153 | 26 | 1444 | 1452 | [ref](reference/home-1024-dark.png) · [gotx](gotx/home-1024-dark.png) · [diff](diff/home-1024-dark.png) · [compare](compare/home-1024-dark.png) |
| home | 640×900 | light | 640×1297 → 640×6902 ⚠ | 92.21 % / 83.53 % | 8/9 | 4/4 | 5/12 | 4 | 5586.6 | 5604.6 | [ref](reference/home-640-light.png) · [gotx](gotx/home-640-light.png) · [diff](diff/home-640-light.png) · [compare](compare/home-640-light.png) |
| home | 640×900 | dark | 640×1297 → 640×6902 ⚠ | 92.10 % / 83.58 % | 8/9 | 4/4 | 5/12 | 4 | 5586.6 | 5604.6 | [ref](reference/home-640-dark.png) · [gotx](gotx/home-640-dark.png) · [diff](diff/home-640-dark.png) · [compare](compare/home-640-dark.png) |
| dashboard-01 | 1440×900 | light | 1440×900 → 1440×900 | 49.02 % / 21.66 % | 69/153 | 38/131 | 17/107 | 32 | 64.5 | 495.5 | [ref](reference/dashboard-01-1440-light.png) · [gotx](gotx/dashboard-01-1440-light.png) · [diff](diff/dashboard-01-1440-light.png) · [compare](compare/dashboard-01-1440-light.png) |
| dashboard-01 | 1440×900 | dark | 1440×900 → 1440×900 | 53.63 % / 26.42 % | 69/153 | 38/131 | 17/107 | 32 | 64.5 | 495.5 | [ref](reference/dashboard-01-1440-dark.png) · [gotx](gotx/dashboard-01-1440-dark.png) · [diff](diff/dashboard-01-1440-dark.png) · [compare](compare/dashboard-01-1440-dark.png) |
| dashboard-01 | 1024×768 | light | 1024×768 → 1024×768 | 53.85 % / 19.54 % | 42/146 | 20/123 | 17/62 | 32 | 59 | 150 | [ref](reference/dashboard-01-1024-light.png) · [gotx](gotx/dashboard-01-1024-light.png) · [diff](diff/dashboard-01-1024-light.png) · [compare](compare/dashboard-01-1024-light.png) |
| dashboard-01 | 1024×768 | dark | 1024×768 → 1024×768 | 57.40 % / 19.60 % | 42/146 | 20/123 | 17/62 | 32 | 59 | 150 | [ref](reference/dashboard-01-1024-dark.png) · [gotx](gotx/dashboard-01-1024-dark.png) · [diff](diff/dashboard-01-1024-dark.png) · [compare](compare/dashboard-01-1024-dark.png) |
| sidebar-07 | 1440×900 | light | 1440×900 → 1440×900 | 0.89 % / 0.68 % | 19/20 | 16/19 | 33/35 | 0 | 1 | 1184 | [ref](reference/sidebar-07-1440-light.png) · [gotx](gotx/sidebar-07-1440-light.png) · [diff](diff/sidebar-07-1440-light.png) · [compare](compare/sidebar-07-1440-light.png) |
| sidebar-07 | 1440×900 | dark | 1440×900 → 1440×900 | 0.99 % / 0.73 % | 19/20 | 16/19 | 33/35 | 0 | 1 | 1184 | [ref](reference/sidebar-07-1440-dark.png) · [gotx](gotx/sidebar-07-1440-dark.png) · [diff](diff/sidebar-07-1440-dark.png) · [compare](compare/sidebar-07-1440-dark.png) |
| sidebar-07 | 1024×768 | light | 1024×768 → 1024×768 | 1.47 % / 1.12 % | 19/20 | 16/19 | 33/35 | 0 | 1 | 768 | [ref](reference/sidebar-07-1024-light.png) · [gotx](gotx/sidebar-07-1024-light.png) · [diff](diff/sidebar-07-1024-light.png) · [compare](compare/sidebar-07-1024-light.png) |
| sidebar-07 | 1024×768 | dark | 1024×768 → 1024×768 | 1.61 % / 1.21 % | 19/20 | 16/19 | 33/35 | 0 | 1 | 768 | [ref](reference/sidebar-07-1024-dark.png) · [gotx](gotx/sidebar-07-1024-dark.png) · [diff](diff/sidebar-07-1024-dark.png) · [compare](compare/sidebar-07-1024-dark.png) |
| sidebar-03 | 1440×900 | light | 1440×900 → 1440×900 | 1.52 % / 1.26 % | 30/36 | 33/42 | 61/63 | 0 | 0 | 1184 | [ref](reference/sidebar-03-1440-light.png) · [gotx](gotx/sidebar-03-1440-light.png) · [diff](diff/sidebar-03-1440-light.png) · [compare](compare/sidebar-03-1440-light.png) |
| sidebar-03 | 1440×900 | dark | 1440×900 → 1440×900 | 1.71 % / 1.35 % | 30/36 | 33/42 | 61/63 | 0 | 0 | 1184 | [ref](reference/sidebar-03-1440-dark.png) · [gotx](gotx/sidebar-03-1440-dark.png) · [diff](diff/sidebar-03-1440-dark.png) · [compare](compare/sidebar-03-1440-dark.png) |
| sidebar-03 | 1024×768 | light | 1024×768 → 1024×768 | 2.15 % / 1.77 % | 26/36 | 29/42 | 53/55 | 0 | 0.5 | 768 | [ref](reference/sidebar-03-1024-light.png) · [gotx](gotx/sidebar-03-1024-light.png) · [diff](diff/sidebar-03-1024-light.png) · [compare](compare/sidebar-03-1024-light.png) |
| sidebar-03 | 1024×768 | dark | 1024×768 → 1024×768 | 2.39 % / 1.91 % | 26/36 | 29/42 | 53/55 | 0 | 0.5 | 768 | [ref](reference/sidebar-03-1024-dark.png) · [gotx](gotx/sidebar-03-1024-dark.png) · [diff](diff/sidebar-03-1024-dark.png) · [compare](compare/sidebar-03-1024-dark.png) |
| login-03 | 1440×900 | light | 1440×900 → 1440×900 | 6.53 % / 3.34 % | 14/15 | 4/6 | 0/18 | 24.1 | 41.6 | 127.1 | [ref](reference/login-03-1440-light.png) · [gotx](gotx/login-03-1440-light.png) · [diff](diff/login-03-1440-light.png) · [compare](compare/login-03-1440-light.png) |
| login-03 | 1440×900 | dark | 1440×900 → 1440×900 | 8.25 % / 3.34 % | 14/15 | 4/6 | 0/18 | 24.1 | 41.6 | 127.1 | [ref](reference/login-03-1440-dark.png) · [gotx](gotx/login-03-1440-dark.png) · [diff](diff/login-03-1440-dark.png) · [compare](compare/login-03-1440-dark.png) |
| login-03 | 1024×768 | light | 1024×768 → 1024×768 | 10.76 % / 5.50 % | 14/15 | 4/6 | 0/18 | 24.1 | 41.6 | 127.1 | [ref](reference/login-03-1024-light.png) · [gotx](gotx/login-03-1024-light.png) · [diff](diff/login-03-1024-light.png) · [compare](compare/login-03-1024-light.png) |
| login-03 | 1024×768 | dark | 1024×768 → 1024×768 | 13.60 % / 5.50 % | 14/15 | 4/6 | 0/18 | 24.1 | 41.6 | 127.1 | [ref](reference/login-03-1024-dark.png) · [gotx](gotx/login-03-1024-dark.png) · [diff](diff/login-03-1024-dark.png) · [compare](compare/login-03-1024-dark.png) |
| login-04 | 1440×900 | light | 1440×900 → 1440×900 | 10.12 % / 2.98 % | 11/15 | 2/7 | 0/13 | 28.9 | 30.1 | 61.3 | [ref](reference/login-04-1440-light.png) · [gotx](gotx/login-04-1440-light.png) · [diff](diff/login-04-1440-light.png) · [compare](compare/login-04-1440-light.png) |
| login-04 | 1440×900 | dark | 1440×900 → 1440×900 | 11.28 % / 2.69 % | 11/15 | 2/7 | 0/13 | 28.9 | 30.1 | 61.3 | [ref](reference/login-04-1440-dark.png) · [gotx](gotx/login-04-1440-dark.png) · [diff](diff/login-04-1440-dark.png) · [compare](compare/login-04-1440-dark.png) |
| login-04 | 1024×768 | light | 1024×768 → 1024×768 | 16.67 % / 4.90 % | 11/15 | 2/7 | 0/13 | 28.9 | 30.1 | 61.3 | [ref](reference/login-04-1024-light.png) · [gotx](gotx/login-04-1024-light.png) · [diff](diff/login-04-1024-light.png) · [compare](compare/login-04-1024-light.png) |
| login-04 | 1024×768 | dark | 1024×768 → 1024×768 | 18.58 % / 4.43 % | 11/15 | 2/7 | 0/13 | 28.9 | 30.1 | 61.3 | [ref](reference/login-04-1024-dark.png) · [gotx](gotx/login-04-1024-dark.png) · [diff](diff/login-04-1024-dark.png) · [compare](compare/login-04-1024-dark.png) |
| signup-03 | 1440×900 | light | 1440×900 → 1440×900 | 6.61 % / 3.39 % | 13/14 | 2/4 | 0/15 | 31.9 | 89.8 | 127.1 | [ref](reference/signup-03-1440-light.png) · [gotx](gotx/signup-03-1440-light.png) · [diff](diff/signup-03-1440-light.png) · [compare](compare/signup-03-1440-light.png) |
| signup-03 | 1440×900 | dark | 1440×900 → 1440×900 | 8.12 % / 3.42 % | 13/14 | 2/4 | 0/15 | 31.9 | 89.8 | 127.1 | [ref](reference/signup-03-1440-dark.png) · [gotx](gotx/signup-03-1440-dark.png) · [diff](diff/signup-03-1440-dark.png) · [compare](compare/signup-03-1440-dark.png) |
| signup-03 | 1024×768 | light | 1024×768 → 1024×768 | 10.89 % / 5.59 % | 13/14 | 2/4 | 0/15 | 31.9 | 89.8 | 127.1 | [ref](reference/signup-03-1024-light.png) · [gotx](gotx/signup-03-1024-light.png) · [diff](diff/signup-03-1024-light.png) · [compare](compare/signup-03-1024-light.png) |
| signup-03 | 1024×768 | dark | 1024×768 → 1024×768 | 13.39 % / 5.63 % | 13/14 | 2/4 | 0/15 | 31.9 | 89.8 | 127.1 | [ref](reference/signup-03-1024-dark.png) · [gotx](gotx/signup-03-1024-dark.png) · [diff](diff/signup-03-1024-dark.png) · [compare](compare/signup-03-1024-dark.png) |
| signup-04 | 1440×900 | light | 1440×900 → 1440×900 | 11.66 % / 3.71 % | 13/17 | 2/7 | 0/15 | 24.8 | 32.3 | 65.5 | [ref](reference/signup-04-1440-light.png) · [gotx](gotx/signup-04-1440-light.png) · [diff](diff/signup-04-1440-light.png) · [compare](compare/signup-04-1440-light.png) |
| signup-04 | 1440×900 | dark | 1440×900 → 1440×900 | 13.20 % / 3.38 % | 13/17 | 2/7 | 0/15 | 24.8 | 32.3 | 65.5 | [ref](reference/signup-04-1440-dark.png) · [gotx](gotx/signup-04-1440-dark.png) · [diff](diff/signup-04-1440-dark.png) · [compare](compare/signup-04-1440-dark.png) |
| signup-04 | 1024×768 | light | 1024×768 → 1024×768 | 19.21 % / 6.12 % | 13/17 | 2/7 | 0/15 | 24.8 | 32.3 | 65.5 | [ref](reference/signup-04-1024-light.png) · [gotx](gotx/signup-04-1024-light.png) · [diff](diff/signup-04-1024-light.png) · [compare](compare/signup-04-1024-light.png) |
| signup-04 | 1024×768 | dark | 1024×768 → 1024×768 | 21.75 % / 5.57 % | 13/17 | 2/7 | 0/15 | 24.8 | 32.3 | 65.5 | [ref](reference/signup-04-1024-dark.png) · [gotx](gotx/signup-04-1024-dark.png) · [diff](diff/signup-04-1024-dark.png) · [compare](compare/signup-04-1024-dark.png) |

## Worst layout offenders

Light theme (layout is theme-independent). Rects are `x,y w×h`; Δ = GotX − reference.

### home 1440×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| box demo | Button Secondary Outline Badge Secondary Alert Dialog Button | 0,350.8 1440×1382 | 0,377 1440×1877 | 0 | 26.2 | 0 | 495 |
| text | Vercel | 630.6,1771.8 41.6×18 | 631,2254 41.6×20 | 0.4 | 483.2 | -0 | 2 |
| text | shadcn | 563.4,1771.8 47.7×18 | 562,2254 47.7×20 | -1.4 | 483.2 | -0 | 2 |
| text | GitHub | 880.4,1771.8 44.9×18 | 883,2254 44.9×20 | 2.6 | 483.2 | 0 | 2 |
| box layout | Trace, Code Block and AI Elements The Foundation for your De | 0,0 1440×1828.8 | 0,0 1440×2310 | 0 | 0 | 0 | 481.2 |
| text | Open the Ledger mobile app and scan this code to link your d | 1142.7,663.8 216.6×38 | 1106,689 290×40 | -36.7 | 26.2 | 73.4 | 2 |
| text | $50 (MIN) | 752,1031.6 63.7×18 | 752,1091 63.9×21 | 0 | 60.9 | 0.3 | 3 |
| text | Notes | 752,1077.6 39.1×18 | 752,1136 39.1×19 | 0 | 58.9 | -0 | 1 |
| text | $10,000 (MAX) | 946.6,1033.6 95.4×18 | 946,1091 95.3×21 | -0.6 | 58.9 | -0.1 | 3 |
| text | What are we working on today? Press send to start a new conv | 1172.7,1024.2 156.7×63.5 | 1150,1027 202×68 | -22.7 | 5.1 | 45.3 | 4.5 |
| text | $2500.00 | 931,968.6 111×31 | 924,995 117.9×64 | -7 | 43 | 6.9 | 33 |
| text | Realty Income | 415,1654.6 93.4×18 | 415,1696 93.4×19 | 0 | 41.9 | -0 | 1 |
| text | 320 Shares | 415,1678.8 74.6×18 | 415,1719 74.6×21 | 0 | 41.7 | -0 | 3 |
| box item | Realty Income 320 Shares | 398,1639.6 290×73.3 | 398,1681 290×74 | 0 | 41.4 | 0 | 0.8 |
| text | 85 Shares | 415,1589.6 65.3×18 | 415,1629 65.2×21 | 0 | 40.9 | -0 | 3 |

### home 1024×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| box layout | Trace, Code Block and AI Elements The Foundation for your De | 0,0 1024×1765 | 0,0 1024×3217 | 0 | 0 | 0 | 1452 |
| text | Manage your repeated card transactions. | 757.7,1612.8 143.7×38 | 91,3060 175×42 | -666.7 | 1449.3 | 31.3 | 4 |
| text | Recurring card payments | 757.7,1588.5 164.1×18 | 91,3037 164.1×19 | -666.7 | 1449 | 0 | 1 |
| box item | Recurring card payments Manage your repeated card transactio | 710.7,1573.5 269.3×93.3 | 44,3022 269×95 | -666.7 | 1448.5 | -0.3 | 1.8 |
| text | Set up a transfer to send at a later date. | 757.7,1503.5 174.8×38 | 91,2949 175×42 | -666.7 | 1447.5 | 0.2 | 4 |
| text | Scheduled transfers | 757.7,1479.3 132.7×18 | 91,2925 132.7×20 | -666.7 | 1446.8 | 0 | 2 |
| box item | Scheduled transfers Set up a transfer to send at a later dat | 710.7,1464.3 269.3×93.3 | 44,2910 269×96 | -666.7 | 1445.8 | -0.3 | 2.8 |
| text | Adjust how much you can send from your balance. | 757.7,1394.3 164.2×38 | 91,2837 175×42 | -666.7 | 1444.8 | 10.8 | 4 |
| text | Change transfer limit | 757.7,1370 138.4×18 | 91,2814 138.4×19 | -666.7 | 1444.5 | 0 | 1 |
| text | Payments | 844.4,1312 63.3×18 | 182,2755 63.3×20 | -662.4 | 1444 | -0 | 2 |
| text | Home | 710.7,1312 37.7×18 | 44,2755 37.7×20 | -666.7 | 1444 | -0 | 2 |
| text | Open the Ledger mobile app and scan this code to link your d | 737.1,620 216.6×38 | 44,2063 269×40 | -693.1 | 1444 | 52.4 | 2 |
| box card-header | New Chat How can I help you today? | 690.7,723 309.3×69 | 24,2167 309×67 | -666.7 | 1444 | -0.3 | -2 |
| box card | Scan to connect your mobile device Open the Ledger mobile ap | 690.7,331 309.3×348 | 24,1775 309×348 | -666.7 | 1444 | -0.3 | 0 |
| box card | New Chat How can I help you today? Morning, shadcn! What are | 690.7,703 309.3×560 | 24,2147 309×560 | -666.7 | 1444 | -0.3 | 0 |

### home 640×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| box layout | Trace, Code Block and AI Elements The Foundation for your De | 0,0 640×1297.4 | 0,0 640×6902 | 0 | 0 | 0 | 5604.6 |
| text | shadcn | 163.4,1260.4 47.7×18 | 162,6846 47.7×20 | -1.4 | 5586.6 | -0 | 2 |
| text | Vercel | 230.6,1260.4 41.6×18 | 231,6846 41.6×20 | 0.4 | 5586.6 | -0 | 2 |
| text | GitHub | 480.4,1260.4 44.9×18 | 483,6846 44.9×20 | 2.6 | 5586.6 | 0 | 2 |
| text | Composable, accessible components with thoughtful defaults.  | 102.3,108 435.4×79 | 32,106 576×84 | -70.3 | 0.5 | 140.6 | 5 |
| box button | View Components | 302.6,206 144.8×36 | 303,206 141×32 | 0.4 | 0 | -3.8 | -4 |
| box button | Get Started | 192.5,206.5 102.1×35 | 196,206 99×32 | 3.5 | -0.5 | -3.1 | -3 |
| text | View Components | 315.6,215 118.8×18 | 314.1,212 118.8×20 | -1.6 | -2 | 0 | 2 |
| text | Get Started | 205.5,215 76.1×18 | 207.4,212 76.1×20 | 1.9 | -2 | -0 | 2 |
| box badge | Trace, Code Block and AI Elements | 205.8,32 228.5×22 | 206,32 229×20 | 0.3 | 0 | 0.5 | -2 |
| text | Trace, Code Block and AI Elements | 214.8,35 194.5×16 | 215.3,34 194.5×16 | 0.5 | -1 | 0 | 0 |
| text | The Foundation for your Design System | 52.7,60 534.5×39 | 53.3,60 534.5×38 | 0.5 | -0.5 | -0.1 | -1 |

### dashboard-01 1440×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | 10 | 1039,1378.5 14.5×18 | 1052,882 14.7×20 | 13.1 | -495.5 | 0.1 | 2 |
| text | Eddie Lake | 1176.7,948.5 70.2×18 | 1167,808 70.2×20 | -9.7 | -139.5 | -0 | 2 |
| text | Done | 908,949.5 29.3×16 | 902,847 29.4×16 | -6 | -102.5 | 0 | 0 |
| box badge | Done | 885,946.5 59.3×22 | 879,845 60×20 | -6 | -101.5 | 0.7 | -2 |
| text | Focus Documents | 687.1,733 117.7×18 | 616,708 117.7×20 | -71.1 | -24 | 0 | 2 |
| box tabs-trigger | Focus Documents | 678.1,727.5 135.7×29 | 607,705 136×26 | -71.1 | -22.5 | 0.3 | -3 |
| text | Narrative | 733.2,949.5 51.4×16 | 736,884 51.3×16 | 2.8 | -65.5 | -0 | 0 |
| text | Executive summary | 391.5,948.5 128×18 | 329,882 124.9×20 | -62.5 | -65.5 | -3.1 | 2 |
| text | Last 7 days | 1292.1,342 73.9×18 | 1307,276 73.9×20 | 14.9 | -65 | -0 | 2 |
| text | Last 30 days | 1174.6,342 84.6×18 | 1201,276 84.6×20 | 26.4 | -65 | -0 | 2 |
| text | Last 3 months | 1048,342 93.6×18 | 1086,276 93.6×20 | 38.1 | -65 | 0 | 2 |
| box badge | Narrative | 726.2,946.5 65.4×22 | 729,882 66×20 | 2.8 | -64.5 | 0.6 | -2 |
| box toggle-group-item | Last 7 days | 1276.1,333 106.9×36 | 1297,270 95×32 | 20.9 | -63 | -11.9 | -4 |
| box toggle-group-item | Last 30 days | 1158.6,333 117.6×36 | 1191,270 106×32 | 32.4 | -63 | -11.6 | -4 |
| box toggle-group-item | Last 3 months | 1031,333 127.6×36 | 1075,270 116×32 | 44.1 | -63 | -11.6 | -4 |

### dashboard-01 1024×768

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Last 3 months | 820,520 91.8×18 | 670,442 93.6×20 | -150 | -77 | 1.8 | 2 |
| box select-trigger | Last 3 months | 807,513 160×32 | 659,436 116×32 | -148 | -77 | -44 | 0 |
| text | Total for the last 3 months | 337,538 167.9×18 | 296,462 167.9×20 | -41 | -75 | -0 | 2 |
| text | Total Visitors | 337,510 100×21 | 296,436 96.8×22 | -41 | -73.5 | -3.2 | 1 |
| text | Meets growth projections | 685,420 163×18 | 660,360 163×20 | -25 | -59 | -0 | 2 |
| text | Steady performance increase | 685,394 192.3×18 | 660,334 192.3×20 | -25 | -59 | 0 | 2 |
| text | Strong user retention | 337,394 139.1×18 | 296,334 139.1×20 | -41 | -59 | 0 | 2 |
| text | Engagement exceed targets | 337,420 180×18 | 296,360 179.9×20 | -41 | -59 | -0 | 2 |
| text | 4.5% | 685,331 68.7×39 | 660,286 56.2×32 | -25 | -48.5 | -12.5 | -7 |
| text | 45,678 | 337,331 100.4×39 | 296,286 79.7×32 | -41 | -48.5 | -20.7 | -7 |
| text | +12.5% | 571,308 39×16 | 563,264 39.1×16 | -8 | -44 | 0.1 | 0 |
| text | +4.5% | 923.7,308 34.3×16 | 932,264 34.3×16 | 8.3 | -44 | -0 | 0 |
| text | Growth Rate | 685,306 79.7×18 | 660,262 79.7×20 | -25 | -43 | 0 | 2 |
| text | Active Accounts | 337,306 103.5×18 | 296,262 103.5×20 | -41 | -43 | -0 | 2 |
| box badge | +12.5% | 546,305 73×22 | 538,262 74×20 | -8 | -43 | 1 | -2 |

### sidebar-07 1440×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| box sidebar-container | Acme Inc Enterprise Platform Playground History Starred Sett | 0,0 256×900 | 0,0 1440×900 | 0 | 0 | 1184 | 0 |
| text | Data Fetching | 494.5,23 89.3×18 | 489,22 89.3×20 | -5.5 | 0 | -0 | 2 |
| box breadcrumb-link | Build Your Application | 321,23 139.5×18 | 321,22 140×20 | 0 | -1 | 0.5 | 2 |
| box sidebar-inner | Acme Inc Enterprise Platform Playground History Starred Sett | 0,0 255×900 | 0,0 256×900 | 0 | 0 | 1 | 0 |
| text | Acme Inc | 56,14.3 60.8×18 | 56,15 60.8×18 | 0 | 0.8 | 0 | 0 |
| text | shadcn | 56,850.3 47.7×18 | 56,851 47.7×18 | 0 | 0.8 | -0 | 0 |
| text | Settings | 41,207 53×18 | 41,206 53.7×20 | 0 | 0 | 0.7 | 2 |
| text | Settings | 40,315 53×18 | 40,314 53.7×20 | 0 | 0 | 0.7 | 2 |
| text | Enterprise | 56,32.8 55.8×16 | 56,33 55.8×16 | 0 | 0.3 | -0 | 0 |
| text | m@example.com | 56,868.8 93.9×16 | 56,869 93.9×16 | 0 | 0.3 | -0 | 0 |
| text | Travel | 40,475 37.8×18 | 40,474 37.8×20 | 0 | 0 | -0 | 2 |
| text | Platform | 16,80 48.3×16 | 16,80 48.3×16 | 0 | 0 | -0 | 0 |
| text | Projects | 16,372 46.6×16 | 16,372 46.5×16 | 0 | 0 | -0 | 0 |
| text | Models | 40,243 46.9×18 | 40,242 46.9×20 | 0 | 0 | -0 | 2 |
| text | History | 41,143 45.5×18 | 41,142 45.5×20 | 0 | 0 | -0 | 2 |

### sidebar-07 1024×768

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| box sidebar-container | Acme Inc Enterprise Platform Playground History Starred Sett | 0,0 256×768 | 0,0 1024×768 | 0 | 0 | 768 | 0 |
| text | Data Fetching | 494.5,23 89.3×18 | 489,22 89.3×20 | -5.5 | 0 | -0 | 2 |
| box breadcrumb-link | Build Your Application | 321,23 139.5×18 | 321,22 140×20 | 0 | -1 | 0.5 | 2 |
| box sidebar-inner | Acme Inc Enterprise Platform Playground History Starred Sett | 0,0 255×768 | 0,0 256×768 | 0 | 0 | 1 | 0 |
| text | Acme Inc | 56,14.3 60.8×18 | 56,15 60.8×18 | 0 | 0.8 | 0 | 0 |
| text | shadcn | 56,718.3 47.7×18 | 56,719 47.7×18 | 0 | 0.8 | -0 | 0 |
| text | Settings | 41,207 53×18 | 41,206 53.7×20 | 0 | 0 | 0.7 | 2 |
| text | Settings | 40,315 53×18 | 40,314 53.7×20 | 0 | 0 | 0.7 | 2 |
| text | Enterprise | 56,32.8 55.8×16 | 56,33 55.8×16 | 0 | 0.3 | -0 | 0 |
| text | m@example.com | 56,736.8 93.9×16 | 56,737 93.9×16 | 0 | 0.3 | -0 | 0 |
| text | Travel | 40,475 37.8×18 | 40,474 37.8×20 | 0 | 0 | -0 | 2 |
| text | Platform | 16,80 48.3×16 | 16,80 48.3×16 | 0 | 0 | -0 | 0 |
| text | Projects | 16,372 46.6×16 | 16,372 46.5×16 | 0 | 0 | -0 | 0 |
| text | Models | 40,243 46.9×18 | 40,242 46.9×20 | 0 | 0 | -0 | 2 |
| text | History | 41,143 45.5×18 | 41,142 45.5×20 | 0 | 0 | -0 | 2 |

### sidebar-03 1440×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| box sidebar-container | Documentation v1.0.0 Getting Started Installation Project St | 0,0 256×900 | 0,0 1440×900 | 0 | 0 | 1184 | 0 |
| text | Data Fetching | 494.5,22.5 89.3×18 | 489,22 89.3×20 | -5.5 | 0.5 | -0 | 2 |
| box breadcrumb-link | Build Your Application | 321,22.5 139.5×18 | 321,22 140×20 | 0 | -0.5 | 0.5 | 2 |
| box sidebar-inner | Documentation v1.0.0 Getting Started Installation Project St | 0,0 255×900 | 0,0 256×900 | 0 | 0 | 1 | 0 |
| text | Getting Started | 16,79 100.7×18 | 16,78 101.4×20 | 0 | 0 | 0.7 | 2 |
| text | Build Your Application | 321,22.5 139.5×18 | 321,22 139.5×20 | 0 | 0.5 | -0 | 2 |
| text | Documentation | 56,15 100.1×18 | 56,17 100.1×14 | 0 | 0 | 0 | -4 |
| text | Upgrading | 41,531 66.4×18 | 41,530 66.4×20 | 0 | 0 | -0 | 2 |
| text | Caching | 41,307 52.8×18 | 41,306 52.8×20 | 0 | 0 | -0 | 2 |
| text | Edge Runtime | 41,791 89.5×18 | 41,790 89.5×20 | 0 | 0 | -0 | 2 |
| text | Styling | 41,339 44.8×18 | 41,338 44.8×20 | 0 | 0 | -0 | 2 |
| text | Installation | 41,111 70.5×18 | 41,110 70.5×20 | 0 | 0 | -0 | 2 |
| text | Optimizing | 41,371 69.7×18 | 41,370 69.7×20 | 0 | 0 | -0 | 2 |
| text | Examples | 41,563 62.2×18 | 41,562 62.2×20 | 0 | 0 | -0 | 2 |
| text | Configuring | 41,403 74.6×18 | 41,402 74.6×20 | 0 | 0 | -0 | 2 |

### sidebar-03 1024×768

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| box sidebar-container | Documentation v1.0.0 Getting Started Installation Project St | 0,0 256×768 | 0,0 1024×768 | 0 | 0 | 768 | 0 |
| text | Data Fetching | 494.5,22.5 89.3×18 | 489,22 89.3×20 | -5.5 | 0.5 | -0 | 2 |
| box breadcrumb-link | Build Your Application | 321,22.5 139.5×18 | 321,22 140×20 | 0 | -0.5 | 0.5 | 2 |
| box sidebar-inner | Documentation v1.0.0 Getting Started Installation Project St | 0,0 255×768 | 0,0 256×768 | 0 | 0 | 1 | 0 |
| text | Getting Started | 16,79 100.7×18 | 16,78 101.4×20 | 0 | 0 | 0.7 | 2 |
| text | Build Your Application | 321,22.5 139.5×18 | 321,22 139.5×20 | 0 | 0.5 | -0 | 2 |
| text | Documentation | 56,15 100.1×18 | 56,17 100.1×14 | 0 | 0 | 0 | -4 |
| text | Upgrading | 41,531 66.4×18 | 41,530 66.4×20 | 0 | 0 | -0 | 2 |
| text | Caching | 41,307 52.8×18 | 41,306 52.8×20 | 0 | 0 | -0 | 2 |
| text | Styling | 41,339 44.8×18 | 41,338 44.8×20 | 0 | 0 | -0 | 2 |
| text | Installation | 41,111 70.5×18 | 41,110 70.5×20 | 0 | 0 | -0 | 2 |
| text | Optimizing | 41,371 69.7×18 | 41,370 69.7×20 | 0 | 0 | -0 | 2 |
| text | Examples | 41,563 62.2×18 | 41,562 62.2×20 | 0 | 0 | -0 | 2 |
| text | Configuring | 41,403 74.6×18 | 41,402 74.6×20 | 0 | 0 | -0 | 2 |
| text | File Conventions | 41,663 105.6×18 | 41,662 105.6×20 | 0 | 0 | -0 | 2 |

### login-03 1440×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Terms of Service | 635.3,733.1 233.5×39 | 605.3,712 106.4×21 | -30 | -30.1 | -127.1 | -18 |
| box card | Welcome back Login with your Apple or Google account Login w | 528,173.9 384×534.3 | 528,215 384×452 | 0 | 41.1 | 0 | -82.3 |
| text | Acme Inc. | 699.6,126.9 72.9×21 | 700,167 72.9×24 | 0.4 | 41.6 | 0 | 3 |
| text | Privacy Policy | 714.1,754.1 87.8×18 | 744.2,712 88.6×21 | 30.1 | -40.6 | 0.8 | 3 |
| text | Welcome back | 649.2,199.9 141.5×26 | 650.8,231 138.4×28 | 1.6 | 32.1 | -3.1 | 2 |
| text | Don't have an account? | 618.9,663.1 153.5×18 | 618.5,630 150×21 | -0.4 | -31.6 | -3.5 | 3 |
| text | Sign up | 772.4,663.1 48.7×18 | 773.1,630 48.7×21 | 0.7 | -31.6 | 0 | 3 |
| text | Login with your Apple or Google account | 590.2,235.9 259.6×18 | 590.2,263 259.5×20 | 0 | 28.1 | -0 | 2 |
| text | Login | 702,623.1 36×18 | 702,596 36×20 | 0 | -26.1 | -0 | 2 |
| box button | Login | 553,614.1 334×36 | 544,590 352×32 | -9 | -24.1 | 18 | -4 |
| box button | Login with Apple | 553,278.9 334×36 | 544,299 352×32 | -9 | 20.1 | 18 | -4 |
| text | Login with Apple | 677.4,287.9 109.3×18 | 676,305 109.3×20 | -1.4 | 18.1 | 0 | 2 |
| box button | Login with Google | 553,326.9 334×36 | 544,339 352×32 | -9 | 12.1 | 18 | -4 |
| text | Login with Google | 673.1,335.9 117.9×18 | 672,345 117.9×20 | -1 | 10.1 | 0 | 2 |
| text | Password | 553,518.5 64×18 | 544,511 64×19 | -9 | -7 | -0 | 1 |

### login-03 1024×768

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Terms of Service | 427.3,667.1 233.5×39 | 397.3,646 106.4×21 | -30 | -30.1 | -127.1 | -18 |
| box card | Welcome back Login with your Apple or Google account Login w | 320,107.9 384×534.3 | 320,149 384×452 | 0 | 41.1 | 0 | -82.3 |
| text | Acme Inc. | 491.6,60.9 72.9×21 | 492,101 72.9×24 | 0.4 | 41.6 | 0 | 3 |
| text | Privacy Policy | 506.1,688.1 87.8×18 | 536.2,646 88.6×21 | 30.1 | -40.6 | 0.8 | 3 |
| text | Welcome back | 441.2,133.9 141.5×26 | 442.8,165 138.4×28 | 1.5 | 32.1 | -3.1 | 2 |
| text | Don't have an account? | 410.9,597.1 153.5×18 | 410.5,564 150×21 | -0.4 | -31.6 | -3.5 | 3 |
| text | Sign up | 564.4,597.1 48.7×18 | 565.1,564 48.7×21 | 0.7 | -31.6 | 0 | 3 |
| text | Login with your Apple or Google account | 382.2,169.9 259.6×18 | 382.2,197 259.5×20 | 0 | 28.1 | -0 | 2 |
| text | Login | 494,557.1 36×18 | 494,530 36×20 | 0 | -26.1 | -0 | 2 |
| box button | Login | 345,548.1 334×36 | 336,524 352×32 | -9 | -24.1 | 18 | -4 |
| box button | Login with Apple | 345,212.9 334×36 | 336,233 352×32 | -9 | 20.1 | 18 | -4 |
| text | Login with Apple | 469.4,221.9 109.3×18 | 468,239 109.3×20 | -1.4 | 18.1 | 0 | 2 |
| box button | Login with Google | 345,260.9 334×36 | 336,273 352×32 | -9 | 12.1 | 18 | -4 |
| text | Login with Google | 465.1,269.9 117.9×18 | 464,279 117.9×20 | -1 | 10.1 | 0 | 2 |
| text | Password | 345,452.5 64×18 | 336,445 64×19 | -9 | -7 | -0 | 1 |

### login-04 1440×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| box card | Welcome back Login to your Acme Inc account Email Password F | 272,162.4 896×530.3 | 272,193 896×469 | 0 | 30.6 | 0 | -61.3 |
| text | Terms of Service | 727.3,717.6 106.4×18 | 727.3,686 106.4×21 | 0 | -30.1 | -0 | 3 |
| text | Privacy Policy | 864.7,717.6 87.8×18 | 866.2,686 88.6×21 | 1.5 | -30.1 | 0.8 | 3 |
| text | Welcome back | 409.7,195.4 173.6×31 | 409.2,225 173.5×32 | -0.5 | 30.1 | -0 | 1 |
| text | Sign up | 548.9,639.6 48.7×18 | 549.1,609 48.7×21 | 0.2 | -29.1 | 0 | 3 |
| text | Don't have an account? | 395.4,639.6 153.5×18 | 394.5,609 150×21 | -0.9 | -29.1 | -3.5 | 3 |
| text | Login to your Acme Inc account | 381,236.4 230.9×21 | 395.5,265 202×20 | 14.4 | 28.1 | -28.9 | -1 |
| text | Email | 305,287.4 36×18 | 304,305 36×20 | -1 | 18.6 | 0 | 2 |
| text | Or continue with | 443.7,535.6 105.7×18 | 443,517 105.6×20 | -0.7 | -17.6 | -0 | 2 |
| text | Login | 478.5,487.6 36×18 | 478,471 36×20 | -0.5 | -15.6 | -0 | 2 |
| box button | Login | 305,478.6 383×36 | 304,465 384×32 | -1 | -13.6 | 1 | -4 |
| text | Password | 305,383 64×18 | 304,385 64×19 | -1 | 2.5 | -0 | 1 |
| text | Forgot your password? | 541,383.6 147×18 | 541,385 147×20 | -0 | 2.4 | -0 | 2 |

### login-04 1024×768

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| box card | Welcome back Login to your Acme Inc account Email Password F | 64,96.4 896×530.3 | 64,127 896×469 | 0 | 30.6 | 0 | -61.3 |
| text | Terms of Service | 519.3,651.6 106.4×18 | 519.3,620 106.4×21 | 0 | -30.1 | -0 | 3 |
| text | Privacy Policy | 656.7,651.6 87.8×18 | 658.2,620 88.6×21 | 1.5 | -30.1 | 0.8 | 3 |
| text | Welcome back | 201.7,129.4 173.6×31 | 201.2,159 173.5×32 | -0.5 | 30.1 | -0 | 1 |
| text | Sign up | 340.9,573.6 48.7×18 | 341.1,543 48.7×21 | 0.2 | -29.1 | 0 | 3 |
| text | Don't have an account? | 187.4,573.6 153.5×18 | 186.5,543 150×21 | -0.9 | -29.1 | -3.5 | 3 |
| text | Login to your Acme Inc account | 173,170.4 230.9×21 | 187.5,199 202×20 | 14.4 | 28.1 | -28.9 | -1 |
| text | Email | 97,221.4 36×18 | 96,239 36×20 | -1 | 18.6 | 0 | 2 |
| text | Or continue with | 235.7,469.6 105.7×18 | 235,451 105.6×20 | -0.7 | -17.6 | -0 | 2 |
| text | Login | 270.5,421.6 36×18 | 270,405 36×20 | -0.5 | -15.6 | -0 | 2 |
| box button | Login | 97,412.6 383×36 | 96,399 384×32 | -1 | -13.6 | 1 | -4 |
| text | Password | 97,317 64×18 | 96,319 64×19 | -1 | 2.5 | -0 | 1 |
| text | Forgot your password? | 333,317.6 147×18 | 333,319 147×20 | -0 | 2.4 | -0 | 2 |

### signup-03 1440×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Terms of Service | 635.3,724.9 233.5×39 | 605.3,700 106.4×21 | -30 | -33.9 | -127.1 | -18 |
| box card | Create your account Enter your email below to create your ac | 528,182.1 384×517.8 | 528,227 384×428 | 0 | 44.9 | 0 | -89.8 |
| text | Acme Inc. | 699.6,135.1 72.9×21 | 700,179 72.9×24 | 0.4 | 45.4 | 0 | 3 |
| text | Privacy Policy | 714.1,745.9 87.8×18 | 744.2,700 88.6×21 | 30.1 | -44.4 | 0.8 | 3 |
| text | Create your account | 622.8,208.1 194.4×26 | 625.2,243 189.7×28 | 2.3 | 35.9 | -4.7 | 2 |
| text | Already have an account? | 613.8,654.9 168.4×18 | 614.1,618 164.9×21 | 0.3 | -35.4 | -3.5 | 3 |
| text | Sign in | 782.2,654.9 44×18 | 783,618 44×21 | 0.8 | -35.4 | -0 | 3 |
| text | Enter your email below to create your account | 573,244.1 294×18 | 573,275 294×20 | 0 | 31.9 | -0 | 2 |
| text | Create Account | 669.1,614.9 101.8×18 | 669,584 101.8×20 | -0.1 | -29.9 | 0 | 2 |
| box button | Create Account | 553,605.9 334×36 | 544,578 352×32 | -9 | -27.9 | 18 | -4 |
| text | Full Name | 553,287.1 65.7×18 | 544,311 65.7×19 | -9 | 24.4 | -0 | 1 |
| text | Must be at least 8 characters long. | 553,557.9 220.2×18 | 544,537 220.2×21 | -9 | -19.4 | -0 | 3 |
| text | Password | 553,477.6 64×18 | 544,470 64×19 | -9 | -7.1 | -0 | 1 |
| text | Email | 553,382.4 36×18 | 544,390 36×20 | -9 | 8.6 | 0 | 2 |
| text | Confirm Password | 728,477.6 119.6×18 | 728,470 119.6×19 | 0 | -7.1 | -0 | 1 |

### signup-03 1024×768

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| text | Terms of Service | 427.3,658.9 233.5×39 | 397.3,634 106.4×21 | -30 | -33.9 | -127.1 | -18 |
| box card | Create your account Enter your email below to create your ac | 320,116.1 384×517.8 | 320,161 384×428 | 0 | 44.9 | 0 | -89.8 |
| text | Acme Inc. | 491.6,69.1 72.9×21 | 492,113 72.9×24 | 0.4 | 45.4 | 0 | 3 |
| text | Privacy Policy | 506.1,679.9 87.8×18 | 536.2,634 88.6×21 | 30.1 | -44.4 | 0.8 | 3 |
| text | Create your account | 414.8,142.1 194.4×26 | 417.2,177 189.7×28 | 2.3 | 35.9 | -4.7 | 2 |
| text | Already have an account? | 405.8,588.9 168.4×18 | 406.1,552 164.9×21 | 0.3 | -35.4 | -3.5 | 3 |
| text | Sign in | 574.2,588.9 44×18 | 575,552 44×21 | 0.8 | -35.4 | -0 | 3 |
| text | Enter your email below to create your account | 365,178.1 294×18 | 365,209 294×20 | 0 | 31.9 | -0 | 2 |
| text | Create Account | 461.1,548.9 101.8×18 | 461,518 101.8×20 | -0.1 | -29.9 | 0 | 2 |
| box button | Create Account | 345,539.9 334×36 | 336,512 352×32 | -9 | -27.9 | 18 | -4 |
| text | Full Name | 345,221.1 65.7×18 | 336,245 65.7×19 | -9 | 24.4 | -0 | 1 |
| text | Must be at least 8 characters long. | 345,491.9 220.2×18 | 336,471 220.2×21 | -9 | -19.4 | -0 | 3 |
| text | Password | 345,411.6 64×18 | 336,404 64×19 | -9 | -7.1 | -0 | 1 |
| text | Email | 345,316.4 36×18 | 336,324 36×20 | -9 | 8.6 | 0 | 2 |
| text | Confirm Password | 520,411.6 119.6×18 | 520,404 119.6×19 | 0 | -7.1 | -0 | 1 |

### signup-04 1440×900

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| box card | Create your account Enter your email below to create your ac | 272,121.3 896×612.5 | 272,154 896×547 | 0 | 32.8 | 0 | -65.5 |
| text | Create your account | 377.1,154.3 238.7×31 | 377.1,186 238.7×32 | -0 | 32.3 | 0 | 1 |
| text | Terms of Service | 727.3,758.8 106.4×18 | 727.3,725 106.4×21 | 0 | -32.3 | -0 | 3 |
| text | Privacy Policy | 864.7,758.8 87.8×18 | 866.2,725 88.6×21 | 1.5 | -32.3 | 0.8 | 3 |
| text | Enter your email below to create your account | 349.5,195.3 294×18 | 349.5,226 294×20 | 0 | 31.8 | -0 | 2 |
| text | Already have an account? | 390.3,680.8 168.4×18 | 390.1,648 164.9×21 | -0.2 | -31.3 | -3.5 | 3 |
| text | Sign in | 558.7,680.8 44×18 | 559,648 44×21 | 0.3 | -31.3 | -0 | 3 |
| text | Email | 305,242.3 36×18 | 304,266 36×20 | -1 | 24.8 | 0 | 2 |
| text | Or continue with | 443.7,576.8 105.7×18 | 443,556 105.6×20 | -0.7 | -19.8 | -0 | 2 |
| text | We'll use this to contact you. We will not share your email  | 305,322.5 365.1×39 | 304,334 384×42 | -1 | 13 | 18.9 | 3 |
| text | Create Account | 445.6,528.8 101.8×18 | 445,510 101.8×20 | -0.6 | -17.8 | 0 | 2 |
| box button | Create Account | 305,519.8 383×36 | 304,504 384×32 | -1 | -15.8 | 1 | -4 |
| text | Must be at least 8 characters long. | 305,471.8 220.2×18 | 304,463 220.2×21 | -1 | -7.3 | -0 | 3 |
| text | Confirm Password | 504.5,391.5 119.6×18 | 504,396 119.6×19 | -0.5 | 5 | -0 | 1 |
| text | Password | 305,391.5 64×18 | 304,396 64×19 | -1 | 5 | -0 | 1 |

### signup-04 1024×768

| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |
|---|---|---|---|---|---|---|---|
| box card | Create your account Enter your email below to create your ac | 64,55.3 896×612.5 | 64,88 896×547 | 0 | 32.8 | 0 | -65.5 |
| text | Create your account | 169.1,88.3 238.7×31 | 169.1,120 238.7×32 | -0 | 32.3 | 0 | 1 |
| text | Terms of Service | 519.3,692.8 106.4×18 | 519.3,659 106.4×21 | 0 | -32.3 | -0 | 3 |
| text | Privacy Policy | 656.7,692.8 87.8×18 | 658.2,659 88.6×21 | 1.5 | -32.3 | 0.8 | 3 |
| text | Enter your email below to create your account | 141.5,129.3 294×18 | 141.5,160 294×20 | 0 | 31.8 | -0 | 2 |
| text | Already have an account? | 182.3,614.8 168.4×18 | 182.1,582 164.9×21 | -0.2 | -31.3 | -3.5 | 3 |
| text | Sign in | 350.7,614.8 44×18 | 351,582 44×21 | 0.3 | -31.3 | -0 | 3 |
| text | Email | 97,176.3 36×18 | 96,200 36×20 | -1 | 24.8 | 0 | 2 |
| text | Or continue with | 235.7,510.8 105.7×18 | 235,490 105.6×20 | -0.7 | -19.8 | -0 | 2 |
| text | We'll use this to contact you. We will not share your email  | 97,256.5 365.1×39 | 96,268 384×42 | -1 | 13 | 18.9 | 3 |
| text | Create Account | 237.6,462.8 101.8×18 | 237,444 101.8×20 | -0.6 | -17.8 | 0 | 2 |
| box button | Create Account | 97,453.8 383×36 | 96,438 384×32 | -1 | -15.8 | 1 | -4 |
| text | Must be at least 8 characters long. | 97,405.8 220.2×18 | 96,397 220.2×21 | -1 | -7.3 | -0 | 3 |
| text | Confirm Password | 296.5,325.5 119.6×18 | 296,330 119.6×19 | -0.5 | 5 | -0 | 1 |
| text | Password | 97,325.5 64×18 | 96,330 64×19 | -1 | 5 | -0 | 1 |

## States

Interaction states (hover, keyboard focus, press, click/open, type) from `tools/ShadcnRef/scenarios.json`, one clipped
region per state: the target box(es) and every floating layer that opened (menu, select list, tooltip), inflated by
24 px — or only the modal content when a dialog opened. Images live in `states/{reference,gotx,diff,compare}/<page>-<state>-<theme>.png`
(+ region layout maps `.json`). Both sides use real input: Edge via CDP `Input.dispatchMouseEvent` / `dispatchKeyEvent`
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
| home / hero-badge-hover | light | 278×70 → 277×68 | 28.6 % / 10.0 % | 0 / 0 | 4 (3) | 2 | region size differs | [ref](states/reference/home-hero-badge-hover-light.png) · [gotx](states/gotx/home-hero-badge-hover-light.png) · [compare](states/compare/home-hero-badge-hover-light.png) |
| home / hero-badge-hover | dark | 278×70 → 277×68 | 28.6 % / 14.7 % | 0 / 0 | 4 (3) | 2 | region size differs | [ref](states/reference/home-hero-badge-hover-dark.png) · [gotx](states/gotx/home-hero-badge-hover-dark.png) · [compare](states/compare/home-hero-badge-hover-dark.png) |
| home / hero-badge-focus-key | light | 278×70 → 277×68 | 18.5 % / 16.3 % | 0 / 0 | 4 (3) | 2 | region size differs | [ref](states/reference/home-hero-badge-focus-key-light.png) · [gotx](states/gotx/home-hero-badge-focus-key-light.png) · [compare](states/compare/home-hero-badge-focus-key-light.png) |
| home / hero-badge-focus-key | dark | 278×70 → 277×68 | 21.7 % / 16.7 % | 0 / 0 | 4 (3) | 2 | region size differs | [ref](states/reference/home-hero-badge-focus-key-dark.png) · [gotx](states/gotx/home-hero-badge-focus-key-dark.png) · [compare](states/compare/home-hero-badge-focus-key-dark.png) |
| home / get-started-hover | light | 151×84 → 147×80 | 34.2 % / 16.7 % | 0 / 0 | 5 (0) | 4 | region size differs | [ref](states/reference/home-get-started-hover-light.png) · [gotx](states/gotx/home-get-started-hover-light.png) · [compare](states/compare/home-get-started-hover-light.png) |
| home / get-started-hover | dark | 151×84 → 147×80 | 19.0 % / 17.4 % | 0 / 0 | 5 (0) | 4 | region size differs | [ref](states/reference/home-get-started-hover-dark.png) · [gotx](states/gotx/home-get-started-hover-dark.png) · [compare](states/compare/home-get-started-hover-dark.png) |
| home / get-started-focus-key | light | 151×84 → 147×80 | 41.8 % / 21.4 % | 0 / 0 | 5 (0) | 4 | region size differs | [ref](states/reference/home-get-started-focus-key-light.png) · [gotx](states/gotx/home-get-started-focus-key-light.png) · [compare](states/compare/home-get-started-focus-key-light.png) |
| home / get-started-focus-key | dark | 151×84 → 147×80 | 26.6 % / 22.3 % | 0 / 0 | 5 (0) | 4 | region size differs | [ref](states/reference/home-get-started-focus-key-dark.png) · [gotx](states/gotx/home-get-started-focus-key-dark.png) · [compare](states/compare/home-get-started-focus-key-dark.png) |
| home / get-started-press | light | 151×84 → 147×80 | 34.2 % / 16.7 % | 0 / 0 | 5 (0) | 4 | region size differs | [ref](states/reference/home-get-started-press-light.png) · [gotx](states/gotx/home-get-started-press-light.png) · [compare](states/compare/home-get-started-press-light.png) |
| home / get-started-press | dark | 151×84 → 147×80 | 19.0 % / 17.4 % | 0 / 0 | 5 (0) | 4 | region size differs | [ref](states/reference/home-get-started-press-dark.png) · [gotx](states/gotx/home-get-started-press-dark.png) · [compare](states/compare/home-get-started-press-dark.png) |
| home / view-components-hover | light | 194×85 → 189×80 | 40.5 % / 19.8 % | 0 / 0 | 5 (0) | 3.1 | region size differs | [ref](states/reference/home-view-components-hover-light.png) · [gotx](states/gotx/home-view-components-hover-light.png) · [compare](states/compare/home-view-components-hover-light.png) |
| home / view-components-hover | dark | 194×85 → 189×80 | 38.6 % / 20.0 % | 0 / 0 | 5 (0) | 3.1 | region size differs | [ref](states/reference/home-view-components-hover-dark.png) · [gotx](states/gotx/home-view-components-hover-dark.png) · [compare](states/compare/home-view-components-hover-dark.png) |
| home / view-components-focus-key | light | 194×85 → 189×80 | 32.7 % / 26.5 % | 0 / 0 | 5 (0) | 3.1 | region size differs | [ref](states/reference/home-view-components-focus-key-light.png) · [gotx](states/gotx/home-view-components-focus-key-light.png) · [compare](states/compare/home-view-components-focus-key-light.png) |
| home / view-components-focus-key | dark | 194×85 → 189×80 | 31.0 % / 27.5 % | 0 / 0 | 5 (0) | 3.1 | region size differs | [ref](states/reference/home-view-components-focus-key-dark.png) · [gotx](states/gotx/home-view-components-focus-key-dark.png) · [compare](states/compare/home-view-components-focus-key-dark.png) |
| home / view-components-press | light | 194×85 → 189×80 | 40.5 % / 19.8 % | 0 / 0 | 5 (0) | 3.1 | region size differs | [ref](states/reference/home-view-components-press-light.png) · [gotx](states/gotx/home-view-components-press-light.png) · [compare](states/compare/home-view-components-press-light.png) |
| home / view-components-press | dark | 194×85 → 189×80 | 38.6 % / 20.0 % | 0 / 0 | 5 (0) | 3.1 | region size differs | [ref](states/reference/home-view-components-press-dark.png) · [gotx](states/gotx/home-view-components-press-dark.png) · [compare](states/compare/home-view-components-press-dark.png) |
| home / button-default-hover | light | 138×81 → 137×80 | 20.8 % / 8.6 % | 0 / 0 | 7 (3) | 2.7 | region size differs | [ref](states/reference/home-button-default-hover-light.png) · [gotx](states/gotx/home-button-default-hover-light.png) · [compare](states/compare/home-button-default-hover-light.png) |
| home / button-default-hover | dark | 138×81 → 137×80 | 31.4 % / 9.4 % | 0 / 0 | 7 (3) | 2.7 | region size differs | [ref](states/reference/home-button-default-hover-dark.png) · [gotx](states/gotx/home-button-default-hover-dark.png) · [compare](states/compare/home-button-default-hover-dark.png) |
| home / button-default-focus-key | light | 138×81 → 137×80 | 29.1 % / 13.1 % | 0 / 0 | 7 (3) | 2.7 | region size differs | [ref](states/reference/home-button-default-focus-key-light.png) · [gotx](states/gotx/home-button-default-focus-key-light.png) · [compare](states/compare/home-button-default-focus-key-light.png) |
| home / button-default-focus-key | dark | 138×81 → 137×80 | 23.4 % / 13.9 % | 0 / 0 | 7 (3) | 2.7 | region size differs | [ref](states/reference/home-button-default-focus-key-dark.png) · [gotx](states/gotx/home-button-default-focus-key-dark.png) · [compare](states/compare/home-button-default-focus-key-dark.png) |
| home / button-default-press | light | 138×81 → 137×80 | 20.8 % / 8.6 % | 0 / 0 | 7 (3) | 2.7 | region size differs | [ref](states/reference/home-button-default-press-light.png) · [gotx](states/gotx/home-button-default-press-light.png) · [compare](states/compare/home-button-default-press-light.png) |
| home / button-default-press | dark | 138×81 → 137×80 | 31.4 % / 9.4 % | 0 / 0 | 7 (3) | 2.7 | region size differs | [ref](states/reference/home-button-default-press-dark.png) · [gotx](states/gotx/home-button-default-press-dark.png) · [compare](states/compare/home-button-default-press-dark.png) |
| home / button-secondary-hover | light | 146×81 → 141×80 | 36.9 % / 10.8 % | 0 / 0 | 8 (2) | 4.1 | region size differs | [ref](states/reference/home-button-secondary-hover-light.png) · [gotx](states/gotx/home-button-secondary-hover-light.png) · [compare](states/compare/home-button-secondary-hover-light.png) |
| home / button-secondary-hover | dark | 146×81 → 141×80 | 35.8 % / 12.4 % | 0 / 0 | 8 (2) | 4.1 | region size differs | [ref](states/reference/home-button-secondary-hover-dark.png) · [gotx](states/gotx/home-button-secondary-hover-dark.png) · [compare](states/compare/home-button-secondary-hover-dark.png) |
| home / button-secondary-focus-key | light | 146×81 → 141×80 | 30.2 % / 17.6 % | 0 / 0 | 8 (2) | 4.1 | region size differs | [ref](states/reference/home-button-secondary-focus-key-light.png) · [gotx](states/gotx/home-button-secondary-focus-key-light.png) · [compare](states/compare/home-button-secondary-focus-key-light.png) |
| home / button-secondary-focus-key | dark | 146×81 → 141×80 | 29.2 % / 19.2 % | 0 / 0 | 8 (2) | 4.1 | region size differs | [ref](states/reference/home-button-secondary-focus-key-dark.png) · [gotx](states/gotx/home-button-secondary-focus-key-dark.png) · [compare](states/compare/home-button-secondary-focus-key-dark.png) |
| home / button-secondary-press | light | 146×81 → 141×80 | 36.9 % / 10.8 % | 0 / 0 | 8 (2) | 4.1 | region size differs | [ref](states/reference/home-button-secondary-press-light.png) · [gotx](states/gotx/home-button-secondary-press-light.png) · [compare](states/compare/home-button-secondary-press-light.png) |
| home / button-secondary-press | dark | 146×81 → 141×80 | 35.8 % / 12.4 % | 0 / 0 | 8 (2) | 4.1 | region size differs | [ref](states/reference/home-button-secondary-press-dark.png) · [gotx](states/gotx/home-button-secondary-press-dark.png) · [compare](states/compare/home-button-secondary-press-dark.png) |
| home / button-outline-hover | light | 122×81 → 118×80 | 21.4 % / 10.1 % | 0 / 0 | 7 (1) | 3.8 | region size differs | [ref](states/reference/home-button-outline-hover-light.png) · [gotx](states/gotx/home-button-outline-hover-light.png) · [compare](states/compare/home-button-outline-hover-light.png) |
| home / button-outline-hover | dark | 122×81 → 118×80 | 31.7 % / 11.7 % | 0 / 0 | 7 (1) | 3.8 | region size differs | [ref](states/reference/home-button-outline-hover-dark.png) · [gotx](states/gotx/home-button-outline-hover-dark.png) · [compare](states/compare/home-button-outline-hover-dark.png) |
| home / button-outline-focus-key | light | 122×81 → 118×80 | 27.8 % / 15.8 % | 0 / 0 | 7 (1) | 3.8 | region size differs | [ref](states/reference/home-button-outline-focus-key-light.png) · [gotx](states/gotx/home-button-outline-focus-key-light.png) · [compare](states/compare/home-button-outline-focus-key-light.png) |
| home / button-outline-focus-key | dark | 122×81 → 118×80 | 37.9 % / 16.6 % | 0 / 0 | 7 (1) | 3.8 | region size differs | [ref](states/reference/home-button-outline-focus-key-dark.png) · [gotx](states/gotx/home-button-outline-focus-key-dark.png) · [compare](states/compare/home-button-outline-focus-key-dark.png) |
| home / button-outline-press | light | 122×81 → 118×80 | 21.4 % / 10.1 % | 0 / 0 | 7 (1) | 3.8 | region size differs | [ref](states/reference/home-button-outline-press-light.png) · [gotx](states/gotx/home-button-outline-press-light.png) · [compare](states/compare/home-button-outline-press-light.png) |
| home / button-outline-press | dark | 122×81 → 118×80 | 31.7 % / 11.7 % | 0 / 0 | 7 (1) | 3.8 | region size differs | [ref](states/reference/home-button-outline-press-dark.png) · [gotx](states/gotx/home-button-outline-press-dark.png) · [compare](states/compare/home-button-outline-press-dark.png) |
| home / input-name-hover | light | 312×81 → 338×80 | 44.6 % / 11.7 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-input-name-hover-light.png) · [gotx](states/gotx/home-input-name-hover-light.png) · [compare](states/compare/home-input-name-hover-light.png) |
| home / input-name-hover | dark | 312×81 → 338×80 | 44.7 % / 11.4 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-input-name-hover-dark.png) · [gotx](states/gotx/home-input-name-hover-dark.png) · [compare](states/compare/home-input-name-hover-dark.png) |
| home / input-name-focus-key | light | 312×81 → 338×80 | 51.3 % / 17.7 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-input-name-focus-key-light.png) · [gotx](states/gotx/home-input-name-focus-key-light.png) · [compare](states/compare/home-input-name-focus-key-light.png) |
| home / input-name-focus-key | dark | 312×81 → 338×80 | 51.3 % / 17.1 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-input-name-focus-key-dark.png) · [gotx](states/gotx/home-input-name-focus-key-dark.png) · [compare](states/compare/home-input-name-focus-key-dark.png) |
| home / input-name-type | light | 312×81 → 338×80 | 51.3 % / 19.3 % | 0 / 0 | 2 (0) | 26 | region size differs | [ref](states/reference/home-input-name-type-light.png) · [gotx](states/gotx/home-input-name-type-light.png) · [compare](states/compare/home-input-name-type-light.png) |
| home / input-name-type | dark | 312×81 → 338×80 | 51.3 % / 18.7 % | 0 / 0 | 2 (0) | 26 | region size differs | [ref](states/reference/home-input-name-type-dark.png) · [gotx](states/gotx/home-input-name-type-dark.png) · [compare](states/compare/home-input-name-type-dark.png) |
| home / textarea-message-hover | light | 338×113 → 338×112 | 52.7 % / 2.7 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-textarea-message-hover-light.png) · [gotx](states/gotx/home-textarea-message-hover-light.png) · [compare](states/compare/home-textarea-message-hover-light.png) |
| home / textarea-message-hover | dark | 338×113 → 338×112 | 52.4 % / 2.8 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-textarea-message-hover-dark.png) · [gotx](states/gotx/home-textarea-message-hover-dark.png) · [compare](states/compare/home-textarea-message-hover-dark.png) |
| home / textarea-message-focus-key | light | 338×113 → 338×112 | 58.2 % / 7.5 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-textarea-message-focus-key-light.png) · [gotx](states/gotx/home-textarea-message-focus-key-light.png) · [compare](states/compare/home-textarea-message-focus-key-light.png) |
| home / textarea-message-focus-key | dark | 338×113 → 338×112 | 57.8 % / 7.2 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-textarea-message-focus-key-dark.png) · [gotx](states/gotx/home-textarea-message-focus-key-dark.png) · [compare](states/compare/home-textarea-message-focus-key-dark.png) |
| home / radio-first-hover | light | 64×65 → 72×64 | 18.8 % / 14.6 % | 0 / 0 | 3 (0) | 481 | region size differs | [ref](states/reference/home-radio-first-hover-light.png) · [gotx](states/gotx/home-radio-first-hover-light.png) · [compare](states/compare/home-radio-first-hover-light.png) |
| home / radio-first-hover | dark | 64×65 → 72×64 | 19.2 % / 17.9 % | 0 / 0 | 3 (0) | 481 | region size differs | [ref](states/reference/home-radio-first-hover-dark.png) · [gotx](states/gotx/home-radio-first-hover-dark.png) · [compare](states/compare/home-radio-first-hover-dark.png) |
| home / radio-first-focus-key | light | 64×65 → 72×64 | 23.4 % / 15.3 % | 0 / 0 | 3 (0) | 481 | region size differs | [ref](states/reference/home-radio-first-focus-key-light.png) · [gotx](states/gotx/home-radio-first-focus-key-light.png) · [compare](states/compare/home-radio-first-focus-key-light.png) |
| home / radio-first-focus-key | dark | 64×65 → 72×64 | 23.9 % / 18.6 % | 0 / 0 | 3 (0) | 481 | region size differs | [ref](states/reference/home-radio-first-focus-key-dark.png) · [gotx](states/gotx/home-radio-first-focus-key-dark.png) · [compare](states/compare/home-radio-first-focus-key-dark.png) |
| home / radio-second-click | light | 64×65 → 72×64 | 22.6 % / 18.1 % | 0 / 0 | 3 (0) | 481 | region size differs | [ref](states/reference/home-radio-second-click-light.png) · [gotx](states/gotx/home-radio-second-click-light.png) · [compare](states/compare/home-radio-second-click-light.png) |
| home / radio-second-click | dark | 64×65 → 72×64 | 23.0 % / 21.4 % | 0 / 0 | 3 (0) | 481 | region size differs | [ref](states/reference/home-radio-second-click-dark.png) · [gotx](states/gotx/home-radio-second-click-dark.png) · [compare](states/compare/home-radio-second-click-dark.png) |
| home / checkbox-hover | light | 64×65 → 64×64 | 13.0 % / 6.0 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-checkbox-hover-light.png) · [gotx](states/gotx/home-checkbox-hover-light.png) · [compare](states/compare/home-checkbox-hover-light.png) |
| home / checkbox-hover | dark | 64×65 → 64×64 | 13.0 % / 7.9 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-checkbox-hover-dark.png) · [gotx](states/gotx/home-checkbox-hover-dark.png) · [compare](states/compare/home-checkbox-hover-dark.png) |
| home / checkbox-focus-key | light | 64×65 → 64×64 | 19.2 % / 8.1 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-checkbox-focus-key-light.png) · [gotx](states/gotx/home-checkbox-focus-key-light.png) · [compare](states/compare/home-checkbox-focus-key-light.png) |
| home / checkbox-focus-key | dark | 64×65 → 64×64 | 19.0 % / 10.0 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-checkbox-focus-key-dark.png) · [gotx](states/gotx/home-checkbox-focus-key-dark.png) · [compare](states/compare/home-checkbox-focus-key-dark.png) |
| home / checkbox-click | light | 64×65 → 64×64 | 16.7 % / 4.2 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-checkbox-click-light.png) · [gotx](states/gotx/home-checkbox-click-light.png) · [compare](states/compare/home-checkbox-click-light.png) |
| home / checkbox-click | dark | 64×65 → 64×64 | 16.7 % / 6.4 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-checkbox-click-dark.png) · [gotx](states/gotx/home-checkbox-click-dark.png) · [compare](states/compare/home-checkbox-click-dark.png) |
| home / switch-hover | light | 80×69 → 80×66 | 16.6 % / 11.2 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-switch-hover-light.png) · [gotx](states/gotx/home-switch-hover-light.png) · [compare](states/compare/home-switch-hover-light.png) |
| home / switch-hover | dark | 80×69 → 80×66 | 17.4 % / 11.1 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-switch-hover-dark.png) · [gotx](states/gotx/home-switch-hover-dark.png) · [compare](states/compare/home-switch-hover-dark.png) |
| home / switch-focus-key | light | 80×69 → 80×66 | 23.3 % / 14.5 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-switch-focus-key-light.png) · [gotx](states/gotx/home-switch-focus-key-light.png) · [compare](states/compare/home-switch-focus-key-light.png) |
| home / switch-focus-key | dark | 80×69 → 80×66 | 24.3 % / 14.4 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-switch-focus-key-dark.png) · [gotx](states/gotx/home-switch-focus-key-dark.png) · [compare](states/compare/home-switch-focus-key-dark.png) |
| home / switch-click | light | 80×69 → 80×66 | 19.1 % / 8.1 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-switch-click-light.png) · [gotx](states/gotx/home-switch-click-light.png) · [compare](states/compare/home-switch-click-light.png) |
| home / switch-click | dark | 80×69 → 80×66 | 20.1 % / 10.6 % | 0 / 0 | 3 (1) | 481 | region size differs | [ref](states/reference/home-switch-click-dark.png) · [gotx](states/gotx/home-switch-click-dark.png) · [compare](states/compare/home-switch-click-dark.png) |
| home / alert-dialog-hover | light | 151×81 → 147×80 | 21.6 % / 12.0 % | 0 / 0 | 5 (2) | 3.8 | region size differs | [ref](states/reference/home-alert-dialog-hover-light.png) · [gotx](states/gotx/home-alert-dialog-hover-light.png) · [compare](states/compare/home-alert-dialog-hover-light.png) |
| home / alert-dialog-hover | dark | 151×81 → 147×80 | 37.7 % / 11.4 % | 0 / 0 | 5 (2) | 3.8 | region size differs | [ref](states/reference/home-alert-dialog-hover-dark.png) · [gotx](states/gotx/home-alert-dialog-hover-dark.png) · [compare](states/compare/home-alert-dialog-hover-dark.png) |
| home / alert-dialog-focus-key | light | 151×81 → 147×80 | 28.1 % / 17.6 % | 0 / 0 | 5 (2) | 3.8 | region size differs | [ref](states/reference/home-alert-dialog-focus-key-light.png) · [gotx](states/gotx/home-alert-dialog-focus-key-light.png) · [compare](states/compare/home-alert-dialog-focus-key-light.png) |
| home / alert-dialog-focus-key | dark | 151×81 → 147×80 | 44.1 % / 16.4 % | 0 / 0 | 5 (2) | 3.8 | region size differs | [ref](states/reference/home-alert-dialog-focus-key-dark.png) · [gotx](states/gotx/home-alert-dialog-focus-key-dark.png) · [compare](states/compare/home-alert-dialog-focus-key-dark.png) |
| home / alert-dialog-click | light | 368×227 → 368×216 | 64.8 % / 44.3 % | 1 / 1 | 13 (0) | 26.9 | region size differs | [ref](states/reference/home-alert-dialog-click-light.png) · [gotx](states/gotx/home-alert-dialog-click-light.png) · [compare](states/compare/home-alert-dialog-click-light.png) |
| home / alert-dialog-click | dark | 368×227 → 368×216 | 67.9 % / 28.1 % | 1 / 1 | 13 (0) | 26.9 | region size differs | [ref](states/reference/home-alert-dialog-click-dark.png) · [gotx](states/gotx/home-alert-dialog-click-dark.png) · [compare](states/compare/home-alert-dialog-click-dark.png) |
| home / group-button-hover | light | 161×81 → 158×80 | 20.3 % / 12.6 % | 0 / 0 | 5 (0) | 3 | region size differs | [ref](states/reference/home-group-button-hover-light.png) · [gotx](states/gotx/home-group-button-hover-light.png) · [compare](states/compare/home-group-button-hover-light.png) |
| home / group-button-hover | dark | 161×81 → 158×80 | 41.2 % / 11.4 % | 0 / 0 | 5 (0) | 3 | region size differs | [ref](states/reference/home-group-button-hover-dark.png) · [gotx](states/gotx/home-group-button-hover-dark.png) · [compare](states/compare/home-group-button-hover-dark.png) |
| home / group-menu-hover | light | 80×81 → 80×80 | 16.0 % / 6.8 % | 0 / 0 | 5 (2) | 2.9 | region size differs | [ref](states/reference/home-group-menu-hover-light.png) · [gotx](states/gotx/home-group-menu-hover-light.png) · [compare](states/compare/home-group-menu-hover-light.png) |
| home / group-menu-hover | dark | 80×81 → 80×80 | 37.4 % / 5.3 % | 0 / 0 | 5 (2) | 2.9 | region size differs | [ref](states/reference/home-group-menu-hover-dark.png) · [gotx](states/gotx/home-group-menu-hover-dark.png) · [compare](states/compare/home-group-menu-hover-dark.png) |
| home / group-menu-focus-key | light | 80×81 → 80×80 | 21.9 % / 10.6 % | 0 / 0 | 5 (2) | 2.9 | region size differs | [ref](states/reference/home-group-menu-focus-key-light.png) · [gotx](states/gotx/home-group-menu-focus-key-light.png) · [compare](states/compare/home-group-menu-focus-key-light.png) |
| home / group-menu-focus-key | dark | 80×81 → 80×80 | 42.0 % / 9.8 % | 0 / 0 | 5 (2) | 2.9 | region size differs | [ref](states/reference/home-group-menu-focus-key-dark.png) · [gotx](states/gotx/home-group-menu-focus-key-dark.png) · [compare](states/compare/home-group-menu-focus-key-dark.png) |
| home / group-menu-click | light | 208×253 → 198×238 | 51.8 % / 24.6 % | 1 / 1 | 13 (1) | 14.8 | region size differs | [ref](states/reference/home-group-menu-click-light.png) · [gotx](states/gotx/home-group-menu-click-light.png) · [compare](states/compare/home-group-menu-click-light.png) |
| home / group-menu-click | dark | 208×253 → 198×238 | 55.1 % / 23.0 % | 1 / 1 | 13 (1) | 14.8 | region size differs | [ref](states/reference/home-group-menu-click-dark.png) · [gotx](states/gotx/home-group-menu-click-dark.png) · [compare](states/compare/home-group-menu-click-dark.png) |
| home / sidebar-active-hover | light | 185×81 → 185×80 | 17.5 % / 12.0 % | 0 / 0 | 8 (6) | 2 | region size differs | [ref](states/reference/home-sidebar-active-hover-light.png) · [gotx](states/gotx/home-sidebar-active-hover-light.png) · [compare](states/compare/home-sidebar-active-hover-light.png) |
| home / sidebar-active-hover | dark | 185×81 → 185×80 | 18.7 % / 12.3 % | 0 / 0 | 8 (6) | 2 | region size differs | [ref](states/reference/home-sidebar-active-hover-dark.png) · [gotx](states/gotx/home-sidebar-active-hover-dark.png) · [compare](states/compare/home-sidebar-active-hover-dark.png) |
| home / sidebar-item-hover | light | 185×81 → 185×80 | 21.4 % / 14.8 % | 0 / 0 | 9 (7) | 2 | region size differs | [ref](states/reference/home-sidebar-item-hover-light.png) · [gotx](states/gotx/home-sidebar-item-hover-light.png) · [compare](states/compare/home-sidebar-item-hover-light.png) |
| home / sidebar-item-hover | dark | 185×81 → 185×80 | 22.9 % / 15.2 % | 0 / 0 | 9 (7) | 2 | region size differs | [ref](states/reference/home-sidebar-item-hover-dark.png) · [gotx](states/gotx/home-sidebar-item-hover-dark.png) · [compare](states/compare/home-sidebar-item-hover-dark.png) |
| home / sidebar-item-focus-key | light | 185×81 → 185×80 | 25.1 % / 19.8 % | 0 / 0 | 9 (7) | 2 | region size differs | [ref](states/reference/home-sidebar-item-focus-key-light.png) · [gotx](states/gotx/home-sidebar-item-focus-key-light.png) · [compare](states/compare/home-sidebar-item-focus-key-light.png) |
| home / sidebar-item-focus-key | dark | 185×81 → 185×80 | 26.3 % / 20.3 % | 0 / 0 | 9 (7) | 2 | region size differs | [ref](states/reference/home-sidebar-item-focus-key-dark.png) · [gotx](states/gotx/home-sidebar-item-focus-key-dark.png) · [compare](states/compare/home-sidebar-item-focus-key-dark.png) |
| home / sidebar-item-press | light | 185×81 → 185×80 | 21.4 % / 14.8 % | 0 / 0 | 9 (7) | 2 | region size differs | [ref](states/reference/home-sidebar-item-press-light.png) · [gotx](states/gotx/home-sidebar-item-press-light.png) · [compare](states/compare/home-sidebar-item-press-light.png) |
| home / sidebar-item-press | dark | 185×81 → 185×80 | 22.9 % / 15.2 % | 0 / 0 | 9 (7) | 2 | region size differs | [ref](states/reference/home-sidebar-item-press-dark.png) · [gotx](states/gotx/home-sidebar-item-press-dark.png) · [compare](states/compare/home-sidebar-item-press-dark.png) |
| home / sidebar-item-click | light | 185×81 → 185×80 | 34.4 % / 14.5 % | 0 / 0 | 9 (6) | 2 | region size differs | [ref](states/reference/home-sidebar-item-click-light.png) · [gotx](states/gotx/home-sidebar-item-click-light.png) · [compare](states/compare/home-sidebar-item-click-light.png) |
| home / sidebar-item-click | dark | 185×81 → 185×80 | 35.7 % / 14.9 % | 0 / 0 | 9 (6) | 2 | region size differs | [ref](states/reference/home-sidebar-item-click-dark.png) · [gotx](states/gotx/home-sidebar-item-click-dark.png) · [compare](states/compare/home-sidebar-item-click-dark.png) |
| home / select-hover | light | 338×81 → 338×80 | 39.5 % / 9.3 % | 0 / 0 | 6 (3) | 2 | region size differs | [ref](states/reference/home-select-hover-light.png) · [gotx](states/gotx/home-select-hover-light.png) · [compare](states/compare/home-select-hover-light.png) |
| home / select-hover | dark | 338×81 → 338×80 | 14.7 % / 9.7 % | 0 / 0 | 6 (3) | 2 | region size differs | [ref](states/reference/home-select-hover-dark.png) · [gotx](states/gotx/home-select-hover-dark.png) · [compare](states/compare/home-select-hover-dark.png) |
| home / select-focus-key | light | 338×81 → 338×80 | 46.5 % / 15.7 % | 0 / 0 | 6 (3) | 2 | region size differs | [ref](states/reference/home-select-focus-key-light.png) · [gotx](states/gotx/home-select-focus-key-light.png) · [compare](states/compare/home-select-focus-key-light.png) |
| home / select-focus-key | dark | 338×81 → 338×80 | 47.1 % / 15.6 % | 0 / 0 | 6 (3) | 2 | region size differs | [ref](states/reference/home-select-focus-key-dark.png) · [gotx](states/gotx/home-select-focus-key-dark.png) · [compare](states/compare/home-select-focus-key-dark.png) |
| home / select-click | light | 339×185 → 338×204 | 57.1 % / 21.1 % | 1 / 1 | 16 (0) | 28 | region size differs | [ref](states/reference/home-select-click-light.png) · [gotx](states/gotx/home-select-click-light.png) · [compare](states/compare/home-select-click-light.png) |
| home / select-click | dark | 339×185 → 338×204 | 57.6 % / 22.1 % | 1 / 1 | 16 (0) | 28 | region size differs | [ref](states/reference/home-select-click-dark.png) · [gotx](states/gotx/home-select-click-dark.png) · [compare](states/compare/home-select-click-dark.png) |
| home / select-open-item-hover | light | 339×185 → 338×204 | 56.5 % / 21.6 % | 1 / 1 | 16 (0) | 28 | region size differs | [ref](states/reference/home-select-open-item-hover-light.png) · [gotx](states/gotx/home-select-open-item-hover-light.png) · [compare](states/compare/home-select-open-item-hover-light.png) |
| home / select-open-item-hover | dark | 339×185 → 338×204 | 67.1 % / 31.9 % | 1 / 1 | 16 (0) | 28 | region size differs | [ref](states/reference/home-select-open-item-hover-dark.png) · [gotx](states/gotx/home-select-open-item-hover-dark.png) · [compare](states/compare/home-select-open-item-hover-dark.png) |
| home / breadcrumb-home-hover | light | 86×69 → 86×68 | 11.3 % / 6.9 % | 0 / 0 | 4 (2) | 481 | region size differs | [ref](states/reference/home-breadcrumb-home-hover-light.png) · [gotx](states/gotx/home-breadcrumb-home-hover-light.png) · [compare](states/compare/home-breadcrumb-home-hover-light.png) |
| home / breadcrumb-home-hover | dark | 86×69 → 86×68 | 11.3 % / 6.2 % | 0 / 0 | 4 (2) | 481 | region size differs | [ref](states/reference/home-breadcrumb-home-hover-dark.png) · [gotx](states/gotx/home-breadcrumb-home-hover-dark.png) · [compare](states/compare/home-breadcrumb-home-hover-dark.png) |
| home / breadcrumb-home-focus-key | light | 86×69 → 86×68 | 16.4 % / 10.2 % | 0 / 0 | 4 (2) | 481 | region size differs | [ref](states/reference/home-breadcrumb-home-focus-key-light.png) · [gotx](states/gotx/home-breadcrumb-home-focus-key-light.png) · [compare](states/compare/home-breadcrumb-home-focus-key-light.png) |
| home / breadcrumb-home-focus-key | dark | 86×69 → 86×68 | 19.2 % / 9.2 % | 0 / 0 | 4 (2) | 481 | region size differs | [ref](states/reference/home-breadcrumb-home-focus-key-dark.png) · [gotx](states/gotx/home-breadcrumb-home-focus-key-dark.png) · [compare](states/compare/home-breadcrumb-home-focus-key-dark.png) |
| home / payments-item-hover | light | 338×143 → 338×143 | 26.5 % / 5.5 % | 0 / 0 | 6 (3) | 1.8 |  | [ref](states/reference/home-payments-item-hover-light.png) · [gotx](states/gotx/home-payments-item-hover-light.png) · [compare](states/compare/home-payments-item-hover-light.png) |
| home / payments-item-hover | dark | 338×143 → 338×143 | 33.9 % / 5.6 % | 0 / 0 | 6 (3) | 1.8 |  | [ref](states/reference/home-payments-item-hover-dark.png) · [gotx](states/gotx/home-payments-item-hover-dark.png) · [compare](states/compare/home-payments-item-hover-dark.png) |
| home / payments-item-focus-key | light | 338×143 → 338×143 | 30.9 % / 8.4 % | 0 / 0 | 6 (3) | 1.8 |  | [ref](states/reference/home-payments-item-focus-key-light.png) · [gotx](states/gotx/home-payments-item-focus-key-light.png) · [compare](states/compare/home-payments-item-focus-key-light.png) |
| home / payments-item-focus-key | dark | 338×143 → 338×143 | 76.4 % / 8.4 % | 0 / 0 | 6 (3) | 1.8 |  | [ref](states/reference/home-payments-item-focus-key-dark.png) · [gotx](states/gotx/home-payments-item-focus-key-dark.png) · [compare](states/compare/home-payments-item-focus-key-dark.png) |
| home / forgot-link-hover | light | 107×65 → 107×64 | 60.6 % / 7.0 % | 0 / 0 | 4 (2) | 481 | region size differs | [ref](states/reference/home-forgot-link-hover-light.png) · [gotx](states/gotx/home-forgot-link-hover-light.png) · [compare](states/compare/home-forgot-link-hover-light.png) |
| home / forgot-link-hover | dark | 107×65 → 107×64 | 58.7 % / 8.3 % | 0 / 0 | 4 (2) | 481 | region size differs | [ref](states/reference/home-forgot-link-hover-dark.png) · [gotx](states/gotx/home-forgot-link-hover-dark.png) · [compare](states/compare/home-forgot-link-hover-dark.png) |
| home / update-security-disabled-hover | light | 338×81 → 338×80 | 99.3 % / 31.8 % | 0 / 0 | 6 (4) | 1.3 | region size differs | [ref](states/reference/home-update-security-disabled-hover-light.png) · [gotx](states/gotx/home-update-security-disabled-hover-light.png) · [compare](states/compare/home-update-security-disabled-hover-light.png) |
| home / update-security-disabled-hover | dark | 338×81 → 338×80 | 98.6 % / 33.0 % | 0 / 0 | 6 (4) | 1.3 | region size differs | [ref](states/reference/home-update-security-disabled-hover-dark.png) · [gotx](states/gotx/home-update-security-disabled-hover-dark.png) · [compare](states/compare/home-update-security-disabled-hover-dark.png) |
| home / dismiss-payout-hover | light | 76×77 → 76×76 | 9.3 % / 1.5 % | 0 / 0 | 5 (1) | 33.6 | region size differs | [ref](states/reference/home-dismiss-payout-hover-light.png) · [gotx](states/gotx/home-dismiss-payout-hover-light.png) · [compare](states/compare/home-dismiss-payout-hover-light.png) |
| home / dismiss-payout-hover | dark | 76×77 → 76×76 | 17.8 % / 1.5 % | 0 / 0 | 5 (1) | 33.6 | region size differs | [ref](states/reference/home-dismiss-payout-hover-dark.png) · [gotx](states/gotx/home-dismiss-payout-hover-dark.png) · [compare](states/compare/home-dismiss-payout-hover-dark.png) |
| home / dismiss-payout-focus-key | light | 76×77 → 76×76 | 16.8 % / 7.3 % | 0 / 0 | 5 (1) | 33.6 | region size differs | [ref](states/reference/home-dismiss-payout-focus-key-light.png) · [gotx](states/gotx/home-dismiss-payout-focus-key-light.png) · [compare](states/compare/home-dismiss-payout-focus-key-light.png) |
| home / dismiss-payout-focus-key | dark | 76×77 → 76×76 | 17.7 % / 7.1 % | 0 / 0 | 5 (1) | 33.6 | region size differs | [ref](states/reference/home-dismiss-payout-focus-key-dark.png) · [gotx](states/gotx/home-dismiss-payout-focus-key-dark.png) · [compare](states/compare/home-dismiss-payout-focus-key-dark.png) |
| home / chat-reset-hover | light | 104×118 → 104×119 | 24.4 % / 6.9 % | 1 / 1 | 8 (6) | 1.2 | region size differs | [ref](states/reference/home-chat-reset-hover-light.png) · [gotx](states/gotx/home-chat-reset-hover-light.png) · [compare](states/compare/home-chat-reset-hover-light.png) |
| home / chat-reset-hover | dark | 104×118 → 104×119 | 18.5 % / 6.8 % | 1 / 1 | 8 (6) | 1.2 | region size differs | [ref](states/reference/home-chat-reset-hover-dark.png) · [gotx](states/gotx/home-chat-reset-hover-dark.png) · [compare](states/compare/home-chat-reset-hover-dark.png) |
| home / chat-reset-focus-key | light | 104×118 → 104×119 | 27.4 % / 10.4 % | 1 / 1 | 8 (6) | 1.2 | region size differs | [ref](states/reference/home-chat-reset-focus-key-light.png) · [gotx](states/gotx/home-chat-reset-focus-key-light.png) · [compare](states/compare/home-chat-reset-focus-key-light.png) |
| home / chat-reset-focus-key | dark | 104×118 → 104×119 | 21.5 % / 9.5 % | 1 / 1 | 8 (6) | 1.2 | region size differs | [ref](states/reference/home-chat-reset-focus-key-dark.png) · [gotx](states/gotx/home-chat-reset-focus-key-dark.png) · [compare](states/compare/home-chat-reset-focus-key-dark.png) |
| home / chat-attach-focus-key | light | 80×81 → 76×76 | 67.3 % / 24.5 % | 0 / 0 | 5 (0) | 44.8 | region size differs | [ref](states/reference/home-chat-attach-focus-key-light.png) · [gotx](states/gotx/home-chat-attach-focus-key-light.png) · [compare](states/compare/home-chat-attach-focus-key-light.png) |
| home / chat-attach-focus-key | dark | 80×81 → 76×76 | 74.2 % / 24.1 % | 0 / 0 | 5 (0) | 44.8 | region size differs | [ref](states/reference/home-chat-attach-focus-key-dark.png) · [gotx](states/gotx/home-chat-attach-focus-key-dark.png) · [compare](states/compare/home-chat-attach-focus-key-dark.png) |
| home / chat-attach-click | light | 224×229 → 210×210 | 52.9 % / 28.1 % | 1 / 1 | 10 (0) | 16 | region size differs | [ref](states/reference/home-chat-attach-click-light.png) · [gotx](states/gotx/home-chat-attach-click-light.png) · [compare](states/compare/home-chat-attach-click-light.png) |
| home / chat-attach-click | dark | 224×229 → 210×210 | 56.0 % / 26.5 % | 1 / 1 | 10 (0) | 16 | region size differs | [ref](states/reference/home-chat-attach-click-dark.png) · [gotx](states/gotx/home-chat-attach-click-dark.png) · [compare](states/compare/home-chat-attach-click-dark.png) |
| home / footer-link-hover | light | 97×66 → 96×68 | 51.1 % / 14.5 % | 0 / 0 | 3 (2) | 1.4 | region size differs | [ref](states/reference/home-footer-link-hover-light.png) · [gotx](states/gotx/home-footer-link-hover-light.png) · [compare](states/compare/home-footer-link-hover-light.png) |
| home / footer-link-hover | dark | 97×66 → 96×68 | 16.3 % / 14.7 % | 0 / 0 | 3 (2) | 1.4 | region size differs | [ref](states/reference/home-footer-link-hover-dark.png) · [gotx](states/gotx/home-footer-link-hover-dark.png) · [compare](states/compare/home-footer-link-hover-dark.png) |

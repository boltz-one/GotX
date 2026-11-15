# Cơ chế utility class của Tailwind CSS (v4) và cách GotX tái hiện

## 1. Tailwind thực chất là gì

Tailwind không phải là một bộ component. Nó là một **trình biên dịch** nhận vào danh sách class, rồi sinh ra đúng những luật CSS mà các class đó cần. Mọi utility đều theo cùng một mô hình:

```
[variant:]*[-]tên[-giá trị][/modifier][!]
 md:hover:   -    mt  -  2                       → @media (min-width:48rem){ .md\:hover\:-mt-2:hover{ margin-top: calc(var(--spacing) * -2) } }
             bg  - blue-500 / 50                 → background-color: color-mix(in oklab, var(--color-blue-500) 50%, transparent)
             w   - [320px]                       → width: 320px
```

Có năm cơ chế chồng lên nhau:

| # | Cơ chế | Tailwind v4 | GotX |
|---|---|---|---|
| 1 | **Theme token** | `@theme { --spacing: .25rem; --color-blue-500: oklch(...); --radius-lg: .5rem; … }` | `Theme` (static): `SpacingRem`, `Colors`, `Radius`, `ShadowLayers`, `FontSizes`, `Leading`, `FontWeights`, `Breakpoints`, `Containers` |
| 2 | **Utility** = tên + cách tra giá trị | utility tĩnh (`flex`), utility có giá trị (`p-<spacing>`), bậc tuỳ ý (`p-17`), phân số (`w-1/2`), giá trị tuỳ ý (`w-[320px]`), số âm (`-mt-2`) | `Tailwind.Parse` → mảng `UtilityOp` (lệnh gán vào `Style`) |
| 3 | **Modifier** sau dấu `/` | độ mờ màu `bg-black/50`, line-height `text-sm/6` | `SplitModifier`, `TryColor`, nhánh `text-` |
| 4 | **Variant** trước dấu `:` | trạng thái (`hover`, `focus`, `active`, `disabled`, `group-hover`, `peer-*`), media (`sm`…`2xl`, `max-*`, `dark`), container (`@md`), thuộc tính (`aria-*`, `data-*`); có thể xếp chồng | `StateVariant` (đánh giá lúc vẽ), `Dark` + `MinRem/MaxRem` (đánh giá lúc dựng cây) |
| 5 | **Thuộc tính ghép** qua biến CSS | `box-shadow` = `inset-shadow, inset-ring, ring-offset, ring, shadow`; transform, gradient, filter cũng ghép từ nhiều biến `--tw-*` | `Style` lưu tách: `Shadow` (preset), `ShadowColor`, `RingWidth`/`RingColor`, `RingOffsetWidth`/`RingOffsetColor`; `Window.Paint` ghép thành các lớp |

Hai cơ chế nền còn lại:

- **Thứ tự / độ ưu tiên.** Mọi utility có cùng độ đặc hiệu, nên class nào được *sinh sau trong stylesheet* thì thắng. Utility có variant luôn được sinh sau utility thường. Hậu tố `!` biến utility thành `!important`.
- **Kế thừa.** `color`, `font-*`, `line-height`, `text-align`, `white-space` được truyền từ cha xuống con theo CSS. Riêng `opacity` thì nhân dồn qua từng cấp.

## 2. Đường đi của một class trong GotX

```
.Class("md:hover:bg-indigo-600/90 px-4")
   │  ConcurrentDictionary: chuỗi → ClassList    (biên dịch 1 lần, giống JIT chỉ sinh class đã dùng)
   ▼
ClassList.Ops = [ {Prop.Bg, State=Hover, MinRem=48, C=indigo-600·0.9}, {Prop.Padding, mask=L|R, 1rem} ]
   │
   ├─ Lúc dựng cây (Div.Class): áp các op KHÔNG có variant trạng thái,
   │   lọc theo dark:/sm:/md: (VariantContext). Qua breakpoint → xoá cache view.
   │   Thứ tự áp: lần 1 các op thường, lần 2 các op "!important".
   │
   └─ Lúc vẽ (Window.Paint): op có hover:/active:/focus:/group-hover:/disabled:
       được áp lên BẢN SAO của Style. Arena không bị sửa, nên cache view vẫn đúng.
       Vùng chuột (HoverRegion) đổi trạng thái → chỉ view chứa nó bị đánh dấu bẩn.
```

**Quy tắc ưu tiên trong GotX:**
- Trong cùng một lần áp, class viết sau thắng. Đây là chỗ khác Tailwind: Tailwind xếp theo thứ tự trong stylesheet, không theo thứ tự trong chuỗi class. Thực tế không ai viết hai utility xung đột nhau trong cùng một chuỗi, nên khác biệt này hiếm khi gặp.
- Variant trạng thái luôn thắng class thường.
- `!important` thắng tất cả, kể cả variant trạng thái.

**Vì sao variant trạng thái chỉ đổi phần vẽ.** `hover:p-4` có được áp vào Style nhưng không làm layout chạy lại. Đổi layout theo hover sẽ làm hỏng cache và gây giật khung hình. Tailwind trên web thì vẫn đổi layout trong trường hợp này.

## 3. Bảng hỗ trợ

| Nhóm | Hỗ trợ | Chưa |
|---|---|---|
| Display | `flex inline-flex grid inline-grid block inline-block hidden` | `contents`, `table` |
| Flex | `flex-row/col`, `flex-wrap/nowrap`, `flex-1/auto/initial/none`, `grow(-0)`, `shrink(-0)`, `basis-*`, `items-*`, `justify-*`, `self-*`, `gap(-x/-y)-*` | `*-reverse`, `order-*`, `content-*` (đang luôn là start) |
| Grid | `grid-cols-N`, `grid-rows-N`, `grid-cols-[200px_1fr_auto]`, `col-span-N/full`, `row-span-N/full`, `col-start/end-N`, `row-start/end-N` (âm được: `-col-end-1`), `col-auto`, `col-[span_2]`, `col-[1_/_-1]`, `row-[2_/_4]`; thuật toán xếp chỗ như CSS (hàng+cột xác định → chỉ hàng → tự động theo con trỏ), `grid-flow-row/col/dense/row-dense/col-dense` (`grid-flow-col` sinh cột ngầm kiểu auto, tối đa 64 cột) | `auto-cols/rows`, `minmax()`, `repeat()` trong giá trị tuỳ ý, cột ngầm khi xếp theo hàng, `subgrid` |
| Spacing | `p/px/py/pt/pr/pb/pl-*`, `m*-*` (âm được), bậc tuỳ ý `p-17`, `p-2.5`, `p-px`, `p-[13px]` | `m*-auto`, `ps/pe`, `space-x/y` (đang xấp xỉ bằng gap) |
| Sizing | `w/h/size/min-*/max-*`: spacing, phân số, `full`, `auto`, `screen`, container `xs…7xl`, `[…]`; `calc()` tối thiểu: tổng/hiệu của `%`, `px`, `rem` (quy theo 16px), `vw`/`vh` (`max-w-[calc(100%-2rem)]`, `h-[calc(100vh-4rem)]`, `w-[80vw]`); `aspect-square/video/auto`, `aspect-[4/3]`, `aspect-3/2` (biết bề rộng → suy chiều cao, biết chiều cao → suy bề rộng, có min/max; trong grid không bị kéo giãn) | `fit/min/max-content` (hiện coi như auto), `dvh/svh`, `calc()` lồng / có `var()`, aspect trong hàng flex không đặt bề rộng (nhận cả hàng) |
| Position | `relative absolute static`, `inset(-x/-y)-*`, `top/right/bottom/left-*` (âm được) | `fixed`, `sticky`, `z-*` (đang vẽ theo thứ tự: absolute nằm trên) |
| Màu | `bg-* text-* border-* ring-* ring-offset-* shadow-*`; bảng 22 họ × 11 sắc độ; token ngữ nghĩa có giá trị sáng/tối qua `Theme.SetColor` (`bg-primary` tự đổi theo dark mode); `/50`, `/[0.35]`; `[#hex]`, `[rgb(...)]` | `current`, `oklch()` |
| Gradient | `bg-linear-to-{t,tr,r,br,b,bl,l,tl}`, `bg-linear-<góc>`, `-bg-linear-<góc>`, `bg-linear-[25deg]` / `[0.25turn]`, `bg-gradient-to-*` (tên v3, v4 vẫn giữ), `bg-radial`, `bg-none`; điểm dừng `from-* via-* to-*` (màu, `/opacity`, `transparent`, token ngữ nghĩa sáng/tối), vị trí `from-10%`, `via-[30%]`, `to-[20px]`, `via-none`; modifier `/srgb`, `/oklab`. Nội suy premultiplied trong Oklab (mặc định của v4) hoặc sRGB (`bg-linear-[…deg]`, `/srgb`) đúng như CSS Tailwind sinh ra; vẽ trên `bg-*` trong cùng một Quad, theo bo góc, viền, biến đổi, vùng cắt | `bg-conic`, `bg-radial-[…]` (vị trí/hình dạng), modifier `oklch/hsl/longer…`, `mask-*`, `bg-[url()]` |
| Viền, bo góc | `border(-n)`, `border-[3px]`, từng cạnh `border-t/r/b/l/x/y/s/e(-n)`, màu từng cạnh `border-l-indigo-500`, mặc định currentColor như v4, `divide-x/y(-n)`, `divide-{màu}`, `border-solid/dashed/dotted/none/hidden`, `outline(-n)`, `outline-{màu}`, `outline-offset-*` (âm được), `outline-solid/dashed/dotted/none/hidden`, `rounded(-t/r/b/l/tl/tr/br/bl)-*`, `rounded-[10px]`, từng góc `rounded-[4px_8px]` / `rounded-[1px_2px_3px_4px]` (cú pháp border-radius) | `border-double`, `divide-*-reverse`, `divide-dashed` |
| Hiệu ứng | `shadow-{2xs…2xl,none}`, `shadow-[0_4px_10px_rgba()]`, màu bóng, `ring-*`, `ring-offset-*`, `opacity-*` | `inset-shadow-*`, `inset-ring-*`, `blur-*`, `backdrop-*`, `mix-blend-*` |
| Chữ | `text-{xs…9xl}`, `text-sm/6`, `text-[15px]`, `font-{thin…black}`, `font-[550]`, `leading-*`, `text-left/center/right`, `truncate`, `whitespace-nowrap/normal`, `text-ellipsis/clip`, `font-sans/mono`, `italic/not-italic`, `underline`, `line-through`, `no-underline`, `uppercase/lowercase/capitalize/normal-case`, `tracking-{tighter…widest}`, `tracking-[0.2em]` / `tracking-[2px]` (âm được); `line-clamp-{n}`, `line-clamp-[n]`, `line-clamp-none` (cao đúng n dòng, dòng cuối cắt + "…"); `text-wrap`, `text-nowrap`, `text-balance` (bề rộng nhỏ nhất giữ số dòng, 2–6 dòng như Chromium), `text-pretty` (tránh dòng cuối một từ); `tabular-nums` / `proportional-nums` / `normal-nums` (GSUB `tnum` của font, script latn); `underline-offset-*` (px từ đường gốc, âm được, `-auto`), `decoration-{màu}`, `decoration-{n}` / `decoration-[3px]`, `decoration-auto` | `lnum/onum`, ligature, `decoration-wavy/double/dotted`, `text-shadow-*`, `text-pretty` đúng thuật toán Chromium (đang xấp xỉ) |
| Tương tác, cuộn, cắt | `cursor-pointer/text/default`, `overflow-hidden/clip/visible` (cắt đúng theo bo góc và theo hình bị xoay/skew, có anti-alias), `overflow(-x/-y)-auto/scroll` (lăn chuột, cuộn mượt, thanh cuộn nổi, cuộn lồng nhau) | kéo thanh cuộn, `scroll-smooth/snap-*`, `pointer-events-*`, `select-*` |
| Biến đổi | `translate-x/y-*` (âm, phân số, tuỳ ý, `-translate-x-full`), `translate-*` hai trục (`translate-1/2`), `scale(-x/-y)-*`, `scale-[1.02]`, `rotate-*`, `rotate-[17deg]` / `[0.25turn]` / `[1rad]`, `skew(-x/-y)-*`, `skew-[10deg]`, `origin-{center,top,top-left,…}`, `origin-[25%_75%]`; thứ tự translate → rotate → scale → skew quanh transform-origin như CSS | 3D (`rotate-x/y`, `perspective`, `translate-z`) |
| Chuyển động | `transition(-all/-colors/-opacity/-shadow/-transform/-none)` (cả xoay), `duration-*`, `delay-*`, `ease-linear/in/out/in-out`, `animate-spin/pulse/ping/bounce/none` | keyframes tuỳ ý, `motion-reduce:` |
| Variant | `hover focus focus-visible active disabled group-hover dark sm md lg xl 2xl max-*`, breakpoint tuỳ ý `min-[1900px]:` / `max-[640px]:` / `min-[40rem]:` (px quy theo 16px như media query; tự tính vào bucket xoá cache view), xếp chồng được (`md:hover:`) | `peer-*`, `group-focus`, `group-data-*`, `has-*`, `first/last/odd/even`, `@container` (`@sm:`), `aria-*`, `data-*`, `[&>*]` |

Class chưa hỗ trợ không bị bỏ qua trong im lặng: chúng được ghi vào `ClassList.Unknown` và gọi `Tailwind.OnUnknownClass`. Hãy nối callback này vào log khi debug.

## 4. Cuộn, transform và chuyển động hoạt động thế nào

- **Phần tử có state qua các frame** (vị trí cuộn, transition) được nhận diện bằng khoá gồm: view chứa nó, và `.Key("...")` hoặc thứ tự của nó trong view. Nên đặt `.Key()` khi cấu trúc view thay đổi động.
- **Cuộn và transform được áp lúc vẽ** dưới dạng phép biến đổi (phóng đều + tịnh tiến), không phải lúc layout. Nhờ vậy cuộn không làm layout chạy lại. View con chỉ bị dời chỗ thì được *phát lại kèm tịnh tiến* theo số pixel thiết bị nguyên. Vùng cắt bên trong view cũng dời theo rồi được xén lại bằng vùng cắt ngoài. Chỉ khi vùng cắt bên trong bị vùng ngoài xén mất, view mới phải render lại.
- **Transition** nội suy giữa giá trị đang hiển thị và giá trị đích mới. Màu được trộn trong không gian premultiplied. Bóng đổi bậc thì được cross-fade. Easing dùng cubic-bezier giống CSS.
- **Animation dựng sẵn** chỉ phụ thuộc đồng hồ, không cần lưu state.
- Khi còn chuyển động, view chứa nó được đánh dấu bẩn cho frame sau và platform hẹn frame mỗi ~16ms (WM_TIMER).
- **Xoay:** phép biến đổi lúc vẽ là phép đồng dạng (phóng đều + xoay + tịnh tiến), ghép lồng nhau vẫn đúng. Mỗi primitive mang hình chữ nhật trước khi xoay cùng góc xoay quanh tâm. Renderer (CPU và shader) đưa từng pixel về hệ toạ độ chưa xoay rồi tính SDF như cũ, nên bo góc, viền và bóng xoay theo mà vẫn mịn. Glyph bị xoay được lấy mẫu song tuyến.
- **Hit-test khi xoay:** hitbox lưu hộp layout cùng phép biến đổi, nên con trỏ được biến ngược về toạ độ phần tử. Góc ngoài hình thoi của một ô `rotate-45` không bấm được.
- **Biến đổi affine đầy đủ:** phép biến đổi lúc vẽ là ma trận 2×3, đủ cho skew, scale lệch trục và transform-origin. Mỗi primitive mang ma trận 2×2 đã chuẩn hoá (định thức ≈ 1). Cỡ chữ, độ dày viền và blur dùng hệ số phóng trung bình √|det|.
- **Vùng cắt theo hình:** `overflow-hidden` trên phần tử bo góc hoặc bị biến đổi tạo một *clip shape* (hình chữ nhật bo góc + ma trận) trong Scene. Primitive con tham chiếu shape đó, renderer nhân thêm độ phủ SDF của shape nên mép cắt mịn.
  - Hiện chỉ shape trong cùng có hiệu lực; các vùng cắt ngoài vẫn được giao theo hộp thẳng trục.
  - Khi một view được phát lại, các shape tạo bên trong nó được chép và dời theo; shape bao ngoài được ánh xạ sang frame mới.
- **Viền gạch / chấm và outline:** viền được tham số hoá theo chiều dài từng cạnh, số gạch/chấm được làm tròn cho chia đều và luôn có gạch ở góc. Outline là một quad chỉ có viền, nằm ngoài border box cách `outline-offset`, bo theo góc, vẽ sau nội dung.
- **Viền từng cạnh:** độ rộng và màu riêng cho từng cạnh. Hộp bên trong lệch tâm theo độ dày từng cạnh. Màu của một pixel trên viền thuộc về cạnh có khoảng cách chuẩn hoá nhỏ nhất, nên chỗ nối hai cạnh là đường chéo giống CSS.
- **Gradient:** `Style.Gradient` lưu hướng + điểm dừng tách nhau (như các biến `--tw-gradient-*`). Lúc vẽ, `Window.AddGradientFill` đưa một `GradientFill` vào bảng `Scene.Gradients`; Quad trỏ tới bằng `Primitive.Gradient` (1-based), giống clip shape. Hình học tính theo Bounds cục bộ của primitive nên phát lại kèm tịnh tiến không phải sửa, chỉ dời chỉ số bảng. Công thức chung ở `GradientMath` / `GradientSampler` (CPU), shader chép y hệt (`D3D11Renderer.Gradient.cs`, buffer t4, instance dùng `Extra.z`). Đường gradient dài `|W·sinθ| + |H·cosθ|` như CSS; `to-tr…` lấy hướng vuông góc với đường chéo của hộp; radial là ellipse farthest-corner tại tâm. Gradient phủ cả border box (CSS đặt ảnh nền theo padding box rồi lặp ra dưới viền – khác biệt chỉ thấy khi viền dày).
- **line-clamp / text-balance / tabular-nums** là thuộc tính chữ kế thừa qua `TextStyle` (CSS không kế thừa `line-clamp`; ở GotX con chữ lồng trong khối con cũng bị giới hạn riêng từng khối). `line-clamp` bật kèm ellipsis để luật lint `text-overflow` không coi phần bị cắt là tràn.
- **Hover theo hộp chưa biến đổi:** `hover:-translate-y-1` không làm thẻ bị nhấp nháy khi chuột ở sát mép dưới.

## 5. Lộ trình tiếp

1. **`mask-*` và `bg-conic`**: cùng công thức gradient, thêm bước nhân alpha / tham số góc.
2. **Biến đổi 3D** (`rotate-x/y`, `perspective`): cần chiếu phối cảnh trong shader.
3. **`peer-*` và `group-*` đầy đủ, variant cấu trúc.** Cần thông tin anh em/vị trí trong lúc vẽ.
4. **Giao nhiều vùng cắt theo hình lồng nhau; hit-test theo vùng cắt theo hình** (hiện theo hộp thẳng trục).
5. **Kéo thanh cuộn, bàn phím trong vùng cuộn (PageUp/Down), tự cuộn tới con trỏ trong ô nhập.**
6. **Kiểm tra lúc build.** Viết Roslyn analyzer quét chuỗi `.Class("…")` và báo class sai ngay trong IDE, tương đương IntelliSense của Tailwind.

## Lớp mở rộng của GotX (không có trong Tailwind)

| Lớp | Tác dụng |
|---|---|
| `scrollbar-always` / `scrollbar-hover` / `scrollbar-scroll` / `scrollbar-none` | Khi nào hiện thanh cuộn của phần tử `overflow-*-auto`: luôn luôn (mặc định); khi con trỏ ở trên vùng cuộn, ẩn sau 600ms khi rời; khi vị trí cuộn đổi, ẩn sau 600ms đứng yên; không bao giờ |
| `scrollbar-w-<n>` | Bề rộng rãnh (thang spacing); con trượt = rãnh trừ 3px (viền trong 1px + đệm 1px), sát mép |
| `scrollbar-thumb-<màu>` | Màu con trượt (có biến thể tối cho token ngữ nghĩa) |

// Injected into the page (Runtime.evaluate) by capture-html.mjs as `(<this file>)(opts)`.
// Returns the layout map of EVERY visible element: tag, id, data-slot, text (<= 60 chars), rect and key computed styles.
// Same JSON shape as tools/ShadcnRef/layout-map.js (so samples/Shadcn's LayoutMapFile reads it) with extra fields.
// opts: { selector?: string } - when set, only that element's subtree and rects are relative to its top-left corner.
(opts) => {
  const root = opts && opts.selector ? document.querySelector(opts.selector) : document.body;
  if (!root) throw new Error(`selector not found: ${opts.selector}`);
  // Output coordinates: document CSS px (scroll added); with a selector, relative to that element's top-left corner.
  const rootRect = root.getBoundingClientRect();
  const offX = opts && opts.selector ? rootRect.left : -window.scrollX, offY = opts && opts.selector ? rootRect.top : -window.scrollY;
  const round = (v) => Math.round(v * 100) / 100;
  const rect = (r) => ({ x: round(r.left - offX), y: round(r.top - offY), w: round(r.width), h: round(r.height) });
  const clip = (s) => (s.length > 60 ? s.slice(0, 60) : s);
  const visible = (e, s) => {
    if (s.display === 'none' || s.visibility === 'hidden' || s.visibility === 'collapse') return false;
    for (let p = e; p; p = p.parentElement) if (Number(getComputedStyle(p).opacity) === 0) return false;
    return true;
  };
  const ownText = (e) => {
    let t = '';
    for (const n of e.childNodes) if (n.nodeType === 3) t += n.nodeValue;
    return t.replace(/\s+/g, ' ').trim();
  };
  const textRect = (e) => {
    const range = document.createRange();
    let box = null;
    for (const n of e.childNodes) {
      if (n.nodeType !== 3 || !n.nodeValue.trim()) continue;
      range.selectNodeContents(n);
      const r = range.getBoundingClientRect();
      if (r.width === 0 && r.height === 0) continue;
      box = box
        ? { left: Math.min(box.left, r.left), top: Math.min(box.top, r.top), right: Math.max(box.right, r.right), bottom: Math.max(box.bottom, r.bottom) }
        : { left: r.left, top: r.top, right: r.right, bottom: r.bottom };
    }
    return box && rect({ left: box.left, top: box.top, width: box.right - box.left, height: box.bottom - box.top });
  };
  // lab()/oklch() computed colours resolve to sRGB through a 1x1 canvas so they compare with GotX's rgb().
  const ctx = Object.assign(document.createElement('canvas'), { width: 1, height: 1 }).getContext('2d', { willReadFrequently: true });
  const cache = new Map();
  const rgb = (c) => {
    if (cache.has(c)) return cache.get(c);
    ctx.clearRect(0, 0, 1, 1);
    ctx.fillStyle = '#000'; ctx.fillStyle = c;
    ctx.fillRect(0, 0, 1, 1);
    const [r, g, b, a] = ctx.getImageData(0, 0, 1, 1).data;
    const v = a === 255 ? `rgb(${r}, ${g}, ${b})` : `rgba(${r}, ${g}, ${b}, ${Math.round((a / 255) * 1000) / 1000})`;
    cache.set(c, v);
    return v;
  };
  const out = [];
  const all = [root, ...root.querySelectorAll('*')];
  for (const e of all) {
    const tag = e.tagName.toLowerCase();
    if (['script', 'style', 'noscript', 'template', 'meta', 'link', 'head', 'title'].includes(tag)) continue;
    if (e.closest('svg') && tag !== 'svg') continue;
    const r = e.getBoundingClientRect();
    if (r.width <= 0 || r.height <= 0) continue;
    const s = getComputedStyle(e);
    if (!visible(e, s)) continue;
    const own = ownText(e);
    const isInput = (tag === 'input' || tag === 'textarea') && (e.value || e.placeholder);
    const inner = own || isInput ? '' : (e.innerText ?? '').replace(/\s+/g, ' ').trim();
    const item = {
      tag,
      id: e.id || undefined,
      slot: e.getAttribute('data-slot') || undefined,
      text: clip(own || (isInput ? (e.value || e.placeholder) : inner)) || undefined,
      own: own ? true : undefined,
      rect: rect(r),
      textRect: own ? textRect(e) ?? undefined : undefined,
      style: {
        fontFamily: s.fontFamily, fontSize: s.fontSize, fontWeight: s.fontWeight, lineHeight: s.lineHeight, letterSpacing: s.letterSpacing,
        color: rgb(s.color), backgroundColor: rgb(s.backgroundColor),
        borderWidth: `${s.borderTopWidth} ${s.borderRightWidth} ${s.borderBottomWidth} ${s.borderLeftWidth}`,
        borderColor: [s.borderTopColor, s.borderRightColor, s.borderBottomColor, s.borderLeftColor].map(rgb).join(' '),
        borderRadius: s.borderRadius, padding: s.padding, gap: s.gap, boxShadow: s.boxShadow,
        display: s.display, position: s.position, overflow: `${s.overflowX} ${s.overflowY}`, opacity: s.opacity,
        flexDirection: s.display.includes('flex') ? s.flexDirection : undefined, flexWrap: s.display.includes('flex') ? s.flexWrap : undefined,
        flexGrow: s.flexGrow, flexShrink: s.flexShrink, flexBasis: s.flexBasis,
        alignItems: s.alignItems, justifyContent: s.justifyContent, textAlign: s.textAlign,
      },
    };
    if (e.className && typeof e.className === 'string') item.cls = e.className.length > 300 ? e.className.slice(0, 300) : e.className;
    out.push(item);
  }
  return out;
}

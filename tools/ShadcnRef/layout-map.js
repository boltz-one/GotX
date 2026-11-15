// Injected into the page (Runtime.evaluate): returns the layout map of every visible data-slot element, text-bearing
// element and svg. Coordinates are document CSS px (scroll offset added), so full-page captures line up with the PNG.
(() => {
  const sx = window.scrollX, sy = window.scrollY;
  const round = (v) => Math.round(v * 100) / 100;
  const rect = (r) => ({ x: round(r.left + sx), y: round(r.top + sy), w: round(r.width), h: round(r.height) });
  const clip = (s) => (s.length > 60 ? s.slice(0, 60) : s);
  const visible = (e, s) => {
    if (s.display === 'none' || s.visibility === 'hidden' || s.visibility === 'collapse' || Number(s.opacity) === 0) return false;
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
  // Computed colors come as lab()/oklab()/oklch(): resolve to sRGB through a 1×1 canvas so they compare with GotX's rgb().
  const ctx = Object.assign(document.createElement('canvas'), { width: 1, height: 1 }).getContext('2d', { willReadFrequently: true });
  const rgbCache = new Map();
  const rgb = (c) => {
    if (rgbCache.has(c)) return rgbCache.get(c);
    ctx.clearRect(0, 0, 1, 1);
    ctx.fillStyle = '#000'; ctx.fillStyle = c;
    ctx.fillRect(0, 0, 1, 1);
    const [r, g, b, a] = ctx.getImageData(0, 0, 1, 1).data;
    const v = a === 255 ? `rgb(${r}, ${g}, ${b})` : `rgba(${r}, ${g}, ${b}, ${Math.round((a / 255) * 1000) / 1000})`;
    rgbCache.set(c, v);
    return v;
  };
  const out = [];
  for (const e of document.body.querySelectorAll('*')) {
    const tag = e.tagName.toLowerCase();
    if (tag === 'script' || tag === 'style' || tag === 'noscript' || tag === 'template') continue;
    if (e.closest('svg') && tag !== 'svg') continue;
    const slot = e.getAttribute('data-slot');
    const own = ownText(e);
    const isInput = (tag === 'input' || tag === 'textarea') && (e.value || e.placeholder);
    if (!slot && !own && tag !== 'svg' && !isInput) continue;
    const r = e.getBoundingClientRect();
    if (r.width <= 0 || r.height <= 0) continue;
    const s = getComputedStyle(e);
    if (!visible(e, s)) continue;
    const item = {
      tag, slot: slot ?? undefined,
      text: clip(own || (isInput ? (e.value || e.placeholder) : (slot ? (e.innerText ?? '').replace(/\s+/g, ' ').trim() : ''))) || undefined,
      own: own ? true : undefined,
      rect: rect(r),
      textRect: own ? textRect(e) ?? undefined : undefined,
      style: {
        fontFamily: s.fontFamily, fontSize: s.fontSize, fontWeight: s.fontWeight, lineHeight: s.lineHeight,
        letterSpacing: s.letterSpacing, color: rgb(s.color), backgroundColor: rgb(s.backgroundColor),
        borderWidth: `${s.borderTopWidth} ${s.borderRightWidth} ${s.borderBottomWidth} ${s.borderLeftWidth}`,
        borderColor: [s.borderTopColor, s.borderRightColor, s.borderBottomColor, s.borderLeftColor].map(rgb).join(' '),
        borderRadius: s.borderRadius, padding: s.padding, gap: s.gap, boxShadow: s.boxShadow,
      },
    };
    if (e.className && typeof e.className === 'string') item.cls = e.className.length > 300 ? e.className.slice(0, 300) : e.className;
    out.push(item);
  }
  return out;
})()

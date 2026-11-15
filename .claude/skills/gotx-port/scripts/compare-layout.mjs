// Compares the browser layout map (capture-html.mjs) with a GotX layout map and prints the worst position/size deltas.
// Usage: node compare-layout.mjs <ref.json> <gotx.json> [--top 15] [--tol 2] [--position-only]
// GotX side, either format:
//   * layout map  { elements: [...] }  written by samples/Shadcn's LayoutMapFile.Write (ShotRenderer/LayoutExport), or
//   * tree dump   { viewport, nodes: [...] } = TreeDump.ToJson(t.Tree)  (GotX.Test; File.WriteAllText("gotx.json", TreeDump.ToJson(t.Tree)))
// Matching: text runs (own text) pair by normalised text in document order; boxes pair with the same subtree text, nearest rect wins;
// elements whose id equals a GotX .Key (or `<tag>-<id>`, or the key ends with the id) pair directly.
// Deltas in CSS px:
//   text = position only: dx = glyph start x (the GotX text node carries its CONTAINER box, so the glyph start is computed from the container
//          with the same text-align the browser used: left / center / right, taken from the GotX item's `textAlign` when present,
//          else derived from where the browser's text run sits inside its element) and dy = first-line vertical centre. Width is never compared for text.
//   box  = max(|dx|, |dy|, |dw|, |dh|)   (--position-only: max(|dx|, |dy|))
import { readFileSync } from 'node:fs';

const args = process.argv.slice(2);
const valued = new Set(['--top', '--tol']);
const opt = (n, d) => { const i = args.indexOf(n); return i >= 0 ? args[i + 1] : d; };
const files = args.filter((a, i) => !a.startsWith('--') && !(i > 0 && valued.has(args[i - 1])));
if (files.length < 2) { console.error('usage: node compare-layout.mjs <ref.json> <gotx.json> [--top 15] [--tol 2] [--position-only]'); process.exit(2); }
const top = Number(opt('--top', '15'));
const tol = Number(opt('--tol', '2'));
const positionOnly = args.includes('--position-only');

const norm = (s) => (s ?? '').toLowerCase().replace(/\s+/g, ' ').trim().slice(0, 60);
const R = (r) => (r ? { x: r.x, y: r.y, w: r.w, h: r.h } : null);
const px = (v) => (typeof v === 'string' && v.endsWith('px') ? parseFloat(v) : NaN);

function load(path) {
  const j = JSON.parse(readFileSync(path, 'utf8'));
  return j.nodes ? fromTree(j) : fromMap(j);
}

function fromMap(j) {
  const items = j.elements.map((e) => ({
    own: !!e.own, text: norm(e.text), key: e.key ?? e.id, rect: R(e.rect), textRect: R(e.textRect) ?? R(e.rect),
    box: e.box ?? (!e.own && !!e.text), tag: e.tag, align: e.textAlign ?? e.style?.textAlign,
    fontSize: px(e.style?.fontSize), lineHeight: px(e.style?.lineHeight),
  }));
  return { width: j.width, height: j.height, items };
}

function fromTree(j) {
  const nodes = j.nodes;
  const kids = nodes.map(() => []);
  for (const n of nodes) if (n.parent >= 0) kids[n.parent].push(n.index);
  const sub = new Array(nodes.length);
  const text = (i) => {
    if (sub[i] !== undefined) return sub[i];
    const n = nodes[i];
    let s = n.kind === 'text' ? n.text ?? '' : kids[i].map(text).filter(Boolean).join(' ');
    if (s.length > 400) s = s.slice(0, 400);
    return (sub[i] = s);
  };
  const rect = (a) => ({ x: a[0], y: a[1], w: a[2], h: a[3] });
  const items = [];
  for (const n of nodes) {
    if (!n.screen || n.screen[2] <= 0 || n.screen[3] <= 0) continue;
    if (n.kind === 'text') {
      if (n.text?.trim()) items.push({ own: true, text: norm(n.text), key: n.key, rect: rect(n.screen), textRect: rect(n.screen), box: false, tag: 'text', align: n.textAlign, fontSize: n.fontSize });
      continue;
    }
    const t = norm(text(n.index));
    if (t || n.key) items.push({ own: false, text: t, key: n.key, rect: rect(n.screen), textRect: rect(n.screen), box: true, tag: n.kind });
  }
  return { width: j.viewport?.[0], height: j.viewport?.[1], items };
}

const ref = load(files[0]);
const got = load(files[1]);
const pairs = [];

// ---- helpers for the text delta ----
/** Alignment the browser used: from computed text-align when present, else from where the text run sits inside its element. */
function refAlign(r) {
  const a = (r.align ?? '').toLowerCase();
  if (a === 'center') return 'center';
  if (a === 'right' || a === 'end') return 'right';
  if (a === 'left' || a === 'start') return 'left';
  const left = r.textRect.x - r.rect.x, right = r.rect.x + r.rect.w - (r.textRect.x + r.textRect.w);
  if (r.rect.w - r.textRect.w < 1) return 'left';
  if (Math.abs(left - right) < 1.5 && left > 1.5) return 'center';
  return right < left - 1.5 ? 'right' : 'left';
}
function lines(r) {
  const lh = Number.isFinite(r.lineHeight) ? r.lineHeight : (Number.isFinite(r.fontSize) ? r.fontSize * 1.25 : r.textRect.h);
  return Math.max(1, Math.round(r.textRect.h / lh));
}
/** Compares glyph start x and first-line vertical centre; the GotX box is a container, glyph start follows the alignment. */
function textDelta(r, g) {
  const align = (g.align ? String(g.align).toLowerCase() : null) ?? refAlign(r);
  const wr = r.textRect.w; // same string, same font size: the glyph advance is the browser's text width
  const gx = align === 'right' || align === 'end' ? g.rect.x + g.rect.w - wr : align === 'center' ? g.rect.x + (g.rect.w - wr) / 2 : g.rect.x;
  const n = lines(r);
  const refLine = r.textRect.h / n, gotLine = g.rect.h / n;
  const dx = gx - r.textRect.x;
  const dy = (g.rect.y + gotLine / 2) - (r.textRect.y + refLine / 2);
  return { d: Math.max(Math.abs(dx), Math.abs(dy)), dx, dy, align };
}
function boxDelta(a, b) {
  const dx = b.x - a.x, dy = b.y - a.y, dw = b.w - a.w, dh = b.h - a.h;
  return { d: positionOnly ? Math.max(Math.abs(dx), Math.abs(dy)) : Math.max(Math.abs(dx), Math.abs(dy), Math.abs(dw), Math.abs(dh)), dx, dy, dw, dh };
}

// 1) key / id pairs: key == id, key == `<tag>-<id>`, or the key ends with the id (unique candidate only)
const used = new Set();
const refUsed = new Set();
const gotKeyed = got.items.filter((i) => i.key);
for (const r of ref.items) {
  if (!r.key) continue;
  const same = gotKeyed.filter((g) => !used.has(g));
  let g = same.find((x) => x.key === r.key) ?? same.find((x) => x.key === `${r.tag}-${r.key}`);
  if (!g) { const suffix = same.filter((x) => x.key.endsWith(r.key)); if (suffix.length === 1) g = suffix[0]; }
  if (g) { used.add(g); refUsed.add(r); pairs.push({ kind: 'key', r, g, ...boxDelta(r.rect, g.rect) }); }
}

// 2) own-text runs: same normalised text, document order
const gotText = new Map();
for (const g of got.items) if (g.own && !used.has(g)) (gotText.get(g.text) ?? gotText.set(g.text, []).get(g.text)).push(g);
for (const r of ref.items) {
  if (!r.own || refUsed.has(r)) continue;
  const g = gotText.get(r.text)?.shift();
  if (g) { used.add(g); refUsed.add(r); pairs.push({ kind: 'text', r, g, ...textDelta(r, g) }); }
}

// 3) boxes: same subtree text, nearest rect
for (const r of ref.items) {
  if (r.own || !r.box || !r.text || refUsed.has(r)) continue;
  let best = null;
  for (const g of got.items) {
    if (g.own || !g.box || used.has(g) || g.text !== r.text) continue;
    const m = boxDelta(r.rect, g.rect);
    if (!best || m.d < best.m.d) best = { g, m };
  }
  if (best) { used.add(best.g); refUsed.add(r); pairs.push({ kind: 'box', r, g: best.g, ...best.m }); }
}

const q = (arr, p) => (arr.length ? arr[Math.min(arr.length - 1, Math.floor(p * arr.length))] : 0);
const f = (v) => (Math.round(v * 10) / 10).toString();

const ds = pairs.map((p) => p.d).sort((a, b) => a - b);
const refText = ref.items.filter((i) => i.own).length, refBox = ref.items.filter((i) => !i.own && i.box && i.text).length;
console.log(`ref ${ref.width}x${ref.height} (${ref.items.length} items)  gotx ${got.width}x${got.height} (${got.items.length} items)${positionOnly ? '  [position only]' : ''}`);
console.log(`matched ${pairs.length}: ${pairs.filter((p) => p.kind === 'text').length}/${refText} texts, ${pairs.filter((p) => p.kind === 'box').length}/${refBox} boxes, ${pairs.filter((p) => p.kind === 'key').length} by key`);
console.log(`delta px: median ${f(q(ds, 0.5))}  p90 ${f(q(ds, 0.9))}  max ${f(ds[ds.length - 1] ?? 0)}  within ${tol}px: ${ds.filter((d) => d <= tol).length}/${ds.length}`);
console.log(`worst ${Math.min(top, pairs.length)} (kind  delta  text  ref -> gotx; text: glyph x / line centre y, box: [x,y wxh]):`);
const box = (r) => `[${f(r.x)},${f(r.y)} ${f(r.w)}x${f(r.h)}]`;
for (const p of pairs.sort((a, b) => b.d - a.d).slice(0, top)) {
  const label = (p.r.text || p.r.key || p.r.tag).slice(0, 36);
  if (p.kind === 'text') console.log(`  text ${f(p.d).padStart(7)}  "${label}"  ref x=${f(p.r.textRect.x)} y=${f(p.r.textRect.y)} -> d=(${f(p.dx)},${f(p.dy)}) align=${p.align}`);
  else console.log(`  ${p.kind.padEnd(4)} ${f(p.d).padStart(7)}  "${label}"  ${box(p.r.rect)} -> ${box(p.g.rect)}  d=(${f(p.dx)},${f(p.dy)}${positionOnly ? '' : `,${f(p.dw)},${f(p.dh)}`})`);
}
const missing = ref.items.filter((i) => (i.own || (i.box && i.text)) && !refUsed.has(i));
if (missing.length) console.log(`unmatched in gotx: ${missing.length} (first: ${missing.slice(0, 6).map((i) => `"${(i.text || i.key).slice(0, 24)}"`).join(', ')})`);

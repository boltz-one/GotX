// Captures interaction states (hover, keyboard focus, press, click/open, type) of ui.shadcn.com with REAL input events
// (CDP Input.dispatchMouseEvent / dispatchKeyEvent / insertText), driven by scenarios.json.
// Usage: node tools/ShadcnRef/states.mjs [--only home] [--state get-started-hover,select-click] [--theme light]
// Output: samples/Shadcn/screenshots/states/reference/<page>-<state>-<theme>.png + .json (layout map of the region).
// The page is reloaded (from cache) for every state; one page at a time.
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { launch, connectPage, sleep } from './cdp.mjs';
import { openPage, settle, layoutMapJs } from './prep.mjs';
import { expandStates, OVERLAY_SLOTS, MODAL_SLOTS, INFLATE } from './scenarios.mjs';

const here = dirname(fileURLToPath(import.meta.url));
const repo = resolve(here, '..', '..');
const args = process.argv.slice(2);
const opt = (name) => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : undefined; };
const only = opt('--only')?.split(',');
const onlyStates = opt('--state')?.split(',');
const onlyTheme = opt('--theme');
const outDir = join(repo, 'samples', 'Shadcn', 'screenshots', 'states', 'reference');
const scenarios = JSON.parse(readFileSync(join(here, 'scenarios.json'), 'utf8'));
mkdirSync(outDir, { recursive: true });

// In-page helpers: target lookup (selector + text / prefix / index) and overlay rectangles.
const HELPERS = `(() => {
  const norm = (s) => (s ?? '').replace(/\\s+/g, ' ').trim().toLowerCase();
  const label = (e) => norm(e.innerText) || norm(e.getAttribute('aria-label')) || norm(e.placeholder);
  window.__find = (spec) => {
    let els = [...document.querySelectorAll(spec.selector)].filter((e) => { const r = e.getBoundingClientRect(); return r.width > 0 && r.height > 0; });
    if (spec.text) els = els.filter((e) => label(e) === norm(spec.text));
    if (spec.prefix) els = els.filter((e) => label(e).startsWith(norm(spec.prefix)));
    return els[spec.index ?? 0] ?? null;
  };
  window.__rect = (e) => { const r = e.getBoundingClientRect(); return { x: r.x, y: r.y, w: r.width, h: r.height }; };
  window.__overlays = (slots, modal) => [...document.querySelectorAll(slots.map((s) => '[data-slot="' + s + '"]').join(','))]
    .filter((e) => { const r = e.getBoundingClientRect(); return r.width > 0 && r.height > 0 && getComputedStyle(e).visibility !== 'hidden'; })
    .map((e) => ({ ...window.__rect(e), modal: modal.includes(e.dataset.slot), slot: e.dataset.slot }));
})()`;

const edge = await launch(9336);
const page = await connectPage(edge.port);
let count = 0;
try {
  await page.send('Page.enable');
  await page.send('Runtime.enable');
  for (const sc of scenarios.pages) {
    if (only && !only.includes(sc.page)) continue;
    for (const theme of sc.themes ?? ['light', 'dark']) {
      if (onlyTheme && theme !== onlyTheme) continue;
      for (const state of expandStates(sc)) {
        if (onlyStates && !onlyStates.includes(state.name)) continue;
        const name = `${sc.page}-${state.name}-${theme}`;
        process.stdout.write(`${name} … `);
        try {
          const info = await captureState(sc, state, theme, name);
          console.log(`${info.region.w}×${info.region.h}${info.overlays ? `, ${info.overlays} overlay(s)` : ''}`);
          count++;
        } catch (e) {
          console.log(`FAILED: ${e.message}`);
        }
        await sleep(500);
      }
    }
  }
} finally {
  page.close();
  await edge.close();
}
console.log(`${count} state captures written to ${outDir}`);

async function captureState(sc, state, theme, name) {
  await openPage(page, sc.url, { width: sc.width, height: sc.height, theme, hideHeader: sc.hideHeader });
  await settle(page, false);
  await page.evaluate(HELPERS);
  const rects = [];
  for (const step of state.steps) {
    const spec = JSON.stringify(step.ref);
    const rect = await page.evaluate(`(() => { const e = window.__find(${spec}); if (!e) return null; window.__target = e; return window.__rect(e); })()`);
    if (!rect) throw new Error(`target not found: ${spec}`);
    rects.push(rect);
    const cx = rect.x + rect.w / 2, cy = rect.y + rect.h / 2;
    const mouse = (type, extra = {}) => page.send('Input.dispatchMouseEvent', { type, x: cx, y: cy, button: 'none', ...extra });
    switch (step.do) {
      case 'hover': await mouse('mouseMoved'); break;
      case 'press':
        await mouse('mouseMoved');
        await mouse('mousePressed', { button: 'left', buttons: 1, clickCount: 1 });
        break;
      case 'click':
      case 'type':
        await mouse('mouseMoved');
        await mouse('mousePressed', { button: 'left', buttons: 1, clickCount: 1 });
        await mouse('mouseReleased', { button: 'left', buttons: 0, clickCount: 1 });
        if (step.do === 'type') { await sleep(100); await page.send('Input.insertText', { text: step.text ?? '' }); }
        break;
      case 'focus-key': await tabTo(); break;
      default: throw new Error(`unknown action ${step.do}`);
    }
    await sleep(step.do === 'hover' ? 1200 : 400); // tooltip delay / menu mount (animations are frozen)
  }
  const overlays = await page.evaluate(`window.__overlays(${JSON.stringify(OVERLAY_SLOTS)}, ${JSON.stringify(MODAL_SLOTS)})`);
  const region = regionOf(rects, overlays, sc.width, sc.height);
  const shot = await page.send('Page.captureScreenshot', { format: 'png', clip: { x: region.x, y: region.y, width: region.w, height: region.h, scale: 1 } });
  writeFileSync(join(outDir, `${name}.png`), Buffer.from(shot.data, 'base64'));
  const elements = (await page.evaluate(layoutMapJs)).filter((e) => intersects(e.rect, region)).map((e) => shift(e, region));
  writeFileSync(join(outDir, `${name}.json`), JSON.stringify({
    page: sc.page, state: state.name, theme, url: sc.url, width: region.w, height: region.h, region,
    target: rects[0], overlays, date: new Date().toISOString(), browser: edge.version.Browser, elements,
  }) + '\n');
  return { region, overlays: overlays.length };
}

/** Real Tab presses until the target (or an element inside / around it) has focus, so :focus-visible applies. */
async function tabTo() {
  for (let i = 0; i < 200; i++) {
    for (const type of ['rawKeyDown', 'keyUp'])
      await page.send('Input.dispatchKeyEvent', { type, key: 'Tab', code: 'Tab', windowsVirtualKeyCode: 9, nativeVirtualKeyCode: 9 });
    const done = await page.evaluate(`(() => { const a = document.activeElement, t = window.__target;
      return !!a && a !== document.body && (t.contains(a) || a.contains(t)); })()`);
    if (done) return;
  }
  throw new Error('target never received keyboard focus (200 Tab presses)');
}

/** Same rule as GotX (Capture/StateRunner.cs): targets + non-modal overlays, or only the modal content, inflated, clamped. */
function regionOf(rects, overlays, vw, vh) {
  const modal = overlays.filter((o) => o.modal);
  const boxes = modal.length ? modal : [...rects, ...overlays];
  let x0 = Infinity, y0 = Infinity, x1 = -Infinity, y1 = -Infinity;
  for (const b of boxes) { x0 = Math.min(x0, b.x); y0 = Math.min(y0, b.y); x1 = Math.max(x1, b.x + b.w); y1 = Math.max(y1, b.y + b.h); }
  x0 = Math.max(0, Math.floor(x0 - INFLATE)); y0 = Math.max(0, Math.floor(y0 - INFLATE));
  x1 = Math.min(vw, Math.ceil(x1 + INFLATE)); y1 = Math.min(vh, Math.ceil(y1 + INFLATE));
  return { x: x0, y: y0, w: x1 - x0, h: y1 - y0 };
}

function intersects(r, g) { return r.x < g.x + g.w && r.x + r.w > g.x && r.y < g.y + g.h && r.y + r.h > g.y; }
function shift(e, g) {
  return {
    ...e, rect: { ...e.rect, x: e.rect.x - g.x, y: e.rect.y - g.y },
    ...(e.textRect ? { textRect: { ...e.textRect, x: e.textRect.x - g.x, y: e.textRect.y - g.y } } : {}),
  };
}

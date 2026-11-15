// Captures a web page (URL or local .html) with headless Edge: screenshots + a layout map for every element.
// Usage: node capture-html.mjs <url|file.html> --out <dir> [--widths 1440,1024,640] [--themes light,dark] [--height 900]
//          [--full] [--selector css] [--name base] [--port 9333]
// Output in <dir>: <name>-<width>-<theme>.png and .json (layout map: { page, url, width, height, theme, viewport, elements[] }).
// Reuses tools/ShadcnRef (cdp.mjs, prep.mjs): throw-away Edge profile, animations frozen, fonts + images awaited, one page at a time.
// Pages using the Tailwind CDN script (cdn.tailwindcss.com / @tailwindcss/browser) are awaited until the generated CSS exists
// (that script itself comes from the network; the capture adds no other requests).
import { mkdirSync, readFileSync, writeFileSync, existsSync } from 'node:fs';
import { basename, dirname, join, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { launch, connectPage, sleep } from '../../../../tools/ShadcnRef/cdp.mjs';
import { openPage, settle } from '../../../../tools/ShadcnRef/prep.mjs';

const here = dirname(fileURLToPath(import.meta.url));
const mapJs = readFileSync(join(here, 'layout-map-all.js'), 'utf8');
const args = process.argv.slice(2);
const opt = (name, def) => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : def; };
const flag = (name) => args.includes(name);
const positional = args.filter((a, i) => !a.startsWith('--') && !(i > 0 && args[i - 1].startsWith('--') && args[i - 1] !== '--full'));
const target = positional[0];
if (!target || !opt('--out')) {
  console.error('usage: node capture-html.mjs <url|file.html> --out <dir> [--widths 1440,1024,640] [--themes light,dark] [--height 900] [--full] [--selector css] [--name base]');
  process.exit(2);
}
const outDir = resolve(opt('--out'));
const widths = opt('--widths', '1440,1024,640').split(',').map(Number);
const themes = opt('--themes', 'light,dark').split(',');
const height = Number(opt('--height', '900'));
const full = flag('--full');
const selector = opt('--selector');
const isUrl = /^https?:\/\//i.test(target) || target.startsWith('file:');
const url = isUrl ? target : pathToFileURL(resolve(target)).href;
if (!isUrl && !existsSync(resolve(target))) { console.error(`not found: ${target}`); process.exit(2); }
const name = opt('--name') ?? (isUrl ? new URL(url).pathname.split('/').filter(Boolean).pop() ?? new URL(url).hostname : basename(target).replace(/\.[^.]+$/, '')).replace(/[^\w.-]+/g, '-');

mkdirSync(outDir, { recursive: true });
const edge = await launch(Number(opt('--port', '9333')));
const page = await connectPage(edge.port);
try {
  await page.send('Page.enable');
  await page.send('Runtime.enable');
  for (const width of widths) {
    for (const theme of themes) {
      const tag = `${name}-${width}-${theme}`;
      process.stdout.write(`${tag} ... `);
      await openPage(page, url, { width, height, theme, hideHeader: false });
      await waitForTailwind(page);
      await settle(page, full);
      await page.evaluate(`document.fonts.ready.then(() => true)`);
      let shotW = width, shotH = height, clip = null;
      if (selector) {
        const r = await page.evaluate(`(() => { const e = document.querySelector(${JSON.stringify(selector)}); if (!e) return null;
          const b = e.getBoundingClientRect(); return { x: b.left + scrollX, y: b.top + scrollY, w: b.width, h: b.height }; })()`);
        if (!r) throw new Error(`selector not found: ${selector}`);
        clip = { x: r.x, y: r.y, width: Math.ceil(r.w), height: Math.ceil(r.h), scale: 1 };
        shotW = clip.width; shotH = clip.height;
      } else if (full) {
        shotH = await page.evaluate('Math.ceil(document.documentElement.scrollHeight)');
        clip = { x: 0, y: 0, width: shotW, height: shotH, scale: 1 };
      }
      const shot = await page.send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: full || !!selector, ...(clip ? { clip } : {}) });
      writeFileSync(join(outDir, `${tag}.png`), Buffer.from(shot.data, 'base64'));
      const elements = await page.evaluate(`(${mapJs})(${JSON.stringify({ selector })})`);
      writeFileSync(join(outDir, `${tag}.json`), JSON.stringify({ page: name, url, width: shotW, height: shotH, theme, viewport: [width, height], selector, elements }) + '\n');
      console.log(`${shotW}x${shotH}, ${elements.length} elements`);
      await sleep(300);
    }
  }
} finally {
  page.close();
  await edge.close();
}

/** Waits (<= 8 s) for the Tailwind browser script to generate its stylesheet, then for the stylesheet to stop growing. */
async function waitForTailwind(page) {
  const uses = await page.evaluate(`[...document.scripts].some(s => /cdn\\.tailwindcss\\.com|@tailwindcss\\/browser/.test(s.src))`);
  if (!uses) return;
  const css = () => page.evaluate(`[...document.querySelectorAll('style')].filter(s => s.type !== 'text/tailwindcss').reduce((n, s) => n + s.textContent.length, 0)`);
  let last = -1, stable = 0;
  for (let i = 0; i < 80 && stable < 3; i++) {
    const now = await css();
    stable = now > 1000 && now === last ? stable + 1 : 0;
    last = now;
    await sleep(100);
  }
}

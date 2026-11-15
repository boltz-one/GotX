// Captures the shadcn/ui reference screenshots + layout maps that samples/Shadcn is compared against.
// Usage: node tools/ShadcnRef/capture.mjs [--only home,login-03] [--out <dir>]
// Output (default samples/Shadcn/screenshots/reference): <page>-<width>-<theme>.png/.json + ../manifest.json.
// One page at a time, sequential, no cookies or personal data: a fresh temporary Edge profile per run.
// Interaction states: see states.mjs.
import { mkdirSync, readFileSync, writeFileSync, existsSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { launch, connectPage, sleep } from './cdp.mjs';
import { openPage, settle, layoutMapJs } from './prep.mjs';

const here = dirname(fileURLToPath(import.meta.url));
const repo = resolve(here, '..', '..');
const args = process.argv.slice(2);
const opt = (name) => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : undefined; };
const outDir = resolve(opt('--out') ?? join(repo, 'samples', 'Shadcn', 'screenshots', 'reference'));
const only = opt('--only')?.split(',');

// Home: full-page captures without the site header (GotX's home view has no site header either; see README).
// Blocks: the standalone preview routes the iframes on /blocks load, captured at the viewport only.
export const PAGES = [
  { id: 'home', url: 'https://ui.shadcn.com/', fullPage: true, hideHeader: true, sizes: [[1440, 900], [1024, 900], [640, 900]] },
  ...['dashboard-01', 'sidebar-07', 'sidebar-03', 'login-03', 'login-04', 'signup-03', 'signup-04'].map((id) => ({
    id, url: `https://ui.shadcn.com/view/new-york-v4/${id}`, fullPage: false, hideHeader: false, sizes: [[1440, 900], [1024, 768]],
  })),
];
const THEMES = ['light', 'dark'];

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) await main();

async function main() {
  mkdirSync(outDir, { recursive: true });
  const manifestPath = join(dirname(outDir), 'manifest.json');
  const manifest = existsSync(manifestPath) ? JSON.parse(readFileSync(manifestPath, 'utf8')) : { captures: {} };
  const edge = await launch();
  const page = await connectPage(edge.port);
  try {
    await page.send('Page.enable');
    await page.send('Runtime.enable');
    manifest.browser = edge.version.Browser;
    manifest.userAgent = edge.version['User-Agent'];
    manifest.notes = 'deviceScaleFactor 1; animations/transitions disabled by injected CSS; prefers-reduced-motion: reduce; '
      + 'theme forced by localStorage "theme" + prefers-color-scheme; home: full page, site header hidden (display:none); '
      + 'blocks: viewport only. Rects in layout maps are document CSS px.';
    manifest.captures ??= {};
    for (const p of PAGES) {
      if (only && !only.includes(p.id)) continue;
      for (const [width, height] of p.sizes) {
        for (const theme of THEMES) {
          const name = `${p.id}-${width}-${theme}`;
          process.stdout.write(`${name} … `);
          const info = await capture(page, p, width, height, theme, name);
          manifest.captures[name] = { url: p.url, date: new Date().toISOString(), viewport: [width, height], ...info };
          console.log(`${info.image[0]}×${info.image[1]}, ${info.elements} elements`);
          await sleep(1000); // politeness between page loads
        }
      }
    }
  } finally {
    page.close();
    await edge.close();
    writeFileSync(manifestPath, JSON.stringify(manifest, null, 2) + '\n');
  }
}

async function capture(page, p, width, height, theme, name) {
  await openPage(page, p.url, { width, height, theme, hideHeader: p.hideHeader });
  await settle(page);
  const isDark = await page.evaluate(`document.documentElement.classList.contains('dark')`);
  if (isDark !== (theme === 'dark')) throw new Error(`${name}: theme did not apply`);
  const shotW = width;
  const shotH = p.fullPage ? await page.evaluate('Math.ceil(document.documentElement.scrollHeight)') : height;
  const shot = await page.send('Page.captureScreenshot', {
    format: 'png', captureBeyondViewport: p.fullPage, clip: { x: 0, y: 0, width: shotW, height: shotH, scale: 1 },
  });
  writeFileSync(join(outDir, `${name}.png`), Buffer.from(shot.data, 'base64'));
  const elements = await page.evaluate(layoutMapJs);
  const map = { page: p.id, url: p.url, width, height: shotH, theme, viewport: [width, height], elements };
  writeFileSync(join(outDir, `${name}.json`), JSON.stringify(map) + '\n');
  return { image: [shotW, shotH], elements: elements.length };
}

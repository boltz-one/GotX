// Page preparation shared by capture.mjs (resting pages) and states.mjs (interaction states).
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));

// Freezes motion: animations jump to their end, no transitions (GotX side runs its transitions to the end instead),
// no caret blink; hides scrollbars that headless may still draw.
export const FREEZE_CSS = `*,*::before,*::after{animation-duration:0s!important;animation-delay:0s!important;animation-iteration-count:1!important;
transition:none!important;caret-color:transparent!important;scroll-behavior:auto!important}
html::-webkit-scrollbar,body::-webkit-scrollbar{display:none}`;
export const HIDE_HEADER_CSS = 'body header.sticky:first-of-type, body > div header.sticky{display:none!important}';
export const layoutMapJs = readFileSync(join(here, 'layout-map.js'), 'utf8');

/** Emulates viewport + theme, loads the URL with the theme stored, injects the freeze (and header-hiding) CSS. */
export async function openPage(page, url, { width, height, theme, hideHeader }) {
  await page.send('Emulation.setDeviceMetricsOverride', { width, height, deviceScaleFactor: 1, mobile: false });
  await page.send('Emulation.setEmulatedMedia', {
    features: [{ name: 'prefers-color-scheme', value: theme }, { name: 'prefers-reduced-motion', value: 'reduce' }],
  });
  const { identifier } = await page.send('Page.addScriptToEvaluateOnNewDocument', {
    source: `try { localStorage.setItem('theme', '${theme}'); } catch {}`,
  });
  const loaded = page.once('Page.loadEventFired', 60000);
  await page.send('Page.navigate', { url });
  await loaded;
  await page.send('Page.removeScriptToEvaluateOnNewDocument', { identifier });
  const css = FREEZE_CSS + (hideHeader ? HIDE_HEADER_CSS : '');
  await page.evaluate(`(() => { const s = document.createElement('style'); s.textContent = ${JSON.stringify(css)}; document.head.appendChild(s);
    document.documentElement.classList.toggle('dark', ${theme === 'dark'}); document.documentElement.style.colorScheme = '${theme}'; })()`);
}

/** Scrolls through the page (lazy content), back to top, waits for fonts and images. */
export async function settle(page, scroll = true) {
  await page.evaluate(`(async () => { const step = innerHeight * 0.8;
    if (${scroll}) { for (let y = 0; y < document.documentElement.scrollHeight; y += step) { scrollTo(0, y); await new Promise(r => setTimeout(r, 150)); } }
    scrollTo(0, 0); await document.fonts.ready;
    await Promise.all([...document.images].map(i => i.complete ? 0 : new Promise(r => { i.onload = i.onerror = r; })));
    await new Promise(r => setTimeout(r, 500)); })()`);
}

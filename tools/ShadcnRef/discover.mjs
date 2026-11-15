// Lists the standalone preview URLs of the blocks shown on https://ui.shadcn.com/blocks (iframes / "open in new tab" links).
// Usage: node tools/ShadcnRef/discover.mjs [category-url ...]
import { launch, connectPage, sleep } from './cdp.mjs';

const pages = process.argv.slice(2);
if (pages.length === 0) pages.push('https://ui.shadcn.com/blocks', 'https://ui.shadcn.com/blocks/sidebar', 'https://ui.shadcn.com/blocks/login');

const edge = await launch();
try {
  const page = await connectPage(edge.port);
  await page.send('Page.enable');
  await page.send('Emulation.setDeviceMetricsOverride', { width: 1440, height: 900, deviceScaleFactor: 1, mobile: false });
  for (const url of pages) {
    const loaded = page.once('Page.loadEventFired');
    await page.send('Page.navigate', { url });
    await loaded;
    await sleep(2000);
    const found = await page.evaluate(`(() => {
      const out = new Set();
      for (const f of document.querySelectorAll('iframe')) out.add('iframe ' + (f.src || f.dataset.src || ''));
      for (const a of document.querySelectorAll('a[href*="/view/"]')) out.add('link ' + a.href);
      return [...out];
    })()`);
    console.log(`# ${url}`);
    for (const f of found) console.log(f);
  }
  page.close();
} finally {
  await edge.close();
}

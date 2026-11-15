// Minimal Chrome DevTools Protocol client for Microsoft Edge (no npm packages: Node built-in fetch + WebSocket).
import { spawn } from 'node:child_process';
import { mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

export const EDGE = process.env.EDGE_PATH ?? 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

/** Launches headless Edge with a throw-away profile and returns { browser, close }. */
export async function launch(port = 9333) {
  const profile = mkdtempSync(join(tmpdir(), 'shadcnref-'));
  const proc = spawn(EDGE, [
    '--headless=new', `--remote-debugging-port=${port}`, `--user-data-dir=${profile}`,
    '--hide-scrollbars', '--no-first-run', '--no-default-browser-check', '--disable-extensions',
    '--force-color-profile=srgb', '--font-render-hinting=none', 'about:blank',
  ], { stdio: 'ignore' });
  let version;
  for (let i = 0; i < 100 && !version; i++) {
    try { version = await (await fetch(`http://127.0.0.1:${port}/json/version`)).json(); } catch { await sleep(100); }
  }
  if (!version) { proc.kill(); throw new Error('Edge did not open the DevTools port'); }
  const close = async () => {
    proc.kill();
    await sleep(500);
    try { rmSync(profile, { recursive: true, force: true }); } catch { /* profile still locked: leave it in temp */ }
  };
  return { port, version, close };
}

/** Connects to the first page target. Returns { send, on, close }. */
export async function connectPage(port) {
  const targets = await (await fetch(`http://127.0.0.1:${port}/json/list`)).json();
  const page = targets.find((t) => t.type === 'page');
  const ws = new WebSocket(page.webSocketDebuggerUrl);
  await new Promise((res, rej) => { ws.onopen = res; ws.onerror = rej; });
  let id = 0;
  const pending = new Map();
  const listeners = new Map();
  ws.onmessage = (ev) => {
    const msg = JSON.parse(ev.data);
    if (msg.id && pending.has(msg.id)) {
      const { resolve, reject } = pending.get(msg.id);
      pending.delete(msg.id);
      msg.error ? reject(new Error(`${msg.error.message} ${msg.error.data ?? ''}`)) : resolve(msg.result);
    } else if (msg.method) (listeners.get(msg.method) ?? []).forEach((f) => f(msg.params));
  };
  const send = (method, params = {}) => new Promise((resolve, reject) => {
    const i = ++id;
    pending.set(i, { resolve, reject });
    ws.send(JSON.stringify({ id: i, method, params }));
  });
  const on = (method, f) => listeners.set(method, [...(listeners.get(method) ?? []), f]);
  const once = (method, timeoutMs = 30000) => new Promise((resolve) => {
    const t = setTimeout(() => resolve(null), timeoutMs);
    on(method, (p) => { clearTimeout(t); resolve(p); });
  });
  /** Evaluates an expression (awaits promises) and returns its JSON value. */
  const evaluate = async (expression) => {
    const r = await send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true });
    if (r.exceptionDetails) throw new Error(`page error: ${r.exceptionDetails.exception?.description ?? r.exceptionDetails.text}`);
    return r.result.value;
  };
  return { send, on, once, evaluate, close: () => ws.close() };
}

export { sleep };

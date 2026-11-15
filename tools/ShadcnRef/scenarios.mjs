// scenarios.json expansion — mirrored by samples/Shadcn/Capture/Scenarios.cs (keep both in sync).
// { target, do: [a, b] } → states "<target>-<a>", "<target>-<b>"; { name, steps: [{ target, do, text? }] } → one state.

/** Floating layers whose box joins the capture region. */
export const OVERLAY_SLOTS = ['select-content', 'dropdown-menu-content', 'dropdown-menu-sub-content', 'popover-content', 'tooltip-content',
  'hover-card-content', 'context-menu-content', 'menubar-content', 'navigation-menu-viewport', 'alert-dialog-content', 'dialog-content',
  'sheet-content', 'drawer-content', 'command-dialog'];
/** Modal layers: the region is the modal content alone (the trigger may sit anywhere behind the backdrop). */
export const MODAL_SLOTS = ['alert-dialog-content', 'dialog-content', 'sheet-content', 'drawer-content'];
/** Margin around the target / overlay boxes (CSS px). */
export const INFLATE = 24;

export function expandStates(sc) {
  const out = [];
  for (const s of sc.states) {
    if (s.steps) out.push({ name: s.name, steps: s.steps.map((st) => step(sc, st)) });
    else for (const action of [].concat(s.do)) out.push({ name: `${s.target}-${action}`, steps: [step(sc, { target: s.target, do: action, text: s.text })] });
  }
  return out;
}

function step(sc, st) {
  const t = sc.targets[st.target];
  if (!t) throw new Error(`scenario ${sc.page}: unknown target ${st.target}`);
  return { target: st.target, do: st.do, text: st.text, ref: t.ref, gotx: t.gotx };
}

/* Първа помощ: раздели със стъпки (с клавиатура), акордеон, списък за колата (localStorage) */
(function () {
  'use strict';
  // ---- стъпки (tablist) ----
  const tabs = [...document.querySelectorAll('.step-tab')];
  function select(i, focus) {
    tabs.forEach((t, k) => {
      const on = k === i;
      t.setAttribute('aria-selected', String(on));
      t.tabIndex = on ? 0 : -1;
      document.getElementById(t.getAttribute('aria-controls')).hidden = !on;
    });
    if (focus) tabs[i].focus();
  }
  tabs.forEach((t, i) => {
    t.addEventListener('click', () => select(i));
    t.addEventListener('keydown', (e) => {
      if (e.key === 'ArrowRight') { e.preventDefault(); select((i + 1) % tabs.length, true); }
      if (e.key === 'ArrowLeft') { e.preventDefault(); select((i - 1 + tabs.length) % tabs.length, true); }
    });
  });

  // ---- акордеон: един отворен раздел ----
  const btns = [...document.querySelectorAll('.acc__btn')];
  btns.forEach((b) => b.addEventListener('click', () => {
    const open = b.getAttribute('aria-expanded') !== 'true';
    btns.forEach((x) => { x.setAttribute('aria-expanded', 'false'); document.getElementById(x.getAttribute('aria-controls')).hidden = true; });
    if (open) { b.setAttribute('aria-expanded', 'true'); document.getElementById(b.getAttribute('aria-controls')).hidden = false; }
  }));

  // ---- списък за колата ----
  const KEY = 'roadsense.kit';
  const boxes = [...document.querySelectorAll('[data-kit]')];
  let saved = {};
  try { saved = JSON.parse(localStorage.getItem(KEY) || '{}'); } catch (_) { saved = {}; }
  const done = document.getElementById('kitDone');
  function update() {
    const n = boxes.filter((b) => b.checked).length;
    done.textContent = n === boxes.length ? 'Всичко е налице.' : n + ' от ' + boxes.length + ' · провери и останалите';
    try { localStorage.setItem(KEY, JSON.stringify(Object.fromEntries(boxes.map((b) => [b.dataset.kit, b.checked])))); } catch (_) { /* недостъпно хранилище */ }
  }
  boxes.forEach((b) => { b.checked = !!saved[b.dataset.kit]; b.addEventListener('change', update); });
  update();
})();

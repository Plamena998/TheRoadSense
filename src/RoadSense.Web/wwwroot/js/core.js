/* RoadSense — общи помощни функции за всички страници */
(function () {
  'use strict';

  const RS = (window.RS = window.RS || {});

  RS.reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  /** GET към API-то. Връща JSON или хвърля грешка с текста от сървъра. */
  RS.api = async function (path, params) {
    const url = new URL(path, window.location.origin);
    if (params) Object.entries(params).forEach(([k, v]) => v != null && url.searchParams.set(k, v));
    const res = await fetch(url, { headers: { Accept: 'application/json' } });
    if (!res.ok) {
      let msg = res.status + ' ' + res.statusText;
      try { const p = await res.json(); msg = p.detail || p.title || msg; } catch (_) { /* няма тяло */ }
      throw new Error(msg);
    }
    return res.status === 204 ? null : res.json();
  };

  RS.esc = (s) => String(s ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  RS.pad = (n) => (n < 10 ? '0' : '') + n;
  RS.clock = function (hours) {
    const x = ((hours % 24) + 24) % 24;
    let h = Math.floor(x), m = Math.round(((x - h) * 60) / 5) * 5;
    if (m === 60) { h = (h + 1) % 24; m = 0; }
    return RS.pad(h) + ':' + RS.pad(m);
  };
  RS.num = (n, digits) => Number(n).toLocaleString('bg-BG', { maximumFractionDigits: digits ?? 0 });

  // ---- риск ----
  RS.level = {
    low: { label: 'Нисък', color: '#D4F2DB', fg: '#000000' },
    moderate: { label: 'Умерен', color: '#7E9686', fg: '#000000' },
    high: { label: 'Висок', color: '#FF3B30', fg: '#000000' },
    veryHigh: { label: 'Много висок', color: '#B3140B', fg: '#D4F2DB' }
  };
  RS.levelOf = (score) => (score < 25 ? 'low' : score < 50 ? 'moderate' : score < 75 ? 'high' : 'veryHigh');

  /** Показва грешка в контейнер, без да чупи страницата. */
  RS.showError = function (el, err) {
    el.innerHTML = '<div class="error-box"><strong>Данните не се заредиха.</strong> <span class="muted">' + RS.esc(err.message || err) + '</span></div>';
  };

  /**
   * Падащо меню в стила на дизайна (вместо обикновен &lt;select&gt;).
   * root: елемент с data-dd; items: [{ value, label, hint }]; onChange(value)
   */
  RS.dropdown = function (root, items, value, onChange) {
    const btn = root.querySelector('.dd__btn');
    const list = root.querySelector('.dd__list');
    const valueEl = root.querySelector('.dd__value');
    const hintEl = root.querySelector('.dd__hint');
    let current = value;

    const check = '<svg class="dd__check" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#FF3B30" stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M5 12.5 10 17 19 7"></path></svg>';
    list.innerHTML = items.map((it) =>
      '<li role="none"><button type="button" role="option" class="dd__opt" data-value="' + RS.esc(it.value) + '" aria-selected="false">' +
      '<span><span style="display:block;font-size:15px;font-weight:700">' + RS.esc(it.label) + '</span>' +
      (it.hint ? '<span style="display:block;font-size:12px;color:var(--muted)">' + RS.esc(it.hint) + '</span>' : '') +
      '</span>' + check + '</button></li>').join('');
    const opts = [...list.querySelectorAll('.dd__opt')];

    function render() {
      const it = items.find((x) => x.value === current) || items[0];
      valueEl.textContent = it.label;
      if (hintEl) hintEl.textContent = it.hint || '';
      opts.forEach((o) => o.setAttribute('aria-selected', String(o.dataset.value === current)));
    }
    function open() {
      list.hidden = false; btn.setAttribute('aria-expanded', 'true');
      (opts.find((o) => o.dataset.value === current) || opts[0]).focus({ preventScroll: true });
    }
    function close(focusBtn) { list.hidden = true; btn.setAttribute('aria-expanded', 'false'); if (focusBtn) btn.focus(); }
    function pick(v) { current = v; render(); close(true); onChange(v); }

    btn.addEventListener('click', () => (list.hidden ? open() : close(false)));
    btn.addEventListener('keydown', (e) => { if (e.key === 'ArrowDown') { e.preventDefault(); open(); } });
    list.addEventListener('click', (e) => { const o = e.target.closest('.dd__opt'); if (o) pick(o.dataset.value); });
    list.addEventListener('keydown', (e) => {
      const i = opts.indexOf(document.activeElement);
      if (e.key === 'Escape') { e.preventDefault(); close(true); }
      else if (e.key === 'ArrowDown') { e.preventDefault(); opts[Math.min(opts.length - 1, i + 1)].focus(); }
      else if (e.key === 'ArrowUp') { e.preventDefault(); opts[Math.max(0, i - 1)].focus(); }
      else if (e.key === 'Tab') close(false);
    });
    document.addEventListener('mousedown', (e) => { if (!list.hidden && !root.contains(e.target)) close(false); });
    render();
    return { set(v) { current = v; render(); }, get value() { return current; } };
  };

  /** Поява на блоковете при скрол (.reveal → .is-in). */
  RS.reveal = function (scope) {
    const els = (scope || document).querySelectorAll('.reveal:not(.is-in)');
    if (!('IntersectionObserver' in window) || RS.reducedMotion) { els.forEach((e) => e.classList.add('is-in')); return; }
    const io = new IntersectionObserver((entries) => entries.forEach((en) => {
      if (!en.isIntersecting) return;
      en.target.classList.add('is-in');
      en.target.dispatchEvent(new CustomEvent('revealed'));
      io.unobserve(en.target);
    }), { rootMargin: '0px 0px -12% 0px' });
    els.forEach((e) => io.observe(e));
  };

  document.addEventListener('DOMContentLoaded', () => RS.reveal());
})();

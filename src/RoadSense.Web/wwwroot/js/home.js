/* Начална страница: плавен скрол (Lenis), история при скрол, бутон „Скролни“, броене на числата */
(function () {
  'use strict';
  const RS = window.RS;
  const story = document.getElementById('story');
  if (!story) return;

  const $ = (s) => story.querySelector(s);
  const camera = $('.story__camera'), strip = $('.story__strip'), intro = $('.story__intro');
  const statsBox = $('.story__stats'), descsBox = $('.story__descs'), outro = $('.story__outro'), src = $('.story__src');
  const stats = [...statsBox.querySelectorAll('.stat')], descs = [...descsBox.querySelectorAll('.desc')];
  const cue = document.querySelector('.cue');

  // 6 сцени: 0 начало → интро, 0.2 здрач → 1-во число, 0.4 дъжд → 2-ро, 0.6 нощ → 3-то, 0.8 мъгла → 4-то, 1 аварийни → финал
  const BEATS = [0, 0.2, 0.4, 0.6, 0.8, 1];
  const TRACKS = {
    cam: [[0, { s: 1.5, y: 60 }], [0.16, { s: 1, y: 0 }], [0.86, { s: 1, y: 0 }], [1, { s: 1.35, y: 40 }]],
    strip: [[0, { x: 0 }], [0.2, { x: -1 }], [0.4, { x: -2 }], [0.6, { x: -3 }], [0.8, { x: -4 }], [1, { x: -5 }]],
    intro: [[0, { o: 1, y: 0 }], [0.1, { o: 0, y: -140 }]],
    stats: [[0.08, { o: 0, y: 260 }], [0.18, { o: 1, y: 0 }], [0.84, { o: 1, y: 0 }], [0.92, { o: 0, y: -160 }]],
    outro: [[0.9, { o: 0, y: 120 }], [0.98, { o: 1, y: 0 }]]
  };
  const sample = (frames, p) => {
    if (p <= frames[0][0]) return frames[0][1];
    for (let i = 1; i < frames.length; i++) {
      if (p <= frames[i][0]) {
        const [pa, a] = frames[i - 1], [pb, b] = frames[i], k = (p - pa) / (pb - pa), o = {};
        for (const key in b) o[key] = a[key] + (b[key] - a[key]) * k;
        return o;
      }
    }
    return frames[frames.length - 1][1];
  };

  let span = [0, 1], beats = [], active = -1;

  function measure() {
    const top = story.getBoundingClientRect().top + window.scrollY;
    const len = story.offsetHeight - window.innerHeight;
    span = [top, top + len];
    beats = BEATS.map((b) => top + b * len);
  }

  function render(p) {
    const cam = sample(TRACKS.cam, p), st = sample(TRACKS.strip, p), it = sample(TRACKS.intro, p);
    const sx = sample(TRACKS.stats, p), ou = sample(TRACKS.outro, p);
    camera.style.transform = `translate3d(0,${cam.y.toFixed(1)}px,0) scale(${cam.s.toFixed(3)})`;
    strip.style.transform = `translate3d(${(st.x * window.innerWidth).toFixed(1)}px,0,0)`;
    intro.style.opacity = it.o.toFixed(3);
    intro.style.transform = `translate3d(0, calc(-50% + ${it.y.toFixed(1)}px), 0)`;
    statsBox.style.opacity = sx.o.toFixed(3);
    statsBox.style.transform = `translate3d(-50%, calc(-50% + ${sx.y.toFixed(1)}px), 0)`;
    descsBox.style.opacity = sx.o.toFixed(3);
    descsBox.style.transform = `translate3d(-50%, calc(-50% + ${(sx.y * 0.7).toFixed(1)}px), 0)`;
    outro.style.opacity = ou.o.toFixed(3);
    outro.style.transform = `translate3d(0, calc(-50% + ${ou.y.toFixed(1)}px), 0)`;
    outro.style.pointerEvents = ou.o > 0.5 ? 'auto' : 'none';

    const a = p < 0.3 ? 0 : p < 0.5 ? 1 : p < 0.7 ? 2 : 3;
    if (a !== active) {
      active = a;
      stats.forEach((s, i) => s.classList.toggle('is-active', i === a));
      descs.forEach((d, i) => d.classList.toggle('is-active', i === a));
      src.textContent = descs[a].dataset.source;
      statsBox.classList.toggle('is-light', a === 3); // мъглата е светла — числото става тъмно, без ореол
    }
  }

  // ---- статичен режим (намалено движение): една сцена, без дълга история ----
  if (RS.reducedMotion) {
    story.classList.add('is-static');
    render(0.2);
    return;
  }

  // ---- плавен скрол ----
  const lenis = window.Lenis ? new window.Lenis({ autoRaf: true, lerp: 0.14 }) : null;

  let raf = 0;
  const tick = () => {
    raf = 0;
    const p = Math.max(0, Math.min(1, (window.scrollY - span[0]) / Math.max(span[1] - span[0], 1)));
    render(p);
  };
  const onScroll = () => { if (!raf) raf = requestAnimationFrame(tick); hideCue(); armSnap(); };

  // ---- „прилепване“ към най-близката сцена ----
  let snapT = 0;
  function armSnap(ms) { clearTimeout(snapT); snapT = setTimeout(settle, ms || 140); }
  function settle() {
    if (!lenis || lenis.isScrolling === 'smooth') return;
    const y = lenis.targetScroll ?? window.scrollY;
    if (y < span[0] - 2 || y > span[1] + 2) return;
    let best = null, d = Infinity;
    beats.forEach((b) => { const dd = Math.abs(b - y); if (dd < d) { d = dd; best = b; } });
    if (best !== null && d > 2 && d < window.innerHeight / 2) {
      lenis.scrollTo(best, { duration: 0.35 + (0.5 * d) / window.innerHeight, easing: (s) => s * s * (3 - 2 * s) });
    }
  }

  // ---- бутон „Скролни“ ----
  let cueT = 0;
  function waitCue(ms) { clearTimeout(cueT); cueT = setTimeout(() => { if (window.scrollY < span[1] - 2) cue.classList.add('is-on'); }, ms); }
  function hideCue() { cue.classList.remove('is-on'); waitCue(1000); }
  cue.addEventListener('click', () => {
    const y = window.scrollY, next = beats.find((b) => b > y + 4);
    const target = next !== undefined ? next : span[1] + window.innerHeight * 0.75;
    if (lenis) lenis.scrollTo(target, { duration: 1.6 }); else window.scrollTo({ top: target, behavior: 'smooth' });
  });

  window.addEventListener('scroll', onScroll, { passive: true });
  window.addEventListener('wheel', (e) => { if (e.deltaY && !e.ctrlKey) armSnap(100); }, { passive: true });
  window.addEventListener('resize', () => { measure(); tick(); });
  window.addEventListener('load', () => { measure(); tick(); });
  measure(); tick(); waitCue(1800);

  // ---- броене на числата за Зимница ----
  const zim = document.getElementById('zim');
  zim && zim.addEventListener('revealed', () => {
    const els = zim.querySelectorAll('[data-count]');
    const t0 = performance.now(), D = 1400;
    const step = (t) => {
      const k = Math.min(1, (t - t0) / D), e = 1 - Math.pow(1 - k, 3);
      els.forEach((el) => (el.textContent = Math.round(+el.dataset.count * e)));
      if (k < 1) requestAnimationFrame(step);
    };
    requestAnimationFrame(step);
  });
})();

// Безкрайна лента „Какво те чака по пътя днес“: копия на плочките, за да се върти без прекъсване
// и без празно място, дори когато плочките са по-малко от ширината на екрана.
(function () {
  const track = document.querySelector('.marquee__track');
  if (!track) return;
  const set = track.querySelector('.tiles');
  function build() {
    track.querySelectorAll('.tiles[aria-hidden]').forEach((n) => n.remove());
    const w = set.getBoundingClientRect().width;
    if (!w) return;
    const copies = Math.max(1, Math.ceil(window.innerWidth / w));
    for (let i = 0; i < copies; i++) {
      const copy = set.cloneNode(true);
      copy.setAttribute('aria-hidden', 'true');
      copy.querySelectorAll('a, button').forEach((el) => el.setAttribute('tabindex', '-1'));
      track.appendChild(copy);
    }
    track.style.setProperty('--shift', w + 'px');
    track.style.setProperty('--marquee-time', Math.round(w / 40) + 's'); // ~40 px/s
  }
  build();
  let t; window.addEventListener('resize', () => { clearTimeout(t); t = setTimeout(build, 200); });
  if (document.fonts) document.fonts.ready.then(build);
})();

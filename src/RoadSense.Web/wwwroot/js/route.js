/* Маршрут: падащи менюта, слайдер за час, карта, оценка от /api/route */
(function () {
  'use strict';
  const RS = window.RS;
  const init = JSON.parse(document.getElementById('route-init').textContent);
  const state = { from: init.from, to: init.to, hour: +init.hour, vehicle: init.vehicle };
  const $ = (id) => document.getElementById(id);

  let network = null;     // { cities, segments } от /api/network
  let reqId = 0;

  // ---------- падащи менюта ----------
  const ddFrom = RS.dropdown(document.querySelector('[data-dd="from"]'), init.cities, state.from, (v) => { state.from = v; load(); });
  const ddTo = RS.dropdown(document.querySelector('[data-dd="to"]'), init.cities, state.to, (v) => { state.to = v; load(); });
  RS.dropdown(document.querySelector('[data-dd="vehicle"]'), init.vehicles, state.vehicle, (v) => { state.vehicle = v; load(); });
  $('swap').addEventListener('click', () => {
    [state.from, state.to] = [state.to, state.from];
    ddFrom.set(state.from); ddTo.set(state.to); load();
  });

  // ---------- слайдер ----------
  const slider = $('dep');
  const syncSlider = () => {
    slider.value = state.hour;
    slider.style.setProperty('--p', ((state.hour / 23) * 100).toFixed(2) + '%');
    slider.setAttribute('aria-valuetext', RS.pad(state.hour) + ':00');
    $('depLabel').textContent = RS.pad(state.hour) + ':00';
  };
  let sliderT = 0;
  slider.addEventListener('input', () => {
    state.hour = +slider.value; syncSlider();
    clearTimeout(sliderT); sliderT = setTimeout(load, 120); // не пращаме заявка на всеки пиксел
  });
  syncSlider();

  // ---------- легенда ----------
  $('legend').innerHTML = Object.values(RS.level).map((l) => '<span><i style="background:' + l.color + '"></i>' + l.label + '</span>').join('');

  // ---------- карта ----------
  function drawMap() {
    const byId = Object.fromEntries(network.cities.map((c) => [c.id, c]));
    $('roads').innerHTML = network.segments.map((s) => {
      const a = byId[s.fromCityId], b = byId[s.toCityId];
      return '<path class="road" data-seg="' + s.id + '" d="M' + a.x + ' ' + a.y + ' L' + b.x + ' ' + b.y + '" stroke="#2F3A32" stroke-width="3"></path>';
    }).join('');
    const right = new Set(['BGS', 'VAR', 'RUS', 'VTR', 'HAS']);
    $('labels').innerHTML = network.cities.map((c) =>
      '<text x="' + (right.has(c.id) ? c.x + 12 : c.x - 10) + '" y="' + (c.y - 10) + '" text-anchor="' + (right.has(c.id) ? 'start' : 'end') + '">' + RS.esc(c.name) + '</text>').join('');
  }

  // ---------- рендер на резултата ----------
  function render(r) {
    const byId = Object.fromEntries(network.cities.map((c) => [c.id, c]));
    const inRoute = new Map(r.legs.map((l) => [l.segmentId, l]));
    document.querySelectorAll('#roads .road').forEach((p) => {
      const leg = inRoute.get(p.dataset.seg);
      p.setAttribute('stroke', leg ? RS.level[leg.level].color : '#2F3A32');
      p.setAttribute('stroke-width', leg ? 9 : 3);
    });
    const pos = (id) => ({ x: (byId[id].x / 800) * 100, y: (byId[id].y / 440) * 100 });
    let pins = '';
    if (r.legs.length) {
      const a = pos(r.fromId), b = pos(r.toId);
      pins += '<span class="map-pin" style="left:' + a.x + '%;top:' + a.y + '%;background:#D4F2DB">А</span>';
      pins += '<span class="map-pin" style="left:' + b.x + '%;top:' + b.y + '%;background:#FF3B30">Б</span>';
    }
    const segById = Object.fromEntries(network.segments.map((s) => [s.id, s]));
    r.legs.filter((l) => l.incident || l.repair).forEach((l) => {
      const s = segById[l.segmentId], a = pos(s.fromCityId), b = pos(s.toCityId);
      pins += '<span class="map-alert" title="' + RS.esc(l.incident || l.repair) + '" style="left:' + ((a.x + b.x) / 2) + '%;top:' + ((a.y + b.y) / 2) + '%;background:' + (l.incident ? '#FF3B30' : '#7E9686') + '">' +
        '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6" stroke-linecap="round" aria-hidden="true"><path d="M12 8v5M12 17h.01"></path></svg></span>';
    });
    $('pins').innerHTML = pins;

    const h = Math.floor(r.hours), m = Math.round((r.hours % 1) * 60);
    $('routeName').textContent = r.fromName + ' → ' + r.toName;
    $('routeMeta').textContent = r.legs.length
      ? r.totalKm + ' км · около ' + h + ' ч ' + m + ' мин · пристигане ' + RS.clock(r.arrivalHour) + (r.arrivesNextDay ? ' на следващия ден' : '')
      : 'Избери две различни точки';
    $('arrLabel').textContent = r.legs.length ? RS.clock(r.arrivalHour) : '--:--';
    $('nextDay').hidden = !r.arrivesNextDay;

    $('score').textContent = r.legs.length ? r.score : '—';
    const badge = $('levelBadge');
    badge.className = 'lvl lvl--' + r.level; badge.textContent = r.levelLabel + ' риск';
    $('marker').style.transform = 'translateX(' + Math.min(99, r.score) + '%)';
    $('verdict').textContent = r.verdict;
    $('mlNote').textContent = r.riskSource === 'ml'
      ? 'Времето в оценката е изчислено от ML модел (ML.NET)' + (r.modelReliable ? '.' : ' — моделът все още не е по-добър от базовата линия, затова разликите между часовете са малки.')
      : '';
    $('factors').innerHTML = r.factors.map((f) =>
      '<div class="factor"><span>' + RS.esc(f.label) + '</span><span class="track"><span class="bar-h" style="transform:scaleX(' + (f.value / 100).toFixed(3) + ');background:' + RS.level[RS.levelOf(f.value)].color + '"></span></span><span class="mono" style="text-align:right;color:var(--text)">' + f.value + '</span></div>').join('');

    if (r.bestHour === r.departureHour) {
      $('bestTitle').textContent = 'Това е най-безопасният час днес';
      $('bestText').textContent = 'През цялото денонощие по-нисък риск няма.';
      $('bestBtn').hidden = true;
    } else {
      $('bestTitle').textContent = 'Тръгни в ' + RS.pad(r.bestHour) + ':00 — риск ' + r.bestScore;
      $('bestText').textContent = 'С ' + (r.score - r.bestScore) + ' точки по-нисък: условията по пътя са по-добри.';
      $('bestBtn').hidden = false;
      $('bestBtn').onclick = () => { state.hour = r.bestHour; syncSlider(); load(); };
    }

    $('legs').innerHTML = r.legs.map((l) =>
      '<div class="leg"><span class="leg__time">' + RS.clock(l.startHour) + (l.nextDay ? ' +1' : '') + '</span>' +
      '<span class="leg__road">' + RS.esc(l.road) + ' · ' + l.km + ' км</span>' +
      '<span class="leg__name">' + RS.esc(l.from) + ' – ' + RS.esc(l.to) + '</span>' +
      '<span class="leg__note">' + RS.esc(l.note) + '</span>' +
      '<span class="lvl lvl--' + l.level + '">' + l.score + ' · ' + RS.level[l.level].label + '</span></div>').join('') ||
      '<p class="muted mt-s">Няма маршрут.</p>';

    $('compare').innerHTML = r.departures.map((d) =>
      '<button type="button" class="press' + (d.hour === r.departureHour ? ' is-on' : '') + '" data-h="' + d.hour + '" title="Тръгване ' + RS.pad(d.hour) + ':00: риск ' + d.score + '" aria-label="Тръгване ' + RS.pad(d.hour) + ':00, риск ' + d.score + '" style="height:' + Math.max(10, d.score * 1.6) + 'px;background:' + RS.level[d.level].color + '"></button>').join('');
    $('compareLabels').innerHTML = r.departures.map((d) => '<span>' + (d.hour % 3 === 0 ? RS.pad(d.hour) : '') + '</span>').join('');
    $('selHour').textContent = RS.pad(r.departureHour) + ':00';
    $('selScore').textContent = r.score;
  }
  $('compare').addEventListener('click', (e) => {
    const b = e.target.closest('button[data-h]'); if (!b) return;
    state.hour = +b.dataset.h; syncSlider(); load();
  });

  // ---------- зареждане ----------
  async function load() {
    const id = ++reqId;
    const url = new URL(window.location.href);
    ['from', 'to', 'hour', 'vehicle'].forEach((k) => url.searchParams.set(k, state[k]));
    history.replaceState(null, '', url);
    document.querySelector('.score-card').classList.add('is-loading');
    try {
      const r = await RS.api('/api/route', state);
      if (id === reqId) render(r);
    } catch (err) {
      if (id === reqId) RS.showError($('legs'), err);
    } finally {
      if (id === reqId) document.querySelector('.score-card').classList.remove('is-loading');
    }
  }

  (async function start() {
    try { network = await RS.api('/api/network'); drawMap(); load(); }
    catch (err) { RS.showError($('legs'), err); }
  })();
})();

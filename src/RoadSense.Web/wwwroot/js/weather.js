/* Времето около мен.
   Прогноза (видимост, валеж, пориви, състояние): Open-Meteo през /api/weather/forecast.
   Измервания от пътя (температура на пътя, налягане): най-близката станция на bgtoll.bg през /api/weather/live.
   И двата източника се опресняват на 15 мин. Докато няма местоположение, навсякъде стоят тирета. */
(function () {
  'use strict';
  const RS = window.RS;
  const $ = (id) => document.getElementById(id);
  const state = { hour: 0, vehicle: 'Car', road: 'Motorway', surface: 'auto' };
  let reqId = 0, fc = null, live = null, located = null;

  const fT = (t) => (t < 0 ? '−' : '') + Math.abs(t).toLocaleString('bg-BG', { maximumFractionDigits: 1 }) + '°';
  const fV = (v) => (v >= 1000 ? (v / 1000).toLocaleString('bg-BG', { maximumFractionDigits: v >= 10000 ? 0 : 1 }) + ' км' : v + ' м');
  const num = (v, d) => (v == null ? '—' : v.toLocaleString('bg-BG', { maximumFractionDigits: d == null ? 1 : d }));
  const SURF = { dry: 'Суха', wet: 'Мокра', snow: 'Сняг', ice: 'Заледена' };
  const TEXT = { clear: ['Слънчево', 'Ясно'], partly: ['Предимно ясно', 'Предимно ясно'], cloudy: ['Облачно', 'Облачно'], rain: ['Дъжд', 'Дъжд'], storm: ['Гръмотевици', 'Гръмотевици'], snow: ['Сняг', 'Сняг'], fog: ['Мъгла', 'Мъгла'] };
  const ICON = {
    fog: '<path d="M4 9h16M3 13h18M5 17h14M8 5h8"></path>',
    rain: '<path d="M17.5 15H8a5 5 0 1 1 1-9.9A6 6 0 0 1 20 7a4 4 0 0 1-2.5 8z"></path><path d="M8 19l-1 2M12 19l-1 2M16 19l-1 2"></path>',
    clear: '<circle cx="12" cy="12" r="4"></circle><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"></path>',
    partly: '<path d="M17.5 19H8a5 5 0 1 1 1-9.9A6 6 0 0 1 20 11a4 4 0 0 1-2.5 8z"></path>',
    night: '<path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z"></path>'
  };
  const icon = (h) => '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' +
    (h.state === 'clear' && h.isNight ? ICON.night : ICON[h.state] || ICON.partly) + '</svg>';

  // данните за избрания час; за текущия час — най-свежото измерване (current)
  const hourData = () => (!fc ? null : state.hour === fc.currentHour ? fc.current : fc.hours[state.hour]);
  const isNow = () => fc && state.hour === fc.currentHour;
  // станцията е полезна само ако е в района (до 50 км) и се гледа текущият час
  const road = () => (live && isNow() && live.distanceKm <= 50 ? live.station : null);

  const surfaceOf = (h) => {
    const r = road();
    if (r && r.surfaceC != null && r.surfaceC <= 0.5 && (r.humidityPct ?? h.humidityPct) >= 90) return 'ice';
    const wet = h.humidityPct >= 95 || h.precipitationMm > 0;
    return h.temperatureC <= 0.5 && wet ? 'ice' : h.state === 'snow' ? 'snow' : wet ? 'wet' : 'dry';
  };

  // ---------- чипове за скоростта ----------
  const GROUPS = {
    vehicle: [['Car', 'Лек автомобил'], ['Motorcycle', 'Мотоциклет'], ['Van', 'Бус до 3,5 т'], ['Truck', 'Камион'], ['Bus', 'Автобус']],
    road: [['Motorway', 'Автомагистрала'], ['OutsideTown', 'Извън населено място'], ['Town', 'Населено място']],
    surface: [['auto', 'Автоматично'], ['Dry', 'Суха'], ['Wet', 'Мокра'], ['Snow', 'Сняг'], ['Ice', 'Лед']]
  };
  function renderChips() {
    const h = hourData();
    document.querySelectorAll('[data-group]').forEach((box) => {
      const g = box.dataset.group;
      box.innerHTML = GROUPS[g].map(([v, l]) => {
        let label = l;
        if (g === 'surface' && v === 'auto' && h) label += ': ' + SURF[surfaceOf(h)].toLowerCase();
        return '<button type="button" class="chip press" data-v="' + v + '" aria-pressed="' + (state[g] === v) + '">' + RS.esc(label) + '</button>';
      }).join('');
    });
  }
  document.querySelectorAll('[data-group]').forEach((box) => box.addEventListener('click', (e) => {
    const b = e.target.closest('[data-v]'); if (!b) return;
    state[box.dataset.group] = b.dataset.v; renderChips(); loadSpeed();
  }));

  // ---------- widget ----------
  function renderWidget() {
    const w = $('widget'), box = $('wxBox'), h = hourData();
    if (!h) {
      w.dataset.state = 'clear'; w.dataset.night = '0';
      if (box) { box.dataset.state = 'clear'; box.dataset.night = '0'; }
      $('wTemp').textContent = '-'; $('wCond').textContent = '-'; $('wCity').textContent = '-'; $('wTime').textContent = '--:--';
      w.setAttribute('aria-label', 'Времето: няма данни');
      return;
    }
    const st = h.state, night = h.isNight, t = Math.round(h.temperatureC);
    w.dataset.state = st; w.dataset.night = night ? '1' : '0';
    if (box) { box.dataset.state = st; box.dataset.night = night ? '1' : '0'; }
    $('wTemp').textContent = t;
    $('wCond').textContent = TEXT[st][night ? 1 : 0];
    $('wCity').textContent = located && located.name ? located.name.split(' ')[0] : '-';
    $('wTimeLabel').textContent = isNow() ? 'Сега' : 'Прогноза';
    $('wTime').textContent = RS.pad(state.hour) + ':00';
    w.setAttribute('aria-label', 'Времето: ' + TEXT[st][night ? 1 : 0] + ', ' + t + ' градуса');
  }

  // ---------- метрики за избрания час ----------
  function renderNow() {
    const m = (label, value, note, bad) => '<div class="metric"><div class="metric__label">' + label + '</div><div class="metric__value">' + value + '</div><div class="metric__note' + (bad ? ' is-bad' : '') + '">' + note + '</div></div>';
    $('dateLabel').textContent = new Date().toLocaleDateString('bg-BG', { weekday: 'long', day: 'numeric', month: 'long' });
    const h = hourData(), box = $('liveBox');
    $('speedHour').textContent = h ? RS.pad(state.hour) + ':00' : '--:--';
    if (!h) {
      $('curHour').textContent = '--:--'; $('feels').textContent = '—';
      $('metrics').innerHTML = m('Видимост', '—', '—') + m('Настилка', '—', '—') + m('Влажност', '—', '—') + m('Валежи', '—', '—') + m('Вятър', '—', '—');
      $('warnings').innerHTML = '';
      box.textContent = located ? 'В момента няма метеоданни за района ти (източникът не отговаря).' : 'Няма метеоданни — определи местоположението си.';
      box.hidden = false;
      return;
    }
    $('curHour').textContent = RS.pad(state.hour) + ':00';
    const feels = Math.round((h.temperatureC - h.windKmh / 8) * 10) / 10;
    $('feels').textContent = 'усеща се като ' + fT(feels) + ' · мин ' + fT(fc.minTemperatureC) + ' / макс ' + fT(fc.maxTemperatureC);
    const surf = surfaceOf(h), r = road();
    $('metrics').innerHTML =
      m('Видимост', fV(h.visibilityM), h.visibilityM < 200 ? 'гъста мъгла' : h.visibilityM < 1000 ? 'намалена' : 'добра', h.visibilityM < 1000) +
      m('Настилка', SURF[surf], surf === 'ice' ? 'опасност от поледица' : surf === 'wet' ? 'по-дълъг спирачен път' : 'нормално сцепление', surf !== 'dry') +
      m('Влажност', h.humidityPct + '%', h.humidityPct >= 95 ? 'наситен въздух' : 'нормална', h.humidityPct >= 95) +
      m('Валежи', h.precipitationMm.toLocaleString('bg-BG', { minimumFractionDigits: 1 }) + ' мм', h.precipitationMm > 0 ? 'има валеж' : 'без валежи', h.precipitationMm > 0) +
      m('Вятър', h.windKmh + ' км/ч', 'пориви до ' + h.windGustKmh + ' км/ч', h.windGustKmh >= 60);
    $('warnings').innerHTML = (state.hour <= 10 ? fc.warnings : fc.warnings.filter((w) => !w.startsWith('Гъста'))).map((w) =>
      '<div class="warning" role="alert"><svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="#FF3B30" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" style="flex:none"><path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z"></path><path d="M12 9v4M12 17h.01"></path></svg><div><strong>Предупреждение</strong>' + RS.esc(w) + '</div></div>').join('');
    // измерване от пътя (bgtoll) — само за текущия час и ако станцията е наблизо
    if (r) {
      const at = r.measuredAtUtc ? new Date(r.measuredAtUtc).toLocaleTimeString('bg-BG', { hour: '2-digit', minute: '2-digit' }) : '—';
      box.textContent = 'Измерено от пътя в ' + at + ' · станция ' + r.name + ' (' + num(live.distanceKm) + ' км) · път ' + (r.surfaceC == null ? '—' : fT(r.surfaceC)) +
        ' · въздух ' + fT(r.airC) + ' · налягане ' + num(r.pressureHpa, 0) + ' hPa · bgtoll.bg';
      box.hidden = false;
    } else { box.hidden = true; }
  }

  function renderHours() {
    $('hours').innerHTML = !fc ? '' : fc.hours.map((h) => {
      const cls = h.visibilityM < 200 ? ' hour__vis--fog' : h.visibilityM < 1000 ? ' hour__vis--haze' : '';
      return '<button type="button" class="hour press" data-h="' + h.hour + '" aria-pressed="' + (h.hour === state.hour) + '" aria-label="' + RS.pad(h.hour) + ':00, ' + fT(h.temperatureC) + ', видимост ' + fV(h.visibilityM) + '">' +
        '<span class="mono small" style="font-weight:700">' + RS.pad(h.hour) + '</span>' + icon(h) + '<span class="hour__t">' + fT(h.temperatureC) + '</span><span class="hour__vis' + cls + '">' + fV(h.visibilityM) + '</span></button>';
    }).join('');
  }
  $('hours').addEventListener('click', (e) => {
    const b = e.target.closest('[data-h]'); if (!b) return;
    state.hour = +b.dataset.h;
    $('hours').querySelectorAll('.hour').forEach((x) => x.setAttribute('aria-pressed', String(x === b)));
    renderAll(false);
  });

  // ---------- скорост ----------
  async function loadSpeed() {
    const id = ++reqId, card = document.querySelector('.speed-card');
    if (!hourData() || !located) {
      ['recSpeed', 'recTitle', 'limitKmh', 'recKmh', 'visLabel', 'stopLimitLabel', 'stopRecLabel'].forEach((k) => { $(k).textContent = '—'; });
      $('recText').textContent = ''; $('multipliers').innerHTML = '';
      return;
    }
    card.classList.add('is-loading');
    try {
      const r = await RS.api('/api/speed', { lat: located.latitude, lon: located.longitude, hour: state.hour, vehicle: state.vehicle, road: state.road, surface: state.surface });
      if (id !== reqId) return;
      $('recSpeed').textContent = r.recommendedKmh;
      $('recTitle').textContent = r.title;
      $('recText').textContent = r.explanation;
      $('limitKmh').textContent = r.limitKmh; $('recKmh').textContent = r.recommendedKmh;
      const max = Math.max(r.stoppingAtLimitM, r.visibilityM, 60);
      const bar = (el, v, color) => { el.style.transform = 'scaleX(' + Math.min(1, v / max).toFixed(3) + ')'; el.style.background = color; };
      $('visLabel').textContent = fV(r.visibilityM); bar($('visBar'), r.visibilityM, '#7E9686');
      $('stopLimitLabel').textContent = r.stoppingAtLimitM + ' м'; bar($('stopLimitBar'), r.stoppingAtLimitM, r.stoppingAtLimitM > r.visibilityM ? '#FF3B30' : '#000');
      $('stopRecLabel').textContent = r.stoppingAtRecommendedM + ' м'; bar($('stopRecBar'), r.stoppingAtRecommendedM, '#000');
      $('multipliers').innerHTML = r.factors.map((f) => '<span>' + RS.esc(f.label) + ' × ' + f.multiplier.toLocaleString('bg-BG') + '</span>').join('');
    } catch (err) {
      if (id === reqId) RS.showError($('multipliers'), err);
    } finally {
      if (id === reqId) card.classList.remove('is-loading');
    }
  }

  function renderAll(withHours) {
    if (withHours !== false) renderHours();
    renderWidget(); renderNow(); renderChips(); loadSpeed();
  }

  // ---------- данни: опресняват се на 15 мин и при нова локация ----------
  async function loadData(firstLoad) {
    if (!located) return;
    const l = located;
    const [f, v] = await Promise.all([
      RS.api('/api/weather/forecast', { lat: l.latitude, lon: l.longitude }).catch(() => undefined),
      RS.api('/api/weather/live', { lat: l.latitude, lon: l.longitude }).catch(() => undefined)
    ]);
    if (l !== located) return; // междувременно е сменено местоположението
    if (f !== undefined) fc = f; // при грешка пазим последните данни
    if (v !== undefined) live = v && v.station && v.station.airC != null ? v : null;
    if (fc && (firstLoad || state.hour > 23)) state.hour = fc.currentHour;
    renderAll();
    if (firstLoad && fc) {
      const cur = $('hours').querySelector('[aria-pressed="true"]'), sc = document.querySelector('.hours-scroll');
      if (cur && sc) sc.scrollLeft = cur.offsetLeft - sc.clientWidth / 2 + cur.offsetWidth / 2;
    }
  }
  setInterval(() => loadData(false), 15 * 60 * 1000);

  // ---------- местоположение: GPS, а ако е отказано / няма — по мрежата (Wi-Fi / мобилни данни) ----------
  const SOURCE = { gps: 'Точно местоположение · GPS', network: 'По мрежата · Wi-Fi / мобилни данни' };
  function clearLocation(note) {
    located = null; fc = null; live = null;
    $('locSource').textContent = 'Местоположение неизвестно';
    $('locStatus').textContent = '';
    $('locName').textContent = '-'; $('locCoords').textContent = '-'; $('locRoad').textContent = '-';
    const n = $('gpsNote'); n.hidden = !note; n.textContent = note || '';
    $('gpsBtn').setAttribute('aria-pressed', 'false');
    renderAll();
  }
  function showLocation(r, note) {
    const l = r.location;
    $('locSource').textContent = SOURCE[r.source];
    $('locStatus').textContent = '';
    $('locName').textContent = l.name;
    $('locCoords').textContent = l.latitude.toFixed(2) + ' ° с.ш. · ' + l.longitude.toFixed(2) + ' ° и.д. · точност ±' + l.accuracyKm + ' км' + (r.network ? ' · ' + r.network : '');
    $('locRoad').textContent = l.nearRoad;
    const n = $('gpsNote'); n.hidden = !note; n.textContent = note || '';
    $('gpsBtn').setAttribute('aria-pressed', String(r.source === 'gps'));
    located = l;
    loadData(true);
  }
  async function byNetwork(reason) {
    $('locStatus').textContent = 'по мрежата…';
    try {
      const r = await RS.api('/api/location/network');
      // „default“ означава, че мрежата не издаде местоположение — не показваме измислен район
      if (r.source !== 'network') return clearLocation(reason + ' Не успяхме да определим местоположението ти по мрежата.');
      showLocation(r, reason + ' Показваме района по IP адреса на мрежата — точността е ±' + r.location.accuracyKm + ' км.');
    } catch (err) { clearLocation(reason + ' Не успяхме да определим местоположението ти по мрежата.'); }
  }
  // състоянието на разрешението: 'granted' | 'prompt' | 'denied' (без Permissions API — 'prompt')
  async function geoPermission() {
    try { return (await navigator.permissions.query({ name: 'geolocation' })).state; } catch (_) { return 'prompt'; }
  }
  async function byGps(userAsked) {
    if (!navigator.geolocation) return byNetwork('Браузърът не поддържа GPS.');
    const perm = await geoPermission();
    if (perm === 'denied') return byNetwork(userAsked ? 'Достъпът до местоположението е отказан — разреши го от настройките на браузъра.' : 'Местоположението не е разрешено.');
    if (perm === 'granted') {
      $('locStatus').textContent = 'определяне на местоположението…';
    } else {
      // браузърът ще покаже въпрос — само тогава искаме разрешение
      $('locStatus').textContent = 'разреши достъп до местоположението…';
      const n = $('gpsNote'); n.hidden = false;
      n.textContent = 'Разреши услугите за местоположение, за да покажем времето около теб. Ако откажеш, ще ползваме мрежата ти (Wi-Fi / мобилни данни).';
    }
    navigator.geolocation.getCurrentPosition(
      async (p) => {
        try { showLocation(await RS.api('/api/location/gps', { lat: p.coords.latitude, lon: p.coords.longitude, accuracy: Math.round(p.coords.accuracy) })); }
        catch (err) { byNetwork('GPS координатите не можаха да се обработят.'); }
      },
      (e) => byNetwork(e.code === 1 ? (userAsked ? 'Достъпът до местоположението е отказан — разреши го от настройките на браузъра.' : 'Местоположението не е разрешено.') : 'GPS не отговори навреме.'),
      { enableHighAccuracy: true, timeout: 15000, maximumAge: 300000 });
  }
  $('gpsBtn').addEventListener('click', () => byGps(true));

  // ---------- старт: тирета и искане на местоположение ----------
  clearLocation();
  byGps(false);
})();

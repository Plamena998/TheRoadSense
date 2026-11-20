/* Статистика: данни по път (/api/stats/road/{id}) и черен календар (/api/stats/calendar) */
(function () {
  'use strict';
  const RS = window.RS;
  const $ = (id) => document.getElementById(id);
  const init = JSON.parse(document.getElementById('stats-init').textContent);
  const state = { road: init.road, period: 'all', year: init.year, month: init.month };
  const MONTHS_GEN = ['януари', 'февруари', 'март', 'април', 'май', 'юни', 'юли', 'август', 'септември', 'октомври', 'ноември', 'декември'];
  let reqId = 0;

  // ---------- избор на път и период ----------
  const press = (box, attr, value) => box.querySelectorAll('[' + attr + ']').forEach((b) => b.setAttribute('aria-pressed', String(b.getAttribute(attr) === value)));
  $('roads').addEventListener('click', (e) => { const b = e.target.closest('[data-road]'); if (!b) return; state.road = b.dataset.road; press($('roads'), 'data-road', state.road); loadRoad(); });
  $('periods').addEventListener('click', (e) => { const b = e.target.closest('[data-period]'); if (!b) return; state.period = b.dataset.period; press($('periods'), 'data-period', state.period); loadRoad(); });

  const vbar = (v, max, color, radius) => '<span><span class="bar" style="border-radius:' + (radius || '2px 2px 0 0') + ';transform:scaleY(' + Math.max(0.04, max ? v / max : 0).toFixed(3) + ');background:' + color + '"></span></span>';

  async function loadRoad() {
    const id = ++reqId;
    $('kpis').classList.add('is-loading');
    try {
      const d = await RS.api('/api/stats/road/' + encodeURIComponent(state.road), { period: state.period });
      if (id !== reqId) return;
      $('roadName').textContent = d.fullName;
      $('kDeaths').textContent = d.deaths;
      $('kDeathsSub').textContent = d.periodLabel + ' · ' + RS.num(d.crashes) + ' тежки ПТП';

      const top = d.regions[0];
      $('kRegion').textContent = top ? top.name : '—';
      $('kRegions').innerHTML = d.regions.slice(0, 3).map((r) =>
        '<div class="kpi__row"><span>' + RS.esc(r.name) + '</span><span class="track"><span class="bar-h" style="background:var(--red);transform:scaleX(' + (r.value / top.value).toFixed(3) + ')"></span></span><span class="mono" style="text-align:right">' + r.value + '</span></div>').join('');

      const mMax = Math.max(...d.months.map((m) => m.value));
      const tm = d.months[d.topMonthIndex];
      $('kMonth').textContent = tm.name.charAt(0).toUpperCase() + tm.name.slice(1);
      $('kMonths').innerHTML = d.months.map((m, i) => vbar(m.value, mMax, i === d.topMonthIndex ? '#FF3B30' : '#7A1712')).join('');
      $('kMonthSub').textContent = tm.value + ' загинали през ' + tm.name + (d.topMonthNote ? ' · ' + d.topMonthNote : '');

      const hMax = Math.max(...d.hours.map((h) => h.value));
      $('kHour').textContent = RS.pad(d.topHour) + ':00–' + RS.pad(d.topHour + 1) + ':00';
      $('kHours').innerHTML = d.hours.map((h, i) => vbar(h.value, hMax, i === d.topHour ? '#FF3B30' : '#7A1712')).join('');
      $('kHourSub').textContent = Math.round(d.hours[d.topHour].value) + ' загинали в този час · вечерният пик';

      $('bins').innerHTML = d.bins.map((b) => '<span title="км ' + b.fromKm + '–' + b.toKm + ': ' + b.crashes + ' тежки ПТП">' +
        '<span class="bar" style="transform:scaleY(' + b.relative + ');background:' + (b.isBlackSpot ? '#FF3B30' : '#39453C') + '"></span></span>').join('');
      $('midKm').textContent = 'км ' + Math.round(d.lengthKm / 2);
      $('endKm').textContent = 'км ' + d.lengthKm;
      $('spots').innerHTML = d.blackSpots.map((s) => '<span class="spot"><i></i><strong class="mono">км ' + s.km + '</strong><span style="color:var(--text-2)">' + RS.esc(s.description) + '</span></span>').join('');
      $('causes').innerHTML = d.causes.map((c) => '<div class="cause"><div class="cause__row"><span>' + RS.esc(c.label) + '</span><span class="mono" style="font-weight:700">' + c.percent + '%</span></div>' +
        '<div class="track"><span class="bar-h" style="transform:scaleX(' + (c.percent / 40).toFixed(3) + ');background:' + (c.label === 'Лоша видимост и време' ? '#D4F2DB' : '#FF3B30') + '"></span></div></div>').join('');
    } catch (err) {
      if (id === reqId) RS.showError($('causes'), err);
    } finally {
      if (id === reqId) $('kpis').classList.remove('is-loading');
    }
  }

  // ---------- черен календар ----------
  $('prevMonth').addEventListener('click', () => { state.month--; if (state.month < 1) { state.month = 12; state.year--; } loadCalendar(); });
  $('nextMonth').addEventListener('click', () => { if ($('nextMonth').disabled) return; state.month++; if (state.month > 12) { state.month = 1; state.year++; } loadCalendar(); });

  async function loadCalendar() {
    try {
      const c = await RS.api('/api/stats/calendar', { year: state.year, month: state.month });
      state.year = c.year; state.month = c.month;
      $('nextMonth').disabled = !c.canGoNext;
      $('nextMonth').setAttribute('aria-label', c.canGoNext ? 'Следващ месец' : 'Следващият месец още не е дошъл');
      const label = $('monthLabel');
      label.textContent = c.name;
      label.classList.toggle('has-events', c.hasEvents);
      let html = '<div class="day day--empty"></div>'.repeat(c.leadingBlanks);
      c.days.forEach((d) => {
        const ev = d.event;
        const cls = 'day' + (ev ? ' day--event' : '') + (d.isToday ? ' day--today' : '') + (d.weekday >= 5 ? ' day--weekend' : '');
        const align = d.weekday <= 1 ? 'left:-6px' : d.weekday >= 5 ? 'right:-6px' : 'left:50%;margin-left:-130px';
        html += '<div class="' + cls + '"><div class="day__top"><span class="day__n">' + d.day + '</span>' +
          (d.isToday ? '<span class="day__today">ДНЕС</span>' : '') +
          (ev ? '<span class="day__dead">† ' + ev.deaths + '</span>' : '') + '</div>' +
          (ev ? '<div class="day__info"><span class="tipwrap"><button type="button" class="ibtn" aria-label="Подробности за ' + d.day + ' ' + MONTHS_GEN[c.month - 1] + '" aria-describedby="tip-' + d.day + '">' +
            '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6" stroke-linecap="round" aria-hidden="true"><path d="M12 11v6"></path><path d="M12 7h.01"></path></svg></button>' +
            '<span role="tooltip" id="tip-' + d.day + '" class="tip" style="' + align + '"><span class="tip__date">' + d.day + ' ' + MONTHS_GEN[c.month - 1].toUpperCase() + ' ' + c.year + ' · † ' + ev.deaths + ' ЗАГИНАЛИ</span>' +
            '<span class="tip__text">' + RS.esc(ev.description) + '</span><span class="tip__src">Източник: ' + RS.esc(ev.source) + '</span></span></span></div>' : '') +
          '</div>';
      });
      const tail = (7 - ((c.leadingBlanks + c.days.length) % 7)) % 7;
      html += '<div class="day day--empty"></div>'.repeat(tail);
      $('calendar').innerHTML = html;
      $('calNote').textContent = c.hasEvents ? 'Записи за месеца: ' + c.days.filter((d) => d.event).length : 'Няма записи за този месец.';
    } catch (err) { RS.showError($('calendar'), err); }
  }

  loadRoad();
  loadCalendar();
})();

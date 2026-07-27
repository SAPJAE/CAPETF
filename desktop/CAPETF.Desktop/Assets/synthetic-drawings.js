(function (global) {
  'use strict';

  const RECORD_TYPES = new Set([
    'trend', 'ray', 'hline', 'vline', 'fib', 'rectangle', 'brush', 'text', 'measure'
  ]);
  const TWO_POINT_TYPES = new Set(['trend', 'ray', 'fib', 'rectangle', 'measure']);
  const HISTORY_LIMIT = 100;
  const MAX_TEXT_LENGTH = 500;
  const HIT_DISTANCE = 8;
  const STORAGE_PREFIX = 'capcom-terminal-drawings:';
  const INTERACTIVE_TAGS = new Set(['A', 'BUTTON', 'INPUT', 'LABEL', 'OPTION', 'SELECT', 'TEXTAREA']);
  const DEFAULT_STYLE = Object.freeze({
    color: '#38bdf8',
    fillColor: 'rgba(56, 189, 248, 0.12)',
    lineWidth: 2,
    lineStyle: 'solid'
  });
  let nextId = 0;

  function isObject(value) {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
  }

  function isJsonSafe(value, seen) {
    if (value === null || typeof value === 'string' || typeof value === 'boolean') return true;
    if (typeof value === 'number') return Number.isFinite(value);
    if (typeof value !== 'object') return false;
    if (seen.has(value)) return false;
    seen.add(value);
    const values = Array.isArray(value) ? value : Object.values(value);
    const safe = values.every(item => isJsonSafe(item, seen));
    seen.delete(value);
    return safe;
  }

  function cloneJson(value) {
    return JSON.parse(JSON.stringify(value));
  }

  function normalizeTimestamp(time) {
    if (typeof time === 'number' && Number.isFinite(time)) {
      return Math.abs(time) < 100000000000 ? time * 1000 : time;
    }
    if (typeof time === 'string' && time.length <= 100) {
      const parsed = Date.parse(time);
      return Number.isFinite(parsed) ? parsed : null;
    }
    if (isObject(time)) {
      const year = Number(time.year);
      const month = Number(time.month);
      const day = Number(time.day);
      if (Number.isInteger(year) && Number.isInteger(month) && Number.isInteger(day) &&
          year >= 1 && month >= 1 && month <= 12 && day >= 1 && day <= 31) {
        const date = new Date(0);
        date.setUTCFullYear(year, month - 1, day);
        if (date.getUTCFullYear() === year && date.getUTCMonth() === month - 1 && date.getUTCDate() === day) {
          return date.getTime();
        }
      }
    }
    return null;
  }

  function sanitizeTime(time) {
    if (normalizeTimestamp(time) === null) return null;
    if (typeof time === 'number' || typeof time === 'string') return time;
    return { year: Number(time.year), month: Number(time.month), day: Number(time.day) };
  }

  function sanitizePoint(point) {
    if (!isObject(point)) return null;
    const time = sanitizeTime(point.time);
    const price = Number(point.price);
    if (time === null || !Number.isFinite(price)) return null;
    return { time, price };
  }

  function boundedString(value, fallback, maximum) {
    if (typeof value !== 'string') return fallback;
    const trimmed = value.trim();
    if (!trimmed) return fallback;
    return trimmed.slice(0, maximum);
  }

  function sanitizeStyle(style) {
    const source = isObject(style) ? style : {};
    const lineWidth = Number(source.lineWidth);
    const lineStyle = ['solid', 'dashed', 'dotted'].includes(source.lineStyle)
      ? source.lineStyle
      : DEFAULT_STYLE.lineStyle;
    return {
      color: boundedString(source.color, DEFAULT_STYLE.color, 64),
      fillColor: boundedString(source.fillColor, DEFAULT_STYLE.fillColor, 96),
      lineWidth: Number.isFinite(lineWidth) ? Math.min(8, Math.max(1, lineWidth)) : DEFAULT_STYLE.lineWidth,
      lineStyle
    };
  }

  function sanitizeRecord(record, truncateText) {
    if (!isObject(record) || !isJsonSafe(record, new Set())) return null;
    const id = boundedString(record.id, '', 128);
    if (!id || !RECORD_TYPES.has(record.type)) return null;

    const result = {
      id,
      type: record.type,
      style: sanitizeStyle(record.style),
      visible: record.visible !== false,
      locked: record.locked === true
    };

    if (TWO_POINT_TYPES.has(record.type)) {
      result.p1 = sanitizePoint(record.p1);
      result.p2 = sanitizePoint(record.p2);
      if (!result.p1 || !result.p2) return null;
    } else if (record.type === 'hline') {
      result.price = Number(record.price);
      if (!Number.isFinite(result.price)) return null;
    } else if (record.type === 'vline') {
      result.time = sanitizeTime(record.time);
      if (result.time === null) return null;
    } else if (record.type === 'brush') {
      if (!Array.isArray(record.points)) return null;
      result.points = record.points.map(sanitizePoint);
      if (result.points.length < 2 || result.points.length > 5000 || result.points.some(point => !point)) return null;
    } else if (record.type === 'text') {
      result.point = sanitizePoint(record.point);
      if (!result.point || typeof record.text !== 'string' || !record.text.trim()) return null;
      if (!truncateText && record.text.trim().length > MAX_TEXT_LENGTH) return null;
      result.text = record.text.trim().slice(0, MAX_TEXT_LENGTH);
    }

    return result;
  }

  function validateRecord(record) {
    try {
      return sanitizeRecord(record, false) !== null;
    } catch (_) {
      return false;
    }
  }

  function sanitizeRecords(records) {
    if (!Array.isArray(records)) return [];
    const sanitized = [];
    for (const record of records) {
      try {
        const clean = sanitizeRecord(record, true);
        if (clean) sanitized.push(clean);
      } catch (_) {
        // A malformed persisted record must not block the remaining drawings.
      }
    }
    return sanitized;
  }

  function calculateMeasurement(start, end, orderedTimes) {
    const startPoint = sanitizePoint(start);
    const endPoint = sanitizePoint(end);
    if (!startPoint || !endPoint) return null;

    const startMs = normalizeTimestamp(startPoint.time);
    const endMs = normalizeTimestamp(endPoint.time);
    const lower = Math.min(startMs, endMs);
    const upper = Math.max(startMs, endMs);
    const normalizedTimes = new Set();
    for (const time of Array.isArray(orderedTimes) ? orderedTimes : []) {
      const normalized = normalizeTimestamp(time);
      if (normalized !== null && normalized >= lower && normalized <= upper) normalizedTimes.add(normalized);
    }

    const priceDelta = endPoint.price - startPoint.price;
    return {
      startPrice: startPoint.price,
      endPrice: endPoint.price,
      priceDelta,
      percentDelta: startPoint.price === 0 ? null : priceDelta / startPoint.price * 100,
      bars: normalizedTimes.size,
      elapsedMs: endMs - startMs
    };
  }

  function conciseAmount(value) {
    return Number.isInteger(value) ? String(value) : value.toFixed(1).replace(/\.0$/, '');
  }

  function formatElapsed(milliseconds) {
    const absolute = Math.abs(Number(milliseconds));
    if (!Number.isFinite(absolute)) return 'n/a';
    if (absolute >= 86400000) return `${conciseAmount(absolute / 86400000)}d`;
    if (absolute >= 3600000) return `${conciseAmount(absolute / 3600000)}h`;
    if (absolute >= 60000) return `${conciseAmount(absolute / 60000)}m`;
    return `${Math.round(absolute / 1000)}s`;
  }

  function formatMeasurement(measurement, precision) {
    if (!isObject(measurement)) return { label: 'Measurement unavailable', tone: 'neutral' };
    const digits = Number.isInteger(precision) ? Math.max(0, Math.min(10, precision)) : 5;
    const start = Number(measurement.startPrice);
    const end = Number(measurement.endPrice);
    const delta = Number(measurement.priceDelta);
    const percent = measurement.percentDelta === null ? null : Number(measurement.percentDelta);
    const bars = Math.max(0, Math.trunc(Number(measurement.bars) || 0));
    if (![start, end, delta].every(Number.isFinite)) {
      return { label: 'Measurement unavailable', tone: 'neutral' };
    }
    const signedDelta = `${delta >= 0 ? '+' : ''}${delta.toFixed(digits)}`;
    const percentLabel = Number.isFinite(percent)
      ? `${percent >= 0 ? '+' : ''}${percent.toFixed(2)}%`
      : 'n/a';
    const tone = !Number.isFinite(percent) || delta === 0 ? 'neutral' : delta > 0 ? 'positive' : 'negative';
    return {
      label: `${start.toFixed(digits)} -> ${end.toFixed(digits)}  ${signedDelta} (${percentLabel})  ${bars} ${bars === 1 ? 'bar' : 'bars'}  ${formatElapsed(measurement.elapsedMs)}`,
      tone
    };
  }

  function drawingStorageKey(identity) {
    const stableIdentity = typeof identity === 'string' ? identity.trim() : '';
    return stableIdentity ? `${STORAGE_PREFIX}${stableIdentity}` : '';
  }

  function loadStoredRecords(storage, identity) {
    const key = drawingStorageKey(identity);
    if (!key || !storage || typeof storage.getItem !== 'function') return [];
    try {
      return sanitizeRecords(JSON.parse(storage.getItem(key) || '[]'));
    } catch (_) {
      return [];
    }
  }

  function persistStoredRecords(storage, identity, records) {
    const key = drawingStorageKey(identity);
    if (!key || !storage || typeof storage.setItem !== 'function') return false;
    try {
      storage.setItem(key, JSON.stringify(sanitizeRecords(records)));
      return true;
    } catch (_) {
      return false;
    }
  }

  function confirmClear(manager, confirmAction) {
    if (!manager || typeof manager.getRecords !== 'function' || typeof manager.clear !== 'function') return false;
    if (manager.getRecords().length === 0) return false;
    if (typeof confirmAction !== 'function' || confirmAction() !== true) return false;
    return manager.clear();
  }

  function normalizeAnnotationText(value) {
    if (typeof value !== 'string') return null;
    const text = value.trim().slice(0, MAX_TEXT_LENGTH);
    return text || null;
  }

  function switchDrawingIdentity(manager, storage, currentIdentity, nextIdentity) {
    const current = typeof currentIdentity === 'string' ? currentIdentity : '';
    const next = typeof nextIdentity === 'string' ? nextIdentity : '';
    if (!manager || current === next) return current;
    manager.cancel();
    manager.setRecords(loadStoredRecords(storage, next), { emit: false });
    return next;
  }

  function createTextDialogController(options) {
    const overlay = options && options.overlay;
    const form = options && options.form;
    const input = options && options.input;
    const cancelButton = options && options.cancelButton;
    if (!overlay || !form || !input || !cancelButton) {
      throw new Error('createTextDialogController requires overlay, form, input, and cancelButton');
    }

    let returnFocus = null;
    const ownerDocument = overlay.ownerDocument || global.document;

    function focusableElements() {
      if (typeof overlay.querySelectorAll !== 'function') return [input, cancelButton];
      return Array.from(overlay.querySelectorAll(
        'button, input, select, textarea, a[href], [tabindex]:not([tabindex="-1"])'))
        .filter(element => !element.disabled &&
          !(typeof element.hasAttribute === 'function' && element.hasAttribute('hidden')));
    }

    function restoreFocus() {
      const target = returnFocus;
      returnFocus = null;
      if (target && typeof target.focus === 'function') target.focus();
    }

    function hide(cancelled) {
      overlay.classList.add('hidden');
      input.value = '';
      try {
        if (cancelled && typeof options.onCancel === 'function') options.onCancel();
      } finally {
        restoreFocus();
      }
    }

    function onSubmit(event) {
      event.preventDefault && event.preventDefault();
      const text = normalizeAnnotationText(input.value);
      if (!text) return;
      overlay.classList.add('hidden');
      input.value = '';
      try {
        if (typeof options.onSubmit === 'function') options.onSubmit(text);
      } finally {
        restoreFocus();
      }
    }

    function onCancel(event) {
      event && event.preventDefault && event.preventDefault();
      hide(true);
    }

    function onKeyDown(event) {
      if (event.key === 'Escape') {
        event.preventDefault && event.preventDefault();
        event.stopPropagation && event.stopPropagation();
        hide(true);
        return;
      }
      if (event.key !== 'Tab') return;
      const focusable = focusableElements();
      if (focusable.length === 0) {
        event.preventDefault && event.preventDefault();
        return;
      }
      const active = ownerDocument && ownerDocument.activeElement;
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      const outside = !active || typeof overlay.contains !== 'function' || !overlay.contains(active);
      if (event.shiftKey && (active === first || outside)) {
        event.preventDefault && event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && (active === last || outside)) {
        event.preventDefault && event.preventDefault();
        first.focus();
      }
    }

    form.addEventListener('submit', onSubmit);
    cancelButton.addEventListener('click', onCancel);
    overlay.addEventListener('keydown', onKeyDown);

    return Object.freeze({
      open(focusTarget) {
        returnFocus = focusTarget || (ownerDocument && ownerDocument.activeElement) || null;
        overlay.classList.remove('hidden');
        input.value = '';
        input.focus();
      },
      close() {
        hide(true);
      },
      dispose() {
        form.removeEventListener('submit', onSubmit);
        cancelButton.removeEventListener('click', onCancel);
        overlay.removeEventListener('keydown', onKeyDown);
      }
    });
  }

  function uniqueId() {
    if (global.crypto && typeof global.crypto.randomUUID === 'function') return global.crypto.randomUUID();
    nextId += 1;
    return `drawing-${Date.now().toString(36)}-${nextId.toString(36)}`;
  }

  function lineDash(style, ratio) {
    if (style.lineStyle === 'dashed') return [7 * ratio, 5 * ratio];
    if (style.lineStyle === 'dotted') return [2 * ratio, 4 * ratio];
    return [];
  }

  function distanceToSegment(point, start, end) {
    const dx = end.x - start.x;
    const dy = end.y - start.y;
    if (dx === 0 && dy === 0) return Math.hypot(point.x - start.x, point.y - start.y);
    const amount = Math.max(0, Math.min(1,
      ((point.x - start.x) * dx + (point.y - start.y) * dy) / (dx * dx + dy * dy)));
    return Math.hypot(point.x - (start.x + amount * dx), point.y - (start.y + amount * dy));
  }

  function buildTimeIndex(orderedTimes) {
    const entries = [];
    for (const time of Array.isArray(orderedTimes) ? orderedTimes : []) {
      const normalized = normalizeTimestamp(time);
      if (normalized !== null) entries.push({ time, normalized });
    }
    entries.sort((left, right) => left.normalized - right.normalized);
    return entries.filter((entry, index) => index === 0 || entry.normalized !== entries[index - 1].normalized);
  }

  function nearestTimeEntry(timeIndex, target) {
    if (!Array.isArray(timeIndex) || timeIndex.length === 0 || target === null) return null;
    let low = 0;
    let high = timeIndex.length;
    while (low < high) {
      const middle = low + Math.floor((high - low) / 2);
      if (timeIndex[middle].normalized < target) low = middle + 1;
      else high = middle;
    }
    if (low === 0) return timeIndex[0];
    if (low === timeIndex.length) return timeIndex[timeIndex.length - 1];
    const before = timeIndex[low - 1];
    const after = timeIndex[low];
    return target - before.normalized <= after.normalized - target ? before : after;
  }

  function timeCoordinate(state, time) {
    const timeScale = state.chart.timeScale();
    const direct = timeScale.timeToCoordinate(time);
    if (direct !== null && Number.isFinite(direct)) return direct;
    const target = normalizeTimestamp(time);
    const nearest = nearestTimeEntry(state.timeIndex, target);
    if (!nearest) return null;
    const coordinate = timeScale.timeToCoordinate(nearest.time);
    return coordinate !== null && Number.isFinite(coordinate) ? coordinate : null;
  }

  function pointCoordinates(state, point) {
    if (!point) return null;
    const x = timeCoordinate(state, point.time);
    const y = state.series.priceToCoordinate(point.price);
    return x === null || y === null || !Number.isFinite(x) || !Number.isFinite(y) ? null : { x, y };
  }

  function recordAnchors(state, record, width, height) {
    if (record.type === 'hline') {
      const y = state.series.priceToCoordinate(record.price);
      return y === null ? [] : [{ name: 'price', x: width / 2, y }];
    }
    if (record.type === 'vline') {
      const x = timeCoordinate(state, record.time);
      return x === null ? [] : [{ name: 'time', x, y: height / 2 }];
    }
    if (record.type === 'brush') {
      return record.points.map((point, index) => {
        const coordinate = pointCoordinates(state, point);
        return coordinate ? { name: `points:${index}`, ...coordinate } : null;
      }).filter(Boolean);
    }
    if (record.type === 'text') {
      const coordinate = pointCoordinates(state, record.point);
      return coordinate ? [{ name: 'point', ...coordinate }] : [];
    }
    const first = pointCoordinates(state, record.p1);
    const second = pointCoordinates(state, record.p2);
    return [first && { name: 'p1', ...first }, second && { name: 'p2', ...second }].filter(Boolean);
  }

  function renderRecords(state) {
    const records = state.records.filter(record => state.visible && record.visible);
    if (state.pending && state.hoverPoint) {
      records.push({
        id: '__preview__',
        type: state.tool,
        p1: state.pending,
        p2: state.hoverPoint,
        style: state.style,
        visible: true,
        locked: true,
        ...calculateMeasurement(state.pending, state.hoverPoint, state.orderedTimes)
      });
    }
    if (state.brushDraft && state.brushDraft.length > 1) {
      records.push({
        id: '__preview__', type: 'brush', points: state.brushDraft,
        style: state.style, visible: true, locked: true
      });
    }
    return records;
  }

  class DrawingRenderer {
    constructor(state) {
      this.state = state;
    }

    draw(target) {
      target.useBitmapCoordinateSpace(scope => {
        const state = this.state;
        const context = scope.context;
        const horizontalPixelRatio = scope.horizontalPixelRatio;
        const verticalPixelRatio = scope.verticalPixelRatio;
        const logicalWidth = scope.bitmapSize.width / horizontalPixelRatio;
        const logicalHeight = scope.bitmapSize.height / verticalPixelRatio;

        context.save();
        for (const record of renderRecords(state)) {
          this.drawRecord(context, record, logicalWidth, logicalHeight, horizontalPixelRatio, verticalPixelRatio);
        }
        context.restore();
      });
    }

    drawRecord(context, record, width, height, horizontalRatio, verticalRatio) {
      const state = this.state;
      const style = sanitizeStyle(record.style);
      const x = value => Math.round(value * horizontalRatio);
      const y = value => Math.round(value * verticalRatio);
      const setStroke = () => {
        context.strokeStyle = style.color;
        context.lineWidth = Math.max(1, style.lineWidth * verticalRatio);
        context.setLineDash(lineDash(style, horizontalRatio));
      };
      const segment = (start, end) => {
        const pixelRatio = Math.abs(end.x - start.x) < 0.0001 ? horizontalRatio : verticalRatio;
        context.lineWidth = Math.max(1, style.lineWidth * pixelRatio);
        context.beginPath();
        context.moveTo(x(start.x), y(start.y));
        context.lineTo(x(end.x), y(end.y));
        context.stroke();
      };
      const anchors = recordAnchors(state, record, width, height);

      context.save();
      setStroke();
      if (record.type === 'hline' && anchors[0]) {
        segment({ x: 0, y: anchors[0].y }, { x: width, y: anchors[0].y });
      } else if (record.type === 'vline' && anchors[0]) {
        segment({ x: anchors[0].x, y: 0 }, { x: anchors[0].x, y: height });
      } else if (record.type === 'brush' && anchors.length > 1) {
        context.beginPath();
        context.moveTo(x(anchors[0].x), y(anchors[0].y));
        for (let index = 1; index < anchors.length; index += 1) {
          context.lineTo(x(anchors[index].x), y(anchors[index].y));
        }
        context.stroke();
      } else if (record.type === 'text' && anchors[0]) {
        context.setLineDash([]);
        context.fillStyle = style.color;
        context.font = `${Math.round(12 * verticalRatio)}px sans-serif`;
        context.textBaseline = 'bottom';
        context.fillText(record.text, x(anchors[0].x + 5), y(anchors[0].y - 5));
      } else if (anchors.length === 2) {
        const first = anchors[0];
        const second = anchors[1];
        if (record.type === 'trend') {
          segment(first, second);
        } else if (record.type === 'ray') {
          const deltaX = second.x - first.x;
          if (deltaX !== 0) {
            const endX = deltaX > 0 ? width : 0;
            const endY = first.y + (second.y - first.y) * ((endX - first.x) / deltaX);
            segment(first, { x: endX, y: endY });
          } else if (second.y !== first.y) {
            segment(first, { x: first.x, y: second.y > first.y ? height : 0 });
          }
        } else if (record.type === 'rectangle' || record.type === 'measure') {
          const left = Math.min(first.x, second.x);
          const top = Math.min(first.y, second.y);
          const boxWidth = Math.abs(second.x - first.x);
          const boxHeight = Math.abs(second.y - first.y);
          const measurementTone = record.type === 'measure'
            ? formatMeasurement(record, state.pricePrecision).tone
            : null;
          context.fillStyle = record.type === 'measure'
            ? measurementTone === 'positive'
              ? 'rgba(34, 197, 94, 0.13)'
              : measurementTone === 'negative'
                ? 'rgba(239, 68, 68, 0.13)'
                : 'rgba(148, 163, 184, 0.12)'
            : style.fillColor;
          context.fillRect(x(left), y(top), Math.round(boxWidth * horizontalRatio), Math.round(boxHeight * verticalRatio));
          context.strokeRect(x(left), y(top), Math.round(boxWidth * horizontalRatio), Math.round(boxHeight * verticalRatio));
          if (record.type === 'measure') {
            this.drawMeasurementLabel(context, record, left, top, width, height, horizontalRatio, verticalRatio);
          }
        } else if (record.type === 'fib') {
          for (const level of [0, 0.236, 0.382, 0.5, 0.618, 0.786, 1]) {
            const levelY = first.y + (second.y - first.y) * level;
            segment({ x: Math.min(first.x, second.x), y: levelY }, { x: Math.max(first.x, second.x), y: levelY });
            context.setLineDash([]);
            context.fillStyle = style.color;
            context.font = `${Math.round(10 * verticalRatio)}px sans-serif`;
            context.fillText(`${(level * 100).toFixed(1)}%`, x(Math.max(first.x, second.x) + 4), y(levelY - 2));
            context.setLineDash(lineDash(style, horizontalRatio));
          }
        }
      }

      if (record.id === state.selectedId && record.id !== '__preview__') {
        context.setLineDash([]);
        for (const anchor of anchors) {
          context.beginPath();
          context.fillStyle = '#f8fafc';
          context.strokeStyle = style.color;
          context.lineWidth = Math.max(1, verticalRatio);
          context.arc(x(anchor.x), y(anchor.y), Math.max(4, 4 * verticalRatio), 0, Math.PI * 2);
          context.fill();
          context.stroke();
        }
      }
      context.restore();
    }

    drawMeasurementLabel(context, record, left, top, width, height, horizontalRatio, verticalRatio) {
      const measurement = formatMeasurement(record, this.state.pricePrecision);
      const parts = measurement.label.split('  ');
      const lines = parts.length === 4
        ? [`${parts[0]}  ${parts[1]}`, `${parts[2]}  ${parts[3]}`]
        : [measurement.label];
      context.setLineDash([]);
      const padding = 5 * horizontalRatio;
      const maxWidth = width * horizontalRatio;
      const maxHeight = height * verticalRatio;
      let fontSize = 11 * verticalRatio;
      context.font = `${Math.round(fontSize)}px sans-serif`;
      let textWidths = lines.map(line => context.measureText(line).width);
      const availableTextWidth = Math.max(1, maxWidth - padding * 2);
      const widest = Math.max(...textWidths);
      if (widest > availableTextWidth) {
        fontSize = Math.max(7 * verticalRatio, fontSize * availableTextWidth / widest);
        context.font = `${Math.round(fontSize)}px sans-serif`;
        textWidths = lines.map(line => context.measureText(line).width);
      }
      const labelWidth = Math.min(maxWidth, Math.max(...textWidths) + padding * 2);
      const lineHeight = Math.ceil(fontSize + 3 * verticalRatio);
      const labelHeight = Math.min(maxHeight, lineHeight * lines.length + 6 * verticalRatio);
      const labelX = Math.max(0, Math.min(Math.round(left * horizontalRatio), maxWidth - labelWidth));
      const labelY = Math.max(0, Math.min(Math.round(top * verticalRatio) - labelHeight, maxHeight - labelHeight));
      context.fillStyle = 'rgba(15, 23, 42, 0.92)';
      context.fillRect(labelX, labelY, labelWidth, labelHeight);
      context.fillStyle = measurement.tone === 'positive' ? '#86efac' : measurement.tone === 'negative' ? '#fca5a5' : '#cbd5e1';
      context.textBaseline = 'top';
      lines.forEach((line, index) => context.fillText(line, labelX + padding, labelY + 3 * verticalRatio + index * lineHeight));
    }
  }

  class DrawingPaneView {
    constructor(state) {
      this.state = state;
      this.drawingRenderer = new DrawingRenderer(state);
    }

    update() {}

    renderer() {
      return this.drawingRenderer;
    }
  }

  class DrawingPrimitive {
    constructor(state) {
      this.state = state;
      this.view = new DrawingPaneView(state);
      this.requestUpdate = null;
    }

    attached(parameter) {
      this.requestUpdate = parameter && typeof parameter.requestUpdate === 'function'
        ? parameter.requestUpdate
        : null;
    }

    detached() {
      this.requestUpdate = null;
    }

    paneViews() {
      return [this.view];
    }

    updateAllViews() {
      this.view.update();
    }

    invalidate() {
      if (this.requestUpdate) this.requestUpdate();
    }
  }

  function createManager(options) {
    if (!options || !options.chart || !options.series || !options.container) {
      throw new Error('createManager requires chart, series, and container');
    }

    const callbacks = {
      records: options.onRecordsChanged || options.onRecordsChange || (() => {}),
      status: options.onStatus || (() => {}),
      selection: options.onSelectionChanged || options.onSelectionChange || (() => {}),
      state: options.onStateChanged || options.onStateChange || (() => {})
    };
    const state = {
      chart: options.chart,
      series: options.series,
      container: options.container,
      orderedTimes: Array.isArray(options.orderedTimes) ? options.orderedTimes.slice() : [],
      timeIndex: buildTimeIndex(options.orderedTimes),
      pricePrecision: Number.isInteger(options.pricePrecision) ? Math.max(0, Math.min(10, options.pricePrecision)) : 5,
      records: [],
      undoStack: [],
      redoStack: [],
      selectedId: null,
      tool: 'cursor',
      text: '',
      style: sanitizeStyle(options.style),
      locked: false,
      visible: true,
      magnet: false,
      pending: null,
      hoverPoint: null,
      brushDraft: null,
      drag: null,
      suppressClick: false,
      disposed: false
    };
    const primitive = new DrawingPrimitive(state);

    function safeCallback(callback, value) {
      try {
        callback(value);
      } catch (_) {
        // Host callbacks are isolated from chart input and rendering.
      }
    }

    function outputRecords() {
      return cloneJson(state.records);
    }

    function status(message) {
      safeCallback(callbacks.status, message);
    }

    function stateSnapshot() {
      const selected = state.records.find(record => record.id === state.selectedId);
      return {
        tool: state.tool,
        selectedId: state.selectedId,
        selectedStyle: selected ? cloneJson(selected.style) : null,
        magnet: state.magnet,
        locked: state.locked,
        visible: state.visible,
        canUndo: state.undoStack.length > 0,
        canRedo: state.redoStack.length > 0,
        recordCount: state.records.length
      };
    }

    function notifyState() {
      safeCallback(callbacks.state, stateSnapshot());
    }

    function notifyRecords() {
      safeCallback(callbacks.records, outputRecords());
      notifyState();
      primitive.invalidate();
    }

    function select(id) {
      state.selectedId = id && state.records.some(record => record.id === id) ? id : null;
      const selected = state.records.find(record => record.id === state.selectedId);
      safeCallback(callbacks.selection, selected ? cloneJson(selected) : null);
      notifyState();
      primitive.invalidate();
    }

    function enrichMeasurements(records) {
      return records.map(record => {
        if (record.type !== 'measure') return record;
        const measurement = calculateMeasurement(record.p1, record.p2, state.orderedTimes);
        return measurement ? { ...record, ...measurement } : record;
      });
    }

    function pushSnapshot(snapshot) {
      state.undoStack.push(cloneJson(snapshot));
      if (state.undoStack.length > HISTORY_LIMIT) state.undoStack.shift();
      state.redoStack = [];
    }

    function replaceRecords(records, emit) {
      state.records = enrichMeasurements(sanitizeRecords(records));
      state.visible = state.records.length === 0 || state.records.some(record => record.visible);
      state.locked = state.records.length > 0 && state.records.every(record => record.locked);
      if (!state.records.some(record => record.id === state.selectedId)) select(null);
      if (emit) notifyRecords();
      else primitive.invalidate();
    }

    function mutate(mutator) {
      const before = outputRecords();
      mutator();
      state.records = enrichMeasurements(sanitizeRecords(state.records));
      if (JSON.stringify(before) === JSON.stringify(state.records)) {
        primitive.invalidate();
        return false;
      }
      pushSnapshot(before);
      notifyRecords();
      return true;
    }

    function makeRecord(type, values) {
      return sanitizeRecord({
        id: uniqueId(),
        type,
        style: state.style,
        visible: state.visible,
        locked: state.locked,
        ...values
      }, true);
    }

    function addRecord(record) {
      if (!record) return false;
      const added = mutate(() => state.records.push(record));
      if (added) {
        select(record.id);
        status(`${record.type} drawing added`);
      }
      return added;
    }

    function eventCoordinate(event) {
      const bounds = state.container.getBoundingClientRect();
      return { x: event.clientX - bounds.left, y: event.clientY - bounds.top };
    }

    function nearestOrderedTime(time) {
      const target = normalizeTimestamp(time);
      const nearest = nearestTimeEntry(state.timeIndex, target);
      return sanitizeTime(nearest?.time) ?? time;
    }

    function dataPoint(coordinate) {
      const time = state.chart.timeScale().coordinateToTime(coordinate.x);
      const price = state.series.coordinateToPrice(coordinate.y);
      let point = sanitizePoint({ time, price });
      if (!point) return null;
      if (state.magnet) {
        point.time = nearestOrderedTime(point.time);
        if (typeof options.snapPoint === 'function') {
          try {
            point = sanitizePoint(options.snapPoint(cloneJson(point))) || point;
          } catch (_) {
            status('Magnet snap unavailable');
          }
        }
      }
      return point;
    }

    function hitTest(coordinate) {
      const bounds = state.container.getBoundingClientRect();
      for (let index = state.records.length - 1; index >= 0; index -= 1) {
        const record = state.records[index];
        if (!state.visible || !record.visible) continue;
        const anchors = recordAnchors(state, record, bounds.width, bounds.height);
        for (const anchor of anchors) {
          if (Math.hypot(coordinate.x - anchor.x, coordinate.y - anchor.y) <= HIT_DISTANCE) {
            return { record, anchor: anchor.name };
          }
        }
        if (record.type === 'hline' && anchors[0] && Math.abs(coordinate.y - anchors[0].y) <= HIT_DISTANCE) {
          return { record, anchor: null };
        }
        if (record.type === 'vline' && anchors[0] && Math.abs(coordinate.x - anchors[0].x) <= HIT_DISTANCE) {
          return { record, anchor: null };
        }
        if (record.type === 'text' && anchors[0] &&
            coordinate.x >= anchors[0].x - HIT_DISTANCE && coordinate.x <= anchors[0].x + 160 &&
            coordinate.y >= anchors[0].y - 30 && coordinate.y <= anchors[0].y + HIT_DISTANCE) {
          return { record, anchor: null };
        }
        if (record.type === 'brush') {
          for (let pointIndex = 1; pointIndex < anchors.length; pointIndex += 1) {
            if (distanceToSegment(coordinate, anchors[pointIndex - 1], anchors[pointIndex]) <= HIT_DISTANCE) {
              return { record, anchor: null };
            }
          }
        } else if (anchors.length === 2) {
          const first = anchors[0];
          const second = anchors[1];
          if (record.type === 'rectangle' || record.type === 'measure' || record.type === 'fib') {
            const left = Math.min(first.x, second.x) - HIT_DISTANCE;
            const right = Math.max(first.x, second.x) + HIT_DISTANCE;
            const top = Math.min(first.y, second.y) - HIT_DISTANCE;
            const bottom = Math.max(first.y, second.y) + HIT_DISTANCE;
            if (coordinate.x >= left && coordinate.x <= right && coordinate.y >= top && coordinate.y <= bottom) {
              return { record, anchor: null };
            }
          } else {
            let end = second;
            if (record.type === 'ray') {
              if (second.x !== first.x) {
                const endX = second.x > first.x ? bounds.width : 0;
                end = { x: endX, y: first.y + (second.y - first.y) * ((endX - first.x) / (second.x - first.x)) };
              } else if (second.y !== first.y) {
                end = { x: first.x, y: second.y > first.y ? bounds.height : 0 };
              }
            }
            if (distanceToSegment(coordinate, first, end) <= HIT_DISTANCE) return { record, anchor: null };
          }
        }
      }
      return null;
    }

    function movePoint(point, delta) {
      const coordinate = pointCoordinates(state, point);
      if (!coordinate) return point;
      return dataPoint({ x: coordinate.x + delta.x, y: coordinate.y + delta.y }) || point;
    }

    function translatedRecord(original, delta) {
      const result = cloneJson(original);
      if (result.type === 'hline') {
        const y = state.series.priceToCoordinate(original.price);
        const price = y === null ? null : state.series.coordinateToPrice(y + delta.y);
        if (Number.isFinite(price)) result.price = price;
      } else if (result.type === 'vline') {
        const x = timeCoordinate(state, original.time);
        const time = x === null ? null : state.chart.timeScale().coordinateToTime(x + delta.x);
        const cleanTime = sanitizeTime(time);
        if (cleanTime !== null) result.time = cleanTime;
      } else if (result.type === 'brush') {
        result.points = original.points.map(point => movePoint(point, delta));
      } else if (result.type === 'text') {
        result.point = movePoint(original.point, delta);
      } else {
        result.p1 = movePoint(original.p1, delta);
        result.p2 = movePoint(original.p2, delta);
      }
      return result;
    }

    function updateDraggedRecord(coordinate) {
      const drag = state.drag;
      if (!drag) return;
      const recordIndex = state.records.findIndex(record => record.id === drag.id);
      if (recordIndex < 0) return;
      let replacement;
      if (drag.anchor) {
        replacement = cloneJson(drag.original);
        const point = dataPoint(coordinate);
        if (!point) return;
        if (drag.anchor === 'price') replacement.price = point.price;
        else if (drag.anchor === 'time') replacement.time = point.time;
        else if (drag.anchor === 'point') replacement.point = point;
        else if (drag.anchor.startsWith('points:')) replacement.points[Number(drag.anchor.split(':')[1])] = point;
        else replacement[drag.anchor] = point;
      } else {
        replacement = translatedRecord(drag.original, {
          x: coordinate.x - drag.start.x,
          y: coordinate.y - drag.start.y
        });
      }
      state.records[recordIndex] = replacement;
      state.records = enrichMeasurements(state.records);
      primitive.invalidate();
    }

    function isInteractiveTarget(target) {
      for (let current = target; current; current = current.parentElement || current.parentNode) {
        const tagName = typeof current.tagName === 'string' ? current.tagName.toUpperCase() : '';
        if (INTERACTIVE_TAGS.has(tagName) || current.isContentEditable === true) return true;
        if (typeof current.hasAttribute === 'function' &&
            (current.hasAttribute('data-drawing-ui') || current.hasAttribute('data-drawing-interactive'))) {
          return true;
        }
        if (current === state.container) break;
      }
      return false;
    }

    function releasePointer(event) {
      if (state.container.releasePointerCapture && event.pointerId !== undefined) {
        try { state.container.releasePointerCapture(event.pointerId); } catch (_) {}
      }
    }

    function cancelPointerGesture(event, suppressClick) {
      state.brushDraft = null;
      if (state.drag) replaceRecords(state.drag.before, false);
      state.drag = null;
      state.hoverPoint = null;
      state.suppressClick = suppressClick;
      releasePointer(event);
      status('Drawing cancelled');
      primitive.invalidate();
    }

    function onPointerDown(event) {
      if (state.disposed || isInteractiveTarget(event.target) ||
          event.button !== undefined && event.button !== 0) return;
      const coordinate = eventCoordinate(event);
      if (state.tool === 'brush') {
        const point = dataPoint(coordinate);
        if (!point) return;
        state.brushDraft = [point];
        state.suppressClick = true;
        if (state.container.setPointerCapture && event.pointerId !== undefined) state.container.setPointerCapture(event.pointerId);
        event.preventDefault && event.preventDefault();
        primitive.invalidate();
        return;
      }
      if (state.tool !== 'cursor') return;
      const hit = hitTest(coordinate);
      select(hit ? hit.record.id : null);
      if (!hit || hit.record.locked || state.locked) return;
      state.drag = {
        id: hit.record.id,
        anchor: hit.anchor,
        start: coordinate,
        original: cloneJson(hit.record),
        before: outputRecords()
      };
      if (state.container.setPointerCapture && event.pointerId !== undefined) state.container.setPointerCapture(event.pointerId);
      event.preventDefault && event.preventDefault();
    }

    function onPointerMove(event) {
      if (state.disposed || isInteractiveTarget(event.target)) return;
      const coordinate = eventCoordinate(event);
      state.hoverPoint = dataPoint(coordinate);
      if (state.brushDraft && state.hoverPoint) {
        const last = pointCoordinates(state, state.brushDraft[state.brushDraft.length - 1]);
        if (!last || Math.hypot(coordinate.x - last.x, coordinate.y - last.y) >= 2) {
          state.brushDraft.push(state.hoverPoint);
        }
      } else if (state.drag) {
        updateDraggedRecord(coordinate);
      }
      primitive.invalidate();
    }

    function onPointerUp(event) {
      if (state.disposed) return;
      if (isInteractiveTarget(event.target)) {
        if (state.brushDraft || state.drag) cancelPointerGesture(event, false);
        return;
      }
      if (state.brushDraft) {
        const points = state.brushDraft;
        state.brushDraft = null;
        if (points.length > 1) addRecord(makeRecord('brush', { points }));
      }
      if (state.drag) {
        const drag = state.drag;
        state.drag = null;
        if (JSON.stringify(drag.before) !== JSON.stringify(state.records)) {
          pushSnapshot(drag.before);
          notifyRecords();
          status('Drawing updated');
          state.suppressClick = true;
        }
      }
      releasePointer(event);
      primitive.invalidate();
    }

    function onPointerCancel(event) {
      if (state.disposed) return;
      if (state.brushDraft || state.drag) cancelPointerGesture(event, true);
    }

    function requestedText(point) {
      let text = state.text;
      if (!text && typeof options.requestText === 'function') {
        try { text = options.requestText(cloneJson(point)); } catch (_) { text = ''; }
      }
      return boundedString(text, boundedString(options.defaultText, 'Text', MAX_TEXT_LENGTH), MAX_TEXT_LENGTH);
    }

    function onClick(event) {
      if (isInteractiveTarget(event.target)) {
        state.suppressClick = false;
        return;
      }
      if (state.disposed || state.tool === 'cursor' || state.tool === 'brush') {
        state.suppressClick = false;
        return;
      }
      if (state.suppressClick) {
        state.suppressClick = false;
        return;
      }
      const point = dataPoint(eventCoordinate(event));
      if (!point) return;
      if (state.tool === 'hline') {
        addRecord(makeRecord('hline', { price: point.price }));
      } else if (state.tool === 'vline') {
        addRecord(makeRecord('vline', { time: point.time }));
      } else if (state.tool === 'text') {
        addRecord(makeRecord('text', { point, text: requestedText(point) }));
      } else if (TWO_POINT_TYPES.has(state.tool)) {
        if (!state.pending) {
          state.pending = point;
          status(`${state.tool} first anchor set`);
          primitive.invalidate();
          return;
        }
        addRecord(makeRecord(state.tool, { p1: state.pending, p2: point }));
        state.pending = null;
        state.hoverPoint = null;
      }
    }

    function cancel() {
      state.pending = null;
      state.hoverPoint = null;
      state.brushDraft = null;
      if (state.drag) replaceRecords(state.drag.before, false);
      state.drag = null;
      state.tool = 'cursor';
      state.text = '';
      state.suppressClick = false;
      status('Drawing cancelled');
      notifyState();
      primitive.invalidate();
    }

    function isEditableTarget(target) {
      if (!target) return false;
      const tagName = typeof target.tagName === 'string' ? target.tagName.toUpperCase() : '';
      if (['INPUT', 'TEXTAREA', 'SELECT'].includes(tagName) || target.isContentEditable === true) return true;
      return typeof target.closest === 'function' &&
        target.closest('input, textarea, select, [contenteditable="true"], [contenteditable=""]') !== null;
    }

    function onKeyDown(event) {
      if (event.key === 'Escape') cancel();
      if ((event.key === 'Delete' || event.key === 'Backspace') && state.selectedId && !isEditableTarget(event.target)) {
        if (manager.deleteSelected()) event.preventDefault && event.preventDefault();
      }
    }

    function addInputListeners() {
      state.container.addEventListener('pointerdown', onPointerDown);
      state.container.addEventListener('pointermove', onPointerMove);
      state.container.addEventListener('pointerup', onPointerUp);
      state.container.addEventListener('pointercancel', onPointerCancel);
      state.container.addEventListener('click', onClick);
      if (global.addEventListener) global.addEventListener('keydown', onKeyDown);
    }

    function removeInputListeners() {
      state.container.removeEventListener('pointerdown', onPointerDown);
      state.container.removeEventListener('pointermove', onPointerMove);
      state.container.removeEventListener('pointerup', onPointerUp);
      state.container.removeEventListener('pointercancel', onPointerCancel);
      state.container.removeEventListener('click', onClick);
      if (global.removeEventListener) global.removeEventListener('keydown', onKeyDown);
    }

    const manager = {
      setTool(tool, text) {
        const normalized = tool === 'crosshair' || tool === 'select' ? 'cursor' : tool;
        if (normalized !== 'cursor' && !RECORD_TYPES.has(normalized)) return false;
        state.pending = null;
        state.brushDraft = null;
        state.tool = normalized;
        state.text = typeof text === 'string' ? text.slice(0, MAX_TEXT_LENGTH) : '';
        status(normalized === 'cursor' ? 'Cursor selected' : `${normalized} tool selected`);
        notifyState();
        primitive.invalidate();
        return true;
      },

      setRecords(records, configuration) {
        state.undoStack = [];
        state.redoStack = [];
        replaceRecords(records, !configuration || configuration.emit !== false);
        select(null);
        return outputRecords();
      },

      getRecords() {
        return outputRecords();
      },

      getState() {
        return stateSnapshot();
      },

      updateContext(context) {
        const next = isObject(context) ? context : {};
        if (Array.isArray(next.orderedTimes)) {
          state.orderedTimes = next.orderedTimes.slice();
          state.timeIndex = buildTimeIndex(state.orderedTimes);
        }
        if (Number.isInteger(next.pricePrecision)) {
          state.pricePrecision = Math.max(0, Math.min(10, next.pricePrecision));
        }
        state.records = enrichMeasurements(state.records);
        notifyState();
        primitive.invalidate();
        return outputRecords();
      },

      cancel() {
        cancel();
        return true;
      },

      undo() {
        if (state.undoStack.length === 0) return false;
        const previous = state.undoStack.pop();
        state.redoStack.push(outputRecords());
        if (state.redoStack.length > HISTORY_LIMIT) state.redoStack.shift();
        replaceRecords(previous, true);
        select(null);
        status('Undo');
        return true;
      },

      redo() {
        if (state.redoStack.length === 0) return false;
        const next = state.redoStack.pop();
        state.undoStack.push(outputRecords());
        if (state.undoStack.length > HISTORY_LIMIT) state.undoStack.shift();
        replaceRecords(next, true);
        select(null);
        status('Redo');
        return true;
      },

      deleteSelected() {
        const selected = state.records.find(record => record.id === state.selectedId);
        if (!selected || selected.locked || state.locked) return false;
        const removed = mutate(() => {
          state.records = state.records.filter(record => record.id !== selected.id);
        });
        if (removed) {
          select(null);
          status('Drawing deleted');
        }
        return removed;
      },

      clear() {
        if (state.records.length === 0) return false;
        const cleared = mutate(() => { state.records = []; });
        select(null);
        if (cleared) status('Drawings cleared');
        return cleared;
      },

      setLocked(locked) {
        const value = locked === true;
        state.locked = value;
        const changed = mutate(() => {
          state.records = state.records.map(record => ({ ...record, locked: value }));
        });
        if (!changed) notifyState();
        status(value ? 'Drawings locked' : 'Drawings unlocked');
        return changed;
      },

      setVisible(visible) {
        const value = visible !== false;
        state.visible = value;
        const changed = mutate(() => {
          state.records = state.records.map(record => ({ ...record, visible: value }));
        });
        if (!changed) notifyState();
        status(value ? 'Drawings visible' : 'Drawings hidden');
        return changed;
      },

      setMagnet(enabled) {
        state.magnet = enabled === true;
        status(state.magnet ? 'Magnet enabled' : 'Magnet disabled');
        notifyState();
        return state.magnet;
      },

      setStyle(style) {
        state.style = sanitizeStyle({ ...state.style, ...(isObject(style) ? style : {}) });
        const selected = state.records.find(record => record.id === state.selectedId);
        if (!selected || selected.locked || state.locked) {
          primitive.invalidate();
          return false;
        }
        const changed = mutate(() => {
          selected.style = state.style;
        });
        if (changed) select(selected.id);
        return changed;
      },

      dispose() {
        if (state.disposed) return;
        state.disposed = true;
        removeInputListeners();
        if (typeof state.series.detachPrimitive === 'function') state.series.detachPrimitive(primitive);
        primitive.detached();
        state.records = [];
        state.undoStack = [];
        state.redoStack = [];
      }
    };

    state.series.attachPrimitive(primitive);
    try {
      addInputListeners();
    } catch (error) {
      removeInputListeners();
      if (typeof state.series.detachPrimitive === 'function') state.series.detachPrimitive(primitive);
      primitive.detached();
      state.disposed = true;
      throw error;
    }

    notifyState();
    return manager;
  }

  window.CapComDrawings = Object.freeze({
    calculateMeasurement,
    formatMeasurement,
    validateRecord,
    sanitizeRecords,
    drawingStorageKey,
    loadStoredRecords,
    persistStoredRecords,
    confirmClear,
    normalizeAnnotationText,
    switchDrawingIdentity,
    createTextDialogController,
    createManager
  });
})(window);

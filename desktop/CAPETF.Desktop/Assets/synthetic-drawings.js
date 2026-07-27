(function (global) {
  'use strict';

  const RECORD_TYPES = new Set([
    'trend', 'ray', 'hline', 'vline', 'fib', 'rectangle', 'brush', 'text', 'measure'
  ]);
  const TWO_POINT_TYPES = new Set(['trend', 'ray', 'fib', 'rectangle', 'measure']);
  const HISTORY_LIMIT = 100;
  const MAX_TEXT_LENGTH = 500;
  const HIT_DISTANCE = 8;
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
        return Date.UTC(year, month - 1, day);
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
      result.points = record.points.map(sanitizePoint).filter(Boolean);
      if (result.points.length < 2 || result.points.length > 5000) return null;
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

  function formatNumber(value) {
    if (!Number.isFinite(value)) return 'n/a';
    const magnitude = Math.abs(value);
    if (magnitude >= 1000) return value.toFixed(2);
    if (magnitude >= 10) return value.toFixed(3);
    return value.toFixed(4);
  }

  function formatElapsed(milliseconds) {
    const absolute = Math.abs(milliseconds);
    if (absolute >= 86400000) return `${(absolute / 86400000).toFixed(1)}d`;
    if (absolute >= 3600000) return `${(absolute / 3600000).toFixed(1)}h`;
    if (absolute >= 60000) return `${(absolute / 60000).toFixed(1)}m`;
    return `${Math.round(absolute / 1000)}s`;
  }

  function distanceToSegment(point, start, end) {
    const dx = end.x - start.x;
    const dy = end.y - start.y;
    if (dx === 0 && dy === 0) return Math.hypot(point.x - start.x, point.y - start.y);
    const amount = Math.max(0, Math.min(1,
      ((point.x - start.x) * dx + (point.y - start.y) * dy) / (dx * dx + dy * dy)));
    return Math.hypot(point.x - (start.x + amount * dx), point.y - (start.y + amount * dy));
  }

  function pointCoordinates(state, point) {
    if (!point) return null;
    const x = state.chart.timeScale().timeToCoordinate(point.time);
    const y = state.series.priceToCoordinate(point.price);
    return x === null || y === null || !Number.isFinite(x) || !Number.isFinite(y) ? null : { x, y };
  }

  function recordAnchors(state, record, width, height) {
    if (record.type === 'hline') {
      const y = state.series.priceToCoordinate(record.price);
      return y === null ? [] : [{ name: 'price', x: width / 2, y }];
    }
    if (record.type === 'vline') {
      const x = state.chart.timeScale().timeToCoordinate(record.time);
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
          }
        } else if (record.type === 'rectangle' || record.type === 'measure') {
          const left = Math.min(first.x, second.x);
          const top = Math.min(first.y, second.y);
          const boxWidth = Math.abs(second.x - first.x);
          const boxHeight = Math.abs(second.y - first.y);
          context.fillStyle = record.type === 'measure'
            ? (record.priceDelta >= 0 ? 'rgba(34, 197, 94, 0.13)' : 'rgba(239, 68, 68, 0.13)')
            : style.fillColor;
          context.fillRect(x(left), y(top), Math.round(boxWidth * horizontalRatio), Math.round(boxHeight * verticalRatio));
          context.strokeRect(x(left), y(top), Math.round(boxWidth * horizontalRatio), Math.round(boxHeight * verticalRatio));
          if (record.type === 'measure') this.drawMeasurementLabel(context, record, left, top, horizontalRatio, verticalRatio);
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

    drawMeasurementLabel(context, record, left, top, horizontalRatio, verticalRatio) {
      const percent = record.percentDelta === null ? 'n/a' : `${record.percentDelta >= 0 ? '+' : ''}${record.percentDelta.toFixed(2)}%`;
      const delta = `${record.priceDelta >= 0 ? '+' : ''}${formatNumber(record.priceDelta)}`;
      const label = `${formatNumber(record.startPrice)} -> ${formatNumber(record.endPrice)}  ${delta} (${percent})  ${record.bars} bars  ${formatElapsed(record.elapsedMs)}`;
      context.setLineDash([]);
      context.font = `${Math.round(11 * verticalRatio)}px sans-serif`;
      const padding = 5 * horizontalRatio;
      const textWidth = context.measureText(label).width;
      const labelX = Math.round(left * horizontalRatio);
      const labelY = Math.max(Math.round(14 * verticalRatio), Math.round(top * verticalRatio));
      context.fillStyle = 'rgba(15, 23, 42, 0.92)';
      context.fillRect(labelX, labelY - Math.round(14 * verticalRatio), textWidth + padding * 2, Math.round(18 * verticalRatio));
      context.fillStyle = record.priceDelta >= 0 ? '#86efac' : '#fca5a5';
      context.textBaseline = 'alphabetic';
      context.fillText(label, labelX + padding, labelY);
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
      selection: options.onSelectionChanged || options.onSelectionChange || (() => {})
    };
    const state = {
      chart: options.chart,
      series: options.series,
      container: options.container,
      orderedTimes: Array.isArray(options.orderedTimes) ? options.orderedTimes.slice() : [],
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

    function notifyRecords() {
      safeCallback(callbacks.records, outputRecords());
      primitive.invalidate();
    }

    function select(id) {
      state.selectedId = id && state.records.some(record => record.id === id) ? id : null;
      const selected = state.records.find(record => record.id === state.selectedId);
      safeCallback(callbacks.selection, selected ? cloneJson(selected) : null);
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
      if (target === null || state.orderedTimes.length === 0) return time;
      let nearest = time;
      let distance = Infinity;
      for (const candidate of state.orderedTimes) {
        const normalized = normalizeTimestamp(candidate);
        if (normalized !== null && Math.abs(normalized - target) < distance) {
          nearest = candidate;
          distance = Math.abs(normalized - target);
        }
      }
      return sanitizeTime(nearest) ?? time;
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
            if (record.type === 'ray' && second.x !== first.x) {
              const endX = second.x > first.x ? bounds.width : 0;
              end = { x: endX, y: first.y + (second.y - first.y) * ((endX - first.x) / (second.x - first.x)) };
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
        const x = state.chart.timeScale().timeToCoordinate(original.time);
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

    function onPointerDown(event) {
      if (state.disposed || event.button !== undefined && event.button !== 0) return;
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
      if (state.disposed) return;
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
      if (state.container.releasePointerCapture && event.pointerId !== undefined) {
        try { state.container.releasePointerCapture(event.pointerId); } catch (_) {}
      }
      primitive.invalidate();
    }

    function requestedText(point) {
      let text = state.text;
      if (!text && typeof options.requestText === 'function') {
        try { text = options.requestText(cloneJson(point)); } catch (_) { text = ''; }
      }
      return boundedString(text, boundedString(options.defaultText, 'Text', MAX_TEXT_LENGTH), MAX_TEXT_LENGTH);
    }

    function onClick(event) {
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
      state.suppressClick = false;
      status('Drawing cancelled');
      primitive.invalidate();
    }

    function onKeyDown(event) {
      if (event.key === 'Escape') cancel();
      if ((event.key === 'Delete' || event.key === 'Backspace') && state.selectedId) {
        if (manager.deleteSelected()) event.preventDefault && event.preventDefault();
      }
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
        primitive.invalidate();
        return true;
      },

      setRecords(records) {
        state.undoStack = [];
        state.redoStack = [];
        replaceRecords(records, true);
        select(null);
        return outputRecords();
      },

      getRecords() {
        return outputRecords();
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
        status(value ? 'Drawings locked' : 'Drawings unlocked');
        return changed;
      },

      setVisible(visible) {
        const value = visible !== false;
        state.visible = value;
        const changed = mutate(() => {
          state.records = state.records.map(record => ({ ...record, visible: value }));
        });
        status(value ? 'Drawings visible' : 'Drawings hidden');
        return changed;
      },

      setMagnet(enabled) {
        state.magnet = enabled === true;
        status(state.magnet ? 'Magnet enabled' : 'Magnet disabled');
        return state.magnet;
      },

      setStyle(style) {
        state.style = sanitizeStyle({ ...state.style, ...(isObject(style) ? style : {}) });
        const selected = state.records.find(record => record.id === state.selectedId);
        if (!selected || selected.locked || state.locked) {
          primitive.invalidate();
          return false;
        }
        return mutate(() => {
          selected.style = state.style;
        });
      },

      dispose() {
        if (state.disposed) return;
        state.disposed = true;
        state.container.removeEventListener('pointerdown', onPointerDown);
        state.container.removeEventListener('pointermove', onPointerMove);
        state.container.removeEventListener('pointerup', onPointerUp);
        state.container.removeEventListener('pointercancel', onPointerUp);
        state.container.removeEventListener('click', onClick);
        if (global.removeEventListener) global.removeEventListener('keydown', onKeyDown);
        if (typeof state.series.detachPrimitive === 'function') state.series.detachPrimitive(primitive);
        primitive.detached();
        state.records = [];
        state.undoStack = [];
        state.redoStack = [];
      }
    };

    state.container.addEventListener('pointerdown', onPointerDown);
    state.container.addEventListener('pointermove', onPointerMove);
    state.container.addEventListener('pointerup', onPointerUp);
    state.container.addEventListener('pointercancel', onPointerUp);
    state.container.addEventListener('click', onClick);
    if (global.addEventListener) global.addEventListener('keydown', onKeyDown);
    state.series.attachPrimitive(primitive);

    return manager;
  }

  window.CapComDrawings = Object.freeze({
    calculateMeasurement,
    validateRecord,
    sanitizeRecords,
    createManager
  });
})(window);

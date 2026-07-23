const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const dashboardPath = path.join(__dirname, '..', 'index.html');
const dashboardSource = fs.readFileSync(dashboardPath, 'utf8');
const scriptMatch = dashboardSource.match(/<script>([\s\S]*)<\/script>/);

assert.ok(scriptMatch, 'dashboard script should be present');

function makeElement(value = '') {
  return {
    value,
    textContent: '',
    innerHTML: '',
    options: [],
    dataset: {},
    style: {},
    classList: {
      add() {},
      remove() {},
      toggle() {},
    },
    addEventListener() {},
    setAttribute(name, nextValue) {
      this[name] = nextValue;
    },
    getAttribute(name) {
      return this[name];
    },
    insertAdjacentHTML(position, html) {
      this.innerHTML += html;
    },
  };
}

const elements = {
  grid: makeElement(),
  search: makeElement(),
  band: makeElement('all'),
  sort: makeElement('investmentRank'),
  'chart-mode': makeElement('monthly'),
  note: makeElement(),
  'refresh-state': makeElement(),
  'manual-refresh': makeElement(),
  password: makeElement(),
  'auth-note': makeElement(),
  'auth-panel': makeElement(),
  'instrument-count': makeElement(),
  'validated-count': makeElement(),
  'source-date': makeElement(),
  'source-name': makeElement(),
  unlock: makeElement(),
};

const context = vm.createContext({
  console,
  crypto: {},
  TextDecoder,
  TextEncoder,
  URL,
  fetch: async () => {
    throw new Error('Unexpected fetch in dashboard test');
  },
  setInterval: () => 1,
  clearInterval() {},
  requestAnimationFrame(callback) {
    callback();
  },
  document: {
    body: { classList: { remove() {} } },
    getElementById(id) {
      if (!elements[id]) elements[id] = makeElement();
      return elements[id];
    },
    querySelectorAll(selector) {
      return selector === '.tab' ? [] : [];
    },
  },
});

vm.runInContext(scriptMatch[1], context, { filename: dashboardPath });

function run(expression) {
  return vm.runInContext(expression, context);
}

function namesFor(items, sortValue) {
  context.testItems = items;
  context.testSortValue = sortValue;
  return Array.from(run(`
    sort.value = testSortValue;
    [...testItems].sort(compareBySort).map(item => item.name);
  `));
}

function baseCardItem(overrides = {}) {
  return {
    epic: 'TEST',
    name: 'Test stock',
    symbol: 'TST',
    validated: false,
    ...overrides,
  };
}

function stockPart(index, generation, items = [], version = 'quality-dip-v1') {
  return {
    summary: {
      chunkIndex: index,
      chunkCount: 3,
      refreshGeneration: generation,
      qualityDipScoringVersion: version,
    },
    items,
  };
}

const tests = [];

function test(name, callback) {
  tests.push({ name, callback });
}

test('Quality Dip comparison sorts scores descending', () => {
  const items = [
    { name: 'Low', qualityDipScore: 12 },
    { name: 'High', qualityDipScore: 91 },
  ];
  context.testItems = items;
  assert.deepEqual(
    Array.from(run('[...testItems].sort(compareQualityDip).map(item => item.name)')),
    ['High', 'Low']
  );
});

test('Quality Dip comparison breaks score ties by name then epic', () => {
  const items = [
    { epic: 'Z', name: 'Zulu', qualityDipScore: 50 },
    { epic: 'ZZ', name: 'Alpha', qualityDipScore: 50 },
    { epic: 'AA', name: 'Alpha', qualityDipScore: 50 },
  ];
  context.testItems = items;
  assert.deepEqual(
    Array.from(run('[...testItems].sort(compareQualityDip).map(item => item.epic)')),
    ['AA', 'ZZ', 'Z']
  );
});

test('Quality Dip comparison puts Unrated stocks last', () => {
  const items = [
    { name: 'Unrated' },
    { name: 'Scored', qualityDipScore: 0 },
  ];
  context.testItems = items;
  assert.deepEqual(
    Array.from(run('[...testItems].sort(compareQualityDip).map(item => item.name)')),
    ['Scored', 'Unrated']
  );
});

test('incomplete rank assignment clears every displayed global rank', () => {
  const items = [
    { name: 'Alpha', qualityDipScore: 80, displayQualityDipRank: 2 },
    { name: 'Beta', qualityDipScore: 70, displayQualityDipRank: 1 },
    { name: 'Unrated', displayQualityDipRank: 3 },
  ];
  context.testItems = items;
  run('assignQualityDipRanks(testItems, false)');
  assert.ok(items.every(item => !Object.hasOwn(item, 'displayQualityDipRank')));
});

test('complete rank assignment ranks validated stocks and omits Unrated or unvalidated', () => {
  const items = [
    { epic: 'B', name: 'Beta', validated: true, qualityDipScore: 70 },
    { epic: 'A', name: 'Alpha', validated: true, qualityDipScore: 80 },
    { epic: 'U', name: 'Unrated', validated: true },
    { epic: 'X', name: 'Unvalidated', validated: false, qualityDipScore: 99 },
  ];
  context.testItems = items;
  run('assignQualityDipRanks(testItems, true)');
  assert.equal(items[1].displayQualityDipRank, 1);
  assert.equal(items[0].displayQualityDipRank, 2);
  assert.ok(!Object.hasOwn(items[2], 'displayQualityDipRank'));
  assert.ok(!Object.hasOwn(items[3], 'displayQualityDipRank'));
});

test('mixed refresh generations are incomplete and receive no global ranks', () => {
  context.testParts = [
    stockPart(0, 'old', [{ epic: 'A', name: 'Alpha', validated: true, qualityDipScore: 80 }]),
    stockPart(1, 'new', [{ epic: 'B', name: 'Beta', validated: true, qualityDipScore: 70 }]),
    stockPart(2, 'new', [{ epic: 'C', name: 'Gamma', validated: true, qualityDipScore: 60 }]),
  ];
  const mixed = run('mergePayloads(testParts, 3)');
  context.testPayload = mixed;
  run('activeDataset = "stocks"; payload = testPayload; prepareDisplayRanks()');

  assert.equal(run('stockBatchesComplete(testPayload.summary)'), false);
  assert.ok(mixed.items.every(item => !Object.hasOwn(item, 'displayQualityDipRank')));
});

test('duplicate or missing chunk indices are incomplete', () => {
  context.testParts = [stockPart(0, 'run'), stockPart(0, 'run'), stockPart(2, 'run')];
  assert.equal(run('stockBatchesComplete(mergePayloads(testParts, 3).summary)'), false);

  context.testParts = [stockPart(0, 'run'), stockPart(2, 'run')];
  assert.equal(run('stockBatchesComplete(mergePayloads(testParts, 3).summary)'), false);
});

test('mismatched scoring versions are incomplete', () => {
  context.testParts = [
    stockPart(0, 'run'),
    stockPart(1, 'run', [], 'quality-dip-v0'),
    stockPart(2, 'run'),
  ];

  assert.equal(run('stockBatchesComplete(mergePayloads(testParts, 3).summary)'), false);
});

test('all expected coherent batches complete and receive validated global ranks', () => {
  context.testParts = [
    stockPart(0, 'run', [{ epic: 'A', name: 'Alpha', validated: true, qualityDipScore: 80 }]),
    stockPart(1, 'run', [{ epic: 'B', name: 'Beta', validated: true, qualityDipScore: 70 }]),
    stockPart(2, 'run', [{ epic: 'C', name: 'Unrated', validated: true }]),
  ];
  const complete = run('mergePayloads(testParts, 3)');
  context.testPayload = complete;
  run('payload = testPayload; prepareDisplayRanks()');

  assert.equal(run('stockBatchesComplete(testPayload.summary)'), true);
  assert.equal(complete.items.find(item => item.epic === 'A').displayQualityDipRank, 1);
  assert.equal(complete.items.find(item => item.epic === 'B').displayQualityDipRank, 2);
  assert.ok(!Object.hasOwn(complete.items.find(item => item.epic === 'C'), 'displayQualityDipRank'));
});

test('stock cache replaces a batch only when its refresh generation changes', () => {
  context.testOldPart = stockPart(0, 'old', [{ epic: 'OLD' }]);
  context.testSamePart = stockPart(0, 'old', [{ epic: 'SAME' }]);
  context.testNewPart = stockPart(0, 'new', [{ epic: 'NEW' }]);

  run(`
    delete stockPartsByFile['stocks-000'];
    cacheStockPart('stocks-000', testOldPart);
  `);
  assert.equal(run("cacheStockPart('stocks-000', testSamePart)"), false);
  assert.equal(run("stockPartsByFile['stocks-000'].items[0].epic"), 'OLD');
  assert.equal(run("cacheStockPart('stocks-000', testNewPart)"), true);
  assert.equal(run("stockPartsByFile['stocks-000'].items[0].epic"), 'NEW');
  run("delete stockPartsByFile['stocks-000']");
});

test('Quality Dip sort uses score, discount magnitude then score, and stabilization rules', () => {
  const items = [
    { name: 'Unrated' },
    { name: 'Alpha', qualityDipScore: 80, qualityDipTrendDistancePct: 8, qualityDipStabilizationScore: 4 },
    { name: 'Beta', qualityDipScore: 70, qualityDipTrendDistancePct: 15, qualityDipStabilizationScore: 17 },
    { name: 'Gamma', qualityDipScore: 90, qualityDipTrendDistancePct: 15, qualityDipStabilizationScore: 10 },
  ];
  assert.deepEqual(namesFor(items, 'qualityDip'), ['Gamma', 'Alpha', 'Beta', 'Unrated']);
  assert.deepEqual(namesFor(items, 'qualityDiscount'), ['Gamma', 'Beta', 'Alpha', 'Unrated']);
  assert.deepEqual(namesFor(items, 'qualityStabilizing'), ['Beta', 'Gamma', 'Alpha', 'Unrated']);
});

test('Quality Dip sort controls are Stocks-only and repair selection outside Stocks', () => {
  assert.match(
    dashboardSource,
    /<option value="qualityDip" data-stock-only="true">Quality dip, best to worst<\/option>/
  );
  assert.match(
    dashboardSource,
    /<option value="qualityDiscount" data-stock-only="true">Largest discount in quality trend<\/option>/
  );
  assert.match(
    dashboardSource,
    /<option value="qualityStabilizing" data-stock-only="true">Stabilizing dips<\/option>/
  );

  run(`
    sort.options = [
      { value: 'investmentRank', dataset: {}, hidden: false, disabled: false },
      { value: 'qualityDip', dataset: { stockOnly: 'true' }, hidden: false, disabled: false },
      { value: 'qualityDiscount', dataset: { stockOnly: 'true' }, hidden: false, disabled: false },
      { value: 'qualityStabilizing', dataset: { stockOnly: 'true' }, hidden: false, disabled: false }
    ];
    activeDataset = 'etfs';
    sort.value = 'qualityDip';
    syncQualityDipSortControls();
  `);
  assert.equal(elements.sort.value, 'investmentRank');
  assert.ok(elements.sort.options.slice(1).every(option => option.hidden && option.disabled));

  run(`
    activeDataset = 'stocks';
    syncQualityDipSortControls();
  `);
  assert.ok(elements.sort.options.slice(1).every(option => !option.hidden && !option.disabled));
  assert.match(
    dashboardSource,
    /function selectDataset\(key\)[\s\S]*?activeDataset = key;[\s\S]*?syncQualityDipSortControls\(\);/
  );
});

test('stock tile shows Pending without rendering a partial rank', () => {
  const item = baseCardItem({
    qualityDipScore: 82.5,
    qualityDipLabel: 'Watch <carefully>',
    qualityDipPartialRank: 98765,
    qualityDipTrendScore: 31,
    qualityDipDiscountScore: 22,
    qualityDipStabilizationScore: 14,
    qualityDipDrawdownPct: 18.4,
    qualityDipTrendDistancePct: 7.2,
  });
  context.testItem = item;
  const html = run(`
    activeDataset = 'stocks';
    payload = { summary: { chunkCount: 1, totalChunks: 2 }, items: [testItem] };
    card(testItem);
  `);
  assert.match(html, /<span>Quality dip<\/span><strong>Pending<\/strong>/);
  assert.match(html, /<span>Score<\/span><strong>82\.5<\/strong>/);
  assert.match(html, /<span>Signal<\/span><strong>Watch &lt;carefully&gt;<\/strong>/);
  assert.match(html, /<span>Trend<\/span><strong>31\.0\/40<\/strong>/);
  assert.match(html, /<span>Discount<\/span><strong>22\.0\/30<\/strong>/);
  assert.match(html, /<span>Stabilizing<\/span><strong>14\.0\/20<\/strong>/);
  assert.match(html, /52W drawdown 18\.4% below .* vs trend 7\.2% below/);
  assert.doesNotMatch(html, /\+18\.4%|\+7\.2%/);
  assert.doesNotMatch(html, /98765|qualityDipPartialRank/);
});

test('stock tile shows Unrated after complete load and numeric rank when scored', () => {
  const scored = baseCardItem({ epic: 'S', name: 'Scored', validated: true, qualityDipScore: 75, qualityDipLabel: 'Candidate' });
  const unrated = baseCardItem({ epic: 'U', name: 'Unrated', validated: true });
  context.testItems = [scored, unrated];
  const html = run(`
    activeDataset = 'stocks';
    payload = { summary: { stockBatchesComplete: true }, items: testItems };
    prepareDisplayRanks();
    [card(testItems[0]), card(testItems[1])];
  `);
  assert.match(html[0], /<span>Quality dip<\/span><strong>1<\/strong>/);
  assert.match(html[1], /<span>Quality dip<\/span><strong>Unrated<\/strong>/);
  assert.match(html[1], /<span>Score<\/span><strong>Unrated<\/strong>/);
});

test('ETF and Sector cards omit the Quality Dip block', () => {
  context.testItem = baseCardItem({ qualityDipScore: 90, qualityDipLabel: 'Candidate' });
  const etfHtml = run(`
    activeDataset = 'etfs';
    payload = { summary: {}, items: [testItem] };
    card(testItem);
  `);
  const sectorHtml = run(`
    activeDataset = 'sectors';
    card(testItem);
  `);
  assert.doesNotMatch(etfHtml, /quality-dip|Quality dip/);
  assert.doesNotMatch(sectorHtml, /quality-dip|Quality dip/);
});

let failures = 0;
for (const { name, callback } of tests) {
  try {
    callback();
    console.log(`ok - ${name}`);
  } catch (error) {
    failures += 1;
    console.error(`not ok - ${name}`);
    console.error(error.stack);
  }
}

if (failures) {
  console.error(`\n${failures} of ${tests.length} tests failed`);
  process.exitCode = 1;
} else {
  console.log(`\n${tests.length} tests passed`);
}

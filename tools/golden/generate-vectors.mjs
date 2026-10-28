#!/usr/bin/env node
// Generates tests/ForgeOracle.Core.Tests/Fixtures/golden-vectors.json from the Forge Oracle reference
// implementation (https://github.com/papptimus/Forge-Oracle, MIT). The reference functions `hash` and `rollNext`
// are extracted verbatim from its index.html and evaluated in an isolated context, so the expected values do not
// come from the C# code under test.
//
// Usage: node tools/golden/generate-vectors.mjs <path/to/Forge-Oracle/index.html> <commit> > golden-vectors.json

import { readFileSync } from 'node:fs';
import vm from 'node:vm';

const [htmlPath, commit] = process.argv.slice(2);
if (!htmlPath || !commit) {
  console.error('usage: generate-vectors.mjs <index.html> <commit>');
  process.exit(2);
}

const html = readFileSync(htmlPath, 'utf8');
const hashSrc = html.match(/function hash\(s\)\{.*?return \(n\+Math\.imul\(n2,1566083941\)\)>>>0;\}/s)?.[0];
const rollSrc = html.match(/function rollNext\(seed\)\{[\s\S]*?next:r2%99999999\};\s*\}/)?.[0];
if (!hashSrc || !rollSrc) {
  throw new Error('reference functions not found in ' + htmlPath);
}

const sandbox = {};
vm.runInNewContext(`var M=0xFFFFFFFF, A=1812433253; ${hashSrc} ${rollSrc} out = { hash, rollNext };`, sandbox);
const ref = sandbox.out;

const i32 = (u) => u | 0;
const bits23 = (roll) => Math.round(roll * 8388607);

// Independent JS model of the DoCrafting branches, with float32 semantics via Math.fround.
const f = Math.fround;
const RECIP = f(1 / 8388607);
const rollMul = (k) => f(f(k) * RECIP);
const rollDiv = (k) => f(f(k) / f(8388607));
function classify(r, success, brk, highPrecision) {
  if (f(success) >= r) return 'Success';
  const comp = highPrecision ? 1 - r : f(1 - r);
  return f(brk) >= comp ? 'Destruction' : 'LevelLoss';
}
function outcome(k, success, brk) {
  const variants = [rollMul(k), rollDiv(k)].flatMap((r) => [classify(r, success, brk, false), classify(r, success, brk, true)]);
  return { expected: classify(rollMul(k), success, brk, false), sensitive: new Set(variants).size > 1 };
}

const RULES = { success: 0.65, break: 0.1 };

function attempt(character, prefab, current, storedSeed = null) {
  const target = current + 1;
  const key = (ref.hash(prefab) + target) >>> 0;
  const seed = storedSeed === null ? (ref.hash(character) + key) >>> 0 : storedSeed >>> 0;
  const r = ref.rollNext(seed);
  const k = bits23(r.roll);
  const o = outcome(k, RULES.success, RULES.break);
  return {
    character, prefab, currentLevel: current, targetLevel: target, key: i32(key),
    storedSeed: storedSeed === null ? null : i32(storedSeed), seed: i32(seed), rawBits23: k, roll: r.roll,
    nextSeed: r.next, referenceClass: r.roll <= 0.65 ? 'S' : 'F', expectedOutcome: o.expected, precisionSensitive: o.sensitive,
  };
}

function chain(seed, steps) {
  const rows = [];
  let s = seed >>> 0;
  for (let i = 0; i < steps; i++) {
    const r = ref.rollNext(s);
    rows.push({ seed: i32(s), rawBits23: bits23(r.roll), roll: r.roll, nextSeed: r.next });
    s = r.next;
  }
  return { start: i32(seed), steps: rows };
}

const texts = ['Hammer', 'SwordBlackmetal', 'AxeIron', 'ArmorDeepNorthMediumChest', 'ShieldBronzeBuckler', 'a', 'ab',
  'abc', '', 'Thor', 'Sigrun', 'Viking01', 'Ægir', 'Þórhildur', 'Björn Ironside', '李小龍', '🔥Ragnar', "O'Neil",
  'SwordDyrnwyn_TW', 'AxeSkollAndHati_TW', 'ArmoryHammerBlackmetal', 'VAKnifeObsidian', 'Item With Spaces 01'];
const hashes = texts.map((text) => ({ text, hash: i32(ref.hash(text)) }));

const characters = ['Thor', 'Sigrun', 'Viking01', 'Ægir', 'Þórhildur', '李小龍', '🔥Ragnar', "O'Neil", 'x'];
const prefabs = ['Hammer', 'SwordBlackmetal', 'AxeIron', 'ArmorDeepNorthMediumChest', 'ShieldBronzeBuckler',
  'SwordDyrnwyn_TW', 'AxeSkollAndHati_TW', 'ArmoryHammerBlackmetal', 'VAKnifeObsidian'];
const attempts = [];
for (const c of characters) {
  for (const p of prefabs) {
    for (const lvl of [1, 3, 4, 9, 20]) attempts.push(attempt(c, p, lvl));
  }
}
for (const stored of [0, 1, 12345678, 99999998, 0x7fffffff, 0x80000000, 0xffffffff]) {
  attempts.push(attempt('Thor', 'Hammer', 3, stored));
}

const chains = [0, 1, -1, 2147483647, -2147483648, 4242, 99999998].map((s) => chain(s, 12));

// Seeds whose first draw lands on the success and break boundaries, found by search and recomputed with the
// reference rollNext so the stored values still come from the reference.
function findBoundarySeeds(targets, limit) {
  const found = new Map();
  for (let s = 0; s < limit && found.size < targets.length; s++) {
    const k = bits23(ref.rollNext(s).roll);
    if (targets.includes(k) && !found.has(k)) found.set(k, s);
  }
  return [...found.entries()].map(([k, s]) => ({ ...chain(s, 1).steps[0], ...outcome(k, RULES.success, RULES.break) }));
}
const boundaries = findBoundarySeeds([0, 1, 5452594, 5452595, 7549746, 7549747, 8388606, 8388607], 120_000_000);

// A prefab whose hash is close to int.MaxValue, so hash + target wraps to a negative key. Found by inverting the
// hash offline (the hash is even + odd * 1566083941 and that multiplier is invertible mod 2^32); checked here with
// the reference hash.
const WRAP_CANDIDATES = ['BAA\u442aA\uc331'];
function findWrappingPrefab() {
  for (const name of WRAP_CANDIDATES) {
    const h = ref.hash(name);
    if (h >= 0x7fffff00 && h <= 0x7fffffff) return { name, distance: 0x7fffffff - h };
  }
  return null;
}
const wrap = findWrappingPrefab();
const wrapping = wrap ? [0, 1, 5].map((d) => attempt('Thor', wrap.name, wrap.distance + d)) : [];

const output = {
  reference: { project: 'papptimus/Forge-Oracle', commit, file: 'index.html', functions: ['hash', 'rollNext'] },
  rules: RULES,
  notes: 'expectedOutcome uses the Valheim 1.0.21 DoCrafting branches with float32 semantics (Math.fround).',
  hashes, attempts, chains, boundaries, wrapping,
};
process.stdout.write(JSON.stringify(output, null, 1) + '\n');

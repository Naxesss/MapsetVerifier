// Generates the Mandarin reading table used by the romanisation check's Chinese romaniser.
//
//   Download https://www.unicode.org/Public/UCD/latest/ucd/Unihan.zip and extract it, then:
//   node scripts/generate-pinyin-table.mjs <path to Unihan_Readings.txt>
//
// Writes MapsetVerifier.Checks/Utils/Romanisation/pinyin.txt: one character per line, a tab, and its readings
// without tone marks, most common first, separated by commas. ü is written as v. Readings come from kMandarin,
// then kTGHZ2013, kXHC1983 and kHanyuPinyin, so characters with several readings keep all of them.
//
// Unihan is provided by Unicode, Inc. under the Unicode License V3, see THIRD-PARTY-NOTICES.txt.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const input = process.argv[2];
if (!input || !fs.existsSync(input)) {
  console.error('Usage: node scripts/generate-pinyin-table.mjs <Unihan_Readings.txt>');
  process.exit(1);
}

const fields = ['kMandarin', 'kTGHZ2013', 'kXHC1983', 'kHanyuPinyin'];

// "lǜ" → "lv", "zhuàng" → "zhuang"
const toneless = (reading) =>
  reading
    .normalize('NFD')
    .replace(/u\u0308/g, 'v')
    .replace(/U\u0308/g, 'v')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase();

const readings = new Map();
for (const line of fs.readFileSync(input, 'utf8').split('\n')) {
  const [codePoint, field, value] = line.trim().split('\t');
  if (!value || !fields.includes(field)) continue;

  const character = String.fromCodePoint(parseInt(codePoint.slice(2), 16));
  // kMandarin is a plain list; the other fields prefix readings with a dictionary position ("10019.020:tiàn").
  const values = value
    .split(' ')
    .flatMap((part) => (part.includes(':') ? part.slice(part.indexOf(':') + 1).split(',') : [part]))
    .map(toneless)
    .filter((reading) => /^[a-z]+$/.test(reading));

  const entry = readings.get(character) ?? { byField: {} };
  entry.byField[field] = [...(entry.byField[field] ?? []), ...values];
  readings.set(character, entry);
}

const lines = [...readings.entries()]
  .sort(([a], [b]) => a.codePointAt(0) - b.codePointAt(0))
  .map(([character, entry]) => {
    const ordered = [...new Set(fields.flatMap((field) => entry.byField[field] ?? []))];
    return ordered.length > 0 ? `${character}\t${ordered.join(',')}` : null;
  })
  .filter(Boolean);

const output = path.join(root, 'MapsetVerifier.Checks/Utils/Romanisation/pinyin.txt');
fs.writeFileSync(output, lines.join('\n') + '\n');
console.log(`${lines.length} characters written to ${path.relative(root, output)}`);

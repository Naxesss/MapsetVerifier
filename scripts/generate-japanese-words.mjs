// Generates the word and name list used by the romanisation check's Japanese romaniser.
//
//   Download JMdict_e.gz and JMnedict.xml.gz from https://www.edrdg.org/pub/Nihongo/, then:
//   node scripts/generate-japanese-words.mjs <path to JMdict_e.gz> <path to JMnedict.xml.gz>
//
// Writes MapsetVerifier.Checks/Utils/Romanisation/japanese-words.txt.br, Brotli compressed: one word per line, a
// tab, and what it may be romanised from, separated by commas, with those of words before those of names. Lines are
// sorted by the bytes of the word.
//   Words and names with kanji have their readings in hiragana (水瀬 → みなせ,みずせ).
//   Katakana words and names have the spelling in their source language (ハート → heart), in lowercase letters only.
//   Long vowel marks are left out of the katakana, since they are often written differently (メロディ, メロディー).
//
// JMdict and JMnedict belong to the Electronic Dictionary Research and Development Group and are used in conformance
// with its licence, which asks for the data to be kept up to date. See THIRD-PARTY-NOTICES.txt and docs/RELEASES.md.

import fs from 'node:fs';
import path from 'node:path';
import zlib from 'node:zlib';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const [wordsFile, namesFile] = process.argv.slice(2);
if (![wordsFile, namesFile].every((file) => file && fs.existsSync(file))) {
  console.error('Usage: node scripts/generate-japanese-words.mjs <JMdict_e.gz> <JMnedict.xml.gz>');
  process.exit(1);
}

const maxNameLength = 6;
const maxGlossWords = 3;

const found = new Map();
const add = (key, value) => {
  if (!found.has(key)) found.set(key, new Set());
  found.get(key).add(value);
};

const entries = (file) =>
  zlib.gunzipSync(fs.readFileSync(file)).toString('utf8').split('<entry>').slice(1);
const values = (entry, tag) =>
  [...entry.matchAll(new RegExp(`<${tag}(?: [^>]*)?>([^<]*)</${tag}>`, 'g'))].map(
    (match) => match[1]
  );

const hasKanji = (text) => /[㐀-鿿々]/.test(text);
const isKatakana = (text) => /^[ァ-ヺー・]+$/.test(text);
const katakanaKey = (text) => text.replace(/[ー・]/g, '');
const hiragana = (text) =>
  text.replace(/[ァ-ヶ]/g, (ch) => String.fromCharCode(ch.charCodeAt(0) - 0x60));
const latinLetters = (gloss) =>
  gloss
    .replace(/\(.*?\)/g, '')
    .replace(/&[a-z]+;/g, '')
    .normalize('NFD')
    .toLowerCase()
    .replace(/[^a-z]/g, '');
const glossWords = (gloss) => gloss.replace(/\(.*?\)/g, '').trim().split(/\s+/).length;

const addReadings = (written, readings) => {
  for (const text of written.filter(hasKanji))
    for (const reading of readings.map(hiragana))
      if (/^[ぁ-ゖー]+$/.test(reading)) add(text, reading);
};

const addSpellings = (katakana, glosses) => {
  for (const text of katakana) {
    const key = katakanaKey(text);
    if (key.length < 2) continue;
    for (const gloss of glosses) if (latinLetters(gloss)) add(key, latinLetters(gloss));
  }
};

for (const entry of entries(wordsFile)) {
  const written = values(entry, 'keb');
  const readings = values(entry, 'reb');
  addReadings(written, readings);

  // Words only ever written in katakana are loan words, so their meaning is what they are spelled like.
  if (written.length === 0 && readings.every(isKatakana))
    addSpellings(
      readings,
      values(entry, 'gloss').filter((gloss) => glossWords(gloss) <= maxGlossWords)
    );
}

for (const entry of entries(namesFile)) {
  const written = values(entry, 'keb');
  const readings = values(entry, 'reb');
  addReadings(
    written.filter((text) => text.length <= maxNameLength),
    readings
  );

  if (written.length === 0 && readings.every((text) => isKatakana(text) && text.length <= 8))
    addSpellings(
      readings,
      values(entry, 'trans_det').filter((name) => glossWords(name) === 1)
    );
}

const lines = [...found]
  .map(([key, list]) => ({ bytes: Buffer.from(key), line: `${key}\t${[...list].join(',')}\n` }))
  .sort((a, b) => Buffer.compare(a.bytes, b.bytes))
  .map(({ line }) => line);

const data = Buffer.from(lines.join(''));
const compressed = zlib.brotliCompressSync(data, {
  params: {
    [zlib.constants.BROTLI_PARAM_QUALITY]: 11,
    [zlib.constants.BROTLI_PARAM_LGWIN]: 24,
    [zlib.constants.BROTLI_PARAM_SIZE_HINT]: data.length,
  },
});

const output = path.join(root, 'MapsetVerifier.Checks/Utils/Romanisation/japanese-words.txt.br');
fs.writeFileSync(output, compressed);
console.log(
  `${lines.length} words written to ${path.relative(root, output)}, ` +
    `${Math.round(compressed.length / 1024)} KB (${Math.round(data.length / 1024)} KB uncompressed)`
);

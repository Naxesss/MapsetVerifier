import { countWord } from '../../utils/countWord';
import type {
  ApiSnapshotChange,
  ApiSnapshotChecks,
  ApiSnapshotFileChange,
  ApiSnapshotHunk,
  ApiSnapshotRollup,
  ApiSnapshotSettingChange,
  DiffType,
  SnapshotChangeKind,
  SnapshotHunkLabel,
} from '../../Types';

/** "00:41:210", the way the editor and the discussion page write a time. */
export function formatClock(ms: number): string {
  const total = Math.trunc(ms);
  if (total < 0) return String(total);

  const minutes = Math.floor(total / 60000);
  const seconds = Math.floor((total % 60000) / 1000);
  const millis = total % 1000;

  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}:${String(millis).padStart(3, '0')}`;
}

/** A timestamp as osu! links it: "00:41:210 - " (the trailing " -" is what makes it a link). */
export function formatStamp(ms: number): string {
  return `${formatClock(ms)} - `;
}

export const KIND_LABEL: Record<SnapshotChangeKind, string> = {
  Rhythm: 'Rhythm',
  Placement: 'Placement',
  Hitsound: 'Hitsounds',
  Timing: 'Timing & SV',
};

export const HUNK_LABEL: Record<SnapshotHunkLabel, string> = {
  Remapped: 'Remapped',
  Rhythm: 'Rhythm',
  Placement: 'Placement',
  Hitsounding: 'Hitsounding',
  Sv: 'SV and timing',
  Mixed: 'Mixed',
  Shifted: 'Moved in time',
  Volume: 'Volume',
};

/** Colours of the four tracks on the change map, kept clear of the added/removed/changed colours. */
export const KIND_COLOR: Record<SnapshotChangeKind, string> = {
  Rhythm: 'var(--mantine-color-pink-4)',
  Placement: 'var(--mantine-color-cyan-4)',
  Hitsound: 'var(--mantine-color-violet-4)',
  Timing: 'var(--mantine-color-orange-4)',
};

export const KIND_ORDER: SnapshotChangeKind[] = ['Rhythm', 'Placement', 'Hitsound', 'Timing'];

const lower = (value: string | null) => (value ?? '').toLowerCase();

function point(value: string | null) {
  return (value ?? '').replace(',', '; ');
}

function names(value: string | null) {
  if (!value || value === 'None' || value === '0') return 'none';
  return value.replace(/,\s*/g, ', ').toLowerCase();
}

function trimNumber(value: string | null) {
  if (value == null) return '';
  const n = Number(value);
  return Number.isFinite(n) ? String(Math.round(n * 100) / 100) : value;
}

/** One sentence for one change, without its timestamp. */
export function describeChange(c: ApiSnapshotChange): string {
  if (c.op === 'Added' || c.op === 'Removed') {
    const what = c.object?.type ?? (c.field === 'RedLine' ? 'Uninherited line' : 'Inherited line');
    return `${what} ${c.op === 'Added' ? 'added' : 'removed'}.`;
  }

  switch (c.field) {
    case 'Type':
      return `Changed from ${lower(c.before)} to ${lower(c.after)}.`;
    case 'Shift':
      return `Moved ${formatShift(c.magnitude ?? 0)}, from ${formatClock(Number(c.before))} to ${formatClock(Number(c.after))}.`;
    case 'Time':
      return `Moved in time from ${formatClock(Number(c.before))} to ${formatClock(Number(c.after))}.`;
    case 'Position':
      return `Moved from (${point(c.before)}) to (${point(c.after)}).`;
    case 'Column':
      return `Moved from column ${c.before} to column ${c.after}.`;
    case 'Note':
      return `Changed from ${lower(c.before)} to ${lower(c.after)}.`;
    case 'NewCombo':
      return c.after === 'True' ? 'Added new combo.' : 'Removed new combo.';
    case 'Hitsound':
      return `Hit sounds changed from ${names(c.before)} to ${names(c.after)}.`;
    case 'Sampleset':
      return `Sampleset changed from ${lower(c.before)} to ${lower(c.after)}.`;
    case 'CustomIndex':
      return `Custom sampleset index changed from ${c.before} to ${c.after}.`;
    case 'Volume':
      return `Hit sound volume changed from ${c.before ?? 'inherited'} to ${c.after ?? 'inherited'}.`;
    case 'Filename':
      return `Hit sound filename changed from ${c.before} to ${c.after}.`;
    case 'Length':
      return `Pixel length changed from ${trimNumber(c.before)} to ${trimNumber(c.after)}.`;
    case 'Reverses':
      return `Reverse amount changed from ${c.before} to ${c.after}.`;
    case 'Shape':
      return 'Slider shape changed.';
    case 'SliderSamples':
      return "Hit sounds on the slider's head, reverses or tail changed.";
    case 'EndTime':
      return `End time changed from ${formatClock(Number(c.before))} to ${formatClock(Number(c.after))}.`;
    case 'Kiai':
      return `Kiai ${c.after === 'True' ? 'enabled' : 'disabled'}.`;
    case 'Meter':
      return `Timing signature changed from ${c.before}/4 to ${c.after}/4.`;
    case 'Bpm':
      return `BPM changed from ${trimNumber(c.before)} to ${trimNumber(c.after)}.`;
    case 'Sv':
      return `Slider velocity multiplier changed from ${trimNumber(c.before)}x to ${trimNumber(c.after)}x.`;
    case 'RedLineSampleset':
    case 'GreenLineSampleset':
      return `Sampleset changed from ${lower(c.before)} to ${lower(c.after)}.`;
    case 'RedLineVolume':
    case 'GreenLineVolume':
      return `Volume changed from ${c.before} to ${c.after}.`;
    default:
      return 'Changed.';
  }
}

/** A line in a hunk: everything that happened to one object or timing line, in one entry. */
export type ChangeLine = {
  key: string;
  /** "00:41:210 (1,2) - ", or empty. */
  stamp: string;
  op: DiffType;
  /** The headline sentence, e.g. "Circle added." or "Moved from (10; 10) to (20; 20).". */
  text: string;
  details: string[];
  time: number;
  minor: boolean;
};

function groupKey(c: ApiSnapshotChange) {
  return c.object
    ? `o|${c.object.stamp}`
    : `l|${c.time}|${(c.field ?? '').replace('RedLine', '').replace('GreenLine', '')}`;
}

/** Groups changes by the object or line they happened to, ordered in song time. */
export function toChangeLines(changes: ApiSnapshotChange[]): ChangeLine[] {
  const groups = new Map<string, ApiSnapshotChange[]>();
  for (const change of changes) {
    const key = groupKey(change);
    const list = groups.get(key);
    if (list) list.push(change);
    else groups.set(key, [change]);
  }

  const lines: ChangeLine[] = [];
  for (const [key, list] of groups) {
    const first = list[0];
    const stamp = first.object?.stamp ?? formatStamp(first.time);
    const type = first.object?.type ?? 'Timing line';
    const single = list.length === 1;
    const isPlain = single && first.op !== 'Changed';

    lines.push({
      key,
      stamp,
      op: isPlain ? first.op : 'Changed',
      text: single ? describeChange(first) : `${type} changed.`,
      details: single ? [] : list.map(describeChange),
      time: first.time,
      minor: list.every((c) => c.minor),
    });
  }

  return lines.sort((a, b) => a.time - b.time);
}

/** A signed time shift, e.g. "+10 ms" or "-12 ms". */
export function formatShift(ms: number): string {
  const sign = ms > 0 ? '+' : '';
  return `${sign}${Math.round(ms * 100) / 100} ms`;
}

/** The time shift a hunk's objects share, if the hunk is a part of the map moved in time. */
export function hunkShift(hunk: ApiSnapshotHunk): number | null {
  return hunk.changes.find((c) => c.field === 'Shift')?.magnitude ?? null;
}

/**
 * How timing lines' volume changed, all the same way: "Volume 100% to 80%" when they all had
 * one value, "Volume set to 80%" when they all got one, otherwise "Volume lowered by 20%".
 */
function volumeChange(
  before: string | null | undefined,
  after: string | null | undefined,
  amount: number | null | undefined
) {
  if (before != null && after != null) return `Volume ${before}% to ${after}%`;
  if (after != null) return `Volume set to ${after}%`;
  const by = Math.abs(amount ?? 0);
  return `Volume ${(amount ?? 0) < 0 ? 'lowered' : 'raised'} by ${by}%`;
}

export function describeRollup(rollup: ApiSnapshotRollup, difficulties?: number): string {
  const scope = difficulties && difficulties > 1 ? ` in all ${difficulties} difficulties` : '';

  if (rollup.kind === 'Volume')
    return `${volumeChange(rollup.before, rollup.after, rollup.amount)} on the timing lines${scope}.`;

  return `Everything was shifted ${formatShift(rollup.amount)}${scope}.`;
}

function movedTogether(count: number): string {
  return `${count} objects and timing lines moved by the same amount, so they are not listed one by one.`;
}

function linesAlike(count: number): string {
  return `${count} timing lines changed the same way, so they are not listed one by one.`;
}

export function describeRollupDetail(rollup: ApiSnapshotRollup): string {
  return rollup.kind === 'Volume' ? linesAlike(rollup.absorbed) : movedTogether(rollup.absorbed);
}

function volumeGroupChange(hunk: ApiSnapshotHunk): string {
  const group = hunk.changes.find((c) => c.field === 'VolumeGroup');
  return volumeChange(group?.before, group?.after, group?.magnitude);
}

/** The headline of a stretch of timing lines whose volume changed the same way. */
export function describeVolumeGroup(hunk: ApiSnapshotHunk): string {
  return `${volumeGroupChange(hunk)}.`;
}

export function describeVolumeGroupDetail(hunk: ApiSnapshotHunk): string {
  return linesAlike(shiftedCount(hunk));
}

/** How many objects and timing lines moved with a part of the map moved in time. */
function shiftedCount(hunk: ApiSnapshotHunk): number {
  return hunk.changes.reduce((sum, c) => sum + (c.count ?? 0), 0);
}

/** The headline of a part of the map moved in time, e.g. "Moved +30 ms." */
export function describeShift(hunk: ApiSnapshotHunk): string {
  return `Moved ${formatShift(hunkShift(hunk) ?? 0)}.`;
}

export function describeShiftDetail(hunk: ApiSnapshotHunk): string {
  return movedTogether(shiftedCount(hunk));
}

const SETTING_LABELS: Record<string, string> = {
  AudioFilename: 'Audio filename',
  AudioLeadIn: 'Audio lead-in',
  PreviewTime: 'Preview time',
  SampleSet: 'Default sample set',
  StackLeniency: 'Stack leniency',
  LetterboxInBreaks: 'Letterboxing in breaks',
  WidescreenStoryboard: 'Widescreen storyboard',
  StoryFireInFront: 'Storyboard in front of combo fire',
  SpecialStyle: 'Special N+1 style',
  UseSkinSprites: 'Use skin sprites in storyboard',
  EpilepsyWarning: 'Epilepsy warning',
  CountdownOffset: 'Countdown offset',
  OverlayPosition: 'Overlay position',
  SkinPreference: 'Skin preference',
  TitleUnicode: 'Title (unicode)',
  ArtistUnicode: 'Artist (unicode)',
  BeatDivisor: 'Beat snap divisor',
  GridSize: 'Grid size',
  DistanceSpacing: 'Distance spacing',
  TimelineZoom: 'Timeline zoom',
  HPDrainRate: 'HP drain',
  CircleSize: 'Circle size',
  OverallDifficulty: 'Overall difficulty',
  ApproachRate: 'Approach rate',
  SliderMultiplier: 'Slider multiplier',
  SliderTickRate: 'Slider tick rate',
  SliderBorder: 'Slider border',
  SliderBody: 'Slider body',
  SliderTrackOverride: 'Slider track override',
};

export function settingLabel(setting: ApiSnapshotSettingChange): string {
  const key = setting.key;
  if (SETTING_LABELS[key]) return SETTING_LABELS[key];
  const combo = /^Combo(\d+)$/.exec(key);
  if (combo) return `Combo ${combo[1]}`;
  return key;
}

function breakRange(value: string | null) {
  if (!value) return '';
  const [start, end] = value.split('-').map(Number);
  return `${formatClock(start)} to ${formatClock(end)}`;
}

/** Display values of a setting's before and after, e.g. a break shown as a time range. */
export function settingValues(setting: ApiSnapshotSettingChange) {
  if (setting.section === 'Events' && setting.key === 'Break') {
    return { before: breakRange(setting.before), after: breakRange(setting.after) };
  }
  if (setting.key === 'PreviewTime') {
    const format = (v: string | null) => (v != null && Number(v) >= 0 ? formatClock(Number(v)) : v);
    return { before: format(setting.before), after: format(setting.after) };
  }
  return { before: setting.before, after: setting.after };
}

export function describeSetting(setting: ApiSnapshotSettingChange): string {
  const label = settingLabel(setting);
  const { before, after } = settingValues(setting);

  if (setting.added || setting.removed) {
    const parts: string[] = [];
    if (setting.added?.length) parts.push(`added ${setting.added.map((v) => `"${v}"`).join(', ')}`);
    if (setting.removed?.length)
      parts.push(`removed ${setting.removed.map((v) => `"${v}"`).join(', ')}`);
    return `${label}: ${parts.join(', ')}.`;
  }

  if (setting.op === 'Added') return `${label} was added and set to "${after}".`;
  if (setting.op === 'Removed') return `${label} was removed (was "${before}").`;
  return `${label} was changed from "${before}" to "${after}".`;
}

export function formatBytes(bytes: number | null): string | null {
  if (bytes == null) return null;
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}

export function describeFile(file: ApiSnapshotFileChange): string {
  const before = formatBytes(file.sizeBefore);
  const after = formatBytes(file.sizeAfter);

  if (file.op === 'Added') return `"${file.name}" was added${after ? ` (${after})` : ''}.`;
  if (file.op === 'Removed') return `"${file.name}" was removed.`;
  return before && after
    ? `"${file.name}" was replaced (${before} to ${after}).`
    : `"${file.name}" was replaced.`;
}

/**
 * What happened to a hunk's objects, from its counts: "2 objects added, 1 removed", or with
 * `changed` for the ones still there, e.g. "3 objects retimed or reshaped, 1 added". Objects
 * that were added or removed are counted as such, never as retimed.
 */
function objectSummary(hunk: ApiSnapshotHunk, changed: string): string {
  const parts = [
    [hunk.counts.changed, changed],
    [hunk.counts.added, 'added'],
    [hunk.counts.removed, 'removed'],
  ] as const;

  return parts
    .filter(([n]) => n > 0)
    .map(([n, what], i) => (i === 0 ? `${countWord(n, 'object')} ${what}` : `${n} ${what}`))
    .join(', ');
}

/**
 * What changed on a hunk's timing lines, per kind of change, e.g. "Volume 100% to 80% on 2 timing
 * lines". The values are named when every line went from the same one to the same one.
 */
function lineSummaries(changes: ApiSnapshotChange[]): string[] {
  const kinds = [
    { what: 'Volume', fields: ['RedLineVolume', 'GreenLineVolume'], unit: '%' },
    { what: 'Sampleset', fields: ['RedLineSampleset', 'GreenLineSampleset'], unit: '' },
  ];

  return kinds.flatMap(({ what, fields, unit }) => {
    const found = changes.filter((c) => fields.includes(c.field ?? ''));
    if (found.length === 0) return [];

    const lines = countWord(new Set(found.map((c) => c.time)).size, 'timing line');
    const befores = new Set(found.map((c) => c.before));
    const afters = new Set(found.map((c) => c.after));

    return befores.size === 1 && afters.size === 1 && what === 'Volume'
      ? [`${what} ${found[0].before}${unit} to ${found[0].after}${unit} on ${lines}`]
      : [`${what} changed on ${lines}`];
  });
}

/** The one-line summary of a hunk, from what dominates it. */
export function hunkSummary(hunk: ApiSnapshotHunk): string {
  const { added, removed, changed } = hunk.counts;
  const objects = hunk.changes.filter((c) => c.object);

  switch (hunk.label) {
    case 'Shifted':
      // A group is never a single object, so no singular is needed.
      return `${shiftedCount(hunk)} objects and timing lines moved ${formatShift(hunkShift(hunk) ?? 0)}`;
    case 'Volume':
      return `${volumeGroupChange(hunk)} on ${shiftedCount(hunk)} timing lines`;
    case 'Remapped':
      return objectSummary(hunk, 'changed');
    case 'Hitsounding': {
      // Objects and timing lines are told apart: a volume pass on green lines touches no object.
      const touched = new Set(objects.map((c) => c.object!.stamp)).size;
      const parts = [
        ...(touched > 0 ? [`Hit sounds changed on ${countWord(touched, 'object')}`] : []),
        ...lineSummaries(hunk.changes.filter((c) => !c.object)),
      ];
      if (parts.length === 0) return 'Hit sounds changed';

      return parts.map((p, i) => (i === 0 ? p : p.toLowerCase())).join(', ');
    }
    case 'Placement':
      // In taiko the placement of a note is its colour and size.
      if (hunk.changes.every((c) => c.field === 'Note'))
        return `${countWord(hunk.changes.length, 'note')} changed colour or size`;
      return objectSummary(hunk, 'moved or reshaped');
    case 'Rhythm':
      return objectSummary(hunk, 'retimed or reshaped');
    case 'Sv': {
      // What the lines were and what they are now, as one value or a range: "SV 0.5x to
      // 0.2x–0.8x on 4 timing lines" says a flat stretch became a ramp. Green lines added or
      // removed are counted with the SV they have or had.
      const range = (values: number[]) => {
        const lo = Math.min(...values);
        const hi = Math.max(...values);
        return lo === hi
          ? `${trimNumber(String(lo))}x`
          : `${trimNumber(String(lo))}x–${trimNumber(String(hi))}x`;
      };
      const green = (op: string) =>
        hunk.changes.filter((c) => c.field === 'GreenLine' && c.op === op);
      const sv = hunk.changes.filter(
        (c) => c.field === 'Sv' && c.before != null && c.after != null
      );
      const newLines = green('Added').filter((c) => c.after != null);
      const goneLines = green('Removed').filter((c) => c.before != null);

      const parts = [
        ...(sv.length > 0
          ? [
              `SV ${range(sv.map((c) => Number(c.before)))} to ${range(sv.map((c) => Number(c.after)))}${
                sv.length > 1 ? ` on ${sv.length} timing lines` : ''
              }`,
            ]
          : []),
        ...(newLines.length > 0
          ? [
              `${countWord(newLines.length, 'timing line')} added (${range(newLines.map((c) => Number(c.after)))})`,
            ]
          : []),
        ...(goneLines.length > 0
          ? [
              `${countWord(goneLines.length, 'timing line')} removed (${range(goneLines.map((c) => Number(c.before)))})`,
            ]
          : []),
      ];
      if (parts.length > 0) return parts.join(', ');
      return `${countWord(hunk.changes.length, 'timing change')}`;
    }
    default: {
      const parts: string[] = [];
      if (added) parts.push(`${added} added`);
      if (removed) parts.push(`${removed} removed`);
      if (changed) parts.push(`${changed} changed`);
      return parts.join(', ');
    }
  }
}

/** A hunk that is one object or line added or removed, which says it all in its own sentence. */
function singleAddOrRemove(hunk: ApiSnapshotHunk) {
  const lines = toChangeLines(hunk.changes);
  return lines.length === 1 && lines[0].op !== 'Changed' ? lines[0] : null;
}

/** The headline of a hunk's row: its summary as a sentence, saying when it is a remap. */
export function hunkHeadline(hunk: ApiSnapshotHunk): string {
  if (hunk.label === 'Shifted') return describeShift(hunk);
  if (hunk.label === 'Volume') return describeVolumeGroup(hunk);
  if (hunk.label === 'Remapped') return `Remapped: ${hunkSummary(hunk)}.`;

  // "Circle removed." rather than "1 object removed." with the same again below it. SV rows say
  // what the line's SV is instead, as their table does.
  const single = hunk.label === 'Sv' ? null : singleAddOrRemove(hunk);
  if (single) return single.text;

  if (hunk.label === 'Mixed' && hunk.kinds.every((k) => k === 'Rhythm' || k === 'Placement'))
    return `${objectSummary(hunk, 'retimed and moved')}.`;
  return `${hunkSummary(hunk)}.`;
}

/**
 * The line under a hunk's headline, when there is one thing worth saying without opening it:
 * the change itself when there is only one (an addition or removal is already the headline).
 */
export function hunkDetail(hunk: ApiSnapshotHunk): string | null {
  if (hunk.label === 'Shifted') return describeShiftDetail(hunk);
  if (hunk.label === 'Volume') return describeVolumeGroupDetail(hunk);
  if (singleAddOrRemove(hunk)) return null;

  const lines = toChangeLines(hunk.changes);
  return lines.length === 1 && lines[0].details.length === 0 ? lines[0].text : null;
}

/** How check results moved since the snapshot before, e.g. "+1 problem, -3 warnings". */
export function describeCheckDelta(
  checks: ApiSnapshotChecks | null,
  previous: ApiSnapshotChecks | null
): { text: string; better: boolean | null } | null {
  if (!checks || !previous) return null;

  const parts: string[] = [];
  const problems = checks.problems - previous.problems;
  const warnings = checks.warnings - previous.warnings;
  const sign = (n: number) => (n > 0 ? `+${n}` : `−${Math.abs(n)}`);

  if (problems !== 0)
    parts.push(`${sign(problems)} ${problems === 1 || problems === -1 ? 'problem' : 'problems'}`);
  if (warnings !== 0)
    parts.push(`${sign(warnings)} ${warnings === 1 || warnings === -1 ? 'warning' : 'warnings'}`);
  if (parts.length === 0) return null;

  const worse = problems > 0 || (problems === 0 && warnings > 0);
  return { text: parts.join(', '), better: !worse };
}

export const TRIGGER_LABEL: Record<string, string> = {
  checkRun: 'Check run',
  pageOpen: 'Opened',
  manual: 'Manual',
  import: 'Imported',
};

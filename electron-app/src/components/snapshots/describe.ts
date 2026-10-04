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

export function describeRollup(rollup: ApiSnapshotRollup, difficulties?: number): string {
  const sign = rollup.amount > 0 ? '+' : '';
  const amount = `${sign}${Math.round(rollup.amount * 100) / 100} ms`;
  const scope = difficulties && difficulties > 1 ? ` in all ${difficulties} difficulties` : '';

  return `Everything was shifted ${amount}${scope}.`;
}

export function describeRollupDetail(rollup: ApiSnapshotRollup): string {
  return `${rollup.absorbed} objects and timing lines moved by the same amount, so they are not listed one by one.`;
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

/** The one-line summary of a hunk, from what dominates it. */
export function hunkSummary(hunk: ApiSnapshotHunk): string {
  const { added, removed, changed } = hunk.counts;
  const objects = hunk.changes.filter((c) => c.object);

  switch (hunk.label) {
    case 'Remapped':
      return `${countWord(added, 'object')} added, ${removed} removed`;
    case 'Hitsounding': {
      const touched = new Set(objects.map((c) => c.object!.stamp)).size || changed;
      return touched > 0
        ? `Hit sounds changed on ${countWord(touched, 'object')}`
        : 'Hit sounds changed';
    }
    case 'Placement':
      // In taiko the placement of a note is its colour and size.
      if (hunk.changes.every((c) => c.field === 'Note'))
        return `${countWord(hunk.changes.length, 'note')} changed colour or size`;
      return `${countWord(changed + added + removed, 'object')} moved or reshaped`;
    case 'Rhythm':
      return `${countWord(changed + added + removed, 'object')} retimed or reshaped`;
    case 'Sv': {
      const sv = hunk.changes.filter((c) => c.field === 'Sv');
      if (sv.length > 0) {
        const first = sv[0];
        const last = sv[sv.length - 1];
        return `SV ${trimNumber(first.before)}x to ${trimNumber(last.after)}x`;
      }
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

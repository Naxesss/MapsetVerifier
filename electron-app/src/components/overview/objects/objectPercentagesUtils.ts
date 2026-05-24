import type { Mode, ObjectsOverviewDifficulty, ObjectsTypeBucket, ObjectsTypeEntry } from '../../../Types';

const HIT_SOUND_WHISTLE = 2;
const HIT_SOUND_FINISH = 4;
const HIT_SOUND_CLAP = 8;

export const TAIKO_TYPE_LABELS = {
  dons: 'Dons',
  kats: 'Kats',
  bigDons: 'Big Dons',
  bigKats: 'Big Kats',
  sliders: 'Sliders',
  spinners: 'Spinners',
} as const;

export type ObjectPercentageColumn =
  | { type: 'single'; label: string }
  | { type: 'combined'; label: string; labels: string[] };

export const OBJECT_PERCENTAGE_COLUMNS: Record<Mode, ObjectPercentageColumn[]> = {
  Standard: [
    { type: 'single', label: 'Circles' },
    { type: 'single', label: 'Sliders' },
    { type: 'single', label: 'Spinners' },
  ],
  Taiko: [
    { type: 'single', label: TAIKO_TYPE_LABELS.dons },
    { type: 'single', label: TAIKO_TYPE_LABELS.kats },
    { type: 'combined', label: 'Dons + Kats', labels: [TAIKO_TYPE_LABELS.dons, TAIKO_TYPE_LABELS.kats] },
    { type: 'single', label: TAIKO_TYPE_LABELS.bigDons },
    { type: 'single', label: TAIKO_TYPE_LABELS.bigKats },
    {
      type: 'combined',
      label: 'Big Notes',
      labels: [TAIKO_TYPE_LABELS.bigDons, TAIKO_TYPE_LABELS.bigKats],
    },
    { type: 'single', label: TAIKO_TYPE_LABELS.sliders },
    { type: 'single', label: TAIKO_TYPE_LABELS.spinners },
  ],
  Catch: [
    { type: 'single', label: 'Fruits' },
    { type: 'single', label: 'Slider heads' },
    { type: 'single', label: 'Slider repeats' },
    { type: 'single', label: 'Slider tails' },
    { type: 'single', label: 'Droplets' },
    { type: 'single', label: 'Spinners' },
  ],
  Mania: [
    { type: 'single', label: 'Notes' },
    { type: 'single', label: 'Hold notes' },
  ],
};

function buildTypeBucketMap(buckets: ObjectsTypeBucket[]) {
  return new Map(buckets.map((bucket) => [bucket.label, bucket]));
}

function finalizeTypeBuckets(
  counts: Map<string, number>,
  entriesByLabel: Map<string, ObjectsTypeEntry[]>
): ObjectsTypeBucket[] {
  const total = Math.max(
    1,
    [...counts.values()].reduce((sum, count) => sum + count, 0)
  );

  return [...counts.entries()]
    .filter(([, count]) => count > 0)
    .map(([label, count]) => ({
      label,
      count,
      percentage: (count * 100) / total,
      entries: [...(entriesByLabel.get(label) ?? [])].sort(
        (left, right) => left.timeMs - right.timeMs || left.detail.localeCompare(right.detail)
      ),
    }));
}

function addEntry(
  counts: Map<string, number>,
  entriesByLabel: Map<string, ObjectsTypeEntry[]>,
  label: string,
  timeMs: number,
  detail: string
) {
  counts.set(label, (counts.get(label) ?? 0) + 1);

  const entries = entriesByLabel.get(label);
  if (entries) {
    entries.push({ timeMs, detail });
  } else {
    entriesByLabel.set(label, [{ timeMs, detail }]);
  }
}

function deriveObjectTypesFromTimeline(
  difficulty: ObjectsOverviewDifficulty,
  mode: Mode
): ObjectsTypeBucket[] {
  const counts = new Map<string, number>();
  const entriesByLabel = new Map<string, ObjectsTypeEntry[]>();

  switch (mode) {
    case 'Standard':
      for (const object of difficulty.timelineObjects) {
        if (object.objectType === 'Circle') {
          addEntry(counts, entriesByLabel, 'Circles', object.startTimeMs, 'Circle');
        } else if (object.objectType === 'Slider') {
          addEntry(counts, entriesByLabel, 'Sliders', object.startTimeMs, 'Slider');
        } else if (object.objectType === 'Spinner') {
          addEntry(counts, entriesByLabel, 'Spinners', object.startTimeMs, 'Spinner');
        }
      }
      break;

    case 'Taiko':
      for (const object of difficulty.timelineObjects) {
        if (object.objectType === 'Spinner') {
          addEntry(counts, entriesByLabel, TAIKO_TYPE_LABELS.spinners, object.startTimeMs, 'Spinner');
          continue;
        }

        if (object.objectType === 'Slider') {
          addEntry(counts, entriesByLabel, TAIKO_TYPE_LABELS.sliders, object.startTimeMs, 'Slider');
          continue;
        }

        if (object.objectType !== 'Circle') continue;

        const flags = object.hitSoundFlags ?? 0;
        const isKat = (flags & HIT_SOUND_WHISTLE) !== 0 || (flags & HIT_SOUND_CLAP) !== 0;
        const isBig = object.hasFinishHitSound || (flags & HIT_SOUND_FINISH) !== 0;
        const detail = isBig
          ? isKat
            ? 'Big Kat'
            : 'Big Don'
          : isKat
            ? 'Kat'
            : 'Don';
        const label = isBig
          ? isKat
            ? TAIKO_TYPE_LABELS.bigKats
            : TAIKO_TYPE_LABELS.bigDons
          : isKat
            ? TAIKO_TYPE_LABELS.kats
            : TAIKO_TYPE_LABELS.dons;

        addEntry(counts, entriesByLabel, label, object.startTimeMs, detail);
      }
      break;

    case 'Catch':
      for (const object of difficulty.timelineObjects) {
        if (object.objectType === 'Circle') {
          addEntry(counts, entriesByLabel, 'Fruits', object.startTimeMs, 'Fruit');
          continue;
        }

        if (object.objectType === 'Spinner') {
          addEntry(counts, entriesByLabel, 'Spinners', object.startTimeMs, 'Spinner');
          continue;
        }

        if (object.objectType !== 'Slider') continue;

        addEntry(counts, entriesByLabel, 'Slider heads', object.startTimeMs, 'Slider head');
        for (const edge of object.edges) {
          if (edge.partName.endsWith(' reverse')) {
            addEntry(counts, entriesByLabel, 'Slider repeats', edge.timeMs, 'Slider repeat');
          } else if (edge.partName.endsWith(' tail')) {
            addEntry(counts, entriesByLabel, 'Slider tails', edge.timeMs, 'Slider tail');
          }
        }
      }
      break;

    case 'Mania':
      for (const object of difficulty.timelineObjects) {
        if (object.objectType === 'Hold note') {
          addEntry(counts, entriesByLabel, 'Hold notes', object.startTimeMs, 'Hold note');
        } else {
          addEntry(counts, entriesByLabel, 'Notes', object.startTimeMs, 'Note');
        }
      }
      break;
  }

  return finalizeTypeBuckets(counts, entriesByLabel);
}

export function getObjectTypeBuckets(
  difficulty: ObjectsOverviewDifficulty,
  mode: Mode
): ObjectsTypeBucket[] {
  if (difficulty.objectTypes && difficulty.objectTypes.length > 0) {
    return difficulty.objectTypes;
  }

  return deriveObjectTypesFromTimeline(difficulty, mode);
}

export function resolveObjectPercentageValue(
  buckets: ObjectsTypeBucket[],
  column: ObjectPercentageColumn
): { count: number; percentage: number } {
  const bucketMap = buildTypeBucketMap(buckets);
  const total = Math.max(
    1,
    buckets.reduce((sum, bucket) => sum + bucket.count, 0)
  );

  if (column.type === 'single') {
    const bucket = bucketMap.get(column.label);
    const count = bucket?.count ?? 0;
    return { count, percentage: (count * 100) / total };
  }

  const count = column.labels.reduce((sum, label) => sum + (bucketMap.get(label)?.count ?? 0), 0);
  return { count, percentage: (count * 100) / total };
}

export function resolveObjectPercentageEntries(
  buckets: ObjectsTypeBucket[],
  column: ObjectPercentageColumn
): ObjectsTypeEntry[] {
  const bucketMap = buildTypeBucketMap(buckets);
  const labels = column.type === 'single' ? [column.label] : column.labels;

  return labels
    .flatMap((label) => bucketMap.get(label)?.entries ?? [])
    .sort((left, right) => left.timeMs - right.timeMs || left.detail.localeCompare(right.detail));
}

export function bucketsHaveClickableEntries(buckets: ObjectsTypeBucket[]) {
  return buckets.some((bucket) => (bucket.entries?.length ?? 0) > 0);
}

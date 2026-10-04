import { Button, Group, SegmentedControl, Stack, Text } from '@mantine/core';
import { IconClockEdit, IconEqual, IconPlus, IconMinus } from '@tabler/icons-react';
import { useMemo, useState } from 'react';
import ChangeCounts from './ChangeCounts';
import { describeRollup, describeRollupDetail } from './describe';
import HunkRow from './HunkRow';
import SettingLines from './SettingLines';
import { countWord } from '../../utils/countWord';
import EmptyState from '../common/EmptyState.tsx';
import { MicroLabel } from '../common/Headings.tsx';
import SectionCard from '../common/SectionCard.tsx';
import StarRatingBadge from '../common/StarRatingBadge.tsx';
import type {
  ApiSnapshotDifficultyComparison,
  ApiSnapshotGeneralComparison,
  ApiSnapshotHunk,
} from '../../Types';

interface DifficultyChangesProps {
  difficulty: ApiSnapshotDifficultyComparison;
  /** Which snapshots the comparison is between, to read the objects around a change from. */
  setKey: string;
  baseId: string;
  targetId: string;
  general: ApiSnapshotGeneralComparison;
  onViewGeneral: () => void;
}

function Stat({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <Stack gap="2xs">
      <MicroLabel>{label}</MicroLabel>
      {children}
    </Stack>
  );
}

function BeforeAfter({ before, after }: { before: React.ReactNode; after: React.ReactNode }) {
  return (
    <Group gap="xs" wrap="nowrap">
      {before}
      <Text size="sm" c="dimmed">
        {'→'}
      </Text>
      {after}
    </Group>
  );
}

/** What changed in one difficulty: its numbers, settings, and the changes as hunks in song time. */
/** The kinds of work a hunk can be: shaping objects, hitsounding, or timing and SV. */
type Category = 'all' | 'objects' | 'hitsound' | 'timing';

function categoryOf(hunk: ApiSnapshotHunk): Exclude<Category, 'all'> {
  if (hunk.kinds.includes('Hitsound')) return 'hitsound';
  if (hunk.kinds.includes('Timing')) return 'timing';
  return 'objects';
}

const CATEGORY_LABEL: Record<Category, string> = {
  all: 'All',
  objects: 'Objects',
  hitsound: 'Hitsounds',
  timing: 'Timing & SV',
};

export default function DifficultyChanges({
  difficulty,
  setKey,
  baseId,
  targetId,
  general,
  onViewGeneral,
}: DifficultyChangesProps) {
  // A time shift every difficulty shares was pulled up into General; it still moved these objects.
  const shift = (difficulty.rollups[0] ?? general.rollups[0])?.amount ?? 0;
  const [category, setCategory] = useState<Category>('all');

  const inRange = difficulty.hunks;

  // How many hunks of each kind of work there are, so the switch only offers what exists.
  const categoryCounts = useMemo(() => {
    const counts: Record<Exclude<Category, 'all'>, number> = { objects: 0, hitsound: 0, timing: 0 };
    for (const hunk of inRange) counts[categoryOf(hunk)]++;
    return counts;
  }, [inRange]);

  // A category that has nothing left (another range or filter was picked) falls back to all.
  const activeCategory = category !== 'all' && categoryCounts[category] === 0 ? 'all' : category;
  const hunks = useMemo(
    () =>
      activeCategory === 'all' ? inRange : inRange.filter((h) => categoryOf(h) === activeCategory),
    [inRange, activeCategory]
  );

  if (difficulty.status === 'Added' || difficulty.status === 'Removed') {
    const added = difficulty.status === 'Added';

    return (
      <SectionCard title="Changes">
        <EmptyState
          icon={added ? IconPlus : IconMinus}
          title={added ? 'This difficulty was added' : 'This difficulty was removed'}
          description={`${countWord(added ? difficulty.objectsAfter : difficulty.objectsBefore, 'object')} ${
            added ? 'in it now' : 'in it before'
          }.`}
        />
      </SectionCard>
    );
  }

  const generalHasChanges =
    general.rollups.length > 0 || general.settings.length > 0 || general.files.length > 0;
  const nothing =
    hunks.length === 0 && difficulty.settings.length === 0 && difficulty.rollups.length === 0;

  const starsChanged =
    difficulty.starsBefore != null &&
    difficulty.starsAfter != null &&
    Math.abs(difficulty.starsBefore - difficulty.starsAfter) >= 0.005;

  return (
    <Stack gap="md">
      {(starsChanged || difficulty.objectsBefore !== difficulty.objectsAfter) && (
        <Group gap="xl" align="flex-start">
          {starsChanged && (
            <Stat label="Star rating">
              <BeforeAfter
                before={<StarRatingBadge rating={difficulty.starsBefore!} />}
                after={<StarRatingBadge rating={difficulty.starsAfter!} />}
              />
            </Stat>
          )}
          {difficulty.objectsBefore !== difficulty.objectsAfter && (
            <Stat label="Objects">
              <BeforeAfter
                before={<Text size="sm">{difficulty.objectsBefore}</Text>}
                after={<Text size="sm">{difficulty.objectsAfter}</Text>}
              />
            </Stat>
          )}
        </Group>
      )}

      {generalHasChanges && (
        <Group
          gap="sm"
          wrap="nowrap"
          px="sm"
          py="xs"
          style={{
            border: '1px dashed var(--mantine-color-dark-3)',
            borderRadius: 'var(--mantine-radius-md)',
          }}
        >
          <MicroLabel>Also in General</MicroLabel>
          <Text size="sm" c="dimmed" truncate style={{ flex: 1, minWidth: 0 }}>
            {[
              general.rollups.length > 0 ? 'time shift' : null,
              general.settings.length > 0
                ? countWord(general.settings.length, 'shared setting')
                : null,
              general.files.length > 0 ? countWord(general.files.length, 'file change') : null,
            ]
              .filter(Boolean)
              .join(', ')}
          </Text>
          <Button variant="subtle" size="compact-sm" onClick={onViewGeneral}>
            View in General
          </Button>
        </Group>
      )}

      {difficulty.rollups.length > 0 && (
        <SectionCard title="Timing">
          <Stack gap="xs">
            {difficulty.rollups.map((rollup) => (
              <Group key={rollup.kind + rollup.amount} gap="sm" wrap="nowrap" align="flex-start">
                <IconClockEdit size={20} stroke={1.5} style={{ flexShrink: 0, marginTop: 2 }} />
                <Stack gap={2}>
                  <Text size="sm" fw={500}>
                    {describeRollup(rollup)}
                  </Text>
                  <Text size="xs" c="dimmed">
                    {describeRollupDetail(rollup)}
                  </Text>
                </Stack>
              </Group>
            ))}
          </Stack>
        </SectionCard>
      )}

      {difficulty.settings.length > 0 && (
        <SectionCard title="Settings">
          <SettingLines settings={difficulty.settings} />
        </SectionCard>
      )}

      {/* Settings or timing alone are shown by their own cards; an empty list adds nothing. */}
      {(nothing || hunks.length > 0) && (
        <SectionCard
          title="Changes"
          actions={
            <>
              <ChangeCounts counts={difficulty.counts} empty={null} />
            </>
          }
        >
          {nothing ? (
            <EmptyState
              icon={IconEqual}
              title="No changes"
              description="This difficulty did not change in this range."
            />
          ) : (
            <Stack gap="xs">
              {Object.values(categoryCounts).filter((n) => n > 0).length > 1 && (
                <SegmentedControl
                  size="xs"
                  value={activeCategory}
                  onChange={(value) => setCategory(value as Category)}
                  data={[
                    { value: 'all', label: `${CATEGORY_LABEL.all} ${inRange.length}` },
                    ...(['objects', 'hitsound', 'timing'] as const)
                      .filter((c) => categoryCounts[c] > 0)
                      .map((c) => ({
                        value: c,
                        label: `${CATEGORY_LABEL[c]} ${categoryCounts[c]}`,
                      })),
                  ]}
                  style={{ alignSelf: 'flex-start' }}
                />
              )}
              {hunks.map((hunk, index) => (
                <HunkRow
                  context={{ setKey, baseId, targetId, difficultyKey: difficulty.key }}
                  shift={shift}
                  key={`${hunk.start}-${hunk.label}-${hunk.kinds.join()}`}
                  hunk={hunk}
                  mode={difficulty.mode}
                  defaultOpen={index === 0 && hunks.length <= 3}
                />
              ))}
            </Stack>
          )}
        </SectionCard>
      )}
    </Stack>
  );
}

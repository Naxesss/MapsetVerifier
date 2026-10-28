import { Button, Group, Stack, Text } from '@mantine/core';
import { IconClockEdit, IconEqual, IconPlus, IconMinus } from '@tabler/icons-react';
import { useMemo } from 'react';
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
  ApiSnapshotRollup,
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

/**
 * The kinds of work a hunk can be: a part moved in time, shaping objects, hitsounding, or timing
 * and SV. A hunk is always one of them, so the changes are listed per kind, in this order.
 */
type Category = 'moved' | 'objects' | 'hitsound' | 'timing';

const CATEGORY_ORDER: Category[] = ['moved', 'objects', 'hitsound', 'timing'];

function categoryOf(hunk: ApiSnapshotHunk): Category {
  if (hunk.label === 'Shifted') return 'moved';
  if (hunk.kinds.includes('Hitsound')) return 'hitsound';
  if (hunk.kinds.includes('Timing')) return 'timing';
  return 'objects';
}

const CATEGORY_LABEL: Record<Category, string> = {
  moved: 'Moved in time',
  objects: 'Objects',
  hitsound: 'Hitsounds',
  timing: 'Timing & SV',
};

/** What changed in one difficulty: its numbers, settings, and the changes per kind of work. */
export default function DifficultyChanges({
  difficulty,
  setKey,
  baseId,
  targetId,
  general,
  onViewGeneral,
}: DifficultyChangesProps) {
  // A time shift every difficulty shares was pulled up into General; it still moved these objects.
  const offset = (rollups: ApiSnapshotRollup[]) => rollups.find((r) => r.kind === 'Offset');
  const shift = (offset(difficulty.rollups) ?? offset(general.rollups))?.amount ?? 0;
  const hunks = difficulty.hunks;

  // Each kind of work with its hunks, still in song time; kinds without any are left out.
  const sections = useMemo(
    () =>
      CATEGORY_ORDER.map((category) => ({
        category,
        hunks: hunks.filter((h) => categoryOf(h) === category),
      })).filter((s) => s.hunks.length > 0),
    [hunks]
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
              general.rollups.some((r) => r.kind === 'Offset') ? 'time shift' : null,
              general.rollups.some((r) => r.kind === 'Volume') ? 'volume change' : null,
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
            <Stack gap="md">
              {sections.map((section, sectionIndex) => (
                <Stack key={section.category} gap="xs">
                  <MicroLabel>
                    {CATEGORY_LABEL[section.category]} · {section.hunks.length}
                  </MicroLabel>
                  {section.hunks.map((hunk, index) => (
                    <HunkRow
                      context={{ setKey, baseId, targetId, difficultyKey: difficulty.key }}
                      shift={shift}
                      key={`${hunk.start}-${hunk.label}-${hunk.kinds.join()}`}
                      hunk={hunk}
                      mode={difficulty.mode}
                      defaultOpen={sectionIndex === 0 && index === 0 && hunks.length <= 3}
                    />
                  ))}
                </Stack>
              ))}
            </Stack>
          )}
        </SectionCard>
      )}
    </Stack>
  );
}

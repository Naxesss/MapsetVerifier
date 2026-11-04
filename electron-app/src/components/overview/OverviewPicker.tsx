import { Box, Group, Menu, Stack, Text, Tooltip, UnstyledButton } from '@mantine/core';
import { useElementSize } from '@mantine/hooks';
import { useMemo } from 'react';
import { OVERVIEW_SECTIONS, SECTION_ICONS, usesDifficultyPicks } from './overviewSections.ts';
import { useDifficultyPicks } from './useDifficultyPicks.ts';
import { useOverviewMode } from './useOverviewMode.ts';
import { useBeatmap } from '../../context/BeatmapContext.tsx';
import { useSettings } from '../../context/SettingsContext.tsx';
import { groupByMode } from '../../utils/gameMode.ts';
import DifficultyButtonRow, { type RowDifficulty } from '../common/DifficultyButtonRow.tsx';
import { PICKER_CHEVRON } from '../common/DifficultyPicker.tsx';
import { useMetadataAnalysis } from './metadata/hooks/useMetadataAnalysis.ts';
import type { OverviewTab } from '../navbar/pageHints.tsx';

interface OverviewPickerProps {
  section: OverviewTab;
  /** Pages with nothing to show for this mapset, with why; greyed out in the views. */
  unavailable: Partial<Record<OverviewTab, string>>;
  onSelect: (section: OverviewTab) => void;
}

const WHOLE_SET = 'This page covers the whole mapset';

/**
 * The Overview's header: the views, then the difficulties as on Checks and Snapshots
 * (DifficultyButtonRow). The views list every page, so any page is a click away from any other:
 * side by side when they fit, as a menu of their own when they don't. The difficulties pick
 * several at once, with All in General's place. A comparison stays within one mode, the one shown;
 * each mode keeps its own picks. Pages that cover the whole set keep the same header with the
 * difficulties greyed out; the picks still apply when going back to a page that compares
 * difficulties.
 */
export default function OverviewPicker({ section, unavailable, onSelect }: OverviewPickerProps) {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();
  // The metadata analysis is the lightest to list the mapset's difficulties, with mode and rating.
  const { data } = useMetadataAnalysis({ folder, songFolder: settings.songFolder });

  const groups = useMemo(() => {
    return groupByMode(data?.success ? data.difficulties : []).map((group) => ({
      ...group,
      difficulties: group.difficulties.sort((a, b) => (a.starRating ?? 0) - (b.starRating ?? 0)),
    }));
  }, [data]);
  type ModeGroup = (typeof groups)[number];

  const { selectedMode, setSelectedMode, selectedGroup } = useOverviewMode(groups);
  const modeDifficulties = selectedGroup?.difficulties ?? [];
  const { showAll, picked, toggle, restart } = useDifficultyPicks(modeDifficulties);

  const comparesDifficulties = usesDifficultyPicks(section);
  const allDifficulties = groups.reduce((total, group) => total + group.difficulties.length, 0);

  const versionsOf = (group: ModeGroup) => group.difficulties.map((d) => d.version);

  // No status to show on the Overview, so no status icons.
  const rowDifficulties: RowDifficulty[] = groups.flatMap((group) =>
    group.difficulties.map((d) => ({
      id: d.version,
      label: d.version,
      mode: group.mode,
      starRating: d.starRating,
      active:
        comparesDifficulties && group.mode === selectedMode && !showAll && picked.has(d.version),
      disabledReason: comparesDifficulties ? undefined : WHOLE_SET,
    }))
  );

  // The views side by side need their own width; otherwise they become a menu.
  const { ref: rowRef, width: rowWidth } = useElementSize();
  const { ref: segmentsRef, width: segmentsWidth } = useElementSize();
  const segmentsFit = segmentsWidth > 0 && segmentsWidth <= rowWidth;

  return (
    <Stack gap="sm" pos="relative">
      {/* Measures the view buttons, whichever form is shown. */}
      <Box
        ref={segmentsRef}
        pos="absolute"
        w="max-content"
        style={{ visibility: 'hidden', pointerEvents: 'none' }}
        aria-hidden
      >
        <ViewButtons section={section} unavailable={unavailable} onSelect={onSelect} />
      </Box>
      <Group ref={rowRef} gap="xs" wrap="nowrap">
        {segmentsFit ? (
          <ViewButtons section={section} unavailable={unavailable} onSelect={onSelect} />
        ) : (
          <ViewMenu section={section} unavailable={unavailable} onSelect={onSelect} />
        )}
      </Group>
      {allDifficulties > 1 && (
        <DifficultyButtonRow
          lead={{
            label: 'All',
            active: !comparesDifficulties || showAll,
            disabledReason: comparesDifficulties ? undefined : WHOLE_SET,
            onClick: () => selectedGroup && restart(versionsOf(selectedGroup)),
          }}
          difficulties={rowDifficulties}
          onPick={(d) => toggle(d.id)}
          shownMode={selectedMode}
          onShowMode={setSelectedMode}
          modesDisabledReason={comparesDifficulties ? undefined : WHOLE_SET}
        />
      )}
    </Stack>
  );
}

/**
 * Every page side by side as buttons with their icons, the one shown selected like General on
 * Checks. A page with nothing to show is greyed out and says why on hover, so it isn't truly
 * disabled.
 */
function ViewButtons({ section, unavailable, onSelect }: OverviewPickerProps) {
  return (
    // The difficulty picker's gap, so both rows of buttons are spaced alike.
    <Group gap={6} wrap="nowrap" style={{ flexShrink: 0 }}>
      {OVERVIEW_SECTIONS.map((s) => {
        const Icon = SECTION_ICONS[s];
        const selected = s === section;
        const reason = unavailable[s];
        return (
          <Tooltip key={s} label={reason} disabled={!reason}>
            <UnstyledButton
              className="mv-picker-button"
              aria-pressed={selected}
              aria-disabled={!!reason || undefined}
              data-active={selected || undefined}
              data-disabled={!!reason || undefined}
              onClick={() => !reason && onSelect(s)}
            >
              <Icon size={18} stroke={1.5} />
              {s}
            </UnstyledButton>
          </Tooltip>
        );
      })}
    </Group>
  );
}

/** The page shown, as a menu of every page. */
function ViewMenu({ section, unavailable, onSelect }: OverviewPickerProps) {
  const Icon = SECTION_ICONS[section];

  return (
    <Menu position="bottom-start" withinPortal>
      <Menu.Target>
        <UnstyledButton className="mv-picker-button">
          <Icon size={18} stroke={1.5} />
          {section}
          {PICKER_CHEVRON}
        </UnstyledButton>
      </Menu.Target>
      <Menu.Dropdown>
        {OVERVIEW_SECTIONS.map((s) => {
          const ItemIcon = SECTION_ICONS[s];
          return (
            <Menu.Item
              key={s}
              leftSection={<ItemIcon size={18} stroke={1.5} />}
              rightSection={
                unavailable[s] && (
                  <Text size="xs" c="dimmed">
                    {unavailable[s]}
                  </Text>
                )
              }
              disabled={!!unavailable[s]}
              style={s === section ? { background: 'var(--mantine-color-dark-5)' } : undefined}
              onClick={() => onSelect(s)}
            >
              {s}
            </Menu.Item>
          );
        })}
      </Menu.Dropdown>
    </Menu>
  );
}

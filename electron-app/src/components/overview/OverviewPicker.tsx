import { Box, Button, Group, Menu, Text, useMantineTheme } from '@mantine/core';
import { IconCheck } from '@tabler/icons-react';
import { useMemo } from 'react';
import { SECTION_ICONS, usesDifficultyPicks } from './overviewSections.ts';
import { useDifficultyPicks } from './useDifficultyPicks.ts';
import { useOverviewMode } from './useOverviewMode.ts';
import { useBeatmap } from '../../context/BeatmapContext.tsx';
import { useSettings } from '../../context/SettingsContext.tsx';
import { groupByMode } from '../../utils/gameMode.ts';
import { getDifficultyColor } from '../common/DifficultyColor.ts';
import DifficultyColorPill from '../common/DifficultyColorPill.tsx';
import {
  ModeMenu,
  PICKER_CHEVRON,
  PickerSeparator,
  StatusSegment,
  StatusSegmentBar,
} from '../common/DifficultyPicker.tsx';
import StarRatingBadge from '../common/StarRatingBadge.tsx';
import { useMetadataAnalysis } from './metadata/hooks/useMetadataAnalysis.ts';
import type { OverviewTab } from '../navbar/pageHints.tsx';

interface OverviewPickerProps {
  section: OverviewTab;
  onSelect: (section: OverviewTab) => void;
}

/** Difficulties the menu shows before it scrolls; its height fits that many rows. */
const DIFF_MENU_MAX_VISIBLE = 12;
const DIFF_MENU_MAX_HEIGHT = 360;

/**
 * The Overview's header, built like the Checks and Snapshots picker: the summary button, and for
 * pages that compare difficulties the game mode and difficulties as menus. You go into a page from
 * the summary and back with its button. The difficulty menu
 * picks several at once, and the bar underneath shows each difficulty as a segment in its star
 * rating colour: a tall segment is shown, and clicking one shows or hides it. Other pages dim the
 * bar.
 */
export default function OverviewPicker({ section, onSelect }: OverviewPickerProps) {
  const theme = useMantineTheme();
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

  const { selectedMode, setSelectedMode, selectedGroup } = useOverviewMode(groups);
  const modeDifficulties = selectedGroup?.difficulties ?? [];
  const { isShown, showAll, picked, toggle, clear } = useDifficultyPicks(modeDifficulties);

  const isSummary = section === 'Summary';
  const comparesDifficulties = usesDifficultyPicks(section);

  const pickedDifficulties = modeDifficulties.filter((d) => picked.has(d.version));
  const difficultiesLabel = showAll
    ? 'All difficulties'
    : pickedDifficulties.length === 1
      ? pickedDifficulties[0].version
      : `${pickedDifficulties.length} difficulties`;
  // Pages that don't compare difficulties show every difficulty of every mode in the bar, dimmed.
  const barDifficulties = comparesDifficulties
    ? modeDifficulties.map((difficulty) => ({ difficulty, groupStart: false }))
    : groups.flatMap((group) =>
        group.difficulties.map((difficulty, i) => ({ difficulty, groupStart: i === 0 }))
      );
  const menuStarRatings = modeDifficulties.flatMap((d) =>
    d.starRating != null ? [d.starRating] : []
  );

  return (
    <Box>
      <Group gap="xs" wrap="nowrap">
        <Button
          variant="default"
          size="sm"
          leftSection={<SECTION_ICONS.Summary size={18} stroke={1.5} />}
          aria-pressed={isSummary}
          style={{ flexShrink: 0, borderColor: isSummary ? theme.colors.dark[2] : undefined }}
          onClick={() => onSelect('Summary')}
        >
          Summary
        </Button>
        {comparesDifficulties && groups.length > 1 && (
          <>
            <PickerSeparator />
            <ModeMenu
              groups={groups.map((group) => ({
                mode: group.mode,
                count: group.difficulties.length,
              }))}
              active={selectedMode}
              onSelect={setSelectedMode}
            />
          </>
        )}
        {comparesDifficulties && modeDifficulties.length > 1 && (
          <>
            <PickerSeparator />
            <Menu position="bottom-start" withinPortal closeOnItemClick={false}>
              <Menu.Target>
                <Button
                  variant="default"
                  size="sm"
                  leftSection={
                    pickedDifficulties.length === 1 && !showAll ? (
                      <DifficultyColorPill
                        color={getDifficultyColor(pickedDifficulties[0].starRating ?? 0)}
                      />
                    ) : undefined
                  }
                  rightSection={PICKER_CHEVRON}
                  maw={340}
                  // Shrinks before anything else in a narrow window, cutting the name off with "…".
                  style={{ minWidth: 0 }}
                  styles={{ inner: { minWidth: 0 }, label: { minWidth: 0, overflow: 'hidden' } }}
                >
                  <Text span inherit truncate display="block">
                    {difficultiesLabel}
                  </Text>
                </Button>
              </Menu.Target>
              <Menu.Dropdown>
                <Menu.Item leftSection={<PickMark checked={showAll} />} onClick={clear}>
                  All difficulties
                </Menu.Item>
                <Menu.Divider />
                {/* Only a list that can't fit scrolls; see the difficulty menu of DifficultyPicker. */}
                <Box
                  mah={DIFF_MENU_MAX_HEIGHT}
                  style={
                    modeDifficulties.length > DIFF_MENU_MAX_VISIBLE
                      ? { overflowY: 'auto', overflowX: 'hidden' }
                      : undefined
                  }
                >
                  {modeDifficulties.map((d) => (
                    <Menu.Item
                      key={d.version}
                      leftSection={
                        <Group gap={6} wrap="nowrap">
                          <PickMark checked={!showAll && picked.has(d.version)} />
                          <DifficultyColorPill color={getDifficultyColor(d.starRating ?? 0)} />
                        </Group>
                      }
                      rightSection={
                        d.starRating != null && (
                          <StarRatingBadge
                            rating={d.starRating}
                            size="xs"
                            sizeTo={menuStarRatings}
                          />
                        )
                      }
                      onClick={() => toggle(d.version)}
                    >
                      {d.version}
                    </Menu.Item>
                  ))}
                </Box>
              </Menu.Dropdown>
            </Menu>
          </>
        )}
      </Group>
      <StatusSegmentBar>
        {barDifficulties.map(({ difficulty: d, groupStart }) => (
          <StatusSegment
            key={d.version}
            groupStart={groupStart}
            label={`${d.version}${d.starRating != null ? ` · ${d.starRating.toFixed(2)}★` : ''}`}
            starRating={d.starRating}
            color={
              comparesDifficulties ? getDifficultyColor(d.starRating ?? 0) : theme.colors.dark[4]
            }
            active={comparesDifficulties && isShown(d.version)}
            disabled={!comparesDifficulties}
            onClick={() => toggle(d.version)}
          />
        ))}
      </StatusSegmentBar>
    </Box>
  );
}

/** A check in front of a picked difficulty; unpicked ones keep the room so the names line up. */
function PickMark({ checked }: { checked: boolean }) {
  return <IconCheck size={14} stroke={2} style={{ opacity: checked ? 1 : 0 }} aria-hidden />;
}

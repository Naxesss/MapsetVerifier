import { Box, Button, Group, Menu, Text, Tooltip, useMantineTheme } from '@mantine/core';
import { useElementSize } from '@mantine/hooks';
import { IconCheck } from '@tabler/icons-react';
import { useMemo, useRef, useState } from 'react';
import { OVERVIEW_SECTIONS, SECTION_ICONS, usesDifficultyPicks } from './overviewSections.ts';
import { useDifficultyPicks } from './useDifficultyPicks.ts';
import { useOverviewMode } from './useOverviewMode.ts';
import { useBeatmap } from '../../context/BeatmapContext.tsx';
import { useSettings } from '../../context/SettingsContext.tsx';
import { formatGameModeLabel, groupByMode } from '../../utils/gameMode.ts';
import { getDifficultyColor } from '../common/DifficultyColor.ts';
import DifficultyColorPill from '../common/DifficultyColorPill.tsx';
import {
  PICKER_CHEVRON,
  PickerList,
  PickerModeGroup,
  StatusSegment,
  StatusSegmentBar,
  useScrollToPicked,
} from '../common/DifficultyPicker.tsx';
import StarRatingBadge from '../common/StarRatingBadge.tsx';
import GameModeIcon from '../icons/GameModeIcon.tsx';
import { useMetadataAnalysis } from './metadata/hooks/useMetadataAnalysis.ts';
import type { OverviewTab } from '../navbar/pageHints.tsx';

interface OverviewPickerProps {
  section: OverviewTab;
  /** Pages with nothing to show for this mapset, with why; greyed out in the views. */
  unavailable: Partial<Record<OverviewTab, string>>;
  onSelect: (section: OverviewTab) => void;
}

/**
 * Room kept free next to the views for the difficulty menu. The menu cuts its label off with "…"
 * below this.
 */
const DIFF_MENU_ROOM = 200;

/**
 * The Overview's header, built like the Checks and Snapshots picker: the views on the left, and
 * the difficulties as one menu on the right. The views list every page, so any page is a click
 * away from any other: side by side when they fit next to the menu, as a menu of their own when
 * they don't. The difficulty menu picks several at once, under a heading per game mode on hybrid
 * sets. A comparison stays within one mode, so a pick in another mode switches to it. The bar
 * underneath shows each difficulty as a segment in its star rating colour: a tall segment is
 * shown, clicking one shows or hides it, and the other modes' segments are grey and switch to
 * their mode. Pages that cover the whole set keep the same header with every difficulty shown
 * and the menu greyed out; the picks still apply when going back to a page that compares
 * difficulties.
 */
export default function OverviewPicker({ section, unavailable, onSelect }: OverviewPickerProps) {
  const theme = useMantineTheme();
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();
  // The metadata analysis is the lightest to list the mapset's difficulties, with mode and rating.
  const { data } = useMetadataAnalysis({ folder, songFolder: settings.songFolder });
  const [menuOpened, setMenuOpened] = useState(false);
  const listRef = useRef<HTMLDivElement>(null);
  useScrollToPicked(listRef, menuOpened);

  const groups = useMemo(() => {
    return groupByMode(data?.success ? data.difficulties : []).map((group) => ({
      ...group,
      difficulties: group.difficulties.sort((a, b) => (a.starRating ?? 0) - (b.starRating ?? 0)),
    }));
  }, [data]);
  type ModeGroup = (typeof groups)[number];

  const { selectedMode, setSelectedMode, selectedGroup } = useOverviewMode(groups);
  const modeDifficulties = selectedGroup?.difficulties ?? [];
  const { isShown, showAll, picked, toggle, restart } = useDifficultyPicks(modeDifficulties);

  const comparesDifficulties = usesDifficultyPicks(section);
  const isHybrid = groups.length > 1;
  const allDifficulties = groups.reduce((total, group) => total + group.difficulties.length, 0);

  const versionsOf = (group: ModeGroup) => group.difficulties.map((d) => d.version);
  // A comparison stays within one mode: a difficulty of another mode switches to that mode, with
  // just that difficulty picked. The other mode's picks are kept for when it's picked again.
  const pick = (group: ModeGroup, version: string) => {
    if (group.mode === selectedMode) {
      toggle(version);
      return;
    }
    setSelectedMode(group.mode);
    restart(versionsOf(group), version);
  };
  const showMode = (group: ModeGroup) => {
    setSelectedMode(group.mode);
    restart(versionsOf(group));
  };

  const pickedDifficulties = modeDifficulties.filter((d) => picked.has(d.version));
  const onePicked = !showAll && pickedDifficulties.length === 1 ? pickedDifficulties[0] : null;
  const difficultiesLabel = showAll
    ? 'All difficulties'
    : onePicked
      ? onePicked.version
      : `${pickedDifficulties.length} difficulties`;
  // Every star badge in the menu takes the width of the widest one, so they line up.
  const menuStarRatings = groups.flatMap((group) =>
    group.difficulties.flatMap((d) => (d.starRating != null ? [d.starRating] : []))
  );

  // The views side by side need their own width plus room for the menu. That room is kept on
  // every page, so the views don't change form when moving between pages with and without it.
  const { ref: rowRef, width: rowWidth } = useElementSize();
  const { ref: segmentsRef, width: segmentsWidth } = useElementSize();
  const menuRoom = allDifficulties > 1 ? DIFF_MENU_ROOM : 0;
  const segmentsFit = segmentsWidth > 0 && segmentsWidth + menuRoom <= rowWidth;

  return (
    <Box pos="relative">
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
        {/* The difficulties sit on the right, as on Checks. */}
        <Group gap="xs" wrap="nowrap" ml="auto" style={{ minWidth: 0 }}>
          {!comparesDifficulties && allDifficulties > 1 && (
            <WholeSetPick label="All difficulties" />
          )}
          {comparesDifficulties && allDifficulties > 1 && (
            <Menu
              trigger="hover"
              position="bottom-end"
              withinPortal
              closeOnItemClick={false}
              opened={menuOpened}
              onChange={setMenuOpened}
            >
              <Menu.Target>
                <Button
                  variant="default"
                  size="sm"
                  leftSection={
                    (isHybrid || onePicked) && (
                      <Group gap={6} wrap="nowrap">
                        {isHybrid && selectedMode && <GameModeIcon mode={selectedMode} size={18} />}
                        {onePicked && (
                          <DifficultyColorPill
                            color={getDifficultyColor(onePicked.starRating ?? 0)}
                          />
                        )}
                      </Group>
                    )
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
                {isHybrid && (
                  <Text size="xs" c="dimmed" px="sm" py={6} maw={300}>
                    Compares one mode at a time. A pick in another mode switches to it.
                  </Text>
                )}
                <PickerList
                  ref={listRef}
                  rows={allDifficulties + groups.length * (isHybrid ? 2 : 1)}
                >
                  {groups.map((group, i) => {
                    const current = group.mode === selectedMode;
                    const allShown = current && showAll;
                    return (
                      <PickerModeGroup
                        key={group.mode}
                        mode={group.mode}
                        count={group.difficulties.length}
                        withHeading={isHybrid}
                        divided={i > 0}
                      >
                        <Menu.Item
                          leftSection={<PickMark checked={allShown} />}
                          data-picked={allShown || undefined}
                          onClick={() => showMode(group)}
                        >
                          {isHybrid
                            ? `All ${group.difficulties.length} difficulties`
                            : 'All difficulties'}
                        </Menu.Item>
                        {!isHybrid && <Menu.Divider />}
                        {group.difficulties.map((d) => {
                          const on = current && !showAll && picked.has(d.version);
                          return (
                            <Menu.Item
                              key={d.version}
                              data-picked={on || undefined}
                              leftSection={
                                <Group gap={6} wrap="nowrap">
                                  <PickMark checked={on} />
                                  <DifficultyColorPill
                                    color={getDifficultyColor(d.starRating ?? 0)}
                                  />
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
                              onClick={() => pick(group, d.version)}
                            >
                              {d.version}
                            </Menu.Item>
                          );
                        })}
                      </PickerModeGroup>
                    );
                  })}
                </PickerList>
              </Menu.Dropdown>
            </Menu>
          )}
        </Group>
      </Group>
      {/* Every difficulty of every mode, a wider gap between modes. */}
      <StatusSegmentBar>
        {groups.flatMap((group, groupIndex) =>
          group.difficulties.map((d, i) => {
            const otherMode = comparesDifficulties && group.mode !== selectedMode;
            const rating = d.starRating != null ? ` · ${d.starRating.toFixed(2)}★` : '';
            return (
              <StatusSegment
                key={d.version}
                groupStart={groupIndex > 0 && i === 0}
                label={
                  otherMode
                    ? `${d.version}${rating} · Compare ${formatGameModeLabel(group.mode)}`
                    : `${d.version}${rating}`
                }
                starRating={d.starRating}
                color={otherMode ? theme.colors.dark[2] : getDifficultyColor(d.starRating ?? 0)}
                active={!comparesDifficulties || (!otherMode && isShown(d.version))}
                disabled={!comparesDifficulties}
                onClick={() => pick(group, d.version)}
              />
            );
          })
        )}
      </StatusSegmentBar>
    </Box>
  );
}

/**
 * Every page side by side as buttons with their icons, the one shown selected like General on
 * Checks. A page with nothing to show is greyed out and says why on hover, so it isn't truly
 * disabled.
 */
function ViewButtons({ section, unavailable, onSelect }: OverviewPickerProps) {
  const theme = useMantineTheme();

  return (
    <Group gap="xs" wrap="nowrap" style={{ flexShrink: 0 }}>
      {OVERVIEW_SECTIONS.map((s) => {
        const Icon = SECTION_ICONS[s];
        const selected = s === section;
        const reason = unavailable[s];
        return (
          <Tooltip key={s} label={reason} disabled={!reason}>
            <Button
              variant="default"
              size="sm"
              // Narrower than the other buttons, so the row fits in more windows.
              px="sm"
              leftSection={<Icon size={18} stroke={1.5} />}
              aria-pressed={selected}
              data-disabled={!!reason || undefined}
              style={
                selected
                  ? { borderColor: theme.colors.dark[2], background: 'var(--mantine-color-dark-5)' }
                  : undefined
              }
              onClick={() => !reason && onSelect(s)}
            >
              {s}
            </Button>
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
        <Button
          variant="default"
          size="sm"
          leftSection={<Icon size={18} stroke={1.5} />}
          rightSection={PICKER_CHEVRON}
          style={{ flexShrink: 0 }}
        >
          {section}
        </Button>
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

/**
 * The difficulties on a page that covers the whole set: greyed out and saying why on hover. It
 * keeps the menu's chevron, so the header doesn't move when switching pages.
 */
function WholeSetPick({ label }: { label: string }) {
  return (
    <Tooltip label="This page covers the whole mapset">
      <Button
        variant="default"
        size="sm"
        data-disabled
        rightSection={PICKER_CHEVRON}
        style={{ flexShrink: 0 }}
        onClick={(event) => event.preventDefault()}
      >
        {label}
      </Button>
    </Tooltip>
  );
}

/** A check in front of a picked difficulty; unpicked ones keep the room so the names line up. */
function PickMark({ checked }: { checked: boolean }) {
  return <IconCheck size={14} stroke={2} style={{ opacity: checked ? 1 : 0 }} aria-hidden />;
}

import {
  Alert,
  Text,
  Box,
  useMantineTheme,
  Group,
  SegmentedControl,
  Button,
  Flex,
  Loader,
  Badge,
  alpha,
  Tooltip,
  LoadingOverlay
} from '@mantine/core';
import {IconFolder, IconRefresh, IconStarFilled, IconWorld} from "@tabler/icons-react";
import React, {useEffect, useMemo} from 'react';
import { useParams } from 'react-router-dom';
import BeatmapHeader from './BeatmapHeader';
import ChecksResults from './ChecksResults';
import { useBeatmapBackground } from './hooks/useBeatmapBackground';
import { useBeatmapChecks } from './hooks/useBeatmapChecks';
import { useDifficultyOverride } from './hooks/useDifficultyOverride';
import { useSettings } from '../../context/SettingsContext.tsx';
import {ApiCategoryCheckResult, ApiCheckResult, DifficultyLevel, Level, Mode} from '../../Types';
import {getDifficultyColor} from "../common/DifficultyColor.ts";
import DifficultyName from "../common/DifficultyName.tsx";
import GameModeIcon from "../icons/GameModeIcon.tsx";
import LevelIcon from "../icons/LevelIcon.tsx";
import {invoke} from "@tauri-apps/api/core";

function getCategoryHighestLevel(checks: ApiCheckResult[], showMinor: boolean): Level {
  if (!Array.isArray(checks) || checks.length === 0) return 'Info';
  const effective = showMinor ? checks : checks.filter((c) => c.level !== 'Minor');
  const severityOrder: Level[] = ['Error', 'Problem', 'Warning', 'Minor', 'Info'];
  const normalizedLevels = effective.map((c) => (c.level === 'Check' ? 'Info' : c.level)) as Level[];
  for (const lvl of severityOrder) {
    if (normalizedLevels.includes(lvl)) {
      return lvl;
    }
  }
  return 'Info';
}

function getHighestLevel(levels: Level[]): Level {
  const severityOrder: Level[] = ['Error', 'Problem', 'Warning', 'Minor', 'Info'];
  for (const lvl of severityOrder) {
    if (levels.includes(lvl)) {
      return lvl;
    }
  }
  return 'Info';
}

function Checks() {
  const theme = useMantineTheme();
  const { folder } = useParams();
  const { settings } = useSettings();
  const [selectedCategory, setSelectedCategory] = React.useState<string | undefined>('General');
  const [hoveredDifficulty, setHoveredDifficulty] = React.useState<ApiCategoryCheckResult | undefined>(undefined);
  const [selectedMode, setSelectedMode] = React.useState<Mode | undefined>();
  
  useEffect(() => {
    // Reset selected category when changing beatmap
    if (folder) {
      setSelectedCategory('General');
      setHoveredDifficulty(undefined);
    }
  }, [folder])

  const { data, isLoading, isError, error, beatmapFolderPath, refetch } = useBeatmapChecks({
    folder,
    songFolder: settings.songFolder,
  });

  const { bgUrl } = useBeatmapBackground(folder);

  const {
    overrides,
    applyOverride,
    clearOverride,
    getOverrideResult,
    getOverrideLevel,
    isLoading: isOverrideLoading,
    reset: resetOverrides,
  } = useDifficultyOverride({ beatmapFolderPath });

  const showingDifficultyLevels: DifficultyLevel[] = ['Easy', 'Normal', 'Hard', 'Insane', 'Expert'];

  const selectedDifficulty = data?.difficulties?.find((d) => d.category === selectedCategory);
  const currentOverrideResult = selectedCategory ? getOverrideResult(selectedCategory) : undefined;
  const currentOverrideLevel = selectedCategory ? getOverrideLevel(selectedCategory) : undefined;

  const categoryHighestLevels = useMemo(() => {
    if (!data) return {};
    const levels: Record<string, Level> = {};
    levels['General'] = getCategoryHighestLevel(data.general.checkResults, settings.showMinor);
    for (const diff of data.difficulties) {
      // Check if there's an override result for this category
      const overrideResult = overrides[diff.category]?.result;
      if (overrideResult) {
        levels[diff.category] = getCategoryHighestLevel(overrideResult.categoryResult.checkResults, settings.showMinor);
      } else {
        levels[diff.category] = getCategoryHighestLevel(diff.checkResults, settings.showMinor);
      }
    }
    return levels;
  }, [data, settings.showMinor, overrides]);

  const groupedDifficulties = useMemo(() => {
    if (!data?.difficulties) return [];

    // Group difficulties by mode
    const modeGroups: Record<Mode, ApiCategoryCheckResult[]> = {
      Standard: [],
      Taiko: [],
      Catch: [],
      Mania: [],
    };

    for (const diff of data.difficulties) {
      const mode = diff.mode ?? 'Standard';
      modeGroups[mode].push(diff);
    }

    // Sort each group by star rating (ascending)
    for (const mode of Object.keys(modeGroups) as Mode[]) {
      modeGroups[mode].sort((a, b) => (a.starRating ?? 0) - (b.starRating ?? 0));
    }

    // Create ordered array of mode groups (only include modes that have difficulties)
    const orderedModes: Mode[] = ['Standard', 'Taiko', 'Catch', 'Mania'];

    
    const result = orderedModes
      .filter((mode) => modeGroups[mode].length > 0)
      .map((mode) => ({
        mode,
        difficulties: modeGroups[mode],
      }));
    setSelectedMode(result[0].mode)
    
    return result;
  }, [data?.difficulties]);
  const selectedGroup = groupedDifficulties.find((g) => g.mode === selectedMode) ?? groupedDifficulties[0];

  if (!folder) {
    return <Text>No BeatmapSet selected.</Text>;
  }

  if (!settings.songFolder) {
    return (
      <Alert color="yellow" title="Song folder not set" withCloseButton>
        <Text size="sm">Please set the song folder in settings to run beatmap checks.</Text>
      </Alert>
    );
  }

  return (
    <Box
      h="100%"
      style={{
        fontFamily: theme.headings.fontFamily,
        position: 'relative',
        width: '100%',
        borderRadius: theme.radius.lg,
        overflow: 'hidden',
        boxShadow: '0 4px 32px rgba(0,0,0,0.4)',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'flex-start',
      }}
    >
      <LoadingOverlay visible={isLoading} zIndex={1000} overlayProps={{ radius: "sm", blur: 2 }} />
      <BeatmapHeader data={data} bgUrl={bgUrl}>
        <Group gap="sm" mb="xs">
          <Group
            p="xs"
            gap="xs"
            bg={theme.colors.dark[8]}
            style={{ borderRadius: theme.radius.md }}
          >
            <Tooltip label="Reparse the beatmap">
              <Button
                size="xs"
                variant="default"
                onClick={async () => {
                  if (!beatmapFolderPath) return;
                  resetOverrides();
                  await refetch();
                }}
              >
                <IconRefresh />
              </Button>
            </Tooltip>
            <Button
              title="Open beatmap folder"
              size="xs"
              variant="default"
              onClick={async () => {
                if (!beatmapFolderPath) return;
                try {
                  await invoke('open_folder', { path: beatmapFolderPath });
                } catch (e) {
                  console.error('Failed to open folder:', e);
                  alert('Failed to open folder. See console for details.');
                }
              }}
              disabled={!beatmapFolderPath}
            >
              <IconFolder />
            </Button>
            <Button
              title="Open beatmap page"
              size="xs"
              variant="default"
              component="a"
              href={data?.beatmapSetId ? `https://osu.ppy.sh/beatmapsets/${data.beatmapSetId}` : undefined}
              target="_blank"
              rel="noopener noreferrer"
              disabled={!data?.beatmapSetId}
            >
              <IconWorld />
            </Button>
          </Group>
          <Group
            ml={'auto'}
            w={'unset'}
            gap="md"
            align="center"
          >
            <SegmentedControl
              radius="md"
              p="xs"
              data={groupedDifficulties.map((group) => {
                const groupLevels = group.difficulties.map((d) => categoryHighestLevels[d.category] || 'Info');
                const groupHighestLevel = getHighestLevel(groupLevels);
                return ({
                  label: (
                    <Flex gap="xs" align="center">
                      <LevelIcon level={groupHighestLevel} size={24}/>
                      <GameModeIcon mode={group.mode} size={24} />
                    </Flex>
                  ),
                  value: group.mode
                })
              })}
              value={selectedMode}
              onChange={(val) => setSelectedMode(val as Mode)}
              fullWidth={false}
            />
          </Group>
        </Group>
        {data?.difficulties && (
          <Group gap="xs">
            <Group
              p="xs"
              gap="xs"
              bg="hsl(200deg 10% 10% / 50%)"
              style={{ borderRadius: theme.radius.md }}
            >
              <Button
                h="fit-content"
                p="xs"
                variant="light"
                style={{
                  "--button-bg": `${alpha(theme.colors.blue[7], 0.25)}`,
                  "--button-hover": `${alpha(theme.colors.blue[7], 0.25)}`
                }}
                onClick={() => setSelectedCategory('General')}
                onMouseEnter={() => setHoveredDifficulty(undefined)}
                onMouseLeave={() => setHoveredDifficulty(selectedDifficulty)}
                bd={selectedCategory === 'General' || hoveredDifficulty === undefined ? '1px solid var(--mantine-color-blue-6)' : '1px solid transparent'}
              >
                <Flex gap="xs" align="center">
                  <LevelIcon level={categoryHighestLevels['General'] || 'Info'} size={24} />
                  <Text c="white">General</Text>
                </Flex>
              </Button>
              {selectedGroup.difficulties.map((diff) => (
                <Button
                  key={diff.category}
                  onClick={() => setSelectedCategory(diff.category)}
                  variant="light"
                  style={{
                    "--button-bg": `${alpha(getDifficultyColor(diff.starRating ?? 0), 0.25)}`,
                    "--button-hover": `${alpha(getDifficultyColor(diff.starRating ?? 0), 0.25)}`
                  }}
                  size="compact-md"
                  h="fit-content"
                  p="xs"
                  color={getDifficultyColor(diff.starRating ?? 0)}
                  onMouseEnter={() => setHoveredDifficulty(diff)}
                  onMouseLeave={() => setHoveredDifficulty(selectedDifficulty)}
                  bd={hoveredDifficulty === diff || selectedCategory === diff.category ? `1px solid ${getDifficultyColor(diff.starRating ?? 0)}` : '1px solid transparent'}
                >
                  <Flex gap="xs" align="center">
                    <LevelIcon level={categoryHighestLevels[diff.category] || 'Info'} size={24} />
                    <Text c="white" style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', maxWidth: 400 }}>{diff.category}</Text>
                  </Flex>
                </Button>
              ))}
            </Group>
          </Group>
        )}
        
      </BeatmapHeader>
      {data &&
        <Flex gap="sm" p="md" direction="column">
          {hoveredDifficulty && (selectedCategory !== 'General' || hoveredDifficulty.category !== selectedCategory) ? (
            <Flex gap="xs" align="center">
              <LevelIcon level={categoryHighestLevels[hoveredDifficulty.category] || 'Info'} size={32} />
              <GameModeIcon mode={hoveredDifficulty.mode!} size={32} starRating={hoveredDifficulty.starRating} />
              <Text maw="60%">{hoveredDifficulty.category}</Text>
              {hoveredDifficulty.difficultyLevel && (
                <Badge size="xs" color="grape" variant="light">
                  {currentOverrideResult
                    ? <DifficultyName difficulty={currentOverrideResult.categoryResult.difficultyLevel} mode={currentOverrideResult.categoryResult.mode} />
                    : <DifficultyName difficulty={hoveredDifficulty.difficultyLevel} mode={hoveredDifficulty.mode} />
                  }
                </Badge>
              )}
              {hoveredDifficulty.starRating != null && hoveredDifficulty.starRating > 0 && (
                <Badge size="xs" color="blue" variant="light" leftSection={<IconStarFilled size={10} />}>
                  {hoveredDifficulty.starRating.toFixed(2)}
                </Badge>
              )}
            </Flex>
          ) : <Flex gap="xs" align="center">
            <LevelIcon level={categoryHighestLevels['General'] || 'Info'} size={32} />
            <Text>General</Text>
          </Flex>}
          {selectedDifficulty &&
            <Group gap="sm" justify="flex-start" wrap="wrap">
              {(() => {
                const current = selectedDifficulty.difficultyLevel || 'Unknown';
                const selected = currentOverrideLevel || current;
                return (
                  <Group gap="md" align="center">
                    <Text size="sm" c="dimmed">Interpreted as</Text>
                    <SegmentedControl
                      radius="md"
                      data={showingDifficultyLevels.map(lvl => ({label: <DifficultyName difficulty={lvl} mode={selectedDifficulty.mode} />, value: lvl}))}
                      value={selected}
                      onChange={(val) => {
                        const isDefault = val === current || (current === 'Expert' && val === 'Ultra');
                        if (isDefault) {
                          clearOverride(selectedDifficulty.category);
                        } else {
                          applyOverride(selectedDifficulty.category, val);
                        }
                      }}
                      fullWidth={false}
                      styles={{ root: { maxWidth: '100%' } }}
                    />
                    {isOverrideLoading && <Loader size="xs" />}
                  </Group>
                );
              })()}
            </Group>
          }
          <ChecksResults
            data={data}
            isLoading={isLoading}
            isError={isError}
            error={error}
            showMinor={settings.showMinor}
            selectedCategory={selectedCategory}
            overrideResult={currentOverrideResult}
          />
        </Flex>
      }
    </Box>
  );
}

export default Checks;

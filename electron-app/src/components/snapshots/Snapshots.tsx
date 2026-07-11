import {
  Alert,
  Text,
  Box,
  useMantineTheme,
  Flex,
  LoadingOverlay,
  Group,
  Title,
} from '@mantine/core';
import { IconAlertCircle, IconPhotoOff } from '@tabler/icons-react';
import { useState, useMemo } from 'react';
import { useSnapshots } from './hooks/useSnapshots';
import SnapshotContent from './SnapshotContent';
import SnapshotGameModeSelector from './SnapshotGameModeSelector';
import {
  difficultyHasChangesAtCommit,
  difficultyHasSnapshot,
  generalHasChangesAtCommit,
  getSnapshotHistory,
} from './snapshotHistory';
import { useBeatmap } from '../../context/BeatmapContext';
import { useBeatmapReparse } from '../../context/BeatmapReparseRegistry.tsx';
import { useSettings } from '../../context/SettingsContext';
import { ApiSnapshotDifficulty, Mode } from '../../Types';
import BeatmapActionButtons, { SnapshotFolderTarget } from '../checks/BeatmapActionButtons';
import { useBeatmapBackground } from '../checks/hooks/useBeatmapBackground';
import BeatmapHeader from '../common/BeatmapHeader';
import DifficultyTabSelector from '../common/DifficultyTabSelector';
import NoBeatmapsetDisplay from '../common/NoBeatmapsetDisplay.tsx';
import StackTraceMessage from '../common/StackTraceMessage.tsx';
import GameModeIcon from '../icons/GameModeIcon';
import SnapshotDifficultyChangesIcon from '../icons/SnapshotDifficultyChangesIcon';

interface ModeGroup {
  mode: Mode;
  difficulties: ApiSnapshotDifficulty[];
}

function Snapshots() {
  const theme = useMantineTheme();
  const { selectedFolder: folder, beatmapFolderPath, beatmapInfo } = useBeatmap();
  const { triggerReparse } = useBeatmapReparse();
  const { settings } = useSettings();
  const [selectedDifficulty, setSelectedDifficulty] = useState<string | undefined>('General');
  const [selectedMode, setSelectedMode] = useState<Mode | undefined>();
  const [selectedCommitId, setSelectedCommitId] = useState<string | undefined>();

  const [prevFolder, setPrevFolder] = useState(folder);

  if (folder !== prevFolder) {
    setPrevFolder(folder);

    // Reset selected difficulty when changing beatmap
    if (folder) {
      setSelectedDifficulty('General');
      setSelectedCommitId(undefined);
    }
  }

  const { data, isLoading, isError, error } = useSnapshots({
    folder,
    songFolder: settings.songFolder,
  });

  const { bgUrl } = useBeatmapBackground(folder, settings.songFolder);

  const dataDifficulties = data?.difficulties;

  // Group difficulties by mode (excluding General which is handled separately)
  const groupedDifficulties = useMemo((): ModeGroup[] => {
    if (!dataDifficulties) return [];

    // Group difficulties by mode
    const modeGroups: Record<Mode, ApiSnapshotDifficulty[]> = {
      Standard: [],
      Taiko: [],
      Catch: [],
      Mania: [],
    };

    dataDifficulties.forEach((diff) => {
      if (!diff.isGeneral && diff.mode) {
        modeGroups[diff.mode].push(diff);
      }
    });

    // Return only modes that have difficulties
    return (['Standard', 'Taiko', 'Catch', 'Mania'] as Mode[])
      .filter((mode) => modeGroups[mode].length > 0)
      .map((mode) => ({
        mode,
        difficulties: modeGroups[mode],
      }));
  }, [dataDifficulties]);

  if (groupedDifficulties.length > 0 && !selectedMode) {
    setSelectedMode(groupedDifficulties[0].mode);
  }

  const selectedGroup =
    groupedDifficulties.find((g) => g.mode === selectedMode) ?? groupedDifficulties[0];

  const selectedSnapshotDifficulty = useMemo(() => {
    if (!data || selectedDifficulty === 'General') return undefined;
    return data.difficulties.find((d) => d.name === selectedDifficulty);
  }, [data, selectedDifficulty]);

  const activeSnapshotHistory = useMemo(
    () => (data ? getSnapshotHistory(data, selectedDifficulty) : null),
    [data, selectedDifficulty]
  );

  const snapshotFolder = useMemo((): SnapshotFolderTarget | null => {
    const beatmapSetId = beatmapInfo?.beatmapSetId;
    if (!beatmapSetId || beatmapSetId < 0) return null;
    if (isLoading || !data || data.errorMessage) return null;

    if (selectedDifficulty === 'General') {
      return { beatmapSetId, subfolder: 'files' };
    }

    const beatmapId = selectedSnapshotDifficulty?.beatmapId;
    if (!beatmapId) return null;

    return { beatmapSetId, subfolder: String(beatmapId) };
  }, [beatmapInfo?.beatmapSetId, data, isLoading, selectedDifficulty, selectedSnapshotDifficulty]);

  const [prevActiveSnapshotHistory, setPrevActiveSnapshotHistory] = useState(activeSnapshotHistory);

  if (activeSnapshotHistory !== prevActiveSnapshotHistory) {
    setPrevActiveSnapshotHistory(activeSnapshotHistory);

    if (!activeSnapshotHistory?.commits.length) {
      setSelectedCommitId(undefined);
    } else {
      setSelectedCommitId((current) => {
        if (current && activeSnapshotHistory.commits.some((c) => c.id === current)) {
          return current;
        }
        return activeSnapshotHistory.commits[0].id;
      });
    }
  }

  if (!folder) {
    return <NoBeatmapsetDisplay />;
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
      <LoadingOverlay visible={isLoading} zIndex={1000} overlayProps={{ radius: 'sm', blur: 2 }} />
      <BeatmapHeader bgUrl={bgUrl}>
        <Group gap="sm">
          <BeatmapActionButtons
            beatmapFolderPath={beatmapFolderPath}
            beatmapId={beatmapInfo?.beatmapId ?? undefined}
            beatmapSetId={beatmapInfo?.beatmapSetId ?? undefined}
            onReparse={triggerReparse}
            snapshotFolder={snapshotFolder}
          />
          <SnapshotGameModeSelector
            groupedDifficulties={groupedDifficulties}
            selectedMode={selectedMode}
            onModeChange={setSelectedMode}
          />
        </Group>
        {data?.difficulties && !data.errorMessage && selectedGroup && (
          <DifficultyTabSelector
            generalLeading={
              <SnapshotDifficultyChangesIcon
                hasChanges={generalHasChangesAtCommit(data, selectedCommitId)}
                size={24}
              />
            }
            tabs={selectedGroup.difficulties.map((diff) => {
              const hasSnapshot = difficultyHasSnapshot(data, diff.name);
              return {
                id: diff.name,
                label: diff.name,
                starRating: diff.starRating,
                leading: hasSnapshot ? (
                  <SnapshotDifficultyChangesIcon
                    hasChanges={difficultyHasChangesAtCommit(data, diff.name, selectedCommitId)}
                    size={24}
                  />
                ) : undefined,
                disabled: !hasSnapshot,
                disabledTooltip: 'This difficulty has no other snapshots to compare against',
              };
            })}
            selectedId={selectedDifficulty}
            onSelect={setSelectedDifficulty}
            sortByStarRating
          />
        )}
      </BeatmapHeader>
      {data && (
        <Flex
          gap="sm"
          p="md"
          direction="column"
          style={{ flex: 1, overflow: 'hidden' }}
          bg="dark.6"
        >
          {data.errorMessage ? (
            <Alert icon={<IconPhotoOff />} color="yellow" title="Snapshots unavailable">
              <Text size="sm">{data.errorMessage}</Text>
            </Alert>
          ) : (
            <>
              <Flex gap="xs" align="center">
                <SnapshotDifficultyChangesIcon
                  hasChanges={
                    selectedDifficulty === 'General'
                      ? generalHasChangesAtCommit(data, selectedCommitId)
                      : difficultyHasChangesAtCommit(data, selectedDifficulty!, selectedCommitId)
                  }
                  size={30}
                />
                {selectedSnapshotDifficulty && (
                  <GameModeIcon
                    mode={selectedSnapshotDifficulty.mode ?? 'Standard'}
                    size={28}
                    starRating={selectedSnapshotDifficulty.starRating}
                  />
                )}
                <Title order={3}>{selectedDifficulty}</Title>
              </Flex>
              <SnapshotContent
                data={data}
                selectedDifficulty={selectedDifficulty}
                selectedCommitId={selectedCommitId}
                onSelectCommitId={setSelectedCommitId}
              />
            </>
          )}
        </Flex>
      )}
      {isError && (
        <Flex p="md">
          <Alert icon={<IconAlertCircle />} color="red" title="Error loading snapshots">
            <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
              {error?.message}
            </Text>
            {error?.stackTrace && <StackTraceMessage stackTrace={error.stackTrace} />}
          </Alert>
        </Flex>
      )}
    </Box>
  );
}

export default Snapshots;

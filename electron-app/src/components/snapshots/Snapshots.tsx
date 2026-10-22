import { Alert, Text, Box, Flex, useMantineTheme } from '@mantine/core';
import { IconAlertCircle, IconPhotoOff } from '@tabler/icons-react';
import { useState, useMemo } from 'react';
import { useSnapshots } from './hooks/useSnapshots';
import SnapshotContent from './SnapshotContent';
import {
  difficultyHasChangesAtCommit,
  difficultyHasSnapshot,
  generalHasChangesAtCommit,
  getSnapshotHistory,
} from './snapshotHistory';
import { useBeatmap } from '../../context/BeatmapContext';
import { useSettings } from '../../context/SettingsContext';
import { ApiSnapshotDifficulty } from '../../Types';
import BeatmapHeader from '../common/BeatmapHeader';
import DifficultyPicker from '../common/DifficultyPicker';
import EmptyState from '../common/EmptyState.tsx';
import { HistorySkeleton } from '../common/LoadingSkeletons.tsx';
import SelectedDifficultyRow from '../common/SelectedDifficultyRow.tsx';
import StackTraceMessage from '../common/StackTraceMessage.tsx';
import StarRatingBadge from '../common/StarRatingBadge.tsx';
import GameModeIcon from '../icons/GameModeIcon';
import SnapshotDifficultyChangesIcon from '../icons/SnapshotDifficultyChangesIcon';

function Snapshots() {
  const theme = useMantineTheme();
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();
  const [selectedDifficulty, setSelectedDifficulty] = useState<string | undefined>('General');
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

  // The difficulties' own tabs; General has its own tab.
  const snapshotDifficulties = useMemo(
    (): ApiSnapshotDifficulty[] => data?.difficulties.filter((diff) => !diff.isGeneral) ?? [],
    [data]
  );

  // The same colours as the changed and unchanged icons, for the picker's segments.
  const changesColor = (hasChanges: boolean) =>
    hasChanges ? theme.colors.blue[6] : theme.colors.dark[2];

  const selectedSnapshotDifficulty = useMemo(() => {
    if (!data || selectedDifficulty === 'General') return undefined;
    return data.difficulties.find((d) => d.name === selectedDifficulty);
  }, [data, selectedDifficulty]);

  const activeSnapshotHistory = useMemo(
    () => (data ? getSnapshotHistory(data, selectedDifficulty) : null),
    [data, selectedDifficulty]
  );

  // Starts empty rather than at the current history: with cached data the history is there on the
  // first render, and the latest snapshot must still get selected.
  const [prevActiveSnapshotHistory, setPrevActiveSnapshotHistory] =
    useState<typeof activeSnapshotHistory>(null);

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

  return (
    <>
      <BeatmapHeader>
        {data && !data.errorMessage && snapshotDifficulties.length > 0 && (
          <DifficultyPicker
            difficulties={snapshotDifficulties.map((diff) => {
              const hasSnapshot = difficultyHasSnapshot(data, diff.name);
              const hasChanges =
                hasSnapshot && difficultyHasChangesAtCommit(data, diff.name, selectedCommitId);
              return {
                id: diff.name,
                label: diff.name,
                mode: diff.mode ?? 'Standard',
                starRating: diff.starRating,
                icon: <SnapshotDifficultyChangesIcon hasChanges={hasChanges} size={18} />,
                statusColor: changesColor(hasChanges),
                disabled: !hasSnapshot,
                disabledReason: 'No snapshots to compare',
              };
            })}
            general={{
              icon: (
                <SnapshotDifficultyChangesIcon
                  hasChanges={generalHasChangesAtCommit(data, selectedCommitId)}
                  size={18}
                />
              ),
              statusColor: changesColor(generalHasChangesAtCommit(data, selectedCommitId)),
            }}
            modeStatus={(_, diffs) => (
              <SnapshotDifficultyChangesIcon
                hasChanges={diffs.some(
                  (d) => !d.disabled && difficultyHasChangesAtCommit(data, d.id, selectedCommitId)
                )}
                size={18}
              />
            )}
            selectedId={selectedDifficulty}
            onSelect={setSelectedDifficulty}
          />
        )}
      </BeatmapHeader>
      {isLoading && (
        <Box bg="dark.6" style={{ flex: 1 }}>
          <HistorySkeleton />
        </Box>
      )}
      {data && (
        // pt="sm": the selected difficulty row follows the picker at the header's row gap.
        <Flex
          gap="sm"
          px="md"
          pb="md"
          pt="sm"
          direction="column"
          style={{ flex: 1, overflow: 'hidden' }}
          bg="dark.6"
        >
          {data.errorMessage ? (
            <EmptyState
              icon={IconPhotoOff}
              title="Snapshots unavailable"
              description={data.errorMessage}
            />
          ) : (
            <>
              {/* The same row as on Checks, so both pages show the selection alike. */}
              <SelectedDifficultyRow
                icons={
                  <>
                    <SnapshotDifficultyChangesIcon
                      hasChanges={
                        selectedDifficulty === 'General'
                          ? generalHasChangesAtCommit(data, selectedCommitId)
                          : difficultyHasChangesAtCommit(
                              data,
                              selectedDifficulty!,
                              selectedCommitId
                            )
                      }
                      size={32}
                    />
                    {selectedSnapshotDifficulty && (
                      <GameModeIcon
                        mode={selectedSnapshotDifficulty.mode ?? 'Standard'}
                        size={32}
                        starRating={selectedSnapshotDifficulty.starRating}
                      />
                    )}
                  </>
                }
                name={selectedDifficulty}
                badges={
                  !!selectedSnapshotDifficulty?.starRating && (
                    <StarRatingBadge rating={selectedSnapshotDifficulty.starRating} />
                  )
                }
              />
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
          <Alert icon={<IconAlertCircle />} color="red" title="Couldn't load the snapshots">
            <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
              {error?.message}
            </Text>
            {error?.stackTrace && <StackTraceMessage stackTrace={error.stackTrace} />}
          </Alert>
        </Flex>
      )}
    </>
  );
}

export default Snapshots;

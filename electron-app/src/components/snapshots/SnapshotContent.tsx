import { Box, Stack, Text, useMantineTheme } from '@mantine/core';
import { useState, useEffect } from 'react';
import SnapshotCommitList from './SnapshotCommitList';
import UnifiedDiffViewer from './UnifiedDiffViewer';
import { ApiSnapshotResult, ApiSnapshotHistory, ApiSnapshotCommit } from '../../Types';

interface SnapshotContentProps {
  data: ApiSnapshotResult;
  selectedDifficulty?: string;
}

function SnapshotContent({ data, selectedDifficulty }: SnapshotContentProps) {
  const theme = useMantineTheme();
  const [selectedCommitId, setSelectedCommitId] = useState<string | undefined>();

  // Find the selected history
  let history: ApiSnapshotHistory | null = null;

  if (selectedDifficulty === 'General' && data.general) {
    history = data.general;
  } else {
    history = data.beatmapHistories.find(
      (h) => h.difficultyName === selectedDifficulty
    ) ?? null;
  }

  // Auto-select first commit when history changes
  useEffect(() => {
    if (history && history.commits.length > 0) {
      setSelectedCommitId(history.commits[0].id);
    } else {
      setSelectedCommitId(undefined);
    }
  }, [history]);

  if (!history) {
    return <Text c="dimmed">No snapshot data available for this difficulty.</Text>;
  }

  if (history.commits.length === 0) {
    return <Text c="dimmed">No changes detected in snapshots.</Text>;
  }

  const selectedCommit: ApiSnapshotCommit | undefined = history.commits.find(
    (c) => c.id === selectedCommitId
  );

  return (
    <Stack gap="md">
      {/* Commit list (horizontal) */}
      <Box
        style={{
          borderRadius: theme.radius.md,
          overflow: 'hidden',
        }}
      >
        <Box p="sm">
          <Text fw={600} size="sm">
            History ({history.commits.length} snapshot{history.commits.length !== 1 ? 's' : ''})
          </Text>
        </Box>
        <SnapshotCommitList
          commits={history.commits}
          selectedCommitId={selectedCommitId}
          onSelectCommit={setSelectedCommitId}
        />
      </Box>

      {/* Diff viewer (full width below) */}
      <Box
        style={{
          backgroundColor: theme.colors.dark[7],
          borderRadius: theme.radius.md,
          overflow: 'hidden',
          flex: 1,
        }}
      >
        <Box p="xs">
          <Text fw={600} size="sm">
            {selectedCommit
              ? `Changes (${selectedCommit.totalChanges} total)`
              : 'Select a commit to view changes'}
          </Text>
        </Box>
        <Box p="xs">
          {selectedCommit ? (
            <UnifiedDiffViewer commit={selectedCommit} />
          ) : (
            <Text c="dimmed" ta="center" py="xl">
              Select a commit from the list to view its changes.
            </Text>
          )}
        </Box>
      </Box>
    </Stack>
  );
}

export default SnapshotContent;


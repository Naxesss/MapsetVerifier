import { Box, Stack, Text, useMantineTheme } from '@mantine/core';
import SnapshotCommitList from './SnapshotCommitList';
import { getSnapshotHistory } from './snapshotHistory';
import UnifiedDiffViewer from './UnifiedDiffViewer';
import { ApiSnapshotResult } from '../../Types';
import { countWord } from '../../utils/countWord';

interface SnapshotContentProps {
  data: ApiSnapshotResult;
  selectedDifficulty?: string;
  selectedCommitId?: string;
  onSelectCommitId: (commitId: string) => void;
}

function SnapshotContent({
  data,
  selectedDifficulty,
  selectedCommitId,
  onSelectCommitId,
}: SnapshotContentProps) {
  const theme = useMantineTheme();
  const history = getSnapshotHistory(data, selectedDifficulty);

  if (!history) {
    return <Text c="dimmed">No snapshot data available for this difficulty.</Text>;
  }

  if (history.commits.length === 0) {
    return <Text c="dimmed">No changes detected in snapshots.</Text>;
  }

  const selectedCommit = history.commits.find((c) => c.id === selectedCommitId);

  return (
    <Stack gap="md">
      <Box
        style={{
          borderRadius: theme.radius.md,
          overflow: 'hidden',
        }}
      >
        <Box p="sm">
          <Text fw={600} size="sm">
            History ({countWord(history.commits.length, 'snapshot')})
          </Text>
        </Box>
        <SnapshotCommitList
          commits={history.commits}
          selectedCommitId={selectedCommitId}
          onSelectCommit={onSelectCommitId}
        />
      </Box>

      <Box
        style={{
          borderRadius: theme.radius.md,
          overflow: 'hidden',
          flex: 1,
        }}
      >
        <Box p="xs" bg={theme.colors.dark[6]}>
          <Text fw={600} size="sm" mt="xs" ml="xs">
            {selectedCommit
              ? !selectedCommit.hasSnapshot
                ? 'No snapshot'
                : selectedCommit.totalChanges === 0
                  ? 'No changes'
                  : `Changes (${selectedCommit.totalChanges} total)`
              : 'Select a commit to view changes'}
          </Text>
        </Box>
        <Box p="xs">
          {selectedCommit && !selectedCommit.hasSnapshot ? (
            <Text c="dimmed" size="sm" py="md">
              This difficulty had no snapshot recorded yet at this point in time.
            </Text>
          ) : selectedCommit ? (
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

import { Badge, Flex, Text } from '@mantine/core';
import { IconClockPause, IconHistoryOff, IconPointer } from '@tabler/icons-react';
import SnapshotCommitList, { CommitChangeSummary } from './SnapshotCommitList';
import { getSnapshotHistory } from './snapshotHistory';
import UnifiedDiffViewer from './UnifiedDiffViewer';
import { useDateTimeFormat } from '../../hooks/useDateTimeFormat';
import { ApiSnapshotResult } from '../../Types';
import EmptyState from '../common/EmptyState.tsx';
import SectionCard from '../common/SectionCard.tsx';

interface SnapshotContentProps {
  data: ApiSnapshotResult;
  selectedDifficulty?: string;
  selectedCommitId?: string;
  onSelectCommitId: (commitId: string) => void;
}

/** Width of the history list; enough for the time, the "Latest" badge and the change counts. */
const HISTORY_WIDTH = 280;

/**
 * The snapshot history as a list on the left, and what changed in the chosen snapshot on the
 * right, like a commit log next to its diff.
 */
function SnapshotContent({
  data,
  selectedDifficulty,
  selectedCommitId,
  onSelectCommitId,
}: SnapshotContentProps) {
  const { formatDateTime } = useDateTimeFormat();
  const history = getSnapshotHistory(data, selectedDifficulty);

  if (!history) {
    return (
      <EmptyState icon={IconHistoryOff} title="No snapshot data available for this difficulty" />
    );
  }

  if (history.commits.length === 0) {
    return <EmptyState icon={IconHistoryOff} title="No changes detected in snapshots" />;
  }

  const selectedCommit = history.commits.find((c) => c.id === selectedCommitId);

  return (
    <Flex gap="md" align="flex-start" direction={{ base: 'column', md: 'row' }}>
      <SectionCard
        title="History"
        actions={<Badge color="gray">{history.commits.length}</Badge>}
        w={{ base: '100%', md: HISTORY_WIDTH }}
        style={{ flexShrink: 0 }}
      >
        <SnapshotCommitList
          commits={history.commits}
          selectedCommitId={selectedCommitId}
          onSelectCommit={onSelectCommitId}
        />
      </SectionCard>

      {/* Same card and title row as History, so both titles line up. */}
      <SectionCard
        title="Changes"
        actions={
          selectedCommit && (
            <>
              <Text size="xs" c="dimmed">
                {formatDateTime(selectedCommit.date, { withSeconds: true })}
              </Text>
              <CommitChangeSummary commit={selectedCommit} />
            </>
          )
        }
        w="100%"
        style={{ flex: 1, minWidth: 0 }}
      >
        {!selectedCommit ? (
          <EmptyState
            icon={IconPointer}
            title="No snapshot selected"
            description="Pick a snapshot from the history to see what changed."
          />
        ) : !selectedCommit.hasSnapshot ? (
          <EmptyState
            icon={IconClockPause}
            title="No snapshot yet"
            description="This difficulty had no snapshot recorded yet at this point in time."
          />
        ) : (
          <UnifiedDiffViewer commit={selectedCommit} />
        )}
      </SectionCard>
    </Flex>
  );
}

export default SnapshotContent;

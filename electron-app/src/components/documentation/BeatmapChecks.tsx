import { Alert, Group } from '@mantine/core';
import { IconAlertCircle, IconListSearch } from '@tabler/icons-react';
import DocumentationCheck from './DocumentationCheck';
import { useBeatmapDocumentationChecks } from './hooks/useDocumentationChecks';
import { Mode } from '../../Types.ts';
import { formatGameModeLabel } from '../../utils/gameMode';
import EmptyState from '../common/EmptyState.tsx';
import { ListSkeleton } from '../common/LoadingSkeletons.tsx';

interface BeatmapChecksProps {
  mode: Mode;
}

function BeatmapChecks({ mode }: BeatmapChecksProps) {
  const { checks, isLoading, isError } = useBeatmapDocumentationChecks(mode);

  if (isLoading) return <ListSkeleton />;
  if (isError) {
    return (
      <Alert icon={<IconAlertCircle />} color="red">
        Failed to load {formatGameModeLabel(mode)} checks.
      </Alert>
    );
  }
  if (!checks || checks.length === 0)
    return (
      <EmptyState icon={IconListSearch} title={`No ${formatGameModeLabel(mode)} checks found`} />
    );

  return (
    <Group gap="xs">
      {checks.map((check) => (
        <DocumentationCheck key={check.id} check={check} />
      ))}
    </Group>
  );
}

export default BeatmapChecks;

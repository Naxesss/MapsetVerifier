import {Alert, Group, Loader, Text} from '@mantine/core';
import DocumentationCheck from './DocumentationCheck';
import { useBeatmapDocumentationChecks } from './hooks/useDocumentationChecks';
import { Mode } from '../../Types.ts';
import { formatGameModeLabel } from '../../utils/gameMode';
import {IconAlertCircle} from "@tabler/icons-react";

interface BeatmapChecksProps {
  mode: Mode;
}

function BeatmapChecks({ mode }: BeatmapChecksProps) {
  const { checks, isLoading, isError } = useBeatmapDocumentationChecks(mode);

  if (isLoading) return <Loader size="sm" />;
  if (isError) {
    return (
      <Alert icon={<IconAlertCircle />} color="red">
        Failed to load {formatGameModeLabel(mode)} checks.
      </Alert>
    );
  }
  if (!checks || checks.length === 0)
    return <Text>No {formatGameModeLabel(mode)} checks found.</Text>;

  return (
    <Group gap="xs">
      {checks.map((check) => (
        <DocumentationCheck key={check.id} check={check} />
      ))}
    </Group>
  );
}

export default BeatmapChecks;

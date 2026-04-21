import { Group, Text } from '@mantine/core';
import DocumentationCheck from './DocumentationCheck';
import { useBeatmapDocumentationChecks } from './hooks/useDocumentationChecks';
import { Mode } from '../../Types.ts';
import { formatGameModeLabel } from '../../utils/gameMode';

interface BeatmapChecksProps {
  mode: Mode;
}

function BeatmapChecks({ mode }: BeatmapChecksProps) {
  const { checks, isLoading, isError } = useBeatmapDocumentationChecks(mode);

  if (isLoading) return <Text>Loading...</Text>;
  if (isError) return <Text>Error loading checks.</Text>;
  if (!checks || checks.length === 0) return <Text>No {formatGameModeLabel(mode)} checks found.</Text>;

  return (
    <Group gap="xs">
      {checks.map((check) => (
        <DocumentationCheck key={check.id} check={check} />
      ))}
    </Group>
  );
}

export default BeatmapChecks;

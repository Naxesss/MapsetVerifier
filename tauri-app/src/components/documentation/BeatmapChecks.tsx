import { Text } from '@mantine/core';
import DocumentationCheck from './DocumentationCheck';
import { useBeatmapDocumentationChecks } from './hooks/useDocumentationChecks';
import { Mode } from '../../Types.ts';

interface BeatmapChecksProps {
  mode: Mode;
}

function BeatmapChecks({ mode }: BeatmapChecksProps) {
  const { checks, isLoading, isError } = useBeatmapDocumentationChecks(mode);

  if (isLoading) return <Text>Loading...</Text>;
  if (isError) return <Text>Error loading checks.</Text>;
  if (!checks || checks.length === 0) return <Text>No {mode} checks found.</Text>;

  return (
    <>
      <Text>
        A total of {checks.length} {mode} checks exist.
      </Text>
      {checks.map((check) => (
        <DocumentationCheck key={check.id} check={check} />
      ))}
    </>
  );
}

export default BeatmapChecks;

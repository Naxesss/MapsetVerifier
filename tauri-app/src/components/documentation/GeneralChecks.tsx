import { Alert, Group, Loader, Text } from '@mantine/core';
import DocumentationCheck from './DocumentationCheck';
import { useGeneralDocumentationChecks } from './hooks/useDocumentationChecks';
import { ApiDocumentationCheck } from '../../Types.ts';

function GeneralChecks() {
  const { checks, isLoading, isError } = useGeneralDocumentationChecks();

  if (isLoading) return <Loader size="sm" />;
  if (isError) return <Alert color="red">Failed to load general checks.</Alert>;
  if (!checks || checks.length === 0) return <Text>No general checks found.</Text>;

  return (
    <Group gap="xs">
      <Text>A total of {checks.length} General checks exist.</Text>
      {checks.map((check: ApiDocumentationCheck) => (
        <DocumentationCheck key={check.id} check={check} />
      ))}
    </Group>
  );
}

export default GeneralChecks;

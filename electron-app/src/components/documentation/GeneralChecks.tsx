import { Alert, Group, Loader, Text } from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';
import DocumentationCheck from './DocumentationCheck';
import { useGeneralDocumentationChecks } from './hooks/useDocumentationChecks';
import { ApiDocumentationCheck } from '../../Types.ts';

function GeneralChecks() {
  const { checks, isLoading, isError } = useGeneralDocumentationChecks();

  if (isLoading) return <Loader size="sm" />;
  if (isError)
    return (
      <Alert icon={<IconAlertCircle />} color="red">
        Failed to load general checks.
      </Alert>
    );
  if (!checks || checks.length === 0) return <Text>No general checks found.</Text>;

  return (
    <Group gap="xs">
      {checks.map((check: ApiDocumentationCheck) => (
        <DocumentationCheck key={check.id} check={check} />
      ))}
    </Group>
  );
}

export default GeneralChecks;

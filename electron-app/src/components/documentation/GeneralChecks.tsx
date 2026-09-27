import { Alert, Group } from '@mantine/core';
import { IconAlertCircle, IconListSearch } from '@tabler/icons-react';
import DocumentationCheck from './DocumentationCheck';
import { useGeneralDocumentationChecks } from './hooks/useDocumentationChecks';
import { ApiDocumentationCheck } from '../../Types.ts';
import EmptyState from '../common/EmptyState.tsx';
import { ListSkeleton } from '../common/LoadingSkeletons.tsx';

function GeneralChecks() {
  const { checks, isLoading, isError } = useGeneralDocumentationChecks();

  if (isLoading) return <ListSkeleton />;
  if (isError)
    return (
      <Alert icon={<IconAlertCircle />} color="red">
        Failed to load general checks.
      </Alert>
    );
  if (!checks || checks.length === 0)
    return <EmptyState icon={IconListSearch} title="No general checks found" />;

  return (
    <Group gap="xs">
      {checks.map((check: ApiDocumentationCheck) => (
        <DocumentationCheck key={check.id} check={check} />
      ))}
    </Group>
  );
}

export default GeneralChecks;

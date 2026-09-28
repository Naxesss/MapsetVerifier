import { Alert, Stack } from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';
import DocumentationCheck from './DocumentationCheck';
import { ListSkeleton } from '../common/LoadingSkeletons.tsx';
import type { ApiDocumentationCheck } from '../../Types.ts';
import type { ReactNode } from 'react';

interface DocumentationCheckListProps {
  checks: ApiDocumentationCheck[] | undefined;
  isLoading: boolean;
  isError: boolean;
  /** What couldn't be loaded, as a sentence. */
  errorMessage: string;
  /** Shown when there are no checks to list. */
  emptyState: ReactNode;
}

/** A list of checks on the Documentation page, for one category or for search results. */
export default function DocumentationCheckList({
  checks,
  isLoading,
  isError,
  errorMessage,
  emptyState,
}: DocumentationCheckListProps) {
  if (isLoading) return <ListSkeleton />;

  if (isError) {
    return (
      <Alert icon={<IconAlertCircle />} color="red">
        {errorMessage}
      </Alert>
    );
  }

  if (!checks || checks.length === 0) return <>{emptyState}</>;

  return (
    <Stack className="mv-deferred-content-enter" w="100%" gap="xs">
      {checks.map((check) => (
        <DocumentationCheck key={check.id} check={check} />
      ))}
    </Stack>
  );
}

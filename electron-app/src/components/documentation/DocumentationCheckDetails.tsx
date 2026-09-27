import { Text, Flex, Alert, Group, Stack, Badge } from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import DocumentationOutcomeBlockquote from './DocumentationOutcomeBlockquote';
import MantineMarkdown from './MantineMarkdown';
import DocumentationApi from '../../client/DocumentationApi';
import { ApiDocumentationCheck, ApiDocumentationCheckDetails } from '../../Types';
import { SectionTitle } from '../common/Headings.tsx';
import { TextSkeleton } from '../common/LoadingSkeletons.tsx';
import GameModeIcon from '../icons/GameModeIcon.tsx';

/** The documentation of a check with its outcomes, shown in the detail modal. */
export default function DocumentationCheckDetails({ check }: { check: ApiDocumentationCheck }) {
  const { data, isLoading, error } = useQuery<ApiDocumentationCheckDetails, Error>({
    queryKey: ['documentationCheckDetails', check.id],
    queryFn: () => DocumentationApi.getCheckDetails(check.id.toString()),
    staleTime: Infinity,
    refetchOnWindowFocus: false,
  });

  return (
    <Flex direction="column" gap="lg">
      <Flex justify="space-between">
        <Group gap="xs">
          <Group gap={0}>
            {check.modes.map((mode) => (
              <GameModeIcon size={16} key={mode} mode={mode} />
            ))}
          </Group>
          <Badge>{check.category}</Badge>
        </Group>
        <Text size="sm" c="dimmed">
          Created by {check.author}
        </Text>
      </Flex>
      {isLoading && <TextSkeleton lines={4} />}
      {error && (
        <Alert icon={<IconAlertCircle />} color="red">
          Failed to load details.
        </Alert>
      )}
      {data && (
        <>
          <Stack gap="md">
            <MantineMarkdown notesForBlockquotes>{data.description}</MantineMarkdown>
          </Stack>
          <SectionTitle>Outcomes</SectionTitle>
          <Stack gap="md">
            {data.outcomes.map((checkDetails, i) => (
              <DocumentationOutcomeBlockquote key={i} outcome={checkDetails} />
            ))}
          </Stack>
        </>
      )}
    </Flex>
  );
}

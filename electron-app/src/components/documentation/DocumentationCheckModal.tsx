import { Modal, Text, Loader, Flex, Alert, Group, Stack, Divider, Badge } from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import DocumentationOutcomeBlockquote from './DocumentationOutcomeBlockquote';
import MantineMarkdown from './MantineMarkdown';
import DocumentationApi from '../../client/DocumentationApi';
import { ApiDocumentationCheck, ApiDocumentationCheckDetails } from '../../Types';
import GameModeIcon from '../icons/GameModeIcon.tsx';

interface DocumentationCheckModalProps {
  opened: boolean;
  onClose: () => void;
  check: ApiDocumentationCheck;
}

export default function DocumentationCheckModal({
  opened,
  onClose,
  check,
}: DocumentationCheckModalProps) {
  const { data, isLoading, error } = useQuery<ApiDocumentationCheckDetails, Error>({
    queryKey: ['documentationCheckDetails', check.id],
    queryFn: () => DocumentationApi.getCheckDetails(check.id.toString()),
    enabled: opened,
    staleTime: Infinity,
    refetchOnWindowFocus: false,
  });

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={
        <Text fw="bold" size="lg">
          {check.description}
        </Text>
      }
      yOffset="120px"
      size="80%"
      styles={{ content: { maxWidth: 1000 } }}
    >
      <Flex direction="column" gap="lg">
        <Flex justify="space-between">
          <Group gap="1">
            {check.modes.map((mode) => (
              <GameModeIcon size={16} key={mode} mode={mode} />
            ))}
            <Badge size="xs" variant="light">
              {`${check.category}`}
            </Badge>
          </Group>
          <Text size="sm" c="dimmed">
            Created by {check.author}
          </Text>
        </Flex>
        {isLoading && <Loader />}
        {error && (
          <Alert icon={<IconAlertCircle />} color="red">
            Failed to load details.
          </Alert>
        )}
        {data && (
          <>
            <Stack gap="md">
              {data.outcomes.map((checkDetails, i) => (
                <DocumentationOutcomeBlockquote key={i} outcome={checkDetails} />
              ))}
            </Stack>
            <Divider />
            <Stack gap="md">
              <MantineMarkdown>{data.description}</MantineMarkdown>
            </Stack>
          </>
        )}
      </Flex>
    </Modal>
  );
}

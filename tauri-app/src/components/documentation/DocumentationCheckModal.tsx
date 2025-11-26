import { Modal, Text, Loader, Flex, Alert } from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import MantineMarkdown from './MantineMarkdown';
import DocumentationApi from '../../client/DocumentationApi';
import { ApiDocumentationCheck, ApiDocumentationCheckDetails } from '../../Types';
import LevelIcon from '../icons/LevelIcon.tsx';

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
      size="80%"
      styles={{ content: { maxWidth: 1000 } }}
    >
      <Flex direction="column" gap="sm">
        <Flex justify="space-between">
          <Text size="sm">{`${check.category} > ${check.subCategory}`}</Text>
          <Text size="sm">Created by {check.author}</Text>
        </Flex>
        {isLoading && <Loader />}
        {error && <Alert color="red">Failed to load details.</Alert>}
        {data && (
          <>
            {data.outcomes.map((checkDetails, i) => (
              <Alert key={i}>
                <Flex direction="column" gap="xs">
                  <Flex align="center" gap="xs">
                    <LevelIcon level={checkDetails.level} />
                    <MantineMarkdown>{checkDetails.description}</MantineMarkdown>
                  </Flex>
                  {checkDetails.cause && <MantineMarkdown>{checkDetails.cause}</MantineMarkdown>}
                </Flex>
              </Alert>
            ))}
            <MantineMarkdown>{data.description}</MantineMarkdown>
          </>
        )}
      </Flex>
    </Modal>
  );
}

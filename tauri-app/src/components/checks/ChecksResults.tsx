import { Stack, Group, Title, Loader, Text, Alert, Box } from '@mantine/core';
import CategoryAccordion from './CategoryAccordion';
import { FetchError } from '../../client/ApiHelper';
import { ApiBeatmapSetCheckResult } from '../../Types';

interface ChecksResultsProps {
  data?: ApiBeatmapSetCheckResult;
  isLoading: boolean;
  isError: boolean;
  error?: FetchError | null;
  showMinor: boolean;
}

function ChecksResults({ data, isLoading, isError, error, showMinor }: ChecksResultsProps) {
  return (
    <Stack gap="sm" p="md">
      <Group>
        <Title order={3}>Checks</Title>
      </Group>

      {isLoading && (
        <Group gap="sm">
          <Loader size="sm" />
          <Text>Running checks...</Text>
        </Group>
      )}

      {isError && (
        <Alert color="red" title="Error loading checks" withCloseButton>
          <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
            {error?.message}
          </Text>
          {error?.stackTrace && (
            <Text mt="sm" size="xs" c="red.3" style={{ whiteSpace: 'pre-wrap' }}>
              {error.stackTrace}
            </Text>
          )}
        </Alert>
      )}

      {data && <CategoryAccordion data={data} showMinor={showMinor} />}

      {!isLoading && !isError && !data && (
        <Box>
          <Text size="sm" c="dimmed">
            No data returned.
          </Text>
        </Box>
      )}
    </Stack>
  );
}

export default ChecksResults;

import { Stack, Group, Title, Loader, Text, Alert, Box } from '@mantine/core';
import CheckCategory from './CheckCategory.tsx';
import { FetchError } from '../../client/ApiHelper';
import { ApiBeatmapSetCheckResult, ApiCategoryCheckResult } from '../../Types';

interface ChecksResultsProps {
  data?: ApiBeatmapSetCheckResult;
  isLoading: boolean;
  isError: boolean;
  error?: FetchError | null;
  showMinor: boolean;
  selectedCategory?: string;
  overrideResult?: ApiCategoryCheckResult;
}

function ChecksResults({ data, isLoading, isError, error, showMinor, selectedCategory, overrideResult }: ChecksResultsProps) {
  return (
    <Stack gap="sm">
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

      {data && <CheckCategory data={data} showMinor={showMinor} selectedCategory={selectedCategory} overrideResult={overrideResult} />}

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

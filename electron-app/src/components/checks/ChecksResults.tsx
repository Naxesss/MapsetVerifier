import { Group, Loader, Text, Alert, Box } from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';
import CheckCategory from './CheckCategory.tsx';
import { FetchError } from '../../client/ApiHelper';
import { ApiBeatmapSetCheckResult, ApiCategoryOverrideCheckResult } from '../../Types';
import StackTraceMessage from '../common/StackTraceMessage.tsx';

interface ChecksResultsProps {
  data?: ApiBeatmapSetCheckResult;
  isLoading: boolean;
  isError: boolean;
  error?: FetchError | null;
  showMinor: boolean;
  selectedCategory?: string;
  overrideResult?: ApiCategoryOverrideCheckResult;
}

function ChecksResults({
  data,
  isLoading,
  isError,
  error,
  showMinor,
  selectedCategory,
  overrideResult,
}: ChecksResultsProps) {
  return (
    <Box>
      {isLoading && (
        <Group gap="sm">
          <Loader size="sm" />
          <Text>Running checks...</Text>
        </Group>
      )}

      {isError && (
        <Alert icon={<IconAlertCircle />} color="red" title="Error loading checks">
          <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
            {error?.message}
          </Text>
          {error?.stackTrace && <StackTraceMessage stackTrace={error.stackTrace} />}
        </Alert>
      )}

      {data && (
        <CheckCategory
          data={data}
          showMinor={showMinor}
          selectedCategory={selectedCategory}
          overrideResult={overrideResult}
        />
      )}

      {!isLoading && !isError && !data && (
        <Box>
          <Text size="sm" c="dimmed">
            No data returned.
          </Text>
        </Box>
      )}
    </Box>
  );
}

export default ChecksResults;

import { Alert, Box, Flex, Text } from '@mantine/core';
import { IconAlertCircle, IconAlertTriangle } from '@tabler/icons-react';
import { CardsSkeleton } from '../common/LoadingSkeletons.tsx';
import StackTraceMessage from '../common/StackTraceMessage.tsx';
import type { ReactNode } from 'react';

interface AnalysisResult {
  success: boolean;
  errorMessage: string | null;
}

interface AnalysisTabProps<T extends AnalysisResult> {
  data: T | undefined;
  isLoading: boolean;
  isError: boolean;
  error: { message: string; stackTrace?: string } | null | undefined;
  /** What was analyzed, for the error title, e.g. "audio" in "Couldn't analyze the audio". */
  subject: string;
  /** The tab's content, once the analysis succeeded. */
  children: (data: T) => ReactNode;
}

/**
 * The shell every Overview tab shares: a skeleton while the analysis runs, an error when the request
 * fails, a warning when the backend couldn't finish, and otherwise the content, padded and spaced
 * like the other tabs.
 */
export default function AnalysisTab<T extends AnalysisResult>({
  data,
  isLoading,
  isError,
  error,
  subject,
  children,
}: AnalysisTabProps<T>) {
  return (
    <Box>
      {isLoading && <CardsSkeleton />}
      {isError && (
        <Flex p="md">
          <Alert icon={<IconAlertCircle />} color="red" title={`Couldn't analyze the ${subject}`}>
            <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
              {error?.message}
            </Text>
            {error?.stackTrace && <StackTraceMessage stackTrace={error.stackTrace} />}
          </Alert>
        </Flex>
      )}

      {data && !data.success && (
        <Flex p="md">
          <Alert icon={<IconAlertTriangle />} color="yellow" title="Couldn't finish the analysis">
            <Text size="sm">{data.errorMessage}</Text>
          </Alert>
        </Flex>
      )}

      {data && data.success && (
        <Flex gap="md" p="md" direction="column">
          {children(data)}
        </Flex>
      )}
    </Box>
  );
}

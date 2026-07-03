import { Alert, Box, Progress, Stack, Text } from '@mantine/core';
import { IconAlertCircle, IconEyeOff } from '@tabler/icons-react';
import { useMemo } from 'react';
import CheckCategory from './CheckCategory.tsx';
import CheckProgressTaskList from './CheckProgressTaskList.tsx';
import {
  getRawCheckResultsForSelectedCategory,
  hasMinorResultsHiddenByUserFilter,
} from './checkResultVisibility';
import ChecksDeltaSummary from './ChecksDeltaSummary.tsx';
import { FetchError } from '../../client/ApiHelper';
import {
  ApiBeatmapSetCheckResult,
  ApiCategoryOverrideCheckResult,
  CheckProgress,
} from '../../Types';
import StackTraceMessage from '../common/StackTraceMessage.tsx';

interface ChecksResultsProps {
  data?: ApiBeatmapSetCheckResult;
  isLoading: boolean;
  isError: boolean;
  error?: FetchError | null;
  progress?: CheckProgress | null;
  showMinor: boolean;
  hiddenMinorCheckIds: readonly number[];
  selectedCategory?: string;
  overrideResult?: ApiCategoryOverrideCheckResult;
  showCheckRunDelta?: boolean;
  checkRunDeltaShowUnchanged?: boolean;
  beatmapFolderPath?: string;
  onCheckRunHistoryCleared?: () => void;
}

function ChecksResults({
  data,
  isLoading,
  isError,
  error,
  progress,
  showMinor,
  hiddenMinorCheckIds,
  selectedCategory,
  overrideResult,
  showCheckRunDelta = true,
  checkRunDeltaShowUnchanged = false,
  beatmapFolderPath,
  onCheckRunHistoryCleared,
}: ChecksResultsProps) {
  const rawForCategory = useMemo(
    () =>
      data ? getRawCheckResultsForSelectedCategory(data, selectedCategory, overrideResult) : [],
    [data, selectedCategory, overrideResult]
  );

  const showMinorFilterNotice = useMemo(
    () =>
      hasMinorResultsHiddenByUserFilter(rawForCategory, {
        showMinor,
        hiddenMinorCheckIds,
      }),
    [rawForCategory, showMinor, hiddenMinorCheckIds]
  );

  const progressPercent =
    progress && progress.total > 0
      ? Math.min(100, Math.round((progress.completed / progress.total) * 100))
      : 0;

  return (
    <Box>
      {isLoading && (
        <Stack gap="xs" py="sm">
          <Text size="sm" c="dimmed">
            Checking for...
          </Text>
          <CheckProgressTaskList progress={progress ?? null} />
          <Progress value={progressPercent} animated size="lg" radius="xl" />
          {progress && progress.total > 0 ? (
            <Text size="xs" c="dimmed">
              {progress.completed} / {progress.total} checks
            </Text>
          ) : null}
        </Stack>
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
        <Stack gap="xs">
          {showCheckRunDelta ? (
            <ChecksDeltaSummary
              delta={data.checkRunDelta}
              showMinor={showMinor}
              hiddenMinorCheckIds={hiddenMinorCheckIds}
              selectedCategory={selectedCategory}
              showUnchanged={checkRunDeltaShowUnchanged}
              beatmapFolderPath={beatmapFolderPath}
              onHistoryCleared={onCheckRunHistoryCleared}
            />
          ) : null}
          {showMinorFilterNotice ? (
            <Alert
              variant="light"
              color="gray"
              icon={<IconEyeOff size={16} />}
              p="xs"
              my="sm"
              styles={{
                wrapper: { alignItems: 'flex-start' },
                icon: { marginTop: 2, marginRight: 4, marginLeft: 8 },
                body: { paddingTop: 0 },
                message: { marginTop: 0 },
              }}
            >
              <Text size="xs" c="dimmed" lh={2}>
                Minor issues exist for checks hidden in{' '}
                <Text span fw={600} inherit>
                  Settings → Minor checks filter
                </Text>
                . They are omitted from this list.
              </Text>
            </Alert>
          ) : null}
          <CheckCategory
            key={selectedCategory ?? 'General'}
            data={data}
            showMinor={showMinor}
            hiddenMinorCheckIds={hiddenMinorCheckIds}
            selectedCategory={selectedCategory}
            overrideResult={overrideResult}
          />
        </Stack>
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

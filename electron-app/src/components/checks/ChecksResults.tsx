import { Alert, Anchor, Box, Progress, Stack, Text } from '@mantine/core';
import { IconEyeOff } from '@tabler/icons-react';
import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import CheckCategory from './CheckCategory.tsx';
import CheckProgressTaskList from './CheckProgressTaskList.tsx';
import {
  getRawCheckResultsForSelectedCategory,
  hasMinorResultsHiddenByUserFilter,
} from './checkResultVisibility';
import ChecksDeltaSummary from './ChecksDeltaSummary.tsx';
import CheckSpeedStatsPanel from './CheckSpeedStatsPanel.tsx';
import {
  ApiBeatmapSetCheckResult,
  ApiCategoryOverrideCheckResult,
  CheckProgress,
} from '../../Types';

interface ChecksResultsProps {
  data?: ApiBeatmapSetCheckResult;
  isLoading: boolean;
  progress?: CheckProgress | null;
  showMinor: boolean;
  hiddenMinorCheckIds: readonly number[];
  selectedCategory?: string;
  overrideResult?: ApiCategoryOverrideCheckResult;
  showCheckRunDelta?: boolean;
  checkRunDeltaShowUnchanged?: boolean;
  beatmapFolderPath?: string;
  onCheckRunHistoryCleared?: () => void;
  showCheckSpeedStats?: boolean;
}

function ChecksResults({
  data,
  isLoading,
  progress,
  showMinor,
  hiddenMinorCheckIds,
  selectedCategory,
  overrideResult,
  showCheckRunDelta = true,
  checkRunDeltaShowUnchanged = false,
  beatmapFolderPath,
  onCheckRunHistoryCleared,
  showCheckSpeedStats = false,
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
            Checking for…
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

      {data && (
        <Stack gap="sm">
          {showCheckSpeedStats && data.checkTimings ? (
            <CheckSpeedStatsPanel report={data.checkTimings} />
          ) : null}
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
              styles={{
                wrapper: { alignItems: 'flex-start' },
                icon: { marginTop: 2, marginRight: 4, marginLeft: 8 },
                body: { paddingTop: 0 },
                message: { marginTop: 0 },
              }}
            >
              <Text size="xs" c="dimmed" lh={2}>
                Some negligible issues are hidden by your{' '}
                <Anchor component={Link} to="/settings/checks" inherit fw={600}>
                  negligible check filter
                </Anchor>
                .
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
    </Box>
  );
}

export default ChecksResults;

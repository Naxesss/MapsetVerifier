import { Alert, Box, Progress, Stack, Text } from "@mantine/core";
import { IconAlertCircle, IconEyeOff } from "@tabler/icons-react";
import { useMemo } from "react";
import CheckCategory from "./CheckCategory.tsx";
import { getRawCheckResultsForSelectedCategory, hasMinorResultsHiddenByUserFilter } from "./checkResultVisibility";
import { FetchError } from "../../client/ApiHelper";
import { ApiBeatmapSetCheckResult, ApiCategoryOverrideCheckResult, CheckProgress } from "../../Types";
import StackTraceMessage from "../common/StackTraceMessage.tsx";

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
}: ChecksResultsProps) {
  const rawForCategory = useMemo(
    () => (data ? getRawCheckResultsForSelectedCategory(data, selectedCategory, overrideResult) : []),
    [data, selectedCategory, overrideResult],
  );

  const showMinorFilterNotice = useMemo(
    () =>
      hasMinorResultsHiddenByUserFilter(rawForCategory, {
        showMinor,
        hiddenMinorCheckIds,
      }),
    [rawForCategory, showMinor, hiddenMinorCheckIds],
  );

  const progressPercent =
    progress && progress.total > 0
      ? Math.min(100, Math.round((progress.completed / progress.total) * 100))
      : 0;

  const statusLabel = (() => {
    if (!progress) return "Starting checks…";

    const { activeLabels, completed, total } = progress;
    if (activeLabels.length === 0) {
      return completed >= total && total > 0 ? "Finishing…" : "Starting checks…";
    }
    if (activeLabels.length === 1) return activeLabels[0];
    if (activeLabels.length <= 3) return activeLabels.join(", ");
    return `${activeLabels.slice(0, 2).join(", ")} and ${activeLabels.length - 2} others`;
  })();

  return (
    <Box>
      {isLoading && (
        <Stack gap="xs" py="sm">
          <Text size="sm" c="dimmed">
            Checking for{" "}
            <Text span fw={600} c="gray.3" inherit>
              {statusLabel}
            </Text>
          </Text>
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
          <Text size="sm" style={{ whiteSpace: "pre-wrap" }}>
            {error?.message}
          </Text>
          {error?.stackTrace && <StackTraceMessage stackTrace={error.stackTrace} />}
        </Alert>
      )}

      {data && (
        <Stack gap="xs">
          {showMinorFilterNotice ? (
            <Alert
              variant="light"
              color="gray"
              icon={<IconEyeOff size={16} />}
              p="xs"
              my="sm"
              styles={{
                wrapper: { alignItems: "flex-start" },
                icon: { marginTop: 2, marginRight: 4, marginLeft: 8 },
                body: { paddingTop: 0 },
                message: { marginTop: 0 },
              }}
            >
              <Text size="xs" c="dimmed" lh={2}>
                Minor issues exist for checks hidden in{" "}
                <Text span fw={600} inherit>
                  Settings → Minor checks filter
                </Text>
                . They are omitted from this list.
              </Text>
            </Alert>
          ) : null}
          <CheckCategory
            key={selectedCategory ?? "General"}
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

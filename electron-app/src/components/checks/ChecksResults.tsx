import { Alert, Box, Group, Loader, Stack, Text } from "@mantine/core";
import { IconAlertCircle, IconEyeOff } from "@tabler/icons-react";
import { useMemo } from "react";
import CheckCategory from "./CheckCategory.tsx";
import { getRawCheckResultsForSelectedCategory, hasMinorResultsHiddenByUserFilter } from "./checkResultVisibility";
import { FetchError } from "../../client/ApiHelper";
import { ApiBeatmapSetCheckResult, ApiCategoryOverrideCheckResult } from "../../Types";
import StackTraceMessage from "../common/StackTraceMessage.tsx";

interface ChecksResultsProps {
  data?: ApiBeatmapSetCheckResult;
  isLoading: boolean;
  isError: boolean;
  error?: FetchError | null;
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

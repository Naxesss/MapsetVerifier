import {useQuery} from "@tanstack/react-query";
import {ApiBeatmapSetCheckResult} from "../../Types.ts";
import {FetchError} from "../../client/ApiHelper.ts";
import BeatmapApi from "../../client/BeatmapApi.ts";
import {Alert, Text, Title, Stack, Group, ScrollArea, Loader, Box, Flex, Anchor, Button} from "@mantine/core";
import {useParams} from "react-router-dom";
import {useSettings} from "../../context/SettingsContext.tsx";
import CategoryAccordion from './CategoryAccordion';
import {openPath} from "@tauri-apps/plugin-opener";
import {IconFolder, IconGlobe} from "@tabler/icons-react";

function Checks() {
  const { folder } = useParams();
  const { settings } = useSettings();
  if (!folder) {
    return <Text>No BeatmapSet selected.</Text>;
  }

  const beatmapFolderPath = `${settings.songFolder}\\${folder}`.replace(/\//g, '\\');

  const { data, isLoading, isError, error } = useQuery<ApiBeatmapSetCheckResult, FetchError>({
    queryKey: ["beatmap-checks", beatmapFolderPath],
    queryFn: () => BeatmapApi.runChecks(beatmapFolderPath!),
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false
  });

  return (
    <ScrollArea h="100%" offsetScrollbars>
      <Stack gap="md">
        <Flex gap="xs" direction="column">
          <Title order={2}>{data?.artist} - {data?.title}</Title>
          <Text>BeatmapSet by <Anchor href={`https://osu.ppy.sh/users/${data?.creator}`}>{data?.creator}</Anchor></Text>
        </Flex>

        <Group gap="sm" mb="xs">
          <Button
            size="xs"
            variant="default"
            onClick={async () => {
              try {
                await openPath(beatmapFolderPath);
              } catch (e) {
                console.error('Failed to open folder:', e);
                alert('Failed to open folder. See console for details.');
              }
            }}
            disabled={!beatmapFolderPath}
          >
            <IconFolder />
          </Button>
          <Button
            size="xs"
            variant="default"
            component="a"
            href={data?.beatmapSetId ? `https://osu.ppy.sh/beatmapsets/${data.beatmapSetId}` : undefined}
            target="_blank"
            rel="noopener noreferrer"
            disabled={!data?.beatmapSetId}
          >
            <IconGlobe />
            Open beatmap page
          </Button>
        </Group>

        <Group>
          <Title order={2}>Checks</Title>
        </Group>

        {isLoading && (
          <Group gap="sm">
            <Loader size="sm" />
            <Text>Running checks...</Text>
          </Group>
        )}

        {isError && (
          <Alert color="red" title="Error loading checks" withCloseButton>
            <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>{error?.message}</Text>
            {error?.stackTrace && (
              <Text mt="sm" size="xs" c="red.3" style={{ whiteSpace: 'pre-wrap' }}>
                {error.stackTrace}
              </Text>
            )}
          </Alert>
        )}

        {data && (
          <CategoryAccordion data={data} showMinor={settings.showMinor} />
        )}

        {!isLoading && !isError && !data && (
          <Box>
            <Text size="sm" c="dimmed">No data returned.</Text>
          </Box>
        )}
      </Stack>
    </ScrollArea>
  );
}

export default Checks;
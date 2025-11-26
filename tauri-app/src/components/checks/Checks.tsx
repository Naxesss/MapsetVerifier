import {useQuery} from "@tanstack/react-query";
import {ApiBeatmapSetCheckResult} from "../../Types.ts";
import {FetchError} from "../../client/ApiHelper.ts";
import BeatmapApi from "../../client/BeatmapApi.ts";
import {
  Alert,
  Text,
  Title,
  Stack,
  Group,
  Loader,
  Box,
  Flex,
  Anchor,
  Button, useMantineTheme
} from "@mantine/core";
import {useParams} from "react-router-dom";
import {useSettings} from "../../context/SettingsContext.tsx";
import CategoryAccordion from './CategoryAccordion';
import { invoke } from "@tauri-apps/api/core";
import {IconFolder, IconWorld} from "@tabler/icons-react";

function Checks() {
  const theme = useMantineTheme();
  const { folder } = useParams();
  const { settings } = useSettings();

  // Compute folder path safely (could be undefined until both are available)
  const beatmapFolderPath = folder && settings.songFolder
    ? `${settings.songFolder}\\${folder}`.replace(/\//g, '\\')
    : undefined;

  const { data, isLoading, isError, error } = useQuery<ApiBeatmapSetCheckResult, FetchError>({
    queryKey: ['beatmap-checks', beatmapFolderPath || 'unavailable'],
    queryFn: () => {
      if (!beatmapFolderPath) {
        // Should never run when enabled is false
        throw new Error('Beatmap folder path unavailable');
      }
      return BeatmapApi.runChecks(beatmapFolderPath);
    },
    enabled: !!beatmapFolderPath, // Only execute when we have a valid path
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false,
  });

  if (!folder) {
    return <Text>No BeatmapSet selected.</Text>;
  }

  if (!settings.songFolder) {
    return (
      <Alert color="yellow" title="Song folder not set" withCloseButton>
        <Text size="sm">Please set the song folder in settings to run beatmap checks.</Text>
      </Alert>
    );
  }

  const beatmapBgUrl = folder ? `http://localhost:5005/beatmap/image?folder=${folder}&original=true` : undefined;

  return (
    <>
      <Box
        h="100%"
        style={{
          fontFamily: theme.headings.fontFamily,
          position: "relative",
          width: "100%",
          borderRadius: theme.radius.lg,
          overflow: "hidden",
          boxShadow: "0 4px 32px rgba(0,0,0,0.4)",
          display: "flex",
          flexDirection: "column",
          justifyContent: "flex-start",
        }}
      >
        {/* Header with dynamic background and overlay */}
        <Box
          style={{
            position: "relative",
            width: "100%",
            backgroundImage: beatmapBgUrl ? `url('${beatmapBgUrl}')` : undefined,
            backgroundSize: "cover",
            backgroundPosition: "center",
            backgroundRepeat: "no-repeat",
            overflow: "hidden",
          }}
        >
          {/* Overlay for readability */}
          <Box
            style={{
              position: "absolute",
              top: 0,
              left: 0,
              width: "100%",
              height: "100%",
              background: "rgba(0,0,0,0.7)",
              zIndex: 1,
              pointerEvents: "none",
            }}
          />
          {/* Header content */}
          <Box p="md" style={{ position: "relative", zIndex: 2 }}>
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
                    if (!beatmapFolderPath) return; // guard for TS
                    try {
                      await invoke('open_folder', { path: beatmapFolderPath });
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
                  <IconWorld /> Open beatmap page
                </Button>
              </Group>
            </Stack>
          </Box>
        </Box>
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
      </Box>
    </>
  );
}

export default Checks;

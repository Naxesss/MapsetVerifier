import { Alert, Flex, Text, ActionIcon, Tooltip } from '@mantine/core';
import { IconAlertCircle, IconInfoCircle, IconListDetails, IconRefresh } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import BeatmapCard from './BeatmapCard.tsx';
import { FetchError } from '../../client/ApiHelper.ts';
import BeatmapApi from '../../client/BeatmapApi.ts';
import { useBeatmap } from '../../context/BeatmapContext.tsx';
import { ApiLazerLookupResult } from '../../Types.ts';

export default function LazerBeatmapsPanel() {
  const { selectedFolderPath, setSelectedFolderPath } = useBeatmap();

  const lazerQuery = useQuery<ApiLazerLookupResult, FetchError>({
    queryKey: ['lazer-current'],
    queryFn: () => BeatmapApi.getLazerCurrent(),
    enabled: true,
    refetchOnWindowFocus: false,
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      return status === 'folder_found' ? 5000 : 1500;
    },
    retry: false,
  });

  const renderPanel = () => {
    if (lazerQuery.isLoading) {
      return (
        <Alert
          icon={<IconRefresh />}
          color="blue"
          title="Detecting current osu! map"
          mt="xs"
          variant="light"
        >
          Waiting for osu! editor window metadata...
        </Alert>
      );
    }

    if (lazerQuery.error) {
      return (
        <Alert icon={<IconAlertCircle />} color="red" title="Error" mt="xs">
          <Text>{lazerQuery.error.message || 'Failed to detect the current lazer map.'}</Text>
        </Alert>
      );
    }

    const result = lazerQuery.data;
    if (!result) {
      return null;
    }

    const metadata = result.detectedMetadata ?? 'Not detected yet';
    const infoColor =
      result.status === 'folder_found'
        ? 'green'
        : result.status === 'unsupported_platform'
          ? 'yellow'
          : 'blue';

    return (
      <Flex direction="column" gap="sm">
        <Alert icon={<IconInfoCircle />} color="blue" title="Instructions" variant="light">
          In the editor, go to <strong>File → Edit externally</strong>. This allows MV to detect and
          parse the current map.
        </Alert>
        <Alert
          icon={<IconListDetails />}
          color={infoColor}
          title="Detected beatmap"
          variant="light"
        >
          <Text size="sm">{metadata}</Text>
          {result.message && (
            <Text size="xs" c="dimmed" mt={4}>
              {result.message}
            </Text>
          )}
        </Alert>
        {result.status === 'folder_found' &&
          result.beatmap &&
          result.folderPath &&
          result.lookupRoot && (
            <BeatmapCard
              beatmap={result.beatmap}
              songFolder={result.lookupRoot}
              isSelectedOverride={selectedFolderPath === result.folderPath}
              onSelect={() => setSelectedFolderPath(result.folderPath ?? undefined)}
            />
          )}
      </Flex>
    );
  };

  return (
    <Flex direction="column" gap="sm" p="xs">
      <Flex justify="flex-end" align="center">
        <Tooltip label="Refresh beatmap search">
          <ActionIcon variant="default" onClick={() => lazerQuery.refetch()} size="36">
            <IconRefresh />
          </ActionIcon>
        </Tooltip>
      </Flex>
      {renderPanel()}
    </Flex>
  );
}

import {Alert, Text, Box, useMantineTheme, Flex, LoadingOverlay, Button, Group, Title} from '@mantine/core';
import { IconRefresh } from '@tabler/icons-react';
import { useState, useEffect, useMemo } from 'react';
import { useSnapshots } from './hooks/useSnapshots';
import SnapshotContent from './SnapshotContent';
import SnapshotDifficultySelector from './SnapshotDifficultySelector';
import SnapshotGameModeSelector from './SnapshotGameModeSelector';
import { useBeatmap } from '../../context/BeatmapContext';
import { useSettings } from '../../context/SettingsContext';
import { ApiSnapshotDifficulty, Mode } from '../../Types';
import { useBeatmapBackground } from '../checks/hooks/useBeatmapBackground';
import BeatmapHeader from '../common/BeatmapHeader';

interface ModeGroup {
  mode: Mode;
  difficulties: ApiSnapshotDifficulty[];
}

function Snapshots() {
  const theme = useMantineTheme();
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();
  const [selectedDifficulty, setSelectedDifficulty] = useState<string | undefined>('General');
  const [selectedMode, setSelectedMode] = useState<Mode | undefined>();

  useEffect(() => {
    // Reset selected difficulty when changing beatmap
    if (folder) {
      setSelectedDifficulty('General');
    }
  }, [folder]);

  const { data, isLoading, isError, error, refetch } = useSnapshots({
    folder,
    songFolder: settings.songFolder,
  });

  const { bgUrl } = useBeatmapBackground(folder);

  // Group difficulties by mode (excluding General which is handled separately)
  const groupedDifficulties = useMemo((): ModeGroup[] => {
    if (!data?.difficulties) return [];

    // Group difficulties by mode
    const modeGroups: Record<Mode, ApiSnapshotDifficulty[]> = {
      Standard: [],
      Taiko: [],
      Catch: [],
      Mania: [],
    };

    data.difficulties.forEach((diff) => {
      if (!diff.isGeneral && diff.mode) {
        modeGroups[diff.mode].push(diff);
      }
    });

    // Return only modes that have difficulties
    const result = (['Standard', 'Taiko', 'Catch', 'Mania'] as Mode[])
      .filter((mode) => modeGroups[mode].length > 0)
      .map((mode) => ({
        mode,
        difficulties: modeGroups[mode],
      }));

    // Set initial selected mode if not set
    if (result.length > 0 && !selectedMode) {
      setSelectedMode(result[0].mode);
    }

    return result;
  }, [data?.difficulties, selectedMode]);

  const selectedGroup = groupedDifficulties.find((g) => g.mode === selectedMode) ?? groupedDifficulties[0];

  if (!folder) {
    return <Text>No BeatmapSet selected.</Text>;
  }

  if (!settings.songFolder) {
    return (
      <Alert color="yellow" title="Song folder not set" withCloseButton>
        <Text size="sm">Please set the song folder in settings to view snapshots.</Text>
      </Alert>
    );
  }

  return (
    <Box
      h="100%"
      style={{
        fontFamily: theme.headings.fontFamily,
        position: 'relative',
        width: '100%',
        borderRadius: theme.radius.lg,
        overflow: 'hidden',
        boxShadow: '0 4px 32px rgba(0,0,0,0.4)',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'flex-start',
      }}
    >
      <LoadingOverlay visible={isLoading} zIndex={1000} overlayProps={{ radius: "sm", blur: 2 }} />
      <BeatmapHeader title={data?.title} artist={data?.artist} creator={data?.creator} bgUrl={bgUrl}>
        <Group gap="sm">
          <Group
            p="xs"
            gap="xs"
            bg={theme.colors.dark[8]}
            style={{ borderRadius: theme.radius.md }}
          >
            <Button
              variant="default"
              size="xs"
              leftSection={<IconRefresh size={16} />}
              onClick={() => refetch()}
            >
              Refresh
            </Button>
          </Group>
          <SnapshotGameModeSelector
            groupedDifficulties={groupedDifficulties}
            selectedMode={selectedMode}
            onModeChange={setSelectedMode}
          />
        </Group>
        {data?.difficulties && selectedGroup && (
          <SnapshotDifficultySelector
            difficulties={selectedGroup.difficulties}
            selectedDifficulty={selectedDifficulty}
            onSelectDifficulty={setSelectedDifficulty}
          />
        )}
      </BeatmapHeader>
      {data && (
        <Flex gap="sm" p="md" direction="column" style={{ flex: 1, overflow: 'hidden' }}>
          <Title order={3}>
            {selectedDifficulty}
          </Title>
          {data.errorMessage ? (
            <Alert color="yellow" title="Snapshots unavailable">
              <Text size="sm">{data.errorMessage}</Text>
            </Alert>
          ) : (
            <>
              <SnapshotContent
                data={data}
                selectedDifficulty={selectedDifficulty}
              />
            </>
          )}
        </Flex>
      )}
      {isError && (
        <Flex p="md">
          <Alert color="red" title="Error loading snapshots" withCloseButton>
            <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
              {error?.message}
            </Text>
            {error?.stackTrace && (
              <Text mt="sm" size="xs" c="red.3" style={{ whiteSpace: 'pre-wrap' }}>
                {error.stackTrace}
              </Text>
            )}
          </Alert>
        </Flex>
      )}
    </Box>
  );
}

export default Snapshots;


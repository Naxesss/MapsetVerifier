import {
  Alert,
  Text,
  Box,
  useMantineTheme,
  Group,
  SegmentedControl,
  Tooltip,
  Button,
  Flex,
  Loader,
  Divider
} from '@mantine/core';
import { useParams } from 'react-router-dom';
import BeatmapHeader from './BeatmapHeader';
import ChecksResults from './ChecksResults';
import { useBeatmapBackground } from './hooks/useBeatmapBackground';
import { useBeatmapChecks } from './hooks/useBeatmapChecks';
import { useDifficultyOverride } from './hooks/useDifficultyOverride';
import { useSettings } from '../../context/SettingsContext.tsx';
import React from 'react';
import GameModeIcon from "../icons/GameModeIcon.tsx";

function Checks() {
  const theme = useMantineTheme();
  const { folder } = useParams();
  const { settings } = useSettings();
  const [selectedCategory, setSelectedCategory] = React.useState<string | undefined>('General');

  const { data, isLoading, isError, error, beatmapFolderPath } = useBeatmapChecks({
    folder,
    songFolder: settings.songFolder,
  });

  const { bgUrl } = useBeatmapBackground(folder);

  const {
    applyOverride,
    clearOverride,
    getOverrideResult,
    getOverrideLevel,
    isLoading: isOverrideLoading,
  } = useDifficultyOverride({ beatmapFolderPath });

  const difficultyLevels = ['Easy', 'Normal', 'Hard', 'Insane', 'Expert'];

  const selectedDifficulty = data?.difficulties?.find((d) => d.category === selectedCategory);
  const currentOverrideResult = selectedCategory ? getOverrideResult(selectedCategory) : undefined;
  const currentOverrideLevel = selectedCategory ? getOverrideLevel(selectedCategory) : undefined;

  const difficultyOverrideControls = selectedDifficulty ? (
    <Group gap="sm" justify="flex-start" wrap="wrap">
      {(() => {
        const current = selectedDifficulty.difficultyLevel || 'Unknown';
        const selected = currentOverrideLevel || current;
        return (
          <Group gap="md" align="center">
            <Text size="sm">Difficulty Level</Text>
            <Tooltip label={`Override difficulty for ${selectedDifficulty.category}`} withArrow withinPortal>
              <SegmentedControl
                radius="md"
                data={difficultyLevels.map((lvl) => ({ label: lvl, value: lvl }))}
                value={selected}
                onChange={(val) => {
                  const mapped = val === 'Ultra' ? 'Expert' : val;
                  const isDefault = mapped === current || (current === 'Expert' && val === 'Ultra');
                  if (isDefault) {
                    clearOverride(selectedDifficulty.category);
                  } else {
                    applyOverride(selectedDifficulty.category, val);
                  }
                }}
                fullWidth={false}
                styles={{ root: { maxWidth: '100%' } }}
              />
            </Tooltip>
            {isOverrideLoading && <Loader size="xs" />}
          </Group>
        );
      })()}
    </Group>
  ) : null;

  const categoryButtons = data?.difficulties ? (
    <Group gap="xs" wrap="wrap">
      <Button
        p="xs"
        variant={selectedCategory === 'General' ? 'filled' : 'default'}
        onClick={() => setSelectedCategory('General')}
      >
        General
      </Button>
      {data.difficulties.map((diff) => (
        <Button
          key={diff.category}
          p="xs"
          variant={selectedCategory === diff.category ? 'filled' : 'default'}
          onClick={() => setSelectedCategory(diff.category)}
        >
          <Flex gap="xs" align="center">
            <GameModeIcon mode={diff.mode!} size={24} />
            {diff.category}
          </Flex>
        </Button>
      ))}
    </Group>
  ) : null;

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
      <BeatmapHeader data={data} beatmapFolderPath={beatmapFolderPath} bgUrl={bgUrl} />
      <Flex gap="md" m="md" direction="column">
        {categoryButtons}
        {difficultyOverrideControls}
        <Divider />
        <ChecksResults
          data={data}
          isLoading={isLoading}
          isError={isError}
          error={error}
          showMinor={settings.showMinor}
          selectedCategory={selectedCategory}
          overrideResult={currentOverrideResult}
        />
      </Flex>
    </Box>
  );
}

export default Checks;

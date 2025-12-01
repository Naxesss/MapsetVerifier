import { Alert, Text, Box, useMantineTheme } from '@mantine/core';
import { useParams } from 'react-router-dom';
import BeatmapHeader from './BeatmapHeader';
import ChecksResults from './ChecksResults';
import { useBeatmapBackground } from './hooks/useBeatmapBackground';
import { useBeatmapChecks } from './hooks/useBeatmapChecks';
import { useSettings } from '../../context/SettingsContext.tsx';

function Checks() {
  const theme = useMantineTheme();
  const { folder } = useParams();
  const { settings } = useSettings();

  // Data & path logic extracted to hook.
  const { data, isLoading, isError, error, beatmapFolderPath } = useBeatmapChecks({
    folder,
    songFolder: settings.songFolder,
  });

  // Background image load logic extracted to hook.
  const { bgUrl } = useBeatmapBackground(folder);

  // Early exits remain for clarity.
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
      {/* Header */}
      <BeatmapHeader data={data} beatmapFolderPath={beatmapFolderPath} bgUrl={bgUrl} />
      {/* Results */}
      <ChecksResults
        data={data}
        isLoading={isLoading}
        isError={isError}
        error={error}
        showMinor={settings.showMinor}
      />
    </Box>
  );
}

export default Checks;

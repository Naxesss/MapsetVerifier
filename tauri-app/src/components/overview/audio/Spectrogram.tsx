import {Box, Button, Center, Flex, Group, Loader, Modal, Paper, Select, Text, Tooltip, useMantineTheme} from '@mantine/core';
import {useCallback, useState} from 'react';
import {SpectralAnalysisResult} from '../../Types';
import {IconInfoCircle, IconZoomIn} from "@tabler/icons-react";
import SpectrogramCanvas, {ColorScheme} from "./SpectrogramCanvas.tsx";
import {useSpectrogram} from "./hooks/useAudioAnalysis.ts";

interface SpectrogramProps {
  folder: string;
  songFolder: string;
}

function Spectrogram({ folder, songFolder }: SpectrogramProps) {
  const magnitudeThreshold = -100;
  const { data, isLoading, isError, error, refetch } = useSpectrogram({
    folder,
    songFolder: songFolder,
  });
  const theme = useMantineTheme();
  const [modalOpened, setModalOpened] = useState(false);
  const [colorScheme, setColorScheme] = useState<ColorScheme>('inferno');

  // Calculate values that depend on data (with safe defaults)
  const duration = data?.timePositions?.length ? data.timePositions[data.timePositions.length - 1] : 0;

  // Calculate the average Nyquist frequency (highest frequency with any energy) across the entire song
  const calculatePeakFrequency = useCallback(() => {
    if (!data?.spectrogramData?.length) return 0;

    let totalNyquistFreq = 0;
    let frameCount = 0;

    data.spectrogramData.forEach((frame) => {
      const numBins = frame.magnitudes.length;
      let highestFreqWithEnergy = 0;

      // Find the highest frequency bin with magnitude above threshold for this frame
      for (let freqIdx = numBins - 1; freqIdx >= 0; freqIdx--) {
        if (frame.magnitudes[freqIdx] > magnitudeThreshold) {
          highestFreqWithEnergy = data.frequencyBins?.[freqIdx] || (freqIdx * (data.sampleRate / 2) / numBins);
          break;
        }
      }

      if (highestFreqWithEnergy > 0) {
        totalNyquistFreq += highestFreqWithEnergy;
        frameCount++;
      }
    });

    return frameCount > 0 ? totalNyquistFreq / frameCount : 0;
  }, [data, magnitudeThreshold]);

  const peakFreq = calculatePeakFrequency();

  // Generate dynamic Y-axis labels based on Nyquist frequency
  const formatFreq = (hz: number) => hz >= 1000 ? `${(hz / 1000).toFixed(1)}kHz` : `${hz}Hz`;

  if (isLoading) {
    return (
      <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
        <Text fw={600} mb="sm">Spectrogram</Text>
        <Center h={250}>
          <Loader size="lg" />
        </Center>
      </Paper>
    );
  }
  
  if (isError) {
    return (
      <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
        <Text fw={600} mb="sm">Spectrogram</Text>
        <Center h={250}>
          <Text c="red">Error loading spectrogram: {error?.message}</Text>
          <Text c="red">{error?.stackTrace}</Text>
          <Button color="red" variant="light" onClick={() => refetch()}>Retry</Button>
        </Center>
      </Paper>
    );
  }

  if (!data) {
    return (
      <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
        <Text fw={600} mb="sm">Spectrogram</Text>
        <Center h={250}>
          <Text c="dimmed">No spectrogram data available</Text>
        </Center>
      </Paper>
    );
  }

  const spectrogramContent = (
    <Box>
      <Group justify="space-between" mb="sm">
        <Group gap="lg">
          <Text size="sm" c="dimmed">
            Average Peak Frequency: <Text span fw={500} c="white">{formatFreq(peakFreq)}</Text>
            {' '}
            <Text span c="cyan" fw={500}>(cyan line)</Text>
          </Text>
        </Group>
        <Group>
          <Text size="sm" c="dimmed">Color Scheme:</Text>
          <Select
            allowDeselect={false}
            value={colorScheme}
            onChange={(value) => setColorScheme(value as ColorScheme)}
            data={[
              { value: 'viridis', label: 'Viridis' },
              { value: 'plasma', label: 'Plasma' },
              { value: 'inferno', label: 'Inferno' },
              { value: 'magma', label: 'Magma' },
              { value: 'cividis', label: 'Cividis' }
            ]}
            size="xs"
            w={120}
          />
        </Group>
      </Group>
      <SpectrogramCanvas
        data={data}
        avgFreq={peakFreq}
        duration={duration}
        colorScheme={colorScheme}
      />
    </Box>
  );

  return (
    <>
      <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
        <Flex direction="column" gap="md">
          <Group justify="space-between" align="center">
            <Group gap="xs">
              <Text fw={600}>Spectrogram</Text>
              <Tooltip
                label={`Displays the frequency content of the audio over time. The average peak frequency is based on a magnitude threshold of ${magnitudeThreshold} dB.`}
                multiline
                w={250}
              >
                <IconInfoCircle size={16} style={{ color: theme.colors.gray[6], cursor: 'help' }} />
              </Tooltip>
            </Group>
            <Button
              leftSection={<IconZoomIn size={16} />}
              variant="light"
              size="sm"
              onClick={() => setModalOpened(true)}
            >
              Zoom
            </Button>
          </Group>
          {spectrogramContent}
        </Flex>
      </Paper>

      <Modal
        opened={modalOpened}
        onClose={() => setModalOpened(false)}
        title="Spectrogram Analysis"
        size="100%"
        centered
      >
        {spectrogramContent}
      </Modal>
    </>
  );
}

export default Spectrogram;


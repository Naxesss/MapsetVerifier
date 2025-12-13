import {Box, Button, Center, Flex, Group, Loader, Modal, Paper, Text, Tooltip, useMantineTheme} from '@mantine/core';
import {useCallback, useState} from 'react';
import {SpectralAnalysisResult} from '../../Types';
import {IconInfoCircle, IconZoomIn} from "@tabler/icons-react";
import {ComposedChart, Customized, ResponsiveContainer, XAxis, YAxis} from "recharts";
import SpectrogramCanvas from "./SpectrogramCanvas.tsx";

interface SpectrogramProps {
  data: SpectralAnalysisResult | undefined;
  isLoading: boolean;
}

function Spectrogram({ data, isLoading }: SpectrogramProps) {
  const magnitudeThreshold = -120;
  const theme = useMantineTheme();
  const [modalOpened, setModalOpened] = useState(false);

  // Calculate values that depend on data (with safe defaults)
  const duration = data?.timePositions?.length ? data.timePositions[data.timePositions.length - 1] : 0;
  const nyquistFreq = data ? data.sampleRate / 2 : 0;

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
  const formatFreq = (hz: number) => hz >= 1000 ? `${(hz / 1000).toFixed(hz >= 10000 ? 0 : 1)}kHz` : `${hz}Hz`;

  // Get the actual frequency range from the data (filtered to 20Hz-20kHz typically)
  const minFreq = data?.frequencyBins?.[0] || 20;
  const maxFreq = data?.frequencyBins?.[data.frequencyBins.length - 1] || nyquistFreq;

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
            Peak Frequency: <Text span fw={500} c="white">{formatFreq(peakFreq)}</Text>
            {' '}
            <Text span c="cyan" fw={500}>(cyan line)</Text>
          </Text>
        </Group>
      </Group>
      <Box style={{ width: '100%', height: 400 }}>
        <ResponsiveContainer width="100%" height="100%">
        <ComposedChart
          data={data.spectrogramData}
          margin={{ top: 20, right: 20, bottom: 20, left: 50 }}
        >
          <XAxis
            dataKey="timeMs"
            type="number"
            domain={[0, duration]}
            label={{ value: 'Time (ms)', position: 'insideBottom', offset: -10 }}
          />
          <YAxis
            dataKey="frequency"
            type="number"
            domain={[minFreq, maxFreq]}
            label={{ value: 'Frequency (Hz)', angle: -90, position: 'insideLeft' }}
          />

          {/* Custom spectrogram canvas layer */}
          <Customized component={SpectrogramCanvas} data={data} avgFreq={peakFreq} />
        </ComposedChart>
      </ResponsiveContainer>
      </Box>
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
                label={`Displays the frequency content of the audio over time. The average frequency is based on a magtitude threshold of ${magnitudeThreshold} dB.`}
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
        size="xl"
        centered
      >
        {spectrogramContent}
      </Modal>
    </>
  );
}

export default Spectrogram;


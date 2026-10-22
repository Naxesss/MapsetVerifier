import { Box, Button, Center, Group, Modal, Select, Text } from '@mantine/core';
import { IconZoomIn } from '@tabler/icons-react';
import { useCallback, useState } from 'react';
import { useSpectrogram } from './hooks/useAudioAnalysis.ts';
import SpectrogramCanvas, { ColorScheme } from './SpectrogramCanvas.tsx';
import { InfoIconTooltip } from '../../common/InfoIconTooltip.tsx';
import { BlockSkeleton } from '../../common/LoadingSkeletons.tsx';
import SectionCard from '../../common/SectionCard.tsx';

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
  const [modalOpened, setModalOpened] = useState(false);
  const [colorScheme, setColorScheme] = useState<ColorScheme>('inferno');

  // Calculate values that depend on data (with safe defaults)
  const duration = data?.timePositions?.length
    ? data.timePositions[data.timePositions.length - 1]
    : 0;

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
          highestFreqWithEnergy =
            data.frequencyBins?.[freqIdx] || (freqIdx * (data.sampleRate / 2)) / numBins;
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
  const formatFreq = (hz: number) => (hz >= 1000 ? `${(hz / 1000).toFixed(1)}kHz` : `${hz}Hz`);

  if (isLoading) {
    return (
      <SectionCard title="Spectrogram">
        <BlockSkeleton height={250} />
      </SectionCard>
    );
  }

  if (isError) {
    return (
      <SectionCard title="Spectrogram">
        <Center h={250}>
          <Text c="red">Error loading spectrogram: {error?.message}</Text>
          <Text c="red">{error?.stackTrace}</Text>
          <Button color="red" variant="light" onClick={() => refetch()}>
            Retry
          </Button>
        </Center>
      </SectionCard>
    );
  }

  if (!data) {
    return (
      <SectionCard title="Spectrogram">
        <Center h={250}>
          <Text c="dimmed">No spectrogram data available.</Text>
        </Center>
      </SectionCard>
    );
  }

  const spectrogramContent = (
    <Box>
      <Group justify="space-between" mb="sm">
        <Group gap="xs" align="center" wrap="nowrap">
          <Text size="sm" c="dimmed">
            Average peak frequency:{' '}
            <Text span fw={500} c="cyan">
              {formatFreq(peakFreq)}
            </Text>
          </Text>
          <InfoIconTooltip label="Indicated by the cyan line below" />
        </Group>
        <Group>
          <Text size="sm" c="dimmed">
            Colour scheme:
          </Text>
          <Select
            allowDeselect={false}
            value={colorScheme}
            onChange={(value) => setColorScheme(value as ColorScheme)}
            data={[
              { value: 'viridis', label: 'Viridis' },
              { value: 'plasma', label: 'Plasma' },
              { value: 'inferno', label: 'Inferno' },
              { value: 'magma', label: 'Magma' },
              { value: 'cividis', label: 'Cividis' },
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
      <SectionCard
        title="Spectrogram"
        info={`Displays the frequency content of the audio over time. The average peak frequency is based on a magnitude threshold of ${magnitudeThreshold} dB.`}
        actions={
          <Button
            leftSection={<IconZoomIn size={16} />}
            variant="light"
            size="xs"
            onClick={() => setModalOpened(true)}
          >
            Zoom
          </Button>
        }
      >
        {spectrogramContent}
      </SectionCard>

      <Modal
        opened={modalOpened}
        onClose={() => setModalOpened(false)}
        title="Spectrogram"
        size="100%"
        centered
      >
        {spectrogramContent}
      </Modal>
    </>
  );
}

export default Spectrogram;

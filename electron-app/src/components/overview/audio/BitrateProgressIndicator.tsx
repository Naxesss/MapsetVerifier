import { Box, Group, Progress, Stack, Text, Tooltip, useMantineTheme } from '@mantine/core';
import { BitrateAnalysisResult } from '../../../Types';

interface BitrateProgressIndicatorProps {
  bitrateData: BitrateAnalysisResult;
}

function BitrateProgressIndicator({ bitrateData }: BitrateProgressIndicatorProps) {
  const theme = useMantineTheme();
  const displayMinBitrate = 64;
  const displayMaxBitrate = 320;
  const displayRange = displayMaxBitrate - displayMinBitrate;

  const isAverageBitrateOutOfRange =
    bitrateData.averageBitrate < bitrateData.minAllowedBitrate ||
    bitrateData.averageBitrate > bitrateData.maxAllowedBitrate;

  const averageBitratePositionPercent = Math.min(
    100,
    Math.max(0, ((bitrateData.averageBitrate - displayMinBitrate) / displayRange) * 100)
  );
  const minAllowedPositionPercent = Math.min(
    100,
    Math.max(0, ((bitrateData.minAllowedBitrate - displayMinBitrate) / displayRange) * 100)
  );
  const maxAllowedPositionPercent = Math.min(
    100,
    Math.max(0, ((bitrateData.maxAllowedBitrate - displayMinBitrate) / displayRange) * 100)
  );
  const allowedZonePercent = Math.max(0, maxAllowedPositionPercent - minAllowedPositionPercent);
  const markerColor = bitrateData.isCompliant ? theme.colors.blue[4] : theme.colors.orange[4];
  const averageBitrateRangeDirection =
    bitrateData.averageBitrate < bitrateData.minAllowedBitrate
      ? 'below'
      : bitrateData.averageBitrate > bitrateData.maxAllowedBitrate
        ? 'above'
        : null;

  return (
    <Stack gap={4}>
      <Group justify="space-between" align="flex-start" mb="xs">
        <Stack gap={0}>
          <Text size="xs" c="dimmed" tt="uppercase">
            Average Bitrate
          </Text>
          <Text fw={700} size="xl" c={isAverageBitrateOutOfRange ? 'red.4' : undefined}>
            {bitrateData.averageBitrate.toLocaleString()} kbps
          </Text>
        </Stack>
        <Text size="xs" c="dimmed" ta="right">
          Allowed: {bitrateData.minAllowedBitrate.toLocaleString()}-
          {bitrateData.maxAllowedBitrate.toLocaleString()} kbps
        </Text>
      </Group>

      <Box pos="relative" mb={4}>
        <Progress.Root size={8} radius="xl" bg={theme.colors.dark[4]}>
          <Progress.Section value={minAllowedPositionPercent} color="red.6" />
          <Progress.Section value={allowedZonePercent} color="green.6" />
          <Progress.Section value={100 - maxAllowedPositionPercent} color="red.6" />
        </Progress.Root>
        <Tooltip label={`${bitrateData.averageBitrate.toLocaleString()} kbps`} withArrow>
          <Box
            pos="absolute"
            top={-6}
            left={`calc(${averageBitratePositionPercent}% - 1.5px)`}
            w={6}
            h={20}
            bg={markerColor}
            style={{
              borderRadius: 999,
              border: `1px solid ${theme.colors.dark[4]}`,
              cursor: 'pointer',
            }}
          />
        </Tooltip>
      </Box>

      <Box pos="relative" h={16}>
        <Text
          size="xs"
          c="dimmed"
          pos="absolute"
          left="0%"
          style={{ transform: 'translateX(0)' }}
        >
          64 kbps
        </Text>
        <Text
          size="xs"
          c="dimmed"
          pos="absolute"
          left={`${minAllowedPositionPercent}%`}
          style={{ transform: 'translateX(-50%)' }}
        >
          {bitrateData.minAllowedBitrate.toLocaleString()} kbps
        </Text>
        <Text
          size="xs"
          c="dimmed"
          pos="absolute"
          left={`${maxAllowedPositionPercent}%`}
          style={{ transform: 'translateX(-50%)' }}
        >
          {bitrateData.maxAllowedBitrate.toLocaleString()} kbps
        </Text>
        <Text
          size="xs"
          c="dimmed"
          pos="absolute"
          left="100%"
          style={{ transform: 'translateX(-100%)', whiteSpace: 'nowrap' }}
        >
          320 kbps
        </Text>
      </Box>

      {isAverageBitrateOutOfRange && (
        <Text size="xs" c="red.4" mt={2}>
          Average bitrate is {averageBitrateRangeDirection} the allowed range.
        </Text>
      )}
    </Stack>
  );
}

export default BitrateProgressIndicator;

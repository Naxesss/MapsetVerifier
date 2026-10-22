import { Badge, Group, SimpleGrid, Text } from '@mantine/core';
import { BitrateAnalysisResult, FormatAnalysisResult } from '../../../Types';
import { InfoIconTooltip } from '../../common/InfoIconTooltip.tsx';
import SectionCard from '../../common/SectionCard.tsx';
import { StatField } from '../../common/StatField.tsx';

interface FormatInfoProps {
  data: FormatAnalysisResult;
  audioFilePath: string;
  bitrateData?: BitrateAnalysisResult | null;
}

const formatKbps = (kbps: number) => `${kbps.toLocaleString()} kbps`;

/**
 * What the audio file is: format, length, channels, sample rate, codec and bitrate. Whether any of
 * it breaks a ranking rule is for Checks to say.
 */
function FormatInfo({ data, audioFilePath, bitrateData }: FormatInfoProps) {
  // A constant bitrate has no range worth showing.
  const lowest = bitrateData?.isVbr ? bitrateData.minBitrate : null;
  const highest = bitrateData?.isVbr ? bitrateData.maxBitrate : null;

  return (
    <SectionCard
      title="Format"
      info="Format information describes the technical properties of an audio file that define how it is stored and played back."
      actions={<Badge>{data.format}</Badge>}
    >
      <SimpleGrid cols={3} style={{ gridTemplateColumns: '1fr 1fr 1fr' }}>
        <StatField label="File name" value={audioFilePath} />
        <StatField label="Duration" value={data.durationFormatted} />
        <StatField label="File size" value={data.fileSizeFormatted} />
        <StatField
          label="Channels"
          value={
            data.channels === 1 ? 'Mono' : data.channels === 2 ? 'Stereo' : `${data.channels}ch`
          }
        />
        <StatField label="Sample rate" value={`${(data.sampleRate / 1000).toFixed(1)} kHz`} />
        <StatField label="Codec" value={data.codec} />
      </SimpleGrid>

      {bitrateData && (
        <>
          <Group gap="xs" mt="md" mb="xs">
            <Text fw={600} size="sm">
              Bitrate
            </Text>
            <InfoIconTooltip
              label="Bitrate represents the amount of data used per second of audio. Higher bitrates generally preserve more detail but result in larger file sizes."
              multiline
              w={250}
              iconSize={14}
            />
            {bitrateData.isVbr && <Badge>VBR</Badge>}
          </Group>
          <SimpleGrid cols={3} style={{ gridTemplateColumns: '1fr 1fr 1fr' }}>
            <StatField label="Average" value={formatKbps(bitrateData.averageBitrate)} />
            {lowest != null && <StatField label="Lowest" value={formatKbps(lowest)} />}
            {highest != null && <StatField label="Highest" value={formatKbps(highest)} />}
          </SimpleGrid>
        </>
      )}
    </SectionCard>
  );
}

export default FormatInfo;

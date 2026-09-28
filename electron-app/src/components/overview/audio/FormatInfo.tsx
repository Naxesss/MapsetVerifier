import { Badge, Box, Group, SimpleGrid, Text } from '@mantine/core';
import BitrateProgressIndicator from './BitrateProgressIndicator.tsx';
import { BitrateAnalysisResult, FormatAnalysisResult } from '../../../Types';
import { InfoIconTooltip } from '../../common/InfoIconTooltip.tsx';
import SectionCard from '../../common/SectionCard.tsx';
import { StatField } from '../../common/StatField.tsx';
import {
  ComplianceBadge,
  ComplianceIssueList,
  formatBadgeColor,
  RequirementsList,
  RuleLabel,
} from '../formatCard.tsx';

interface FormatInfoProps {
  data: FormatAnalysisResult;
  audioFilePath: string;
  bitrateData?: BitrateAnalysisResult | null;
}

function FormatInfo({ data, audioFilePath, bitrateData }: FormatInfoProps) {
  // Check if sample rate exceeds 48 kHz
  const sampleRateExceeds48kHz = data.sampleRate > 48000;

  // Check format compliance (MP3 or Ogg Vorbis)
  const isValidFormat = data.format.toLowerCase() === 'mp3' || data.format.toLowerCase() === 'ogg';

  return (
    <SectionCard
      title="Format"
      info="Format information describes the technical properties of an audio file that define how it is stored and played back."
      actions={
        <>
          <Badge color={formatBadgeColor(data.badgeType)}>{data.format}</Badge>
          <ComplianceBadge compliant={data.isCompliant} />
        </>
      }
    >
      <SimpleGrid cols={3} mb="md" style={{ gridTemplateColumns: '1fr 1fr 1fr' }}>
        <StatField label="File name" value={audioFilePath} />
        <StatField label="Duration" value={data.durationFormatted} />
        <StatField label="File size" value={data.fileSizeFormatted} />
        <StatField
          label="Channels"
          value={
            data.channels === 1 ? 'Mono' : data.channels === 2 ? 'Stereo' : `${data.channels}ch`
          }
        />
        <StatField
          label="Sample rate"
          value={`${(data.sampleRate / 1000).toFixed(1)} kHz`}
          note={sampleRateExceeds48kHz ? 'Exceeds 48 kHz limit' : undefined}
          noteColor="red.4"
        />
        <StatField
          label={
            <RuleLabel
              label="Codec"
              brokenRule={isValidFormat ? undefined : 'Must be MP3 or Ogg Vorbis'}
            />
          }
          value={data.codec}
          valueColor={isValidFormat ? undefined : 'red.4'}
        />
      </SimpleGrid>

      {bitrateData && (
        <>
          <Group justify="space-between" mb="xs">
            <Group gap="xs">
              <Text fw={600} size="sm">
                Bitrate
              </Text>
              <InfoIconTooltip
                label="Bitrate represents the amount of data used per second of audio. Higher bitrates generally preserve more detail but result in larger file sizes."
                multiline
                w={250}
                iconSize={14}
              />
            </Group>
            <Group gap="xs">
              <ComplianceBadge compliant={bitrateData.isCompliant} />
              {bitrateData.isVbr && <Badge color="blue">VBR</Badge>}
            </Group>
          </Group>
          <Box mb="md">
            <BitrateProgressIndicator bitrateData={bitrateData} />
          </Box>
        </>
      )}

      <RequirementsList
        requirements={[
          { label: 'Format is MP3 or Ogg Vorbis', met: isValidFormat },
          { label: 'Sample rate 48 kHz or below', met: !sampleRateExceeds48kHz },
        ]}
      />

      <ComplianceIssueList issues={data.complianceIssues ?? []} />
    </SectionCard>
  );
}

export default FormatInfo;

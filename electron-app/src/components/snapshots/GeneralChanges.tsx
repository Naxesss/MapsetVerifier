import { Badge, Box, Group, Stack, Text } from '@mantine/core';
import {
  IconClockEdit,
  IconFile,
  IconMusic,
  IconPhoto,
  IconSettings,
  IconVideo,
  IconEqual,
} from '@tabler/icons-react';
import { DiffOpIcon } from './ChangeCounts';
import { describeFile, describeRollup, describeRollupDetail } from './describe';
import SettingLines from './SettingLines';
import EmptyState from '../common/EmptyState.tsx';
import { MicroLabel } from '../common/Headings.tsx';
import SectionCard from '../common/SectionCard.tsx';
import type { ApiSnapshotFileChange, ApiSnapshotGeneralComparison } from '../../Types';

const FILE_ICONS: Record<string, typeof IconFile> = {
  Audio: IconMusic,
  Video: IconVideo,
  Image: IconPhoto,
};

function FileRow({ file }: { file: ApiSnapshotFileChange }) {
  const Icon = FILE_ICONS[file.category] ?? IconFile;

  return (
    <Box
      p="xs"
      px="sm"
      style={{
        borderRadius: 'var(--mantine-radius-sm)',
        background: 'var(--mantine-color-dark-7)',
      }}
    >
      <Group gap="xs" wrap="nowrap" align="flex-start">
        <Box mt={3} style={{ display: 'flex', flexShrink: 0 }}>
          <DiffOpIcon op={file.op} />
        </Box>
        <Icon size={16} stroke={1.5} style={{ marginTop: 3, flexShrink: 0, opacity: 0.7 }} />
        <Text size="sm" style={{ flex: 1, minWidth: 0, overflowWrap: 'anywhere' }}>
          {describeFile(file)}
        </Text>
        <Badge color="gray" style={{ flexShrink: 0 }}>
          {file.category}
        </Badge>
      </Group>
    </Box>
  );
}

/**
 * What changed about the set as a whole: a time shift every difficulty shares, settings changed
 * the same way everywhere (listed once, with how many difficulties they apply to), and files.
 */
export default function GeneralChanges({
  general,
  difficultyCount,
}: {
  general: ApiSnapshotGeneralComparison;
  difficultyCount: number;
}) {
  const nothing =
    general.rollups.length === 0 && general.settings.length === 0 && general.files.length === 0;

  if (nothing) {
    return (
      <SectionCard title="General">
        <EmptyState
          icon={IconEqual}
          title="Nothing changed for the set as a whole"
          description="Shared settings, a shared time shift and files are listed here when they change."
        />
      </SectionCard>
    );
  }

  return (
    <Stack gap="md">
      {general.rollups.length > 0 && (
        <SectionCard
          title="Timing"
          info="A shift that every difficulty shares is listed once here, not in each of them."
        >
          <Stack gap="xs">
            {general.rollups.map((rollup) => (
              <Box
                key={rollup.kind + rollup.amount}
                p="sm"
                style={{
                  borderRadius: 'var(--mantine-radius-sm)',
                  background: 'var(--mantine-color-dark-7)',
                }}
              >
                <Group gap="sm" wrap="nowrap" align="flex-start">
                  <IconClockEdit size={20} stroke={1.5} style={{ flexShrink: 0, marginTop: 2 }} />
                  <Stack gap={2}>
                    <Text size="sm" fw={500}>
                      {describeRollup(rollup, difficultyCount)}
                    </Text>
                    <Text size="xs" c="dimmed">
                      {describeRollupDetail(rollup)}
                    </Text>
                  </Stack>
                </Group>
              </Box>
            ))}
          </Stack>
        </SectionCard>
      )}
      {general.settings.length > 0 && (
        <SectionCard
          title="Settings"
          info="Changed the same way in every difficulty, so shown once."
          actions={<IconSettings size={16} stroke={1.5} style={{ opacity: 0.6 }} />}
        >
          <SettingLines settings={general.settings} />
        </SectionCard>
      )}
      {general.files.length > 0 && (
        <SectionCard title="Files">
          <Stack gap="md">
            {[...new Set(general.files.map((f) => f.category))].map((category) => (
              <Stack key={category} gap="xs">
                <MicroLabel>{category}</MicroLabel>
                {general.files
                  .filter((f) => f.category === category)
                  .map((f) => (
                    <FileRow key={f.name} file={f} />
                  ))}
              </Stack>
            ))}
          </Stack>
        </SectionCard>
      )}
    </Stack>
  );
}

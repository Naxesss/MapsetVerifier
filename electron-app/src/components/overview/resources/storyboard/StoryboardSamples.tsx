import {
  alpha,
  Box,
  parseThemeColor,
  Stack,
  Table,
  Text,
  UnstyledButton,
  useMantineTheme,
} from '@mantine/core';
import { useElementSize } from '@mantine/hooks';
import { useState } from 'react';
import { buildBeatmapResourceUrl } from '../../../../utils/buildBeatmapFolderPath.ts';
import AppTable from '../../../common/AppTable.tsx';
import AudioWaveformPlayer from '../../../common/audio/AudioWaveformPlayer.tsx';
import SectionCard from '../../../common/SectionCard.tsx';
import TimeAxis from '../../../common/TimeAxis.tsx';
import TimestampLink from '../../../common/TimestampLink.tsx';
import { KIND_COLORS } from '../resourceFiles.ts';
import type { StoryboardSampleEvent, StoryboardSampleFile } from '../../../../Types';

interface StoryboardSamplesProps {
  samples: StoryboardSampleEvent[];
  sampleFiles: StoryboardSampleFile[];
  songDurationMs: number;
  beatmapFolderPath: string | undefined;
}

const STRIP_HEIGHT = 48;

/** Timestamps end in " - " for pasting into a discussion; links show them without it. */
const linkTimestamp = (timestamp: string) => timestamp.replace(/\s*-\s*$/, '');

/**
 * When the storyboard plays its samples, over the song's length, and each sample file to listen
 * to. Picking a file in the list brings its marks forward on the strip.
 */
function StoryboardSamples({
  samples,
  sampleFiles,
  songDurationMs,
  beatmapFolderPath,
}: StoryboardSamplesProps) {
  const theme = useMantineTheme();
  const [selectedPath, setSelectedPath] = useState(sampleFiles[0]?.path ?? null);
  const selected = sampleFiles.find((file) => file.path === selectedPath) ?? sampleFiles[0];

  // Like the images table: columns drop by the card's width, so the file name keeps room.
  const { ref, width } = useElementSize();
  const showUses = width === 0 || width >= 440;
  const showFirst = width === 0 || width >= 340;

  const lastTime = samples.length > 0 ? samples[samples.length - 1].time : 0;
  // Samples after the song ends still get a place on the strip.
  const durationMs = Math.max(songDurationMs, lastTime + 1000, 1000);
  const color = parseThemeColor({ color: KIND_COLORS.storyboard, theme }).value;

  const url =
    beatmapFolderPath && selected?.exists
      ? buildBeatmapResourceUrl(beatmapFolderPath, selected.path)
      : null;

  return (
    <SectionCard
      title="Samples"
      info="Each mark is a sample the storyboard plays, taller the louder it is. Pick a file below to listen to it and see where it plays."
    >
      <Stack gap="md">
        <Box>
          <Box
            style={{
              height: STRIP_HEIGHT,
              borderRadius: 'var(--mantine-radius-sm)',
              background: 'var(--mantine-color-body)',
            }}
          >
            <svg width="100%" height={STRIP_HEIGHT} style={{ display: 'block' }}>
              {samples.map((sample, index) => {
                const isSelected = sample.path.toLowerCase() === selected?.path.toLowerCase();
                const height = Math.max(6, (Math.min(100, sample.volume) / 100) * STRIP_HEIGHT);
                return (
                  <rect
                    key={index}
                    x={`${(sample.time / durationMs) * 100}%`}
                    y={(STRIP_HEIGHT - height) / 2}
                    width={2}
                    height={height}
                    fill={isSelected ? color : alpha(color, 0.3)}
                    onClick={() => setSelectedPath(sample.path)}
                    style={{ cursor: 'pointer' }}
                  >
                    <title>
                      {`${linkTimestamp(sample.timestamp)} · ${sample.path} · ${sample.volume}%`}
                    </title>
                  </rect>
                );
              })}
            </svg>
          </Box>
          <TimeAxis durationMs={durationMs} />
        </Box>

        <Box ref={ref}>
          <AppTable verticalSpacing="xs">
            <Table.Thead>
              <Table.Tr>
                <Table.Th className="mv-table-left">File</Table.Th>
                {showUses && <Table.Th>Uses</Table.Th>}
                {showFirst && <Table.Th>First</Table.Th>}
                <Table.Th>Size</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {sampleFiles.map((file) => {
                const isSelected = file.path === selected?.path;
                const first = linkTimestamp(file.firstTimestamp);
                return (
                  <Table.Tr
                    key={file.path}
                    onClick={() => setSelectedPath(file.path)}
                    bg={isSelected ? 'var(--mantine-color-default-hover)' : undefined}
                    style={{ cursor: 'pointer' }}
                  >
                    <Table.Td className="mv-table-left" style={{ maxWidth: 0, width: '100%' }}>
                      <UnstyledButton
                        onClick={(event) => {
                          event.stopPropagation();
                          setSelectedPath(file.path);
                        }}
                        aria-pressed={isSelected}
                        style={{ maxWidth: '100%' }}
                      >
                        <Text size="sm" fw={isSelected ? 600 : 400} truncate title={file.path}>
                          {file.path}
                        </Text>
                      </UnstyledButton>
                    </Table.Td>
                    {showUses && <Table.Td>{file.useCount.toLocaleString()}</Table.Td>}
                    {showFirst && (
                      <Table.Td onClick={(event) => event.stopPropagation()}>
                        {first && !first.startsWith('-') && (
                          <TimestampLink displayTimestamp={first} />
                        )}
                      </Table.Td>
                    )}
                    <Table.Td>
                      {file.exists ? (
                        file.fileSizeFormatted
                      ) : (
                        <Text span size="sm" c="orange">
                          Missing
                        </Text>
                      )}
                    </Table.Td>
                  </Table.Tr>
                );
              })}
            </Table.Tbody>
          </AppTable>
        </Box>

        {selected &&
          (url ? (
            <AudioWaveformPlayer key={url} url={url} color={KIND_COLORS.storyboard} />
          ) : (
            <Text size="sm" c="dimmed">
              {selected.path} isn&apos;t in the mapset folder, so it can&apos;t be played.
            </Text>
          ))}
      </Stack>
    </SectionCard>
  );
}

export default StoryboardSamples;

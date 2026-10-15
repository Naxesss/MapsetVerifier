import {
  Text,
  Badge,
  Group,
  Paper,
  useMantineTheme,
  Stack,
  SimpleGrid,
  Box,
  Code,
} from '@mantine/core';
import TagsDiffDisplay from './TagsDiffDisplay.tsx';
import { DifficultyMetadata } from '../../../Types';
import { countWord } from '../../../utils/countWord';
import { getModeAccentColor } from '../../../utils/gameMode.ts';
import { InfoIconTooltip } from '../../common/InfoIconTooltip.tsx';
import GameModeIcon from '../../icons/GameModeIcon.tsx';
import type { ReactNode } from 'react';

interface MetadataInfoProps {
  difficulties: DifficultyMetadata[];
}

/** Two-column grid: version badges align; values share a fixed gap from the badge column. */
function MetadataDifficultyGrid({
  difficulties,
  children,
}: {
  difficulties: DifficultyMetadata[];
  children: (d: DifficultyMetadata) => ReactNode;
}) {
  return (
    <Box
      style={{
        display: 'grid',
        gridTemplateColumns: 'max-content 1fr',
        columnGap: 'var(--mantine-spacing-md)',
        rowGap: 'var(--mantine-spacing-xs)',
        alignItems: 'start',
      }}
    >
      {difficulties.map((d, idx) => [
        <Badge key={`${idx}-badge`} size="xs" variant="light">
          {d.version}
        </Badge>,
        <Box key={`${idx}-value`}>{children(d)}</Box>,
      ])}
    </Box>
  );
}

function MetadataInfo({ difficulties }: MetadataInfoProps) {
  const theme = useMantineTheme();

  if (difficulties.length === 0) {
    return null;
  }

  // Check if all difficulties have the same value for a field
  const allSame = (field: keyof DifficultyMetadata) => {
    const first = difficulties[0][field];
    return difficulties.every((d) => d[field] === first);
  };

  const first = difficulties[0];
  const hasUnicodeArtist = first.artist !== first.artistUnicode;
  const hasUnicodeTitle = first.title !== first.titleUnicode;

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
      <Group justify="space-between" mb="md">
        <Group gap="xs">
          <Text fw={600}>Metadata</Text>
          <InfoIconTooltip
            label="Beatmap metadata information across all difficulties"
            multiline
            w={250}
          />
        </Group>
        <Badge color="blue" variant="light">
          {countWord(difficulties.length, 'difficulty')}
        </Badge>
      </Group>

      <Stack gap="md">
        {/* Artist */}
        <Box>
          <Text size="xs" c="dimmed" mb={4}>
            Artist
          </Text>
          {allSame('artist') ? (
            <Stack gap={2}>
              <Text fw={500}>{first.artist}</Text>
              {hasUnicodeArtist && (
                <Text size="sm" c="dimmed">
                  {first.artistUnicode}
                </Text>
              )}
            </Stack>
          ) : (
            <MetadataDifficultyGrid difficulties={difficulties}>
              {(d) => (
                <Stack gap={2}>
                  <Text size="sm">{d.artist}</Text>
                  {d.artist !== d.artistUnicode && (
                    <Text size="xs" c="dimmed">
                      {d.artistUnicode}
                    </Text>
                  )}
                </Stack>
              )}
            </MetadataDifficultyGrid>
          )}
        </Box>

        {/* Title */}
        <Box>
          <Text size="xs" c="dimmed" mb={4}>
            Title
          </Text>
          {allSame('title') ? (
            <Stack gap={2}>
              <Text fw={500}>{first.title}</Text>
              {hasUnicodeTitle && (
                <Text size="sm" c="dimmed">
                  {first.titleUnicode}
                </Text>
              )}
            </Stack>
          ) : (
            <MetadataDifficultyGrid difficulties={difficulties}>
              {(d) => (
                <Stack gap={2}>
                  <Text size="sm">{d.title}</Text>
                  {d.title !== d.titleUnicode && (
                    <Text size="xs" c="dimmed">
                      {d.titleUnicode}
                    </Text>
                  )}
                </Stack>
              )}
            </MetadataDifficultyGrid>
          )}
        </Box>

        <SimpleGrid cols={2}>
          {/* Creator */}
          <Box>
            <Text size="xs" c="dimmed" mb={4}>
              Creator
            </Text>
            {allSame('creator') ? (
              <Text fw={500}>{first.creator}</Text>
            ) : (
              <MetadataDifficultyGrid difficulties={difficulties}>
                {(d) => <Text size="sm">{d.creator}</Text>}
              </MetadataDifficultyGrid>
            )}
          </Box>

          {/* Source */}
          <Box>
            <Text size="xs" c="dimmed" mb={4}>
              Source
            </Text>
            {allSame('source') ? (
              <Text fw={500}>
                {first.source ? (
                  <Text size="sm">{first.source}</Text>
                ) : (
                  <Text size="xs" fs="italic">
                    none
                  </Text>
                )}
              </Text>
            ) : (
              <MetadataDifficultyGrid difficulties={difficulties}>
                {(d) =>
                  d.source ? (
                    <Text size="sm">{d.source}</Text>
                  ) : (
                    <Text size="xs" c="dimmed">
                      (none)
                    </Text>
                  )
                }
              </MetadataDifficultyGrid>
            )}
          </Box>
        </SimpleGrid>

        {/* Tags */}
        <Box>
          <Text size="xs" c="dimmed" mb={4}>
            Tags
          </Text>
          {allSame('tags') ? (
            <Code block fz="sm" style={{ wordBreak: 'break-word', whiteSpace: 'pre-wrap' }}>
              {first.tags || '(none)'}
            </Code>
          ) : (
            <TagsDiffDisplay difficulties={difficulties} />
          )}
        </Box>

        {/* IDs */}
        <SimpleGrid cols={2}>
          <Box>
            <Text size="xs" c="dimmed" mb={4}>
              Beatmapset ID
            </Text>
            <Text fw={500}>{first.beatmapSetId ?? 'Not submitted'}</Text>
          </Box>
          <Box>
            <Text size="xs" c="dimmed" mb={4}>
              Modes
            </Text>
            <Group gap="xs">
              {[...new Set(difficulties.map((d) => d.mode))].map((mode) => (
                <GameModeIcon key={mode} mode={mode} size={16} color={getModeAccentColor(mode)} />
              ))}
            </Group>
          </Box>
        </SimpleGrid>
      </Stack>
    </Paper>
  );
}

export default MetadataInfo;

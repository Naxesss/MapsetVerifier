import { Text, Badge, Group, Stack, SimpleGrid, Box, Code } from '@mantine/core';
import TagsDiffDisplay from './TagsDiffDisplay.tsx';
import { DifficultyMetadata } from '../../../Types';
import { countWord } from '../../../utils/countWord';
import { getModeAccentColor } from '../../../utils/gameMode.ts';
import SectionCard from '../../common/SectionCard.tsx';
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
        <Badge key={`${idx}-badge`}>{d.version}</Badge>,
        <Box key={`${idx}-value`}>{children(d)}</Box>,
      ])}
    </Box>
  );
}

function MetadataInfo({ difficulties }: MetadataInfoProps) {
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
    <SectionCard
      title="Metadata"
      info="Artist, title, source and tags, compared across all difficulties."
      actions={<Badge color="blue">{countWord(difficulties.length, 'difficulty')}</Badge>}
    >
      <Stack gap="md">
        {/* Artist */}
        <Box>
          <Text size="xs" c="dimmed" mb="xs">
            Artist
          </Text>
          {allSame('artist') ? (
            <Stack gap="2xs">
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
                <Stack gap="2xs">
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
          <Text size="xs" c="dimmed" mb="xs">
            Title
          </Text>
          {allSame('title') ? (
            <Stack gap="2xs">
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
                <Stack gap="2xs">
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
            <Text size="xs" c="dimmed" mb="xs">
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
            <Text size="xs" c="dimmed" mb="xs">
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
          <Text size="xs" c="dimmed" mb="xs">
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
            <Text size="xs" c="dimmed" mb="xs">
              Mapset ID
            </Text>
            <Text fw={500}>{first.beatmapSetId ?? 'Not submitted'}</Text>
          </Box>
          <Box>
            <Text size="xs" c="dimmed" mb="xs">
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
    </SectionCard>
  );
}

export default MetadataInfo;

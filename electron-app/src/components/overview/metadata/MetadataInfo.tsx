import { Text, Badge, Group, Stack, SimpleGrid, Box } from '@mantine/core';
import TagChips from './TagChips.tsx';
import TagsDiffDisplay from './TagsDiffDisplay.tsx';
import { DifficultyMetadata } from '../../../Types';
import { countWord } from '../../../utils/countWord';
import { formatGameModeLabel, getModeAccentColor } from '../../../utils/gameMode.ts';
import SectionCard from '../../common/SectionCard.tsx';
import { StatField } from '../../common/StatField.tsx';
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

/** A name and, when it differs, its Unicode spelling below it. */
function NameWithUnicode({ name, unicode }: { name: string; unicode?: string | null }) {
  return (
    <Stack gap="2xs">
      <Text size="sm">{name}</Text>
      {unicode && unicode !== name && (
        <Text size="xs" c="dimmed">
          {unicode}
        </Text>
      )}
    </Stack>
  );
}

/** An empty optional field. */
function EmptyValue({ size }: { size?: 'sm' }) {
  return (
    <Text size={size} c="dimmed">
      None
    </Text>
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
        <StatField
          label="Artist"
          value={
            allSame('artist') ? (
              first.artist
            ) : (
              <MetadataDifficultyGrid difficulties={difficulties}>
                {(d) => <NameWithUnicode name={d.artist} unicode={d.artistUnicode} />}
              </MetadataDifficultyGrid>
            )
          }
          note={allSame('artist') && hasUnicodeArtist ? first.artistUnicode : undefined}
        />

        <StatField
          label="Title"
          value={
            allSame('title') ? (
              first.title
            ) : (
              <MetadataDifficultyGrid difficulties={difficulties}>
                {(d) => <NameWithUnicode name={d.title} unicode={d.titleUnicode} />}
              </MetadataDifficultyGrid>
            )
          }
          note={allSame('title') && hasUnicodeTitle ? first.titleUnicode : undefined}
        />

        <SimpleGrid cols={2}>
          <StatField
            label="Creator"
            value={
              allSame('creator') ? (
                first.creator
              ) : (
                <MetadataDifficultyGrid difficulties={difficulties}>
                  {(d) => <Text size="sm">{d.creator}</Text>}
                </MetadataDifficultyGrid>
              )
            }
          />
          <StatField
            label="Source"
            value={
              allSame('source') ? (
                (first.source ?? '') || <EmptyValue />
              ) : (
                <MetadataDifficultyGrid difficulties={difficulties}>
                  {(d) => (d.source ? <Text size="sm">{d.source}</Text> : <EmptyValue size="sm" />)}
                </MetadataDifficultyGrid>
              )
            }
          />
        </SimpleGrid>

        <StatField
          label="Tags"
          value={
            allSame('tags') ? (
              <TagChips tags={first.tags} />
            ) : (
              <TagsDiffDisplay difficulties={difficulties} />
            )
          }
        />

        <SimpleGrid cols={2}>
          <StatField label="Mapset ID" value={first.beatmapSetId ?? 'Not submitted'} />
          <StatField
            label="Modes"
            value={
              <Group gap="md">
                {[...new Set(difficulties.map((d) => d.mode))].map((mode) => (
                  <Group key={mode} gap={6} wrap="nowrap">
                    <GameModeIcon mode={mode} size={16} color={getModeAccentColor(mode)} />
                    <Text fw={600}>{formatGameModeLabel(mode)}</Text>
                  </Group>
                ))}
              </Group>
            }
          />
        </SimpleGrid>
      </Stack>
    </SectionCard>
  );
}

export default MetadataInfo;

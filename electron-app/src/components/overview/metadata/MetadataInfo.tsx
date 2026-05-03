import {
  Text,
  Badge,
  Group,
  Paper,
  useMantineTheme,
  Stack,
  SimpleGrid,
  Tooltip,
  Accordion,
  Box,
  Code,
} from '@mantine/core';
import { IconInfoCircle } from '@tabler/icons-react';
import { DifficultyMetadata } from '../../../Types';
import { countWord } from '../../../utils/countWord';
import GameModeIcon from '../../icons/GameModeIcon.tsx';

interface MetadataInfoProps {
  difficulties: DifficultyMetadata[];
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
          <Tooltip label="Beatmap metadata information across all difficulties" multiline w={250}>
            <IconInfoCircle size={16} style={{ color: theme.colors.gray[6], cursor: 'help' }} />
          </Tooltip>
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
            <Accordion variant="contained" radius="sm">
              {difficulties.map((d, idx) => (
                <Accordion.Item key={idx} value={d.version}>
                  <Accordion.Control>
                    <Text size="sm">{d.version}</Text>
                  </Accordion.Control>
                  <Accordion.Panel>
                    <Text>{d.artist}</Text>
                    {d.artist !== d.artistUnicode && (
                      <Text size="sm" c="dimmed">
                        {d.artistUnicode}
                      </Text>
                    )}
                  </Accordion.Panel>
                </Accordion.Item>
              ))}
            </Accordion>
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
            <Accordion variant="contained" radius="sm">
              {difficulties.map((d, idx) => (
                <Accordion.Item key={idx} value={d.version}>
                  <Accordion.Control>
                    <Text size="sm">{d.version}</Text>
                  </Accordion.Control>
                  <Accordion.Panel>
                    <Text>{d.title}</Text>
                    {d.title !== d.titleUnicode && (
                      <Text size="sm" c="dimmed">
                        {d.titleUnicode}
                      </Text>
                    )}
                  </Accordion.Panel>
                </Accordion.Item>
              ))}
            </Accordion>
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
              <Stack gap={2}>
                {difficulties.map((d, idx) => (
                  <Group key={idx} gap="xs">
                    <Badge size="xs" variant="light">
                      {d.version}
                    </Badge>
                    <Text size="sm">{d.creator}</Text>
                  </Group>
                ))}
              </Stack>
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
              <Stack gap={2}>
                {difficulties.map((d, idx) => (
                  <Group key={idx} gap="xs">
                    <Badge size="xs" variant="light">
                      {d.version}
                    </Badge>
                    {d.source ? (
                      <Text size="sm">{d.source}</Text>
                    ) : (
                      <Text size="xs" c="dimmed">
                        (none)
                      </Text>
                    )}
                  </Group>
                ))}
              </Stack>
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
            <Accordion variant="contained" radius="sm">
              {difficulties.map((d, idx) => (
                <Accordion.Item key={idx} value={d.version}>
                  <Accordion.Control>
                    <Text size="sm">{d.version}</Text>
                  </Accordion.Control>
                  <Accordion.Panel>
                    <Code block fz="sm" style={{ wordBreak: 'break-word', whiteSpace: 'pre-wrap' }}>
                      {d.tags || '(none)'}
                    </Code>
                  </Accordion.Panel>
                </Accordion.Item>
              ))}
            </Accordion>
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
                <GameModeIcon key={mode} mode={mode} size={16} />
              ))}
            </Group>
          </Box>
        </SimpleGrid>
      </Stack>
    </Paper>
  );
}

export default MetadataInfo;

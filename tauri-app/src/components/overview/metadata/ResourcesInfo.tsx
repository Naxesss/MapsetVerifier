import { Text, Badge, Group, Paper, useMantineTheme, Stack, Tooltip, Accordion, Box, Table, ThemeIcon } from '@mantine/core';
import { IconInfoCircle, IconPhoto, IconVideo, IconMusic, IconVolume, IconFolder, IconCheck, IconX } from '@tabler/icons-react';
import { ResourcesInfo as ResourcesInfoType } from '../../../Types';

interface ResourcesInfoProps {
  resources: ResourcesInfoType;
}

function ResourcesInfo({ resources }: ResourcesInfoProps) {
  const theme = useMantineTheme();

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
      <Group justify="space-between" mb="md">
        <Group gap="xs">
          <Text fw={600}>Resources</Text>
          <Tooltip label="Files and resources used by the beatmap set" multiline w={250}>
            <IconInfoCircle size={16} style={{ color: theme.colors.gray[6], cursor: 'help' }} />
          </Tooltip>
        </Group>
        <Badge color="blue" variant="light">
          <Group gap={4}>
            <IconFolder size={12} />
            {resources.totalFolderSizeFormatted}
          </Group>
        </Badge>
      </Group>

      <Stack gap="md">
        {/* Audio File */}
        {resources.audioFile && (
          <Box>
            <Group gap="xs" mb={4}>
              <IconMusic size={14} style={{ color: theme.colors.blue[4] }} />
              <Text size="xs" c="dimmed">Audio File</Text>
            </Group>
            <Group gap="md">
              <Text size="sm" fw={500}>{resources.audioFile.fileName}</Text>
              <Badge size="xs" variant="light">{resources.audioFile.format}</Badge>
              <Text size="xs" c="dimmed">{resources.audioFile.fileSizeFormatted}</Text>
              <Text size="xs" c="dimmed">{resources.audioFile.durationFormatted}</Text>
              <Text size="xs" c="dimmed">{resources.audioFile.averageBitrate} kbps</Text>
            </Group>
          </Box>
        )}

        {/* Backgrounds */}
        {resources.backgrounds.length > 0 && (
          <Box>
            <Group gap="xs" mb={4}>
              <IconPhoto size={14} style={{ color: theme.colors.green[4] }} />
              <Text size="xs" c="dimmed">Background{resources.backgrounds.length > 1 ? 's' : ''}</Text>
            </Group>
            <Stack gap="xs">
              {resources.backgrounds.map((bg, idx) => (
                <Group key={idx} gap="md">
                  <Text size="sm" fw={500}>{bg.fileName}</Text>
                  <Badge size="xs" variant="light">{bg.resolution}</Badge>
                  <Text size="xs" c="dimmed">{bg.fileSizeFormatted}</Text>
                  {bg.usedByDifficulties.length < 10 && (
                    <Text size="xs" c="dimmed">Used by: {bg.usedByDifficulties.join(', ')}</Text>
                  )}
                </Group>
              ))}
            </Stack>
          </Box>
        )}

        {/* Videos */}
        {resources.videos.length > 0 && (
          <Box>
            <Group gap="xs" mb={4}>
              <IconVideo size={14} style={{ color: theme.colors.violet[4] }} />
              <Text size="xs" c="dimmed">Video{resources.videos.length > 1 ? 's' : ''}</Text>
            </Group>
            <Stack gap="xs">
              {resources.videos.map((video, idx) => (
                <Group key={idx} gap="md">
                  <Text size="sm" fw={500}>{video.fileName}</Text>
                  <Badge size="xs" variant="light">{video.resolution}</Badge>
                  <Text size="xs" c="dimmed">{video.fileSizeFormatted}</Text>
                  <Text size="xs" c="dimmed">{video.durationFormatted}</Text>
                  <Text size="xs" c="dimmed">Offset: {video.offsetMs}ms</Text>
                </Group>
              ))}
            </Stack>
          </Box>
        )}

        {/* Storyboard */}
        <Box>
          <Group gap="xs" mb={4}>
            <Text size="xs" c="dimmed">Storyboard</Text>
          </Group>
          <Stack gap="xs">
            <Group gap="xs">
              <ThemeIcon size="xs" color={resources.storyboard.osbIsUsed ? 'green' : 'gray'} variant="light">
                {resources.storyboard.osbIsUsed ? <IconCheck size={10} /> : <IconX size={10} />}
              </ThemeIcon>
              <Text size="sm">.osb file: {resources.storyboard.osbIsUsed ? 'Used' : 'Not used'}</Text>
              {resources.storyboard.osbFileName && (
                <Text size="xs" c="dimmed">({resources.storyboard.osbFileName})</Text>
              )}
            </Group>
            {resources.storyboard.difficultySpecificStoryboards.some(d => d.hasStoryboard) && (
              <Box>
                <Text size="xs" c="dimmed" mb={2}>Difficulty-specific storyboards:</Text>
                {resources.storyboard.difficultySpecificStoryboards.filter(d => d.hasStoryboard).map((d, idx) => (
                  <Group key={idx} gap="xs">
                    <Badge size="xs" variant="light">{d.version}</Badge>
                    <Text size="xs">{d.spriteCount} sprites, {d.animationCount} animations, {d.sampleCount} samples</Text>
                  </Group>
                ))}
              </Box>
            )}
          </Stack>
        </Box>

        {/* Hit Sounds */}
        {resources.hitSounds.length > 0 && (
          <Box>
            <Group gap="xs" mb={4}>
              <IconVolume size={14} style={{ color: theme.colors.orange[4] }} />
              <Text size="xs" c="dimmed">Hit Sounds ({resources.hitSounds.length} files)</Text>
            </Group>
            <Accordion variant="contained" radius="sm">
              <Accordion.Item value="hitsounds">
                <Accordion.Control>
                  <Text size="sm">View hit sound usage</Text>
                </Accordion.Control>
                <Accordion.Panel>
                  <Table striped highlightOnHover withTableBorder>
                    <Table.Thead>
                      <Table.Tr>
                        <Table.Th>File</Table.Th>
                        <Table.Th>Format</Table.Th>
                        <Table.Th>Size</Table.Th>
                        <Table.Th>Duration</Table.Th>
                        <Table.Th>Uses</Table.Th>
                      </Table.Tr>
                    </Table.Thead>
                    <Table.Tbody>
                      {resources.hitSounds.slice(0, 20).map((hs, idx) => (
                        <Table.Tr key={idx}>
                          <Table.Td><Text size="xs">{hs.fileName}</Text></Table.Td>
                          <Table.Td><Badge size="xs" variant="light">{hs.format}</Badge></Table.Td>
                          <Table.Td><Text size="xs">{hs.fileSizeFormatted}</Text></Table.Td>
                          <Table.Td><Text size="xs">{hs.durationMs.toFixed(0)} ms</Text></Table.Td>
                          <Table.Td><Text size="xs">{hs.totalUsageCount}</Text></Table.Td>
                        </Table.Tr>
                      ))}
                    </Table.Tbody>
                  </Table>
                  {resources.hitSounds.length > 20 && (
                    <Text size="xs" c="dimmed" mt="xs">...and {resources.hitSounds.length - 20} more</Text>
                  )}
                </Accordion.Panel>
              </Accordion.Item>
            </Accordion>
          </Box>
        )}
      </Stack>
    </Paper>
  );
}

export default ResourcesInfo;


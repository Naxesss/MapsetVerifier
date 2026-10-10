import {
  Text,
  Badge,
  Group,
  useMantineTheme,
  Stack,
  Accordion,
  Box,
  Table,
  ThemeIcon,
} from '@mantine/core';
import {
  IconPhoto,
  IconVideo,
  IconMusic,
  IconVolume,
  IconFolder,
  IconCheck,
  IconX,
} from '@tabler/icons-react';
import { ResourcesInfo as ResourcesInfoType } from '../../../Types';
import { countWord } from '../../../utils/countWord';
import AppTable from '../../common/AppTable.tsx';
import SectionCard from '../../common/SectionCard.tsx';

interface ResourcesInfoProps {
  resources: ResourcesInfoType;
}

function ResourcesInfo({ resources }: ResourcesInfoProps) {
  const theme = useMantineTheme();

  return (
    <SectionCard
      title="Resources"
      info="Files the mapset uses, and whether they exist in its folder."
      actions={
        <Badge color="blue" leftSection={<IconFolder size={12} />}>
          {resources.totalFolderSizeFormatted}
        </Badge>
      }
    >
      <Stack gap="md">
        {/* Audio File */}
        {resources.audioFile && (
          <Box>
            <Group gap="xs" mb="xs">
              <IconMusic size={14} style={{ color: theme.colors.blue[4] }} />
              <Text size="xs" c="dimmed">
                Audio file
              </Text>
            </Group>
            <Group gap="md">
              <Text size="sm" fw={500}>
                {resources.audioFile.fileName}
              </Text>
              <Badge>{resources.audioFile.format}</Badge>
              <Text size="xs" c="dimmed">
                {resources.audioFile.fileSizeFormatted}
              </Text>
              <Text size="xs" c="dimmed">
                {resources.audioFile.durationFormatted}
              </Text>
              <Text size="xs" c="dimmed">
                {resources.audioFile.averageBitrate} kbps
              </Text>
            </Group>
          </Box>
        )}

        {/* Backgrounds */}
        {resources.backgrounds.length > 0 && (
          <Box>
            <Group gap="xs" mb="xs">
              <IconPhoto size={14} style={{ color: theme.colors.green[4] }} />
              <Text size="xs" c="dimmed">
                Background{resources.backgrounds.length > 1 ? 's' : ''}
              </Text>
            </Group>
            <Stack gap="xs">
              {resources.backgrounds.map((bg, idx) => (
                <Group key={idx} gap="md">
                  <Text size="sm" fw={500}>
                    {bg.fileName}
                  </Text>
                  <Badge>{bg.resolution}</Badge>
                  <Text size="xs" c="dimmed">
                    {bg.fileSizeFormatted}
                  </Text>
                  {bg.usedByDifficulties.length < 10 && (
                    <Text size="xs" c="dimmed">
                      Used by: {bg.usedByDifficulties.join(', ')}
                    </Text>
                  )}
                </Group>
              ))}
            </Stack>
          </Box>
        )}

        {/* Videos */}
        {resources.videos.length > 0 && (
          <Box>
            <Group gap="xs" mb="xs">
              <IconVideo size={14} style={{ color: theme.colors.violet[4] }} />
              <Text size="xs" c="dimmed">
                Video{resources.videos.length > 1 ? 's' : ''}
              </Text>
            </Group>
            <Stack gap="xs">
              {resources.videos.map((video, idx) => (
                <Group key={idx} gap="md">
                  <Text size="sm" fw={500}>
                    {video.fileName}
                  </Text>
                  <Badge>{video.resolution}</Badge>
                  <Text size="xs" c="dimmed">
                    {video.fileSizeFormatted}
                  </Text>
                  <Text size="xs" c="dimmed">
                    {video.durationFormatted}
                  </Text>
                  {video.codec && (
                    <Text size="xs" c="dimmed">
                      {video.codec}
                    </Text>
                  )}
                  {video.frameRate && (
                    <Text size="xs" c="dimmed">
                      {Number(video.frameRate.toFixed(3))} FPS
                    </Text>
                  )}
                  <Text size="xs" c="dimmed">
                    Offset: {video.offsetMs} ms
                  </Text>
                  {video.hasAudioTrack && <Badge color="gray">Audio track</Badge>}
                </Group>
              ))}
            </Stack>
          </Box>
        )}

        {/* Storyboard */}
        <Box>
          <Group gap="xs" mb="xs">
            <Text size="xs" c="dimmed">
              Storyboard
            </Text>
          </Group>
          <Stack gap="xs">
            <Group gap="xs">
              <ThemeIcon
                size="xs"
                color={resources.storyboard.osbIsUsed ? 'green' : 'gray'}
                variant="light"
              >
                {resources.storyboard.osbIsUsed ? <IconCheck size={10} /> : <IconX size={10} />}
              </ThemeIcon>
              <Text size="sm">
                .osb file: {resources.storyboard.osbIsUsed ? 'Used' : 'Not used'}
              </Text>
              {resources.storyboard.osbFileName && (
                <Text size="xs" c="dimmed">
                  ({resources.storyboard.osbFileName})
                </Text>
              )}
            </Group>
            {resources.storyboard.difficultySpecificStoryboards.some((d) => d.hasStoryboard) && (
              <Box>
                <Text size="xs" c="dimmed" mb="2xs">
                  Difficulty-specific storyboards:
                </Text>
                {resources.storyboard.difficultySpecificStoryboards
                  .filter((d) => d.hasStoryboard)
                  .map((d, idx) => (
                    <Group key={idx} gap="xs">
                      <Badge>{d.version}</Badge>
                      <Text size="xs">
                        {d.spriteCount} sprites, {d.animationCount} animations, {d.sampleCount}{' '}
                        samples
                      </Text>
                    </Group>
                  ))}
              </Box>
            )}
          </Stack>
        </Box>

        {/* Hit Sounds */}
        {resources.hitSounds.length > 0 && (
          <Box>
            <Group gap="xs" mb="xs">
              <IconVolume size={14} style={{ color: theme.colors.orange[4] }} />
              <Text size="xs" c="dimmed">
                Hit Sounds ({countWord(resources.hitSounds.length, 'file')})
              </Text>
            </Group>
            <Accordion variant="contained" radius="sm">
              <Accordion.Item value="hitsounds">
                <Accordion.Control>
                  <Text size="sm">View hit sound usage</Text>
                </Accordion.Control>
                <Accordion.Panel>
                  <AppTable>
                    <Table.Thead>
                      <Table.Tr>
                        <Table.Th className="mv-table-left">File</Table.Th>
                        <Table.Th>Format</Table.Th>
                        <Table.Th>Size</Table.Th>
                        <Table.Th>Duration</Table.Th>
                        <Table.Th>Uses</Table.Th>
                      </Table.Tr>
                    </Table.Thead>
                    <Table.Tbody>
                      {resources.hitSounds.slice(0, 20).map((hs, idx) => (
                        <Table.Tr key={idx}>
                          <Table.Td className="mv-table-left">
                            <Text size="xs">{hs.fileName}</Text>
                          </Table.Td>
                          <Table.Td>
                            <Badge>{hs.format}</Badge>
                          </Table.Td>
                          <Table.Td>
                            <Text size="xs">{hs.fileSizeFormatted}</Text>
                          </Table.Td>
                          <Table.Td>
                            <Text size="xs">{hs.durationMs.toFixed(0)} ms</Text>
                          </Table.Td>
                          <Table.Td>
                            <Text size="xs">{hs.totalUsageCount}</Text>
                          </Table.Td>
                        </Table.Tr>
                      ))}
                    </Table.Tbody>
                  </AppTable>
                  {resources.hitSounds.length > 20 && (
                    <Text size="xs" c="dimmed" mt="xs">
                      ...and {resources.hitSounds.length - 20} more
                    </Text>
                  )}
                </Accordion.Panel>
              </Accordion.Item>
            </Accordion>
          </Box>
        )}
      </Stack>
    </SectionCard>
  );
}

export default ResourcesInfo;

import { Badge, Group, Stack, Text } from '@mantine/core';
import BackgroundDetail from './BackgroundDetail.tsx';
import HitSoundDetail from './HitSoundDetail.tsx';
import { KIND_LABELS, type ResourceFile } from './resourceFiles.ts';
import StoryboardDetail from './StoryboardDetail.tsx';
import SectionCard from '../../common/SectionCard.tsx';
import AudioOverview from '../audio/AudioOverview.tsx';
import VideoOverview from '../video/VideoOverview.tsx';

interface FileDetailsProps {
  file: ResourceFile;
  beatmapFolderPath: string | undefined;
}

function extensionOf(fileName: string) {
  const dot = fileName.lastIndexOf('.');
  return dot >= 0 ? fileName.slice(dot + 1).toUpperCase() : '';
}

/** The picked file: its name and kind, then what there is to see for that kind of file. */
function FileDetails({ file, beatmapFolderPath }: FileDetailsProps) {
  const extension = extensionOf(file.name);
  // The song and video open on a Format card that already names the file.
  const showHeader = file.kind !== 'audio' && file.kind !== 'video';

  return (
    <Stack gap="md">
      {showHeader && (
        <SectionCard padding="md">
          <Group justify="space-between" wrap="nowrap" gap="sm">
            <Stack gap="2xs" style={{ minWidth: 0 }}>
              <Text fw={700} size="lg" truncate>
                {file.name}
              </Text>
              <Text size="sm" c="dimmed">
                {KIND_LABELS[file.kind]}
                {file.size && ` · ${file.size}`}
              </Text>
            </Stack>
            {extension && <Badge style={{ flexShrink: 0 }}>{extension}</Badge>}
          </Group>
        </SectionCard>
      )}

      {file.kind === 'audio' && <AudioOverview />}
      {file.kind === 'video' && <VideoOverview fileName={file.video.fileName} />}
      {file.kind === 'background' && (
        <BackgroundDetail background={file.background} beatmapFolderPath={beatmapFolderPath} />
      )}
      {file.kind === 'storyboard' && <StoryboardDetail storyboard={file.storyboard} />}
      {file.kind === 'hitSound' && (
        <HitSoundDetail hitSound={file.hitSound} beatmapFolderPath={beatmapFolderPath} />
      )}
    </Stack>
  );
}

export default FileDetails;

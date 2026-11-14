import { Image, SimpleGrid, Stack } from '@mantine/core';
import { IconPhotoOff } from '@tabler/icons-react';
import { useState } from 'react';
import { buildBeatmapResourceUrl } from '../../../utils/buildBeatmapFolderPath.ts';
import EmptyState from '../../common/EmptyState.tsx';
import SectionCard from '../../common/SectionCard.tsx';
import { StatField } from '../../common/StatField.tsx';
import UsedByField from '../UsedByField.tsx';
import type { BackgroundInfo } from '../../../Types';

interface BackgroundDetailProps {
  background: BackgroundInfo;
  beatmapFolderPath: string | undefined;
}

/** A background image as it is, with its size and the difficulties that show it. */
function BackgroundDetail({ background, beatmapFolderPath }: BackgroundDetailProps) {
  const [failed, setFailed] = useState(false);

  return (
    <SimpleGrid cols={{ base: 1, lg: 2 }} spacing="md">
      <SectionCard title="Preview">
        {failed || !beatmapFolderPath ? (
          <EmptyState
            icon={IconPhotoOff}
            title="Can't show this image"
            description="The file is missing or isn't an image this app can show."
          />
        ) : (
          <Image
            src={buildBeatmapResourceUrl(beatmapFolderPath, background.fileName)}
            alt={background.fileName}
            radius="sm"
            mah={360}
            fit="contain"
            bg="dark.8"
            onError={() => setFailed(true)}
          />
        )}
      </SectionCard>
      <SectionCard title="Details">
        <Stack gap="md">
          <SimpleGrid cols={2} spacing="md">
            <StatField
              label="Resolution"
              value={background.width > 0 ? background.resolution : 'Unknown'}
            />
            <StatField label="File size" value={background.fileSizeFormatted || 'Unknown'} />
          </SimpleGrid>
          <UsedByField difficulties={background.usedByDifficulties} />
        </Stack>
      </SectionCard>
    </SimpleGrid>
  );
}

export default BackgroundDetail;

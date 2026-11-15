import { Image } from '@mantine/core';
import { IconPhotoOff } from '@tabler/icons-react';
import { useState } from 'react';
import FileCard, { WideStat } from './FileCard.tsx';
import { buildBeatmapResourceUrl } from '../../../utils/buildBeatmapFolderPath.ts';
import EmptyState from '../../common/EmptyState.tsx';
import { StatField } from '../../common/StatField.tsx';
import UsedByField from '../UsedByField.tsx';
import type { BackgroundInfo } from '../../../Types';

interface BackgroundDetailProps {
  background: BackgroundInfo;
  beatmapFolderPath: string | undefined;
}

/** A background image as it is, with its resolution and the difficulties that show it. */
function BackgroundDetail({ background, beatmapFolderPath }: BackgroundDetailProps) {
  const [failed, setFailed] = useState(false);

  return (
    <FileCard
      title="Image"
      stats={
        <>
          <StatField
            label="Resolution"
            value={background.width > 0 ? background.resolution : 'Unknown'}
          />
          <WideStat>
            <UsedByField difficulties={background.usedByDifficulties} />
          </WideStat>
        </>
      }
    >
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
          mah={420}
          fit="contain"
          bg="dark.8"
          onError={() => setFailed(true)}
        />
      )}
    </FileCard>
  );
}

export default BackgroundDetail;

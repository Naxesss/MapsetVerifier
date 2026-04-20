import { Skeleton } from '@mantine/core';
import BeatmapCard from './BeatmapCard.tsx';

function PlaceholderBeatmapCard() {
  return (
    <Skeleton>
      <BeatmapCard
        key="placeholder"
        beatmap={{
          folder: 'placeholder',
          title: '',
          artist: '',
          creator: '',
          beatmapID: '',
          beatmapSetID: '',
        }}
      />
    </Skeleton>
  );
}

export default PlaceholderBeatmapCard;

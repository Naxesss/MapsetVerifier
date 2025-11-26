import { Skeleton } from '@mantine/core';
import BeatmapCard from './BeatmapCard.tsx';

export default function PlaceholderBeatmapCard() {
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

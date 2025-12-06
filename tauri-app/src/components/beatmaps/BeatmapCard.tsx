import { Flex, Text } from '@mantine/core';
import { useEffect, useState } from 'react';
import { useBeatmap } from '../../context/BeatmapContext';
import { Beatmap } from '../../Types.ts';

function BeatmapCard({ beatmap }: { beatmap: Beatmap }) {
  const { selectedFolder, setSelectedFolder } = useBeatmap();
  const [bgUrl, setBgUrl] = useState<string | undefined>(undefined);

  useEffect(() => {
    if (!beatmap.folder) {
      setBgUrl(undefined);
      return;
    }

    const candidate = `http://localhost:5005/beatmap/image?folder=${beatmap.folder}`;
    let cancelled = false;
    const img = new Image();

    img.onload = () => {
      if (!cancelled) setBgUrl(candidate);
    };

    img.onerror = () => {
      if (!cancelled) setBgUrl(undefined);
    };

    img.src = candidate;

    return () => {
      cancelled = true;
    };
  }, [beatmap.folder]);

  const bgStyle = bgUrl ? { backgroundImage: `url('${bgUrl}')` } : { backgroundImage: 'none' };
  const isSelected = selectedFolder === beatmap.folder;

  return (
    <Flex
      className="mapset-container"
      h={96}
      key={beatmap.folder}
      style={{
        cursor: 'pointer',
        outline: isSelected ? '2px solid var(--mantine-color-blue-6)' : 'none',
        outlineOffset: '-2px',
      }}
      onClick={() => setSelectedFolder(beatmap.folder)}
    >
      <div className="mapset-bg" style={bgStyle} />
      <div className="mapset-bg-overlay" />
      <div className="mapset-text">
        <div className="mapset-title">
          <Text fw="700">{beatmap.artist}</Text>
          <Text fw="700">{beatmap.title}</Text>
        </div>
        <div className="mapset-creator">
          <Text fs="italic">Mapped by {beatmap.creator}</Text>
        </div>
      </div>
    </Flex>
  );
}

export default BeatmapCard;

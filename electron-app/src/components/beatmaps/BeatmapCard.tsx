import { Box, Flex, Text } from '@mantine/core';
import { useEffect, useState } from 'react';
import { useLocation, useNavigate } from "react-router-dom";
import {BACKEND_BASE_URL} from "../../Constants.ts";
import { useBeatmap } from '../../context/BeatmapContext';
import { Beatmap } from '../../Types.ts';

interface BeatmapCardProps {
  beatmap: Beatmap;
}

function BeatmapCard({ beatmap }: BeatmapCardProps) {
  const location = useLocation();
  const navigate = useNavigate();
  const { selectedFolder, setSelectedFolder } = useBeatmap();
  const [bgUrl, setBgUrl] = useState<string | undefined>(undefined);
  const [isHovered, setIsHovered] = useState(false);

  useEffect(() => {
    if (!beatmap.folder || beatmap.folder === "placeholder") {
      setBgUrl(undefined);
      return;
    }

    const candidate = `${BACKEND_BASE_URL}/beatmap/image?folder=${encodeURIComponent(beatmap.folder)}`;
    let cancelled = false;
    const img = new Image();

    img.onload = () => {
      if (!cancelled) setBgUrl(candidate);
    };

    img.onerror = () => {
      console.error("Could not load image: " + candidate);
      if (!cancelled) setBgUrl(undefined);
    };

    img.src = candidate;

    return () => {
      cancelled = true;
    };
  }, [beatmap.folder]);

  const isSelected = selectedFolder === beatmap.folder;

  const textStyle = {
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
    display: 'block',
    maxWidth: '100%',
  } as const;

  return (
    <Flex
      h={96}
      style={{
        justifyContent: 'center',
        alignItems: 'center',
        borderRadius: 'var(--mantine-radius-md)',
        position: 'relative',
        overflow: 'hidden',
        cursor: 'pointer',
        border: isSelected || isHovered ? '1px solid var(--mantine-color-blue-6)' : '1px solid var(--mantine-color-dark-4)',
      }}
      onClick={() => {
        setSelectedFolder(beatmap.folder)
        // No page open that uses a beatmap, redirect to checks page as default
        if (location.pathname !== '/checks' && location.pathname !== '/snapshots' && location.pathname !== '/overview') {
          navigate('/checks');
        }
      }}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      {/* Background image */}
      <Box
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          width: '100%',
          height: '100%',
          backgroundSize: 'cover',
          backgroundPosition: 'center',
          backgroundRepeat: 'no-repeat',
          borderRadius: 'var(--mantine-radius-md)',
          zIndex: 0,
          backgroundImage: bgUrl ? `url('${bgUrl}')` : 'none',
        }}
      />
      {/* Dark overlay */}
      <Box
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          width: '100%',
          height: '100%',
          background: isSelected || isHovered ? 'rgba(0, 0, 0, 0.4)' : 'rgba(0, 0, 0, 0.6)',
          borderRadius: 'var(--mantine-radius-md)',
          zIndex: 1,
          pointerEvents: 'none',
        }}
      />
      {/* Text content */}
      <Flex
        direction="column"
        gap={0}
        p="xs"
        style={{
          position: 'relative',
          zIndex: 2,
          overflow: 'hidden',
          textAlign: 'center',
        }}
      >
        <Text style={textStyle}>
          {beatmap.artist}
        </Text>
        <Text style={textStyle}>
          {beatmap.title}
        </Text>
        <Text
          fs="italic"
          size="xs"
          c="dimmed"
          style={textStyle}>
          Mapped by {beatmap.creator}
        </Text>
      </Flex>
    </Flex>
  );
}

export default BeatmapCard;

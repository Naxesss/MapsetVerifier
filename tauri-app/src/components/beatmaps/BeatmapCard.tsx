import { Box, Flex, Text } from '@mantine/core';
import { useEffect, useState } from 'react';
import { useLocation, useNavigate } from "react-router-dom";
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
        border: isSelected ? '2px solid var(--mantine-color-blue-6)' : '2px solid transparent'
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
          background: isHovered ? 'rgba(0, 0, 0, 0.4)' : 'rgba(0, 0, 0, 0.6)',
          borderRadius: 'var(--mantine-radius-md)',
          zIndex: 1,
          pointerEvents: 'none',
          transition: 'background 0.3s',
        }}
      />
      {/* Text content */}
      <Flex
        direction="column"
        gap={4}
        style={{
          padding: 'var(--mantine-spacing-md)',
          position: 'relative',
          zIndex: 2,
          overflow: 'hidden',
          textOverflow: 'ellipsis',
          color: 'var(--mantine-color-text)',
          textAlign: 'center',
          width: '100%',
          maxWidth: '100%',
        }}
      >
        <Flex
          direction="column"
          gap={2}
          style={{
            fontWeight: 'bold',
            fontSize: 12,
            overflow: 'hidden',
            maxWidth: '100%',
          }}
        >
          <Text fw={700} style={textStyle}>
            {beatmap.artist}
          </Text>
          <Text fw={700} style={textStyle}>
            {beatmap.title}
          </Text>
        </Flex>
        <Box style={{ fontSize: 10, overflow: 'hidden', maxWidth: '100%' }}>
          <Text fs="italic" style={textStyle}>
            Mapped by {beatmap.creator}
          </Text>
        </Box>
      </Flex>
    </Flex>
  );
}

export default BeatmapCard;

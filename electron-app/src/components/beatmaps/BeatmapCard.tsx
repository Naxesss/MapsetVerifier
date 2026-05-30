import { Box, Flex, Stack, Text } from '@mantine/core';
import { useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useBeatmap } from '../../context/BeatmapContext';
import { useBeatmapReparse } from '../../context/BeatmapReparseRegistry.tsx';
import { Beatmap } from '../../Types.ts';
import { buildBeatmapImageUrl } from '../../utils/buildBeatmapFolderPath.ts';

interface BeatmapCardProps {
  beatmap: Beatmap;
  songFolder?: string;
  onSelect?: () => void;
  isSelectedOverride?: boolean;
}

function BeatmapCard({ beatmap, songFolder, onSelect, isSelectedOverride }: BeatmapCardProps) {
  const location = useLocation();
  const navigate = useNavigate();
  const { selectedFolder, setSelectedFolder } = useBeatmap();
  const [bgUrl, setBgUrl] = useState<string | undefined>(undefined);
  const [isHovered, setIsHovered] = useState(false);
  const { triggerReparse } = useBeatmapReparse();

  useEffect(() => {
    if (!beatmap.folder || beatmap.folder === 'placeholder') {
      setBgUrl(undefined);
      return;
    }

    const candidate = buildBeatmapImageUrl(beatmap.folder, { songFolder });
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
  }, [beatmap.folder, songFolder]);

  const isSelected = isSelectedOverride ?? selectedFolder === beatmap.folder;

  const transitionMs = '0.22s ease';
  const active = isSelected || isHovered;

  const textStyle = {
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
    display: 'block',
    maxWidth: '100%',
    opacity: 0.9,
    textShadow:
      '0 1px 2px rgba(0, 0, 0, 0.62), 0 0 8px rgba(0, 0, 0, 0.28), 0 0 1px rgba(0, 0, 0, 0.55)',
  } as const;

  const artistTitleStyle = { ...textStyle, lineHeight: 1.15 };

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
        border: active
          ? '1px solid var(--mantine-color-blue-6)'
          : '1px solid var(--mantine-color-dark-4)',
        boxShadow: active
          ? '0 0 0 1px color-mix(in srgb, var(--mantine-color-blue-6) 35%, transparent), 0 8px 24px rgba(0, 0, 0, 0.35)'
          : '0 2px 8px rgba(0, 0, 0, 0.2)',
        transition: `border-color ${transitionMs}, box-shadow ${transitionMs}`,
      }}
      onClick={() => {
        if (beatmap.folder === selectedFolder) {
          return triggerReparse();
        }

        if (onSelect) {
          onSelect();
        } else {
          setSelectedFolder(beatmap.folder);
        }
        // No page open that uses a beatmap, redirect to checks page as default
        if (
          location.pathname !== '/checks' &&
          location.pathname !== '/snapshots' &&
          location.pathname !== '/overview'
        ) {
          navigate('/checks', { viewTransition: true });
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
          transform: active ? 'scale(1.045)' : 'scale(1)',
          transformOrigin: 'center center',
          transition: `transform ${transitionMs}`,
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
          background: active ? 'rgba(0, 0, 0, 0.4)' : 'rgba(0, 0, 0, 0.6)',
          borderRadius: 'var(--mantine-radius-md)',
          zIndex: 1,
          pointerEvents: 'none',
          transition: `background ${transitionMs}`,
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
        <Stack gap="sm">
          <Stack gap={0}>
            <Text style={artistTitleStyle}>{beatmap.artist}</Text>
            <Text style={artistTitleStyle}>{beatmap.title}</Text>
          </Stack>

          <Text fs="italic" size="xs" style={textStyle}>
            Mapped by {beatmap.creator}
          </Text>
        </Stack>
      </Flex>
    </Flex>
  );
}

export default BeatmapCard;

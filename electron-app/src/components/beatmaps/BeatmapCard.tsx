import { ActionIcon, Box, Flex, Stack, Text, Tooltip } from '@mantine/core';
import { IconPin, IconPinFilled } from '@tabler/icons-react';
import { useEffect, useState } from 'react';
import { useBeatmap } from '../../context/BeatmapContext';
import { useBeatmapReparse } from '../../context/BeatmapReparseRegistry.tsx';
import { useSettings } from '../../context/SettingsContext';
import { Beatmap } from '../../Types.ts';
import { buildBeatmapImageUrl } from '../../utils/buildBeatmapFolderPath.ts';

interface BeatmapCardProps {
  beatmap: Beatmap;
  songFolder?: string;
  onSelect?: () => void;
  isSelectedOverride?: boolean;
}

function BeatmapCard({ beatmap, songFolder, onSelect, isSelectedOverride }: BeatmapCardProps) {
  const { selectedFolder, setSelectedFolder } = useBeatmap();
  const { settings, setSettings } = useSettings();
  const [bgUrl, setBgUrl] = useState<string | undefined>(undefined);
  const [isHovered, setIsHovered] = useState(false);
  const { triggerReparse } = useBeatmapReparse();

  const isBookmarked = settings.bookmarkedFolders.includes(beatmap.folder);

  const toggleBookmark = () => {
    setSettings((prev) => ({
      ...prev,
      bookmarkedFolders: isBookmarked
        ? prev.bookmarkedFolders.filter((folder) => folder !== beatmap.folder)
        : [...prev.bookmarkedFolders, beatmap.folder],
    }));
  };

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

  const cardVisual = (() => {
    if (isSelected && isHovered) {
      return {
        border: '1px solid var(--mantine-color-blue-4)',
        boxShadow: '0 10px 28px rgba(0, 0, 0, 0.4)',
        overlay: 'rgba(0, 0, 0, 0.38)',
        scale: 1.045,
      };
    }

    if (isSelected) {
      return {
        border: '1px solid var(--mantine-color-blue-6)',
        boxShadow: '0 4px 16px rgba(0, 0, 0, 0.28)',
        overlay: 'rgba(0, 0, 0, 0.48)',
        scale: 1,
      };
    }

    if (isHovered) {
      return {
        border: '1px solid var(--mantine-color-dark-2)',
        boxShadow: '0 6px 20px rgba(0, 0, 0, 0.32)',
        overlay: 'rgba(0, 0, 0, 0.52)',
        scale: 1.025,
      };
    }

    return {
      border: '1px solid var(--mantine-color-dark-4)',
      boxShadow: '0 2px 8px rgba(0, 0, 0, 0.2)',
      overlay: 'rgba(0, 0, 0, 0.6)',
      scale: 1,
    };
  })();

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
        border: cardVisual.border,
        boxShadow: cardVisual.boxShadow,
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
          transform: `scale(${cardVisual.scale})`,
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
          background: cardVisual.overlay,
          borderRadius: 'var(--mantine-radius-md)',
          zIndex: 1,
          pointerEvents: 'none',
          transition: `background ${transitionMs}`,
        }}
      />
      {settings.bookmarksEnabled && beatmap.folder !== 'placeholder' && (
        <Tooltip label={isBookmarked ? 'Remove bookmark' : 'Bookmark this beatmapset'}>
          <ActionIcon
            variant="subtle"
            color="yellow"
            size="sm"
            style={{ position: 'absolute', top: 4, right: 4, zIndex: 3 }}
            onClick={(e) => {
              e.stopPropagation();
              toggleBookmark();
            }}
            aria-label={isBookmarked ? 'Remove bookmark' : 'Bookmark this beatmapset'}
          >
            {isBookmarked ? <IconPinFilled size={16} /> : <IconPin size={16} />}
          </ActionIcon>
        </Tooltip>
      )}
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
          cursor: 'pointer',
          userSelect: 'none',
          WebkitUserSelect: 'none',
          MozUserSelect: 'none',
          msUserSelect: 'none',
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

import { ActionIcon, Flex, Skeleton, Stack, Text, Tooltip } from '@mantine/core';
import { IconPin, IconPinFilled } from '@tabler/icons-react';
import { useEffect, useState } from 'react';
import { useBeatmap } from '../../context/BeatmapContext';
import { useBeatmapReparse } from '../../context/BeatmapReparseRegistry.tsx';
import { useSettings } from '../../context/SettingsContext';
import { Beatmap } from '../../Types.ts';
import {
  buildBeatmapImageUrl,
  resolveLazerBeatmapImageUrl,
} from '../../utils/buildBeatmapFolderPath.ts';

interface BeatmapCardProps {
  beatmap: Beatmap;
  songFolder?: string;
  /** 'lazer' resolves the background via the realm-backed CAS image endpoint instead of a Songs-folder path. */
  source?: 'stable' | 'lazer';
  lazerDataDir?: string;
  onSelect?: () => void;
  isSelectedOverride?: boolean;
  enterIndex?: number;
}

function BeatmapCard({
  beatmap,
  songFolder,
  source = 'stable',
  lazerDataDir,
  onSelect,
  isSelectedOverride,
  enterIndex,
}: BeatmapCardProps) {
  const { selectedFolder, setSelectedFolder } = useBeatmap();
  const { settings, setSettings } = useSettings();
  const [bgUrl, setBgUrl] = useState<string | undefined>(undefined);
  const [loadedCandidate, setLoadedCandidate] = useState<string | undefined>(undefined);
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

  const hasFolder = !!beatmap.folder && beatmap.folder !== 'placeholder';
  const candidate = hasFolder
    ? source === 'lazer'
      ? resolveLazerBeatmapImageUrl(beatmap, lazerDataDir)
      : buildBeatmapImageUrl(beatmap.folder, { songFolder })
    : undefined;

  useEffect(() => {
    if (!candidate) return;

    let cancelled = false;
    const img = new Image();

    img.onload = () => {
      if (!cancelled) {
        setBgUrl(candidate);
        setLoadedCandidate(candidate);
      }
    };

    img.onerror = () => {
      if (!cancelled) {
        setBgUrl(undefined);
        setLoadedCandidate(candidate);
      }
    };

    img.src = candidate;

    return () => {
      cancelled = true;
    };
  }, [candidate]);

  const displayedBgUrl = hasFolder && loadedCandidate === candidate ? bgUrl : undefined;
  const imageLoading = hasFolder && loadedCandidate !== candidate;

  const isSelected = isSelectedOverride ?? selectedFolder === beatmap.folder;

  const transitionMs = '0.22s ease';

  // The art and its left-to-right shade (dark enough behind the text on any artwork, lighter on
  // the right so the art still shows) are one layer (`.mv-beatmap-card::before`), not separate
  // art and shade layers: those are clipped separately at the rounded corners and leave a light
  // fringe there. Hover and selection lighten the shade (`lift`), and hovering zooms the art in
  // slightly (`zoom`). The ring is one 2px border in
  // every state (only its colour changes), so nothing shifts and the corner is a single smooth
  // curve; stacking a border and a box-shadow drew two slightly different curves there. No drop
  // shadows: they darkened the sidebar around a hovered or selected card.
  const cardVisual = (() => {
    if (isSelected) {
      return {
        borderColor: 'var(--mantine-color-primary-2)',
        lift: isHovered ? 0.18 : 0.12,
        zoom: isHovered ? 1.045 : 1,
      };
    }

    if (isHovered) {
      return {
        borderColor: 'var(--mantine-color-dark-2)',
        lift: 0.06,
        zoom: 1.025,
      };
    }

    return {
      borderColor: 'var(--mantine-color-dark-4)',
      lift: 0,
      zoom: 1,
    };
  })();

  const textStyle = {
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
    display: 'block',
    maxWidth: '100%',
    textShadow:
      '0 1px 2px rgba(0, 0, 0, 0.62), 0 0 8px rgba(0, 0, 0, 0.28), 0 0 1px rgba(0, 0, 0, 0.55)',
  } as const;

  const artistTitleStyle = { ...textStyle, lineHeight: 1.15 };

  const enterDelayMs = enterIndex !== undefined ? Math.min(enterIndex, 10) * 22 : 0;
  const isPlaceholder = beatmap.folder === 'placeholder';

  const select = () => {
    // Prefer isSelected so lazer cards (GUID vs temp path) and current-map overrides still reparse.
    if (isSelected) {
      return triggerReparse();
    }

    if (onSelect) {
      onSelect();
    } else {
      setSelectedFolder(beatmap.folder);
    }
  };

  return (
    <Flex
      h={96}
      role={isPlaceholder ? undefined : 'button'}
      tabIndex={isPlaceholder ? undefined : 0}
      aria-current={isSelected || undefined}
      className={!isPlaceholder ? 'mv-beatmap-card mv-beatmap-card-enter' : 'mv-beatmap-card'}
      style={{
        '--mv-card-art': displayedBgUrl ? `url('${displayedBgUrl}')` : 'none',
        '--mv-card-lift': cardVisual.lift,
        '--mv-card-zoom': cardVisual.zoom,
        justifyContent: 'flex-start',
        alignItems: 'center',
        borderRadius: 'var(--mantine-radius-md)',
        position: 'relative',
        overflow: 'hidden',
        cursor: 'pointer',
        border: `2px solid ${cardVisual.borderColor}`,
        transition: `border-color ${transitionMs}, --mv-card-lift ${transitionMs}`,
        ...(!isPlaceholder
          ? {
              animation: 'mv-beatmap-card-enter 280ms cubic-bezier(0.4, 0, 0.2, 1) both',
              animationDelay: `${enterDelayMs}ms`,
            }
          : {}),
      }}
      onClick={select}
      onKeyDown={(event) => {
        // Keys pressed on the bookmark button inside the card are that button's own.
        if (isPlaceholder || event.target !== event.currentTarget) return;
        if (event.key === 'Enter' || event.key === ' ') {
          event.preventDefault();
          void select();
        }
      }}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      {/* Loading shimmer, shown until the background image settles */}
      {!isPlaceholder && imageLoading && (
        <Skeleton
          radius="calc(var(--mantine-radius-md) - 2px)"
          style={{ position: 'absolute', inset: 0, zIndex: 0 }}
        />
      )}
      {settings.bookmarksEnabled && !isPlaceholder && (
        <Tooltip label={isBookmarked ? 'Remove bookmark' : 'Bookmark this mapset'}>
          <ActionIcon
            variant="subtle"
            color="yellow"
            size="sm"
            style={{
              position: 'absolute',
              top: 4,
              right: 4,
              zIndex: 3,
              opacity: isHovered || isSelected || isBookmarked ? 1 : 0,
              pointerEvents: isHovered || isSelected || isBookmarked ? 'auto' : 'none',
              transition: `opacity ${transitionMs}`,
            }}
            onClick={(e) => {
              e.stopPropagation();
              toggleBookmark();
            }}
            aria-label={isBookmarked ? 'Remove bookmark' : 'Bookmark this mapset'}
          >
            {isBookmarked ? <IconPinFilled size={16} /> : <IconPin size={16} />}
          </ActionIcon>
        </Tooltip>
      )}
      {/* Text content */}
      <Flex
        direction="column"
        gap={0}
        w="100%"
        py="xs"
        px="md"
        style={{
          position: 'relative',
          zIndex: 2,
          overflow: 'hidden',
          minWidth: 0,
          maxWidth: '100%',
          textAlign: 'left',
          cursor: 'pointer',
          userSelect: 'none',
          WebkitUserSelect: 'none',
          MozUserSelect: 'none',
          msUserSelect: 'none',
        }}
      >
        {/* Title first, then artist and mapper, so the three lines scan top to bottom. Only the
            title shares a row with the bookmark pin, so only it reserves room for the pin. */}
        <Stack gap="xs">
          <Stack gap={0}>
            <Text fw={700} pr={settings.bookmarksEnabled ? 12 : 0} style={artistTitleStyle}>
              {beatmap.title}
            </Text>
            <Text size="sm" style={artistTitleStyle}>
              {beatmap.artist}
            </Text>
          </Stack>
          <Text size="xs" c="gray.4" style={textStyle}>
            Mapped by {beatmap.creator}
          </Text>
        </Stack>
      </Flex>
    </Flex>
  );
}

export default BeatmapCard;

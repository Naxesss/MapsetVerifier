import { Box, Group, Stack, Title, Text, Anchor, useMantineTheme } from '@mantine/core';
import { ReactNode, useLayoutEffect, useRef, useState } from 'react';
import MapsetActions from './MapsetActions.tsx';
import { MapsetFrameContext } from './mapsetFrameContext.ts';
import { useBeatmap } from '../../context/BeatmapContext.tsx';
import { useSettings } from '../../context/SettingsContext.tsx';
import { useOpenExternal } from '../../hooks/useOpenExternal.ts';
import { useBeatmapBackground } from '../checks/hooks/useBeatmapBackground.ts';

/** Height of the artwork; fits the title plus two rows of controls. */
const BANNER_HEIGHT = 220;
/** How far above the art's visible bottom edge it starts fading into the page colour. */
const FADE_HEIGHT = 48;
/** Corner radius of the page panel the banner sits at the top of. */
const PANEL_RADIUS = 'var(--mantine-radius-lg)';
/** Top strip of the panel left without a background, clear of its rounded corners (see below). */
const CORNER_CLEARANCE = 32;

interface MapsetFrameProps {
  children: ReactNode;
}

function artLayerStyle(url: string) {
  return {
    position: 'absolute',
    top: 0,
    left: 0,
    width: '100%',
    height: BANNER_HEIGHT,
    // Darkened in the same layer, so art and shade are clipped identically at the rounded corners.
    backgroundImage: `linear-gradient(rgba(0, 0, 0, 0.7), rgba(0, 0, 0, 0.7)), url('${url}')`,
    backgroundSize: 'cover',
    backgroundPosition: 'center',
    backgroundRepeat: 'no-repeat',
    // Rounded like the page panel's top corners, so each layer draws the curve itself instead of
    // relying only on the panel's clip, which can leave the art's edge showing at the corners.
    borderTopLeftRadius: PANEL_RADIUS,
    borderTopRightRadius: PANEL_RADIUS,
  } as const;
}

/**
 * The page panel of the mapset pages (Checks, Snapshots, Overview) with the mapset's art, title and
 * mapper at the top. It stays mounted while switching between those pages, so only the page below
 * the title (its BeatmapHeader controls and content) swaps; the art and text only change, with a
 * fade, when a different mapset is selected.
 */
function MapsetFrame({ children }: MapsetFrameProps) {
  const theme = useMantineTheme();
  const { selectedFolder, beatmapInfo, isBeatmapInfoLoading } = useBeatmap();
  const { settings } = useSettings();
  const { bgUrl, isLoading: isBgLoading } = useBeatmapBackground(
    selectedFolder,
    settings.songFolder
  );
  const openExternal = useOpenExternal();

  // Switching mapsets keeps the previous art and text on screen until the next mapset's are ready,
  // then fades to them, instead of blanking the banner and letting it jump while it loads.
  const [shownInfo, setShownInfo] = useState(beatmapInfo);
  if (!isBeatmapInfoLoading && beatmapInfo !== shownInfo) {
    setShownInfo(beatmapInfo);
  }

  const [art, setArt] = useState<{ current?: string; previous?: string }>({ current: bgUrl });
  if (!isBgLoading && bgUrl !== art.current) {
    setArt({ current: bgUrl, previous: art.current });
  }

  // The art window ends where the page's BeatmapHeader (its controls) ends. While a page is swapped
  // out the last position is kept, so the art only changes once the next page's header is there.
  const frameRef = useRef<HTMLDivElement>(null);
  const titleRef = useRef<HTMLDivElement>(null);
  const [headerEl, setHeaderEl] = useState<HTMLElement | null>(null);
  const [headerBottom, setHeaderBottom] = useState<number>();

  useLayoutEffect(() => {
    const frame = frameRef.current;
    if (!frame || !headerEl) return;

    const measure = () => {
      const px = headerEl.getBoundingClientRect().bottom - frame.getBoundingClientRect().top;
      setHeaderBottom(px);
    };
    measure();
    const observer = new ResizeObserver(measure);
    observer.observe(headerEl);
    if (titleRef.current) observer.observe(titleRef.current);
    return () => observer.disconnect();
  }, [headerEl]);

  const artHeight = Math.min(headerBottom ?? BANNER_HEIGHT, BANNER_HEIGHT);

  const title = shownInfo?.title;
  const artist = shownInfo?.artist;
  const creator = shownInfo?.creator;

  // The artwork is its own fixed-size layer (always BANNER_HEIGHT tall, anchored to the top) rather
  // than a background stretched over the banner, so it is cropped the same on every page. Its window
  // is cut off at the header's bottom (or at the artwork's end) and always fades into the page
  // content colour there, so the header runs into the content without an edge.
  return (
    <Box
      ref={frameRef}
      h="100%"
      style={{
        fontFamily: theme.headings.fontFamily,
        position: 'relative',
        width: '100%',
        borderRadius: theme.radius.lg,
        overflow: 'hidden',
        // Clip the banner's layers in one pass, so its rounded top corners stay clean.
        isolation: 'isolate',
        boxShadow: '0 4px 32px rgba(0,0,0,0.4)',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'flex-start',
        // Nothing but the page sits under the art at the top: a coloured background there would
        // show as a light fringe where the panel's rounded corners cut the art. Everywhere else,
        // and without art, the panel uses the page content colour.
        background: art.current
          ? `linear-gradient(to bottom, transparent ${CORNER_CLEARANCE}px, var(--mantine-color-dark-6) ${CORNER_CLEARANCE}px)`
          : 'var(--mantine-color-dark-6)',
      }}
    >
      <Box
        aria-hidden
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          width: '100%',
          height: artHeight,
          overflow: 'hidden',
          borderTopLeftRadius: PANEL_RADIUS,
          borderTopRightRadius: PANEL_RADIUS,
          zIndex: 0,
          pointerEvents: 'none',
        }}
      >
        {/* The previous art stays underneath while the new art fades in over it; a mapset without
            art clears both. */}
        {art.current && art.previous && <Box style={artLayerStyle(art.previous)} />}
        {art.current && (
          <Box key={art.current} className="mv-fade-in" style={artLayerStyle(art.current)} />
        )}
        {/* Fade at the bottom only, away from the panel's rounded corners. */}
        <Box
          style={{
            position: 'absolute',
            left: 0,
            right: 0,
            bottom: 0,
            height: FADE_HEIGHT,
            background: 'linear-gradient(to bottom, transparent, var(--mantine-color-dark-6))',
          }}
        />
      </Box>
      {/* Who and what on the left, in the same order as the sidebar cards (title, artist, mapper);
          what can be done with the mapset as a whole on the right. */}
      <Group
        ref={titleRef}
        pt="md"
        px="md"
        justify="space-between"
        align="flex-start"
        gap="md"
        wrap="nowrap"
        style={{ position: 'relative', zIndex: 1 }}
      >
        <Stack
          key={`${artist ?? ''}-${title ?? ''}-${creator ?? ''}`}
          className="mv-fade-in"
          gap={2}
          style={{ minWidth: 0 }}
        >
          {title && <Title order={2}>{title}</Title>}
          {artist && <Text size="lg">{artist}</Text>}
          {creator && (
            <Text size="sm" c="gray.4">
              Mapped by{' '}
              <Anchor
                inherit
                href={`https://osu.ppy.sh/users/@${creator}`}
                onClick={(e) => {
                  e.preventDefault();
                  void openExternal(`https://osu.ppy.sh/users/@${creator}`);
                }}
              >
                {creator}
              </Anchor>
            </Text>
          )}
        </Stack>
        <MapsetActions />
      </Group>
      {/* The page, above the art: its header controls sit on the art, its content covers it. */}
      <Box style={{ position: 'relative', zIndex: 1 }}>
        <MapsetFrameContext.Provider value={setHeaderEl}>{children}</MapsetFrameContext.Provider>
      </Box>
    </Box>
  );
}

export default MapsetFrame;

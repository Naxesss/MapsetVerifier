import { Box, Stack, Flex, Title, Text, Anchor } from '@mantine/core';
import { ReactNode, useState } from 'react';
import { useBeatmap } from '../../context/BeatmapContext.tsx';
import { useSettings } from '../../context/SettingsContext.tsx';
import { useOpenExternal } from '../../hooks/useOpenExternal.ts';
import { useBeatmapBackground } from '../checks/hooks/useBeatmapBackground.ts';

/** Height of the artwork; fits the title plus two rows of controls. */
const BANNER_HEIGHT = 220;
/** How far above the art's visible bottom edge it starts fading into the page colour. */
const FADE_HEIGHT = 48;
/** Corner radius of the page panel the header sits at the top of (Checks, Snapshots, Overview). */
const PANEL_RADIUS = 'var(--mantine-radius-lg)';

interface BeatmapHeaderProps {
  children?: ReactNode;
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

function BeatmapHeader({ children }: BeatmapHeaderProps) {
  const { selectedFolder, beatmapInfo, isBeatmapInfoLoading } = useBeatmap();
  const { settings } = useSettings();
  const { bgUrl, isLoading: isBgLoading } = useBeatmapBackground(
    selectedFolder,
    settings.songFolder
  );
  const openExternal = useOpenExternal();

  // Switching mapsets keeps the previous art and text on screen until the next mapset's are ready,
  // then fades to them, instead of blanking the header and letting it jump while it loads.
  const [shownInfo, setShownInfo] = useState(beatmapInfo);
  if (!isBeatmapInfoLoading && beatmapInfo !== shownInfo) {
    setShownInfo(beatmapInfo);
  }

  const [art, setArt] = useState<{ current?: string; previous?: string }>({ current: bgUrl });
  if (!isBgLoading && bgUrl !== art.current) {
    setArt({ current: bgUrl, previous: art.current });
  }

  const title = shownInfo?.title;
  const artist = shownInfo?.artist;
  const creator = shownInfo?.creator;

  // The artwork is its own fixed-size layer (always BANNER_HEIGHT tall, anchored to the top) rather
  // than a background stretched over the header, so it is cropped the same on Checks, Snapshots and
  // Overview. The header itself is only as tall as its content: the art window is cut off at the
  // header's bottom (or at the artwork's end) and always fades into the page content colour there,
  // so the header runs into the content without an edge. No bottom padding: the content below adds
  // the one gap between the header's last row and the page.
  return (
    <Box
      style={{
        position: 'relative',
        width: '100%',
        overflow: 'hidden',
        // Nothing but the page sits under the art: a coloured background there would show as a
        // light fringe where the panel's rounded corners cut the art. Below the art (a header
        // taller than it) and without art, the header uses the page content colour.
        background: art.current
          ? `linear-gradient(to bottom, transparent ${BANNER_HEIGHT}px, var(--mantine-color-dark-6) ${BANNER_HEIGHT}px)`
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
          height: '100%',
          maxHeight: BANNER_HEIGHT,
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
      <Box pt="md" px="md" style={{ position: 'relative', zIndex: 1 }}>
        <Stack gap="sm">
          <Flex
            key={`${artist ?? ''}-${title ?? ''}-${creator ?? ''}`}
            className="mv-fade-in"
            gap="xs"
            direction="column"
          >
            {title && artist && (
              <Title order={2}>
                {artist} - {title}
              </Title>
            )}
            {creator && (
              <Text>
                Mapset by{' '}
                <Anchor
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
          </Flex>
          {children}
        </Stack>
      </Box>
    </Box>
  );
}

export default BeatmapHeader;

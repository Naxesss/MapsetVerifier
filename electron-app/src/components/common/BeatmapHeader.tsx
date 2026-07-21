import { Box, Stack, Flex, Title, Text, Anchor } from '@mantine/core';
import { ReactNode } from 'react';
import { useBeatmap } from '../../context/BeatmapContext.tsx';
import { useOpenExternal } from '../../hooks/useOpenExternal.ts';

interface BeatmapHeaderProps {
  bgUrl?: string;
  children?: ReactNode;
}

function BeatmapHeader({ bgUrl, children }: BeatmapHeaderProps) {
  const { beatmapInfo } = useBeatmap();
  const openExternal = useOpenExternal();
  const title = beatmapInfo?.title;
  const artist = beatmapInfo?.artist;
  const creator = beatmapInfo?.creator;

  return (
    <Box
      style={{
        position: 'relative',
        width: '100%',
        backgroundImage: bgUrl ? `url('${bgUrl}')` : 'none',
        backgroundSize: 'cover',
        backgroundPosition: 'center',
        backgroundRepeat: 'no-repeat',
        overflow: 'hidden',
      }}
    >
      <Box
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          width: '100%',
          height: '100%',
          background: 'rgba(0,0,0,0.7)',
          zIndex: 1,
          pointerEvents: 'none',
        }}
      />
      <Box p="md" style={{ position: 'relative', zIndex: 2 }}>
        <Stack gap="sm">
          <Flex gap="xs" direction="column">
            {title && artist && (
              <Title order={2}>
                {artist} - {title}
              </Title>
            )}
            {creator && (
              <Text>
                Beatmapset by{' '}
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

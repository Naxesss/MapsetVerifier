import { Box, Stack, Flex, Title, Text, Anchor } from '@mantine/core';
import { ReactNode } from 'react';

interface BeatmapHeaderProps {
  title?: string | null;
  artist?: string | null;
  creator?: string | null;
  bgUrl?: string;
  children?: ReactNode;
}

function BeatmapHeader({ title, artist, creator, bgUrl, children }: BeatmapHeaderProps) {
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
                BeatmapSet by{' '}
                <Anchor
                  href={`https://osu.ppy.sh/users/${creator}`}
                  target="_blank"
                  rel="noopener noreferrer"
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


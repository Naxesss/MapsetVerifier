import {Box, Stack, Flex, Title, Text, Anchor} from '@mantine/core';
import { ApiBeatmapSetCheckResult } from '../../Types';
import React from "react";

interface BeatmapHeaderProps {
  data?: ApiBeatmapSetCheckResult;
  bgUrl?: string;
  children?: React.ReactNode;
}

function BeatmapHeader({ data, bgUrl, children }: BeatmapHeaderProps) {
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
            {data?.title && data?.artist &&
              <Title order={2}>
                {data?.artist} - {data?.title}
              </Title>
            }
            {data?.creator &&
              <Text>
                BeatmapSet by{' '}
                <Anchor
                  href={data?.creator ? `https://osu.ppy.sh/users/${data.creator}` : undefined}
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  {data?.creator}
                </Anchor>
              </Text>
            }
          </Flex>
          {children}
        </Stack>
      </Box>
    </Box>
  );
}

export default BeatmapHeader;

import { Box, Button, Group, Stack, Text } from '@mantine/core';
import { IconBrandGithub, IconMessage } from '@tabler/icons-react';
import Changelog from './Changelog.tsx';
import { useOpenExternal } from '../../hooks/useOpenExternal.ts';
import { SectionTitle } from '../common/Headings.tsx';

const ISSUES_URL = 'https://github.com/Naxesss/MapsetVerifier/issues';
const GREAPER_URL = 'https://osu.ppy.sh/users/2369776';

export default function Home() {
  const openExternal = useOpenExternal();

  // Opens with a toolbar row like Documentation and Ranking criteria: 36px high, so it lines up
  // with the sidebar's search row, followed by the content.
  return (
    <Box maw={960}>
      <Stack gap="sm">
        <Group justify="space-between" gap="sm" mih="var(--mv-control-height)">
          <SectionTitle>Changelog</SectionTitle>
          {/* The question says what the buttons are for, so they read as a feedback prompt. */}
          <Group gap="sm">
            <Text size="sm" c="dimmed">
              Found a bug or have an idea?
            </Text>
            <Button
              variant="default"
              leftSection={<IconBrandGithub size={18} stroke={1.5} />}
              onClick={() => void openExternal(ISSUES_URL)}
            >
              Report on GitHub
            </Button>
            <Button
              variant="default"
              leftSection={<IconMessage size={18} stroke={1.5} />}
              onClick={() => void openExternal(GREAPER_URL)}
            >
              Message Greaper
            </Button>
          </Group>
        </Group>
        <Changelog />
      </Stack>
    </Box>
  );
}

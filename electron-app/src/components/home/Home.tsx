import { Alert, Button, Container, Text } from '@mantine/core';
import { IconBug, IconLifebuoy, IconMessage } from '@tabler/icons-react';
import Changelog from './Changelog.tsx';
import { useOpenExternal } from '../../hooks/useOpenExternal.ts';

export default function Home() {
  const openExternal = useOpenExternal();

  return (
    <Container size="md" p={0}>
      <Alert
        icon={<IconLifebuoy />}
        title="Having an issue while using Mapset Verifier?"
        color="yellow"
        variant="light"
        mb="md"
      >
        <Text size="sm" mb="sm">
          With the Mapset Verifier 2.0 release, a lot has changed, and while we&apos;ve worked hard
          to ensure stability, some issues may still arise. If you encounter any bugs, have
          suggestions for new features, or need assistance, please don&apos;t hesitate to reach out!
        </Text>
        <Button
          color="green"
          variant="light"
          leftSection={<IconBug />}
          onClick={async () => {
            try {
              await openExternal('https://github.com/Naxesss/MapsetVerifier/issues');
            } catch (e) {
              console.error('Failed to open beatmap page:', e);
              alert('Failed to open beatmap page. See console for details.');
            }
          }}
        >
          Report an issue on GitHub
        </Button>
        <Button
          color="pink"
          variant="light"
          ml="md"
          leftSection={<IconMessage />}
          onClick={async () => {
            try {
              await openExternal('https://osu.ppy.sh/users/2369776');
            } catch (e) {
              console.error('Failed to open beatmap page:', e);
              alert('Failed to open beatmap page. See console for details.');
            }
          }}
        >
          Contact Greaper on osu!
        </Button>
      </Alert>
      <Changelog />
    </Container>
  );
}

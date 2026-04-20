import {Alert, Button, Container, Text} from "@mantine/core";
import changelog from "../../content/CHANGELOG.md?raw";
import MantineMarkdown from "../documentation/MantineMarkdown.tsx";

export default function Home() {
  return (
    <Container size="md">
      <Alert
        title="Having an issue while using MV?"
        color="yellow"
        variant="light"
        mb="md"
      >
        <Text size="sm" mb="sm">
          With the Mapset Verifier 2 release a lot has changed, and while we&apos;ve worked hard to ensure stability, some issues may still arise.
          If you encounter any bugs, have suggestions for new features, or need assistance, please don&apos;t hesitate to reach out!
        </Text>
        <Button
          color="green"
          variant="light"
          component="a"
          href="https://github.com/Naxesss/MapsetVerifier/issues"
          target="_blank"
          rel="noreferrer"
        >
          Report an issue on GitHub
        </Button>
        <Button
          color="pink"
          variant="light"
          ml="md"
          component="a"
          href="https://osu.ppy.sh/users/2369776"
          target="_blank"
          rel="noreferrer"
        >
          Contact Greaper on osu!
        </Button>
      </Alert>
      <MantineMarkdown>{changelog}</MantineMarkdown>
    </Container>
  );
}

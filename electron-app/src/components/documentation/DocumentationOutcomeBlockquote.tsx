import { Blockquote, Stack, Text } from '@mantine/core';
import MantineMarkdown from './MantineMarkdown';
import LevelIcon from '../icons/LevelIcon';
import type { ApiDocumentationCheckDetailsOutcome, Level } from '../../Types';

const BLOCKQUOTE_COLOR_BY_LEVEL: Record<Level, string> = {
  Problem: 'red.6',
  Warning: 'orange.6',
  Minor: 'green.6',
  Error: 'gray.6',
  Info: 'blue.6',
  Check: 'green.6',
};

interface DocumentationOutcomeBlockquoteProps {
  outcome: ApiDocumentationCheckDetailsOutcome;
}

export default function DocumentationOutcomeBlockquote({
  outcome,
}: DocumentationOutcomeBlockquoteProps) {
  const color = BLOCKQUOTE_COLOR_BY_LEVEL[outcome.level] ?? 'gray';

  return (
    <Blockquote
      color={color}
      icon={<LevelIcon level={outcome.level} size={25} />}
      radius="md"
      p="lg"
    >
      <Stack gap="xs">
        <MantineMarkdown>{outcome.description}</MantineMarkdown>
        {outcome.cause && (
          <>
            <Text size="xs" c="dimmed" mt="xs">
              Cause: {outcome.cause}
            </Text>
          </>
        )}
      </Stack>
    </Blockquote>
  );
}

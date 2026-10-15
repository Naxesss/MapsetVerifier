import { Blockquote, Stack, Text } from '@mantine/core';
import MantineMarkdown from './MantineMarkdown';
import LevelIcon from '../icons/LevelIcon';
import RuleReferences from '../rankingCriteria/RuleReferences';
import type { ApiDocumentationCheckDetailsOutcome, Level } from '../../Types';

const BLOCKQUOTE_COLOR_BY_LEVEL: Record<Level, string> = {
  Problem: 'red.6',
  Warning: 'orange.6',
  Minor: 'green.6',
  Error: 'gray.6',
  Info: 'teal.6',
  Check: 'green.6',
};

interface DocumentationOutcomeBlockquoteProps {
  outcome: ApiDocumentationCheckDetailsOutcome;
  /** Whether to list the ranking criteria the outcome enforces. */
  showRules?: boolean;
}

export default function DocumentationOutcomeBlockquote({
  outcome,
  showRules = true,
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
          <Text size="sm" c="dimmed">
            Cause: {outcome.cause}
          </Text>
        )}
        {showRules && <RuleReferences ruleIds={outcome.ruleIds} />}
      </Stack>
    </Blockquote>
  );
}

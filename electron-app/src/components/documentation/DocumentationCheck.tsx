import { Badge, Flex, Group, Text, useMantineTheme } from '@mantine/core';
import { useState } from 'react';
import { ApiDocumentationCheck, Level } from '../../Types.ts';
import ClickableRow from '../common/ClickableRow.tsx';
import DetailModal from '../details/DetailModal';
import GameModeIcon from '../icons/GameModeIcon.tsx';
import LevelIcon from '../icons/LevelIcon.tsx';

/** Left to right: what the check can report, most severe first, and whether it can fail to run. */
const OUTCOME_ORDER: Level[] = ['Problem', 'Warning', 'Minor', 'Info', 'Check', 'Error'];

/** Each level a check can report, once, in {@link OUTCOME_ORDER}. */
function outcomeLevels(outcomes: Level[]): Level[] {
  return OUTCOME_ORDER.filter((level) => outcomes.includes(level));
}

interface DocumentationCheckProps {
  check: ApiDocumentationCheck;
}

function DocumentationCheck({ check }: DocumentationCheckProps) {
  const theme = useMantineTheme();
  const [modalOpen, setModalOpen] = useState(false);

  return (
    <>
      <ClickableRow onClick={() => setModalOpen(true)}>
        <Flex direction="column" style={{ flex: 1 }}>
          <Text fw="bold">{check.description}</Text>
          <Group gap="xs">
            <Group gap={0}>
              {check.modes.map((mode) => (
                <GameModeIcon
                  size={16}
                  key={mode}
                  mode={mode}
                  color={theme.colors.gray[5]}
                  withTooltip
                />
              ))}
            </Group>
            <Badge>{check.category}</Badge>
          </Group>
        </Flex>
        <Flex direction="column">
          <Group gap="xs" style={{ alignSelf: 'end' }}>
            {outcomeLevels(check.outcomes).map((level) => (
              <LevelIcon key={level} level={level} />
            ))}
          </Group>
          <Text size="sm" c="dimmed" style={{ alignSelf: 'end' }}>
            {check.author}
          </Text>
        </Flex>
      </ClickableRow>
      <DetailModal
        view={modalOpen ? { kind: 'check', check } : null}
        onClose={() => setModalOpen(false)}
      />
    </>
  );
}

export default DocumentationCheck;

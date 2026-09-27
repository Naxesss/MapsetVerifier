import { Badge, Flex, Group, Text, useMantineTheme } from '@mantine/core';
import { useState } from 'react';
import { ApiDocumentationCheck } from '../../Types.ts';
import ClickableRow from '../common/ClickableRow.tsx';
import DetailModal from '../details/DetailModal';
import GameModeIcon from '../icons/GameModeIcon.tsx';
import LevelIcon from '../icons/LevelIcon.tsx';

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
            {check.outcomes.map((level, index) => (
              <LevelIcon key={`${check.id}-outcome-${index}`} level={level} />
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

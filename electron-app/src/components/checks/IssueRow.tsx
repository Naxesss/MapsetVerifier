import { Group, Text, useMantineTheme } from '@mantine/core';
import React from 'react';
import { ApiCheckResult } from '../../Types';
import OsuLink from '../common/OsuLink';
import LevelIcon from '../icons/LevelIcon';

interface IssueRowProps {
  item: ApiCheckResult;
  onOpen?: () => void;
}

const IssueRow: React.FC<IssueRowProps> = ({ item, onOpen }) => {
  const theme = useMantineTheme();

  const handleOpen = (event: React.MouseEvent) => {
    const target = event.target;
    if (target instanceof Element && target.closest('a, button')) {
      return;
    }

    onOpen?.();
  };

  const handleKeyDown: React.KeyboardEventHandler = (event) => {
    if (!onOpen || event.key !== 'Enter') {
      return;
    }

    event.preventDefault();
    onOpen();
  };

  return (
    <Group
      gap="xs"
      align="center"
      role={onOpen ? 'button' : undefined}
      tabIndex={onOpen ? 0 : undefined}
      onClick={handleOpen}
      onKeyDown={handleKeyDown}
      onMouseEnter={(event) => {
        if (onOpen) {
          event.currentTarget.style.background = theme.colors.dark[6];
        }
      }}
      onMouseLeave={(event) => {
        event.currentTarget.style.background = 'transparent';
      }}
      style={{
        display: 'grid',
        gridTemplateColumns: 'auto 1fr',
        alignItems: 'start',
        borderRadius: theme.radius.sm,
        cursor: onOpen ? 'pointer' : undefined,
        marginLeft: -4,
        marginRight: -4,
        padding: '2px 4px',
      }}
    >
      <div style={{ alignSelf: 'start', userSelect: 'none' }}>
        <LevelIcon level={item.level === 'Check' ? 'Info' : item.level} size={16} />
      </div>
      <Text
        component="span"
        size="sm"
        style={{
          whiteSpace: 'normal',
          overflowWrap: 'anywhere',
          wordBreak: 'break-word',
          minWidth: 0,
          width: '100%',
        }}
      >
        <OsuLink text={item.message} />
      </Text>
    </Group>
  );
};

export default IssueRow;

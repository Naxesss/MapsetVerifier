import { Box, Group, Stack, Text, useMantineTheme } from '@mantine/core';
import React, { useState } from 'react';
import { ApiCheckResult } from '../../Types';
import OsuLink from '../common/OsuLink';
import LevelIcon from '../icons/LevelIcon';

interface IssueRowProps {
  item: ApiCheckResult;
  onOpen?: () => void;
  prefix?: React.ReactNode;
}

const IssueRow: React.FC<IssueRowProps> = ({ item, onOpen, prefix }) => {
  const theme = useMantineTheme();
  const [hovered, setHovered] = useState(false);
  const isInteractive = Boolean(onOpen);

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

  const row = (
    <Group
      gap="xs"
      align="flex-start"
      wrap="nowrap"
      style={{
        display: 'inline-flex',
        maxWidth: '100%',
        verticalAlign: 'top',
      }}
    >
      <div style={{ flexShrink: 0, userSelect: 'none' }}>
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
        }}
      >
        <OsuLink text={item.message} />
      </Text>
    </Group>
  );

  const content = (
    <Stack gap={0} style={{ width: 'fit-content', maxWidth: '100%' }}>
      {prefix}
      {row}
    </Stack>
  );

  if (!isInteractive) {
    return content;
  }

  return (
    <Box
      role="button"
      tabIndex={0}
      onClick={handleOpen}
      onKeyDown={handleKeyDown}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
      style={{
        width: 'fit-content',
        maxWidth: '100%',
        borderRadius: theme.radius.sm,
        cursor: 'pointer',
        padding: '2px 4px',
        backgroundColor: hovered ? 'var(--mantine-color-default-hover)' : undefined,
        transition: 'background-color 120ms ease',
      }}
    >
      {content}
    </Box>
  );
};

export default React.memo(IssueRow);

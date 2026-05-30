import { Group, Text } from '@mantine/core';
import React from 'react';
import { ApiCheckResult } from '../../Types';
import OsuLink from '../common/OsuLink';
import LevelIcon from '../icons/LevelIcon';

interface IssueRowProps {
  item: ApiCheckResult;
}

const IssueRow: React.FC<IssueRowProps> = ({ item }) => {
  return (
    <Group
      gap="xs"
      align="center"
      style={{ display: 'grid', gridTemplateColumns: 'auto 1fr', alignItems: 'start' }}
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

import React from 'react';
import {Group, Text} from '@mantine/core';
import LevelIcon from '../icons/LevelIcon';
import OsuLink from '../common/OsuLink';
import { ApiCheckResult } from '../../Types';

interface IssueRowProps {
  item: ApiCheckResult;
}

const IssueRow: React.FC<IssueRowProps> = ({ item }) => {
  return (
    <Group
      gap="xs"
      ml="xl"
      align="center"
      style={{ display: 'grid', gridTemplateColumns: 'auto 1fr', alignItems: 'start' }}
    >
      <div style={{ alignSelf: 'start' }}>
        <LevelIcon level={item.level} size={16} />
      </div>
      <Text
        size="sm"
        style={{
          whiteSpace: 'normal',
          overflowWrap: 'anywhere',
          wordBreak: 'break-word',
          display: 'block',
          minWidth: 0,
          width: '100%'
        }}
        title={item.message}
      >
        <OsuLink text={item.message} />
      </Text>
    </Group>
  );
};

export default IssueRow;

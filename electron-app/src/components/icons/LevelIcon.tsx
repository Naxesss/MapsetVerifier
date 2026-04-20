import { Text } from '@mantine/core';
import ErrorIcon from './ErrorIcon.tsx';
import MinorIcon from './MinorIcon.tsx';
import NoIssueIcon from './NoIssueIcon.tsx';
import ProblemIcon from './ProblemIcon.tsx';
import WarningIcon from './WarningIcon.tsx';
import { Level } from '../../Types.ts';

function LevelIcon({ level, size = 32 }: { level: Level; size?: number }) {
  const wrap = (node: React.ReactNode) => (
    <div
      style={{
        width: size,
        height: size,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      {node}
    </div>
  );
  if (level === 'Info') {
    return wrap(<NoIssueIcon size={size} />);
  } else if (level === 'Error') {
    return wrap(<ErrorIcon size={size} />);
  } else if (level === 'Problem') {
    return wrap(<ProblemIcon size={size} />);
  } else if (level === 'Warning') {
    return wrap(<WarningIcon size={size} />);
  } else if (level === 'Minor') {
    return wrap(<MinorIcon size={size} />);
  } else {
    return wrap(<Text size="xs">Missing</Text>);
  }
}

export default LevelIcon;

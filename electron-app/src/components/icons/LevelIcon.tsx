import { Loader, Text } from '@mantine/core';
import ErrorIcon from './ErrorIcon.tsx';
import InfoLevelIcon from './InfoLevelIcon.tsx';
import MinorIcon from './MinorIcon.tsx';
import NoIssueIcon from './NoIssueIcon.tsx';
import ProblemIcon from './ProblemIcon.tsx';
import WarningIcon from './WarningIcon.tsx';
import { Level } from '../../Types.ts';

function LevelIcon({
  level,
  size = 32,
  loading = false,
}: {
  level: Level;
  size?: number;
  loading?: boolean;
}) {
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

  if (loading) {
    return wrap(<Loader size="xs" color="blue" />);
  }

  switch (level) {
    case 'Check':
      return wrap(<NoIssueIcon size={size} />);
    case 'Info':
      return wrap(<InfoLevelIcon size={size} />);
    case 'Error':
      return wrap(<ErrorIcon size={size} />);
    case 'Problem':
      return wrap(<ProblemIcon size={size} />);
    case 'Warning':
      return wrap(<WarningIcon size={size} />);
    case 'Minor':
    case 'Negligible':
      return wrap(<MinorIcon size={size} />);
    default:
      return wrap(<Text size="xs">Missing</Text>);
  }
}

export default LevelIcon;

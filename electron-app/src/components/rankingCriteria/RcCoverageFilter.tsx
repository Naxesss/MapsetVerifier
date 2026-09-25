import { Button, Group, useMantineTheme } from '@mantine/core';
import {
  IconAlertTriangleFilled,
  IconCircleCheckFilled,
  IconCircleDashed,
  IconCircleHalf2,
  IconUser,
} from '@tabler/icons-react';
import { ReactNode } from 'react';
import { CoverageFilter, matchesCoverageFilter } from './rcUtils';
import { ApiRcStatement } from '../../Types';

const ICON_SIZE = 14;

interface RcCoverageFilterProps {
  /** Rules and guidelines to count, without intros. */
  rules: ApiRcStatement[];
  value: CoverageFilter;
  onChange: (value: CoverageFilter) => void;
}

/** Quick filter pills, one per coverage status, with the same icons as the statement rows. */
function RcCoverageFilter({ rules, value, onChange }: RcCoverageFilterProps) {
  const theme = useMantineTheme();

  const options: { value: CoverageFilter; label: string; color: string; icon?: ReactNode }[] = [
    { value: 'all', label: 'All', color: 'gray' },
    {
      value: 'covered',
      label: 'Covered',
      color: 'green',
      icon: <IconCircleCheckFilled size={ICON_SIZE} color={theme.colors.green[6]} />,
    },
    {
      value: 'partial',
      label: 'Partial',
      color: 'yellow',
      icon: <IconCircleHalf2 size={ICON_SIZE} color={theme.colors.yellow[6]} />,
    },
    {
      value: 'outdated',
      label: 'Outdated',
      color: 'red',
      icon: <IconAlertTriangleFilled size={ICON_SIZE} color={theme.colors.red[6]} />,
    },
    {
      value: 'uncovered',
      label: 'Not covered',
      color: 'gray',
      icon: <IconCircleDashed size={ICON_SIZE} color={theme.colors.gray[6]} />,
    },
    {
      value: 'manual',
      label: 'Manual',
      color: 'gray',
      icon: <IconUser size={ICON_SIZE} color={theme.colors.gray[6]} />,
    },
  ];

  return (
    <Group gap={6}>
      {options.map((option) => {
        const count = rules.filter((rule) =>
          matchesCoverageFilter(rule.coverage, option.value)
        ).length;
        const active = value === option.value;

        return (
          <Button
            key={option.value}
            size="compact-sm"
            radius="xl"
            variant={active ? 'light' : 'subtle'}
            color={active ? option.color : 'gray'}
            leftSection={option.icon}
            disabled={count === 0 && !active}
            aria-pressed={active}
            onClick={() => onChange(option.value)}
          >
            {option.label} {count}
          </Button>
        );
      })}
    </Group>
  );
}

export default RcCoverageFilter;

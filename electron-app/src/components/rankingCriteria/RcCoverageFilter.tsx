import { Group } from '@mantine/core';
import { ReactNode } from 'react';
import { COVERAGE_ORDER, COVERAGE_STATUSES, CoverageIcon } from './coverageStatus';
import { CoverageFilter, matchesCoverageFilter } from './rcUtils';
import { ApiRcStatement } from '../../Types';
import FilterChip from '../common/FilterChip';

const ICON_SIZE = 14;

interface RcCoverageFilterProps {
  /** Rules and guidelines to count, without intros. */
  rules: ApiRcStatement[];
  value: CoverageFilter;
  onChange: (value: CoverageFilter) => void;
}

/** Quick filter pills, one per coverage status, with the same icons as the statement rows. */
function RcCoverageFilter({ rules, value, onChange }: RcCoverageFilterProps) {
  const options: { value: CoverageFilter; label: string; color: string; icon?: ReactNode }[] = [
    { value: 'all', label: 'All', color: 'gray' },
    ...COVERAGE_ORDER.map((coverage) => ({
      value: COVERAGE_STATUSES[coverage].filter,
      label: COVERAGE_STATUSES[coverage].label,
      color: COVERAGE_STATUSES[coverage].color,
      icon: <CoverageIcon coverage={coverage} size={ICON_SIZE} />,
    })),
  ];

  return (
    <Group gap="xs">
      {options.map((option) => {
        const count = rules.filter((rule) =>
          matchesCoverageFilter(rule.coverage, option.value)
        ).length;
        const active = value === option.value;
        // A status nothing has is left out rather than shown disabled.
        if (count === 0 && !active && option.value !== 'all') return null;

        return (
          <FilterChip
            key={option.value}
            label={option.label}
            count={count}
            color={option.color}
            icon={option.icon}
            active={active}
            onClick={() => onChange(option.value)}
          />
        );
      })}
    </Group>
  );
}

export default RcCoverageFilter;

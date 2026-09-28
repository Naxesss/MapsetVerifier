import { useMantineTheme } from '@mantine/core';
import {
  IconAlertTriangleFilled,
  IconCircleCheckFilled,
  IconCircleDashed,
  IconCircleHalf2,
  IconUser,
  type Icon,
} from '@tabler/icons-react';
import type { CoverageFilter } from './rcUtils';
import type { RcCoverage } from '../../Types';

/** A coverage status a rule or guideline can have; informational statements have none. */
export type CoverageStatusId = Exclude<RcCoverage, 'Informational'>;

interface CoverageStatus {
  label: string;
  /** What the status means, for tooltips. */
  description: string;
  color: 'green' | 'yellow' | 'red' | 'gray';
  icon: Icon;
  /** The coverage filter that shows only this status. */
  filter: Exclude<CoverageFilter, 'all'>;
  /** Drawn striped in the coverage bar: grey like "Not covered", but not work left for a check. */
  striped?: boolean;
}

/** The one definition of each coverage status, used by the row icons, the filter and the bar. */
export const COVERAGE_STATUSES: Record<CoverageStatusId, CoverageStatus> = {
  Covered: {
    label: 'Covered',
    description: 'Covered by a check',
    color: 'green',
    icon: IconCircleCheckFilled,
    filter: 'covered',
  },
  Partial: {
    label: 'Partly covered',
    description: 'Partly covered by checks',
    color: 'yellow',
    icon: IconCircleHalf2,
    filter: 'partial',
  },
  Outdated: {
    label: 'Outdated',
    description: 'Changed on the wiki since its checks were reviewed',
    color: 'red',
    icon: IconAlertTriangleFilled,
    filter: 'outdated',
  },
  Uncovered: {
    label: 'Not covered',
    description: 'No check yet',
    color: 'gray',
    icon: IconCircleDashed,
    filter: 'uncovered',
  },
  Manual: {
    label: 'Manual',
    description: 'Needs human judgement',
    color: 'gray',
    icon: IconUser,
    filter: 'manual',
    striped: true,
  },
};

/** Statuses in the order the filter and the bar show them. */
export const COVERAGE_ORDER: CoverageStatusId[] = [
  'Covered',
  'Partial',
  'Outdated',
  'Uncovered',
  'Manual',
];

export function isCoverageStatus(coverage: RcCoverage): coverage is CoverageStatusId {
  return coverage in COVERAGE_STATUSES;
}

/** The icon of a coverage status, in its colour. */
export function CoverageIcon({ coverage, size }: { coverage: CoverageStatusId; size: number }) {
  const theme = useMantineTheme();
  const { icon: StatusIcon, color, label } = COVERAGE_STATUSES[coverage];

  return <StatusIcon size={size} color={theme.colors[color][6]} aria-label={label} />;
}

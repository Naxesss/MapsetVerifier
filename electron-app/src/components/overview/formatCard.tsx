import {
  Alert,
  Badge,
  Group,
  List,
  Stack,
  Text,
  ThemeIcon,
  Tooltip,
  type MantineSpacing,
} from '@mantine/core';
import { IconAlertTriangle, IconCheck, IconRulerMeasure, IconX } from '@tabler/icons-react';
import { MicroLabel } from '../common/Headings.tsx';

/*
 * Pieces shared by the Audio and Video tabs' Format cards: the format badge, the compliance badge,
 * field labels that flag a broken rule, the ranking requirements checklist and the issue list.
 */

/** Colour of the backend's format badge type. */
export function formatBadgeColor(badgeType: string): string {
  switch (badgeType) {
    case 'success':
      return 'green';
    case 'warning':
      return 'yellow';
    case 'error':
      return 'red';
    default:
      return 'gray';
  }
}

export function ComplianceBadge({ compliant }: { compliant: boolean }) {
  return (
    <Badge color={compliant ? 'green' : 'red'}>{compliant ? 'Compliant' : 'Non-compliant'}</Badge>
  );
}

/** A field label with a warning icon while the field breaks a rule, e.g. the wrong codec. */
export function RuleLabel({ label, brokenRule }: { label: string; brokenRule?: string }) {
  if (!brokenRule) return label;

  return (
    <Group component="span" gap="xs" align="center" wrap="nowrap">
      {label}
      <Tooltip label={brokenRule}>
        <IconAlertTriangle
          size={12}
          color="var(--mantine-color-red-5)"
          style={{ cursor: 'help' }}
          aria-label={brokenRule}
        />
      </Tooltip>
    </Group>
  );
}

export interface Requirement {
  label: string;
  met: boolean;
}

/** The ranking requirements a file is checked against, each ticked or crossed. */
export function RequirementsList({ requirements }: { requirements: Requirement[] }) {
  return (
    <Stack gap="xs" mb="md">
      <MicroLabel>Ranking requirements</MicroLabel>
      {requirements.map(({ label, met }) => (
        <Group key={label} gap="xs">
          <ThemeIcon size="xs" color={met ? 'green' : 'red'} variant="light">
            {met ? <IconCheck size={12} /> : <IconX size={12} />}
          </ThemeIcon>
          <Text size="xs" c={met ? 'green.4' : 'red.4'}>
            {label}
          </Text>
        </Group>
      ))}
    </Stack>
  );
}

/** Why a file is not compliant, as a list; nothing when it is. */
export function ComplianceIssueList({ issues, mb = 0 }: { issues: string[]; mb?: MantineSpacing }) {
  if (issues.length === 0) return null;

  return (
    <Stack gap="xs" mb={mb}>
      <Text size="sm" fw={500} c="red.4">
        Compliance issues
      </Text>
      <List
        size="sm"
        spacing="xs"
        icon={
          <ThemeIcon color="red" size="sm" variant="light">
            <IconX size={14} />
          </ThemeIcon>
        }
      >
        {issues.map((issue, index) => (
          <List.Item key={index}>{issue}</List.Item>
        ))}
      </List>
    </Stack>
  );
}

/** The tab-level summary of what keeps the file from being rankable; nothing when it is. */
export function ComplianceAlert({ issues }: { issues: string[] }) {
  if (issues.length === 0) return null;

  return (
    <Alert icon={<IconRulerMeasure />} color="yellow" title="Compliance issues">
      <List size="sm" spacing="xs">
        {issues.map((issue, index) => (
          <List.Item key={index}>{issue}</List.Item>
        ))}
      </List>
    </Alert>
  );
}

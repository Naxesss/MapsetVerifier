import { Stack, Text, type MantineColor } from '@mantine/core';
import { MicroLabel } from './Headings.tsx';
import SectionCard from './SectionCard.tsx';
import type { ReactNode } from 'react';

interface StatFieldProps {
  /** Short name of the value; may carry an icon, e.g. a warning. */
  label: ReactNode;
  /** Text is set as the value; any other content (a list, a grid) is shown as given. */
  value: ReactNode;
  /** A line under the value, e.g. a unit's source or why the value is a problem. */
  note?: ReactNode;
  noteColor?: MantineColor;
  /** Colour of a text value, e.g. red when it breaks a rule. */
  valueColor?: MantineColor;
  /** `lg` for the big number of a stat card. */
  size?: 'md' | 'lg';
}

/**
 * The one way to show a value under its label (format details, chart stats, metadata fields,
 * stat cards): an uppercase micro label, the value, and an optional note.
 */
export function StatField({
  label,
  value,
  note,
  noteColor = 'dimmed',
  valueColor,
  size = 'md',
}: StatFieldProps) {
  const isText = typeof value === 'string' || typeof value === 'number';

  return (
    <Stack gap="2xs" style={{ minWidth: 0 }}>
      <MicroLabel>{label}</MicroLabel>
      {isText ? (
        <Text
          fw={size === 'lg' ? 700 : 600}
          size={size === 'lg' ? 'lg' : 'md'}
          c={valueColor}
          style={{ overflowWrap: 'anywhere' }}
        >
          {value}
        </Text>
      ) : (
        value
      )}
      {note && (
        <Text size="xs" c={noteColor} truncate={size === 'lg'}>
          {note}
        </Text>
      )}
    </Stack>
  );
}

/** A {@link StatField} on its own card, for the big numbers at the top of an Overview tab. */
export function StatCard({ label, value, note }: Pick<StatFieldProps, 'label' | 'value' | 'note'>) {
  return (
    <SectionCard>
      <StatField label={label} value={value} note={note} size="lg" />
    </SectionCard>
  );
}

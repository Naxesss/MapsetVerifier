import { Group, Text } from '@mantine/core';
import { PALETTE } from './pairing';
import { formatClock } from '../describe';
import type { CSSProperties, ReactNode } from 'react';

/** The pictures scale with the space they get; their labels are kept out of them so text stays readable. */
export function PlotLabel({ children, mono }: { children: ReactNode; mono?: boolean }) {
  return (
    <Text
      size="xs"
      fw={mono ? 400 : 700}
      ff={mono ? 'monospace' : undefined}
      c={PALETTE.dim}
      style={{ whiteSpace: 'nowrap', lineHeight: 1 }}
    >
      {children}
    </Text>
  );
}

type Row = { label: string; y: number };

/**
 * Labels beside the rows of a picture, each at its row's height as a share of the picture's height,
 * so it stays level with its row at any size.
 */
function RowLabels({
  rows,
  top,
  height,
  mono,
  align,
}: {
  rows: Row[];
  top: number;
  height: number;
  mono?: boolean;
  align: 'left' | 'right';
}) {
  const hidden: CSSProperties = { visibility: 'hidden', height: 0, overflow: 'hidden' };

  return (
    <div style={{ position: 'relative' }}>
      {/* Invisible copies give the column the width of its widest label. */}
      {rows.map((row) => (
        <div key={`width-${row.label}`} style={hidden} aria-hidden>
          <PlotLabel mono={mono}>{row.label}</PlotLabel>
        </div>
      ))}
      {rows.map((row) => (
        <div
          key={row.label}
          style={{
            position: 'absolute',
            [align]: 0,
            top: `${((row.y - top) / height) * 100}%`,
            transform: 'translateY(-50%)',
          }}
        >
          <PlotLabel mono={mono}>{row.label}</PlotLabel>
        </div>
      ))}
    </div>
  );
}

/**
 * A picture with its row labels on the left and, given a time range, the times at its two ends
 * below it. `view` is the picture's viewBox top and height, which the rows' `y` are measured in.
 */
export function LabelledPlot({
  rows,
  view: [top, height],
  mono,
  start,
  end,
  children,
}: {
  rows: Row[];
  view: [number, number];
  /** The labels are values, like the ticks of an axis, instead of names. */
  mono?: boolean;
  start?: number;
  end?: number;
  children: ReactNode;
}) {
  return (
    <div
      style={{
        display: 'grid',
        gridTemplateColumns: 'auto minmax(0, 1fr)',
        columnGap: 8,
        rowGap: 4,
      }}
    >
      <RowLabels
        rows={rows}
        top={top}
        height={height}
        mono={mono}
        align={mono ? 'right' : 'left'}
      />
      {children}
      {start != null && end != null && (
        <>
          <div />
          <Group justify="space-between" wrap="nowrap">
            <PlotLabel mono>{formatClock(start)}</PlotLabel>
            <PlotLabel mono>{formatClock(end)}</PlotLabel>
          </Group>
        </>
      )}
    </div>
  );
}

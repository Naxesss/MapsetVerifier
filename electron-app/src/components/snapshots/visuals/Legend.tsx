import { Box, Group, Text } from '@mantine/core';
import { PALETTE } from './pairing';
import { SnapLegend } from './SnapGrid';

/** How a legend item is drawn: a small copy of what it stands for in the picture. */
export type LegendMark =
  | 'dot'
  | 'bigDot'
  | 'dashed'
  | 'cross'
  | 'ring'
  | 'ringDot'
  | 'block'
  | 'blockOutline'
  | 'blockDashed'
  | 'blockCross'
  | 'letter'
  | 'arrow'
  | 'line';

export type LegendItem = {
  mark: LegendMark;
  color: string;
  label: string;
  /** The letter shown, for a "letter" mark. */
  letter?: string;
};

const SIZE = 14;
const C = SIZE / 2;

function Mark({ item }: { item: LegendItem }) {
  const { mark, color } = item;

  return (
    <svg width={SIZE} height={SIZE} viewBox={`0 0 ${SIZE} ${SIZE}`} aria-hidden>
      {mark === 'dot' && <circle cx={C} cy={C} r={4.5} fill={color} />}
      {mark === 'bigDot' && <circle cx={C} cy={C} r={6.5} fill={color} />}
      {mark === 'dashed' && (
        <circle
          cx={C}
          cy={C}
          r={5}
          fill="none"
          stroke={color}
          strokeWidth={1.5}
          strokeDasharray="2.5 2"
        />
      )}
      {mark === 'cross' && (
        <>
          <circle
            cx={C}
            cy={C}
            r={5}
            fill="none"
            stroke={color}
            strokeWidth={1.5}
            strokeDasharray="2.5 2"
          />
          <path d="M4 4 L10 10 M10 4 L4 10" stroke={color} strokeWidth={1.5} />
        </>
      )}
      {mark === 'ring' && <circle cx={C} cy={C} r={5} fill="none" stroke={color} strokeWidth={2} />}
      {mark === 'ringDot' && (
        <>
          <circle cx={C} cy={C} r={5} fill="none" stroke={color} strokeWidth={2} />
          <circle cx={C} cy={C} r={2} fill={color} />
        </>
      )}
      {mark === 'block' && <rect x={2} y={3} width={10} height={8} rx={2} fill={color} />}
      {mark === 'blockOutline' && (
        <rect
          x={2}
          y={3}
          width={10}
          height={8}
          rx={2}
          fill="none"
          stroke={color}
          strokeWidth={1.5}
        />
      )}
      {mark === 'blockDashed' && (
        <rect
          x={2}
          y={3}
          width={10}
          height={8}
          rx={2}
          fill="none"
          stroke={color}
          strokeWidth={1.5}
          strokeDasharray="2.5 2"
        />
      )}
      {mark === 'blockCross' && (
        <>
          <rect
            x={2}
            y={3}
            width={10}
            height={8}
            rx={2}
            fill="none"
            stroke={color}
            strokeWidth={1.5}
          />
          <path d="M4 10 L10 4" stroke={color} strokeWidth={1.25} />
        </>
      )}
      {mark === 'letter' && (
        <>
          <rect x={1} y={1} width={12} height={12} rx={3} fill={color} />
          <text
            x={C}
            y={C + 3.5}
            textAnchor="middle"
            fontSize={9}
            fontWeight={800}
            fill={PALETTE.ink}
          >
            {item.letter}
          </text>
        </>
      )}
      {mark === 'line' && (
        <>
          <path d="M3.5 1 h7 l-3.5 4 z" fill={color} />
          <path d="M7 5 V13" stroke={color} strokeWidth={1.5} strokeDasharray="2.5 2" />
        </>
      )}
      {mark === 'arrow' && (
        <path
          d="M7 2 V11 M3.5 7.5 L7 11 L10.5 7.5"
          fill="none"
          stroke={color}
          strokeWidth={1.75}
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      )}
    </svg>
  );
}

/**
 * What the marks in a picture mean, one small mark and a word each, followed by the snap
 * colours of the grid when the picture has one.
 */
export default function Legend({ items, divisors }: { items: LegendItem[]; divisors?: number[] }) {
  return (
    <Group gap="md" wrap="wrap">
      <Group gap="sm" wrap="wrap">
        {items.map((item) => (
          <Group key={item.label} gap={4} wrap="nowrap">
            <Box style={{ display: 'flex' }}>
              <Mark item={item} />
            </Box>
            <Text size="xs" c="dimmed">
              {item.label}
            </Text>
          </Group>
        ))}
      </Group>
      {divisors && divisors.length > 0 && <SnapLegend divisors={divisors} />}
    </Group>
  );
}

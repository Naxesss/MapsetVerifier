import { Stack, Text, Tooltip } from '@mantine/core';
import { formatClock } from '../describe';
import { useObjectLink } from './useObjectLink';
import type { PairedObject } from './pairing';
import type { ReactElement } from 'react';

const HEADLINE = {
  added: 'added',
  removed: 'removed',
  changed: 'changed',
  same: '',
} as const;

/**
 * Hovering an object in a picture says what happened to it; untouched objects say nothing. Clicking
 * any object opens it in osu!.
 */
export default function PairTip({
  pair,
  children,
}: {
  pair: PairedObject;
  children: ReactElement;
}) {
  const link = useObjectLink();
  const object = (pair.after ?? pair.before)!;
  const lines =
    pair.status === 'changed'
      ? pair.details
      : pair.status === 'same'
        ? []
        : [`This ${object.type.toLowerCase()} was ${HEADLINE[pair.status]}`];

  const linked = <g {...link(pair)}>{children}</g>;

  if (lines.length === 0) return linked;

  return (
    <Tooltip
      withinPortal
      multiline
      position="top"
      openDelay={80}
      label={
        <Stack gap={2}>
          <Text size="xs" fw={700}>
            {object.type} at {formatClock(object.time)}
          </Text>
          {lines.map((line) => (
            <Text key={line} size="xs">
              {line}
            </Text>
          ))}
          <Text size="xs" c="dimmed">
            Click to open in osu!, ctrl+click to copy
          </Text>
        </Stack>
      }
    >
      {linked}
    </Tooltip>
  );
}

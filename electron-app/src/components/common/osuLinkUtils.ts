import type { MantineTheme } from '@mantine/core';
import type { MouseEvent } from 'react';

// Matches patterns like 00:04:714 -, 00:04:714 (1,2) -, or -12.5 -
const TIMESTAMP_PATTERN = /((\d{2}:\d{2}:\d{3})(?: \([^)]+\))?|(-\d+(?:\.\d+)?)) -/g;

export type OsuLinkSegment =
  | { kind: 'text'; value: string }
  | { kind: 'timestamp'; value: string; clickable: boolean };

export function buildOsuEditHref(timestamp: string) {
  return `osu://edit/${timestamp.replace(/ /g, '%20')}`;
}

export function isCopyModifierClick(event: MouseEvent) {
  return event.button === 0 && (event.ctrlKey || event.metaKey);
}

export function getTimestampChipStyles(theme: MantineTheme) {
  const baseBg = theme.colors.dark?.[5];

  return {
    baseBg,
    hoverBg: theme.colors.dark?.[4],
    textColor: theme.colors.blue?.[4],
    chip: {
      fontFamily: theme.fontFamilyMonospace,
      padding: `0 ${theme.spacing.xs}`,
      borderRadius: theme.radius.sm,
      backgroundColor: baseBg,
    },
  };
}

export function parseOsuLinkSegments(text: string): OsuLinkSegment[] {
  const segments: OsuLinkSegment[] = [];
  const regex = new RegExp(TIMESTAMP_PATTERN.source, 'g');
  let lastIndex = 0;
  let match: RegExpExecArray | null;

  while ((match = regex.exec(text)) !== null) {
    const start = match.index;
    const displayTimestamp = match[1];
    const positiveTimestamp = match[2];

    if (start > lastIndex) {
      segments.push({ kind: 'text', value: text.slice(lastIndex, start) });
    }

    segments.push({
      kind: 'timestamp',
      value: displayTimestamp,
      clickable: Boolean(positiveTimestamp),
    });

    lastIndex = start + match[0].length;
  }

  if (lastIndex < text.length) {
    segments.push({ kind: 'text', value: text.slice(lastIndex) });
  }

  return segments;
}

import { Anchor, Text, useMantineTheme } from '@mantine/core';
import React from 'react';

// Matches patterns like 00:04:714 -, 00:04:714 (1,2) -, or -12.5 -
const TIMESTAMP_REGEX = /((\d{2}:\d{2}:\d{3})(?: \([^)]+\))?|(-\d+(?:\.\d+)?)) -/g;

function buildOsuEditHref(timestamp: string) {
  return `osu://edit/${timestamp.replace(/ /g, '%20')}`;
}

interface OsuLinkProps {
  text: string;
  /** When true, omit the ` -` printed after each timestamp link (issue copy keeps it; table cells often don’t). */
  disableSeparators?: boolean;
}

const OsuLink: React.FC<OsuLinkProps> = ({ text, disableSeparators = false }) => {
  const theme = useMantineTheme();
  const baseBg = theme.colors.dark?.[5];
  const hoverBg = theme.colors.dark?.[4];
  const textColor = theme.colors.blue?.[4];

  const nodes: React.ReactNode[] = [];
  let lastIndex = 0;
  let match: RegExpExecArray | null;

  while ((match = TIMESTAMP_REGEX.exec(text)) !== null) {
    const fullMatch = match[0];
    const displayTimestamp = match[1];
    const positiveTimestamp = match[2];
    const isClickableTimestamp = Boolean(positiveTimestamp);
    const start = match.index;

    if (start > lastIndex) nodes.push(text.slice(lastIndex, start));

    nodes.push(
      <React.Fragment key={`osu-link-${start}`}>
        {isClickableTimestamp ? (
          <Anchor
            href={buildOsuEditHref(displayTimestamp)}
            underline="never"
            aria-label={`Edit at ${displayTimestamp}`}
            style={{
              fontFamily: theme.fontFamilyMonospace,
              padding: `0 ${theme.spacing.xs}`,
              borderRadius: theme.radius.sm,
              backgroundColor: baseBg,
              color: textColor,
              textDecoration: 'none',
              cursor: 'pointer',
              transition: 'background-color 120ms, box-shadow 120ms',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.backgroundColor = hoverBg;
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.backgroundColor = baseBg;
            }}
            onFocus={(e) => {
              e.currentTarget.style.boxShadow = `0 0 0 2px ${theme.colors.indigo[5]}`;
            }}
            onBlur={(e) => {
              e.currentTarget.style.boxShadow = 'none';
            }}
          >
            {displayTimestamp}
          </Anchor>
        ) : (
          <Text
            component="span"
            style={{
              fontFamily: theme.fontFamilyMonospace,
              padding: `0 ${theme.spacing.xs}`,
              borderRadius: theme.radius.sm,
              backgroundColor: baseBg,
            }}
          >
            {displayTimestamp}
          </Text>
        )}
        {!disableSeparators ? ' -' : null}
      </React.Fragment>
    );

    lastIndex = start + fullMatch.length;
  }

  if (lastIndex < text.length) nodes.push(text.slice(lastIndex));

  return <>{nodes}</>;
};

export default OsuLink;

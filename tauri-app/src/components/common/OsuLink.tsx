import React from 'react';
import { Anchor, useMantineTheme } from '@mantine/core';

// Matches patterns like 00:04:714 - or 00:04:714 (1,2) -
const TIMESTAMP_REGEX = /(\d{2}:\d{2}:\d{3})(?: \([^)]+\))? -/g;

interface OsuLinkProps { text: string }

export const OsuLink: React.FC<OsuLinkProps> = ({ text }) => {
  const theme = useMantineTheme();
  const baseBg = theme.colors.dark?.[5];
  const hoverBg = theme.colors.dark?.[4];
  const textColor = theme.colors.blue?.[4];

  const nodes: React.ReactNode[] = [];
  let lastIndex = 0;
  let match: RegExpExecArray | null;

  while ((match = TIMESTAMP_REGEX.exec(text)) !== null) {
    const fullMatch = match[0];
    const timestamp = match[1];
    const start = match.index;

    if (start > lastIndex) nodes.push(text.slice(lastIndex, start));

    nodes.push(
      <Anchor
        key={`osu-link-${start}`}
        href={`osu://edit/${timestamp}`}
        underline="never"
        aria-label={`Edit at ${timestamp}`}
        style={{
          fontFamily: theme.fontFamilyMonospace,
          padding: `0 ${theme.spacing.xs}`,
          borderRadius: theme.radius.sm,
          backgroundColor: baseBg,
          color: textColor,
          fontWeight: 600,
          textDecoration: 'none',
          cursor: 'pointer',
          transition: 'background-color 120ms, box-shadow 120ms'
        }}
        onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = hoverBg; }}
        onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = baseBg; }}
        onFocus={(e) => { e.currentTarget.style.boxShadow = `0 0 0 2px ${theme.colors.indigo[5]}`; }}
        onBlur={(e) => { e.currentTarget.style.boxShadow = 'none'; }}
      >
        {fullMatch}
      </Anchor>
    );

    lastIndex = start + fullMatch.length;
  }

  if (lastIndex < text.length) nodes.push(text.slice(lastIndex));

  return <>{nodes}</>;
};

export default OsuLink;

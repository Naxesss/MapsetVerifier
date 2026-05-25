import { Anchor, Box, Text, useMantineTheme } from '@mantine/core';
import { IconCopy } from '@tabler/icons-react';
import React from 'react';
import { buildOsuEditHref, getTimestampChipStyles, isCopyModifierClick } from './osuLinkUtils.ts';
import { useFadeUpCopyFeedback } from './useFadeUpCopyFeedback.ts';

interface TimestampLinkProps {
  displayTimestamp: string;
}

const TimestampLink: React.FC<TimestampLinkProps> = ({ displayTimestamp }) => {
  const theme = useMantineTheme();
  const { baseBg, hoverBg, textColor, chip } = getTimestampChipStyles(theme);
  const { showCopied, copiedAnimating, triggerCopyFeedback } = useFadeUpCopyFeedback();

  const handleClick = async (event: React.MouseEvent<HTMLAnchorElement>) => {
    if (!isCopyModifierClick(event)) return;

    event.preventDefault();
    event.stopPropagation();

    try {
      await navigator.clipboard.writeText(displayTimestamp);
      triggerCopyFeedback();
    } catch {
      // Clipboard may be unavailable; ignore.
    }
  };

  return (
    <Box component="span" style={{ position: 'relative', display: 'inline' }}>
      <Anchor
        href={buildOsuEditHref(displayTimestamp)}
        underline="never"
        aria-label={`Edit at ${displayTimestamp}`}
        style={{
          ...chip,
          color: textColor,
          textDecoration: 'none',
          cursor: 'pointer',
          transition: 'background-color 120ms, box-shadow 120ms',
        }}
        onClick={handleClick}
        onMouseEnter={(e) => {
          e.currentTarget.style.backgroundColor = hoverBg ?? '';
        }}
        onMouseLeave={(e) => {
          e.currentTarget.style.backgroundColor = baseBg ?? '';
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
      {showCopied ? (
        <Text
          component="span"
          aria-live="polite"
          style={{
            position: 'absolute',
            left: '50%',
            bottom: 'calc(100% + 6px)',
            transform: `translate(-50%, ${copiedAnimating ? -10 : 0}px)`,
            opacity: copiedAnimating ? 0 : 1,
            transition: 'opacity 600ms ease-out, transform 600ms ease-out',
            fontSize: theme.fontSizes.xs,
            lineHeight: 1.25,
            fontWeight: 600,
            letterSpacing: '0.02em',
            color: theme.colors.green[4],
            backgroundColor: theme.colors.dark[7],
            padding: `${theme.spacing.xs} ${theme.spacing.sm}`,
            borderRadius: theme.radius.md,
            border: `1px solid ${theme.colors.dark[4]}`,
            boxShadow: theme.shadows.sm,
            pointerEvents: 'none',
            whiteSpace: 'nowrap',
            zIndex: 10,
            userSelect: 'none',
            display: 'inline-flex',
            alignItems: 'center',
            gap: 4,
          }}
        >
          <IconCopy size={12} stroke={2.25} aria-hidden />
          copied!
        </Text>
      ) : null}
    </Box>
  );
};

export default TimestampLink;

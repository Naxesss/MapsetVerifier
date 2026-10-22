import { Anchor, Box, useMantineTheme } from '@mantine/core';
import { IconCheck } from '@tabler/icons-react';
import React from 'react';
import {
  buildOsuEditHref,
  getTimestampChipStyles,
  isCopyModifierClick,
  shouldInterceptOsuOpen,
} from './osuLinkUtils.ts';
import { useFadeUpCopyFeedback } from './useFadeUpCopyFeedback.ts';
import { useOpenOsuTimestamp } from '../../hooks/useOpenOsuTimestamp.ts';
import { notifyError } from '../../utils/notify.tsx';

interface TimestampLinkProps {
  displayTimestamp: string;
}

const TimestampLink: React.FC<TimestampLinkProps> = ({ displayTimestamp }) => {
  const theme = useMantineTheme();
  const { baseBg, hoverBg, textColor, chip } = getTimestampChipStyles(theme);
  const { showCopied, copiedAnimating, triggerCopyFeedback } = useFadeUpCopyFeedback();
  const openOsuTimestamp = useOpenOsuTimestamp();

  const handleClick = async (event: React.MouseEvent<HTMLAnchorElement>) => {
    if (isCopyModifierClick(event)) {
      event.preventDefault();
      event.stopPropagation();

      try {
        await navigator.clipboard.writeText(displayTimestamp);
        triggerCopyFeedback();
      } catch {
        notifyError('Clipboard is unavailable.');
      }
      return;
    }

    if (!shouldInterceptOsuOpen(event)) return;

    event.preventDefault();
    await openOsuTimestamp(displayTimestamp);
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
        onAuxClick={handleClick}
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
        <span
          className="mv-copy-bubble"
          aria-live="polite"
          data-leaving={copiedAnimating || undefined}
        >
          <IconCheck size={14} aria-hidden />
          Copied
        </span>
      ) : null}
    </Box>
  );
};

export default TimestampLink;

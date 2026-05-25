import { Text, useMantineTheme } from '@mantine/core';
import React from 'react';
import { getTimestampChipStyles, parseOsuLinkSegments } from './osuLinkUtils.ts';
import TimestampLink from './TimestampLink.tsx';

interface OsuLinkProps {
  text: string;
  /** When true, omit the ` -` printed after each timestamp link (issue copy keeps it; table cells often don’t). */
  disableSeparators?: boolean;
}

const OsuLink: React.FC<OsuLinkProps> = ({ text, disableSeparators = false }) => {
  const theme = useMantineTheme();
  const { chip } = getTimestampChipStyles(theme);
  const segments = parseOsuLinkSegments(text);

  return (
    <>
      {segments.map((segment, index) => {
        if (segment.kind === 'text') {
          return <React.Fragment key={`text-${index}`}>{segment.value}</React.Fragment>;
        }

        return (
          <React.Fragment key={`timestamp-${index}`}>
            {segment.clickable ? (
              <TimestampLink displayTimestamp={segment.value} />
            ) : (
              <Text component="span" style={chip}>
                {segment.value}
              </Text>
            )}
            {!disableSeparators ? ' -' : null}
          </React.Fragment>
        );
      })}
    </>
  );
};

export default OsuLink;

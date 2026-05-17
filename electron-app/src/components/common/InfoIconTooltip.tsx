import { Box, Tooltip, type TooltipProps } from '@mantine/core';
import { IconInfoCircle } from '@tabler/icons-react';

export type InfoIconTooltipProps = Omit<TooltipProps, 'children'> & {
  /** @default 16 */
  iconSize?: number;
  /** Passed to `IconInfoCircle` `color` (CSS color string). */
  iconColor?: string;
};

/** Info circle with `cursor: help` and a Mantine {@link Tooltip}; forwards extra props to `Tooltip`. */
export function InfoIconTooltip({
  iconSize = 16,
  iconColor = 'var(--mantine-color-gray-6)',
  ...tooltipProps
}: InfoIconTooltipProps) {
  return (
    <Tooltip {...tooltipProps}>
      <Box
        component="span"
        style={{ display: 'inline-flex', alignItems: 'center', cursor: 'help' }}
      >
        <IconInfoCircle size={iconSize} color={iconColor} />
      </Box>
    </Tooltip>
  );
}

import { useMantineTheme } from '@mantine/core';

// Tabler doesn't have this specific icon...
const CIRCLE_MINUS_FILLED_PATH =
  'M4.929 4.929a10 10 0 1 1 14.141 14.141a10 10 0 0 1 -14.14 -14.14 M8 11h8a1 1 0 0 1 0 2H8a1 1 0 0 1 0-2z';

export default function SnapshotNoChangesIcon({ size = 32 }: { size?: number }) {
  const theme = useMantineTheme();

  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill={theme.colors.gray[5]}
      fillRule="evenodd"
      clipRule="evenodd"
      stroke="none"
      aria-hidden
    >
      <path d={CIRCLE_MINUS_FILLED_PATH} />
    </svg>
  );
}

import { Box, Flex, Group, Skeleton, Stack } from '@mantine/core';
import { StatusSegmentBar } from '../common/DifficultyPicker';
import SectionCard from '../common/SectionCard.tsx';

/** Width of the history list; enough for the time, its badges and the change counts. */
export const HISTORY_WIDTH = 300;

/** The header's difficulty picker: General, the picked difficulty and the segments under them. */
export function PickerSkeleton() {
  return (
    <Box aria-busy aria-label="Loading">
      <Group gap="xs" wrap="nowrap">
        <Skeleton height={36} width={104} />
        <Skeleton height={36} width={220} />
      </Group>
      <StatusSegmentBar>
        {Array.from({ length: 6 }).map((_, i) => (
          <Skeleton key={i} height={6} radius={2} style={{ flex: 1 }} />
        ))}
      </StatusSegmentBar>
    </Box>
  );
}

/** The history toggle and the compare bar's preset, "from" and "to" pickers. */
function ToolbarSkeleton() {
  return (
    <Group gap="xs" wrap="nowrap" align="center">
      <Skeleton height={28} width={28} />
      <Group gap="xs" wrap="wrap" align="center">
        <Skeleton height={30} width={150} />
        <Skeleton height={10} width={24} />
        <Skeleton height={30} width={170} />
        <Skeleton height={10} width={14} />
        <Skeleton height={30} width={170} />
      </Group>
    </Group>
  );
}

/** Laid out like a SnapshotHistoryList row: the mark, the time, its trigger and the counts. */
function HistoryRowSkeleton({ detail }: { detail?: boolean }) {
  return (
    <Box pos="relative" style={{ padding: '5px 30px 5px 34px' }}>
      <Skeleton
        circle
        height={8}
        pos="absolute"
        left={11}
        top="50%"
        style={{ transform: 'translateY(-50%)' }}
      />
      <Group gap={7} wrap="nowrap" h={20}>
        <Skeleton height={12} width={56} />
        <Skeleton height={12} width={14} />
        <Box style={{ flex: 1 }} />
        <Skeleton height={10} width={36} />
      </Group>
      {detail && (
        <Group h={18} mt={2}>
          <Skeleton height={8} width="55%" />
        </Group>
      )}
    </Box>
  );
}

function HistorySkeleton() {
  const days = [
    [true, false, true],
    [false, true],
  ];

  return (
    <SectionCard
      title="History"
      actions={<Skeleton height={18} width={26} radius="xl" />}
      w={{ base: '100%', md: HISTORY_WIDTH }}
      style={{ flexShrink: 0 }}
    >
      <Stack gap={3} p="2xs">
        {days.map((rows, day) => (
          <Stack key={day} gap={3}>
            <Box pl={34} pt={day === 0 ? 0 : 'xs'} pb="2xs">
              <Skeleton height={8} width={64} />
            </Box>
            {rows.map((detail, i) => (
              <HistoryRowSkeleton key={i} detail={detail} />
            ))}
          </Stack>
        ))}
      </Stack>
    </SectionCard>
  );
}

/** Laid out like a closed HunkRow: the toggle, the kind's icon, what changed and its counts. */
function HunkRowSkeleton({ headline }: { headline: string }) {
  return (
    <Group
      gap="sm"
      wrap="nowrap"
      align="flex-start"
      p="sm"
      bg="dark.6"
      style={{ borderRadius: 'var(--mantine-radius-md)' }}
    >
      <Skeleton height={16} width={16} mt={2} />
      <Skeleton height={20} width={20} />
      <Stack gap={8} py={4} style={{ flex: 1, minWidth: 0 }}>
        <Skeleton height={12} width={headline} />
        <Skeleton height={8} width="22%" />
      </Stack>
      <Skeleton height={10} width={40} mt={5} />
    </Group>
  );
}

/** The selected-difficulty row over a card of changes, while a comparison loads. */
export function ChangesSkeleton() {
  return (
    <Stack gap="md" w="100%" style={{ flex: 1, minWidth: 0 }} aria-busy aria-label="Loading">
      <Group gap="xs" wrap="nowrap" mih="var(--mv-control-height)">
        <Skeleton circle height={32} />
        <Skeleton circle height={32} />
        <Skeleton height={14} width={180} />
      </Group>
      <SectionCard
        title={<Skeleton height={14} width={90} />}
        actions={<Skeleton height={10} width={60} />}
      >
        <Stack gap="xs">
          <Skeleton height={8} width={90} my={2} />
          {['38%', '52%', '30%', '45%'].map((headline, i) => (
            <HunkRowSkeleton key={i} headline={headline} />
          ))}
        </Stack>
      </SectionCard>
    </Stack>
  );
}

/** The whole page below the header, padded like it, with the history only if it is shown. */
export default function SnapshotsSkeleton({ historyOpen }: { historyOpen: boolean }) {
  return (
    <Flex
      gap="md"
      px="md"
      pb="md"
      pt="sm"
      direction="column"
      style={{ flex: 1, minWidth: 0 }}
      bg="dark.6"
      aria-busy
      aria-label="Loading"
    >
      <ToolbarSkeleton />
      <Flex gap="md" align="flex-start" direction={{ base: 'column', md: 'row' }}>
        {historyOpen && <HistorySkeleton />}
        <ChangesSkeleton />
      </Flex>
    </Flex>
  );
}

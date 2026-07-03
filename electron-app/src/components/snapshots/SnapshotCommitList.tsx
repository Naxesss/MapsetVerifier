import {
  Box,
  Group,
  Stack,
  Text,
  UnstyledButton,
  Badge,
  useMantineTheme,
  Flex,
  ScrollArea,
  ActionIcon,
  Tooltip,
} from '@mantine/core';
import {
  IconGitCommit,
  IconPlus,
  IconMinus,
  IconArrowsExchange,
  IconChevronLeft,
  IconChevronRight,
} from '@tabler/icons-react';
import { useVirtualizer } from '@tanstack/react-virtual';
import { useEffect, useRef } from 'react';
import { ApiSnapshotCommit } from '../../Types';

interface SnapshotCommitListProps {
  commits: ApiSnapshotCommit[];
  selectedCommitId?: string;
  onSelectCommit: (commitId: string) => void;
}

function formatDate(dateString: string): string {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffDays === 0) {
    return 'Today';
  } else if (diffDays === 1) {
    return 'Yesterday';
  } else if (diffDays < 7) {
    return `${diffDays} days ago`;
  } else {
    return date.toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }
}

function formatTime(dateString: string): string {
  const date = new Date(dateString);
  return date.toLocaleTimeString(undefined, {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
}

function SnapshotCommitList({
  commits,
  selectedCommitId,
  onSelectCommit,
}: SnapshotCommitListProps) {
  const theme = useMantineTheme();
  const viewportRef = useRef<HTMLDivElement>(null);

  // Find the current index of the selected commit
  const currentIndex = selectedCommitId
    ? commits.findIndex((commit) => commit.id === selectedCommitId)
    : -1;
  const commitVirtualizer = useVirtualizer({
    count: commits.length,
    estimateSize: () => 153,
    getItemKey: (index) => commits[index]?.id ?? index,
    getScrollElement: () => viewportRef.current,
    horizontal: true,
    overscan: 4,
  });

  // Scroll to selected commit when it changes
  useEffect(() => {
    if (currentIndex < 0) return;

    commitVirtualizer.scrollToIndex(currentIndex, {
      align: 'center',
      behavior: 'smooth',
    });
  }, [commitVirtualizer, currentIndex]);

  const isPreviousDisabled = currentIndex <= 0;
  const isNextDisabled = currentIndex === -1 || currentIndex >= commits.length - 1;

  const handlePrevious = () => {
    if (!isPreviousDisabled) {
      onSelectCommit(commits[currentIndex - 1].id);
    }
  };

  const handleNext = () => {
    if (!isNextDisabled) {
      onSelectCommit(commits[currentIndex + 1].id);
    }
  };

  if (commits.length === 0) {
    return (
      <Text c="dimmed" size="sm" ta="center" py="md">
        No snapshot history available.
      </Text>
    );
  }

  return (
    <Flex gap="xs" align="stretch">
      <ActionIcon
        variant="default"
        onClick={handlePrevious}
        disabled={isPreviousDisabled}
        style={{
          alignSelf: 'stretch',
          height: 'auto',
          borderRadius: theme.radius.md,
        }}
      >
        <IconChevronLeft size={20} />
      </ActionIcon>
      <ScrollArea
        p="xs"
        bg={theme.colors.dark[8]}
        style={{ borderRadius: theme.radius.md, flex: 1 }}
        viewportRef={viewportRef}
      >
        <Box
          style={{
            height: 78,
            position: 'relative',
            width: `${commitVirtualizer.getTotalSize()}px`,
          }}
        >
          {commitVirtualizer.getVirtualItems().map((virtualCommit) => {
            const commit = commits[virtualCommit.index];
            if (!commit) return null;

            const index = virtualCommit.index;
            const isSelected = selectedCommitId === commit.id;
            const isFirst = index === 0;

            return (
              <Box
                key={virtualCommit.key}
                data-index={virtualCommit.index}
                ref={commitVirtualizer.measureElement}
                style={{
                  left: 0,
                  paddingRight: 'var(--mantine-spacing-xs)',
                  position: 'absolute',
                  top: 0,
                  transform: `translateX(${virtualCommit.start}px)`,
                }}
              >
                <UnstyledButton
                  onClick={() => onSelectCommit(commit.id)}
                  style={{
                    borderTop: `2px solid ${isSelected ? theme.colors.blue[6] : theme.colors.dark[5]}`,
                    backgroundColor: isSelected ? theme.colors.dark[6] : theme.colors.dark[7],
                    transition: 'all 0.15s ease',
                    borderRadius: theme.radius.sm,
                    width: '145px',
                  }}
                >
                  <Box px="xs" py={6}>
                    <Stack gap="sm">
                      <Group gap={6} wrap="nowrap" align="center">
                        <Tooltip label="Latest Snapshot" disabled={!isFirst}>
                          <Box style={{ display: 'flex', flexShrink: 0 }}>
                            <IconGitCommit
                              size={15}
                              color={isFirst ? theme.colors.green[5] : theme.colors.dark[3]}
                              stroke={isFirst ? 3 : 2}
                            />
                          </Box>
                        </Tooltip>
                        <Group
                          gap={4}
                          wrap="nowrap"
                          align="baseline"
                          style={{ minWidth: 0, flex: 1 }}
                        >
                          <Text size="xs" fw={500} lh={1.1} truncate>
                            {formatDate(commit.date)}
                          </Text>
                          <Text c="dimmed" fz="10px" lh={1.1} style={{ flexShrink: 0 }}>
                            {formatTime(commit.date)}
                          </Text>
                        </Group>
                      </Group>
                      <Group gap={4} wrap="nowrap">
                        {commit.totalChanges === 0 && (
                          <Badge size="xs" variant="light" color="gray">
                            No changes
                          </Badge>
                        )}
                        {commit.additions > 0 && (
                          <Badge
                            size="xs"
                            variant="light"
                            color="green"
                            leftSection={<IconPlus size={8} />}
                          >
                            {commit.additions}
                          </Badge>
                        )}
                        {commit.removals > 0 && (
                          <Badge
                            size="xs"
                            variant="light"
                            color="red"
                            leftSection={<IconMinus size={8} />}
                          >
                            {commit.removals}
                          </Badge>
                        )}
                        {commit.modifications > 0 && (
                          <Badge
                            size="xs"
                            variant="light"
                            color="yellow"
                            leftSection={<IconArrowsExchange size={8} />}
                          >
                            {commit.modifications}
                          </Badge>
                        )}
                      </Group>
                    </Stack>
                  </Box>
                </UnstyledButton>
              </Box>
            );
          })}
        </Box>
      </ScrollArea>
      <ActionIcon
        variant="default"
        onClick={handleNext}
        disabled={isNextDisabled}
        style={{
          alignSelf: 'stretch',
          height: 'auto',
          borderRadius: theme.radius.md,
        }}
      >
        <IconChevronRight size={20} />
      </ActionIcon>
    </Flex>
  );
}

export default SnapshotCommitList;

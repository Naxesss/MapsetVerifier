import {Box, Group, Stack, Text, UnstyledButton, Badge, useMantineTheme, Flex, ScrollArea, ActionIcon} from '@mantine/core';
import { IconGitCommit, IconPlus, IconMinus, IconArrowsExchange, IconChevronLeft, IconChevronRight } from '@tabler/icons-react';
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
  return date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit', second: '2-digit' });
}

function SnapshotCommitList({ commits, selectedCommitId, onSelectCommit }: SnapshotCommitListProps) {
  const theme = useMantineTheme();
  const viewportRef = useRef<HTMLDivElement>(null);
  const commitRefs = useRef<Map<string, HTMLButtonElement>>(new Map());

  // Scroll to selected commit when it changes
  useEffect(() => {
    if (!selectedCommitId || !viewportRef.current) return;

    const selectedElement = commitRefs.current.get(selectedCommitId);
    if (!selectedElement) return;

    const viewport = viewportRef.current;

    // Check if viewport has overflow (scrollbar exists)
    const hasOverflow = viewport.scrollWidth > viewport.clientWidth;
    if (!hasOverflow) return;

    // Calculate the position to scroll to center the selected commit
    const viewportRect = viewport.getBoundingClientRect();
    const elementRect = selectedElement.getBoundingClientRect();

    // Calculate the scroll position to center the element
    const scrollLeft =
      viewport.scrollLeft +
      elementRect.left -
      viewportRect.left -
      (viewportRect.width / 2) +
      (elementRect.width / 2);

    // Smooth scroll to the calculated position
    viewport.scrollTo({
      left: scrollLeft,
      behavior: 'smooth',
    });
  }, [selectedCommitId]);

  // Find the current index of the selected commit
  const currentIndex = selectedCommitId
    ? commits.findIndex((commit) => commit.id === selectedCommitId)
    : -1;

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
        p="sm"
        bg={theme.colors.dark[8]}
        style={{ borderRadius: theme.radius.md, flex: 1 }}
        viewportRef={viewportRef}
      >
        <Flex
          gap="xs"
          wrap="nowrap"
        >
        {commits.map((commit, index) => {
          const isSelected = selectedCommitId === commit.id;
          const isFirst = index === 0;
  
          return (
            <UnstyledButton
              key={commit.id}
              ref={(el) => {
                if (el) {
                  commitRefs.current.set(commit.id, el);
                } else {
                  commitRefs.current.delete(commit.id);
                }
              }}
              onClick={() => onSelectCommit(commit.id)}
              style={{
                borderTop: `3px solid ${isSelected ? theme.colors.blue[6] : theme.colors.dark[5]}`,
                backgroundColor: isSelected ? theme.colors.dark[6] : theme.colors.dark[7],
                transition: 'all 0.15s ease',
                borderRadius: theme.radius.sm,
                minWidth: '175px',
                flexShrink: 0,
              }}
            >
              <Box p="sm">
                <Stack gap="xs">
                  <Group gap="xs" wrap="nowrap">
                    <IconGitCommit
                      size={18}
                      color={isFirst ? theme.colors.green[5] : theme.colors.dark[3]}
                      style={{ flexShrink: 0 }}
                    />
                    <Text size="sm" fw={500} truncate>
                      {formatDate(commit.date)}
                    </Text>
                  </Group>
                  <Text size="xs" c="dimmed">
                    {formatTime(commit.date)}
                  </Text>
                  <Group gap="xs" wrap="nowrap">
                    {commit.additions > 0 && (
                      <Badge
                        size="xs"
                        variant="light"
                        color="green"
                        leftSection={<IconPlus size={10} />}
                      >
                        {commit.additions}
                      </Badge>
                    )}
                    {commit.removals > 0 && (
                      <Badge
                        size="xs"
                        variant="light"
                        color="red"
                        leftSection={<IconMinus size={10} />}
                      >
                        {commit.removals}
                      </Badge>
                    )}
                    {commit.modifications > 0 && (
                      <Badge
                        size="xs"
                        variant="light"
                        color="yellow"
                        leftSection={<IconArrowsExchange size={10} />}
                      >
                        {commit.modifications}
                      </Badge>
                    )}
                  </Group>
                </Stack>
              </Box>
            </UnstyledButton>
          );
        })}
        </Flex>
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


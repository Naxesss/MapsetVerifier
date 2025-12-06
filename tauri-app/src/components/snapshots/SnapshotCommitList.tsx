import { Box, Group, Stack, Text, UnstyledButton, Badge, useMantineTheme, Flex } from '@mantine/core';
import { IconGitCommit, IconPlus, IconMinus, IconArrowsExchange } from '@tabler/icons-react';
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
    return 'Today ' + date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
  } else if (diffDays === 1) {
    return 'Yesterday ' + date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
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

function SnapshotCommitList({ commits, selectedCommitId, onSelectCommit }: SnapshotCommitListProps) {
  const theme = useMantineTheme();
  const containerRef = useRef<HTMLDivElement>(null);
  const commitRefs = useRef<Map<string, HTMLButtonElement>>(new Map());

  // Scroll to selected commit when it changes
  useEffect(() => {
    if (!selectedCommitId || !containerRef.current) return;

    const selectedElement = commitRefs.current.get(selectedCommitId);
    if (!selectedElement) return;

    const container = containerRef.current;

    // Check if container has overflow (scrollbar exists)
    const hasOverflow = container.scrollWidth > container.clientWidth;
    if (!hasOverflow) return;

    // Calculate the position to scroll to center the selected commit
    const containerRect = container.getBoundingClientRect();
    const elementRect = selectedElement.getBoundingClientRect();

    // Calculate the scroll position to center the element
    const scrollLeft =
      container.scrollLeft +
      elementRect.left -
      containerRect.left -
      (containerRect.width / 2) +
      (elementRect.width / 2);

    // Smooth scroll to the calculated position
    container.scrollTo({
      left: scrollLeft,
      behavior: 'smooth',
    });
  }, [selectedCommitId]);

  if (commits.length === 0) {
    return (
      <Text c="dimmed" size="sm" ta="center" py="md">
        No snapshot history available.
      </Text>
    );
  }

  return (
    <Flex
      ref={containerRef}
      gap="xs"
      p="sm"
      wrap="nowrap"
      bg={theme.colors.dark[8]}
      style={{ borderRadius: theme.radius.md }}
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
              <Stack gap="md">
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
  );
}

export default SnapshotCommitList;


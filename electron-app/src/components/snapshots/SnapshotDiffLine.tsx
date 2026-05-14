import { Box, Group, Stack, Text, UnstyledButton, useMantineTheme } from '@mantine/core';
import { IconArrowsExchange, IconMinus, IconPlus } from '@tabler/icons-react';
import { ReactNode, useState } from 'react';
import { ApiSnapshotDiff, DiffType } from '../../Types';
import OsuLink from '../common/OsuLink.tsx';

export function getDiffTypeIcon(diffType: DiffType, size: number = 16) {
  switch (diffType) {
    case 'Added':
      return <IconPlus size={size} color="var(--mantine-color-green-6)" />;
    case 'Removed':
      return <IconMinus size={size} color="var(--mantine-color-red-6)" />;
    case 'Changed':
      return <IconArrowsExchange size={size} color="var(--mantine-color-yellow-6)" />;
    default:
      return null;
  }
}

function parseDiffDetail(detail: string):
  | { type: 'transition'; before: string; after: string }
  | { type: 'keyValue'; key: string; value: string }
  | { type: 'plain'; text: string } {
  const transitionMatch = detail.match(/^(.*)\s->\s(.*)$/);
  if (transitionMatch && transitionMatch[1].trim().length > 0 && transitionMatch[2].trim().length > 0) {
    return {
      type: 'transition',
      before: transitionMatch[1].trim(),
      after: transitionMatch[2].trim(),
    };
  }

  const keyValueMatch = detail.match(/^([^:]{2,}):\s(.+)$/);
  if (keyValueMatch) {
    return {
      type: 'keyValue',
      key: keyValueMatch[1].trim(),
      value: keyValueMatch[2].trim(),
    };
  }

  return { type: 'plain', text: detail };
}

interface SnapshotDiffLineProps {
  diff: ApiSnapshotDiff;
}

function SnapshotDiffLine({ diff }: SnapshotDiffLineProps) {
  const theme = useMantineTheme();
  const [isExpanded, setIsExpanded] = useState(false);

  const diffTypeColor = (() => {
    switch (diff.diffType) {
      case 'Added':
        return theme.colors.green[6];
      case 'Removed':
        return theme.colors.red[6];
      case 'Changed':
        return theme.colors.yellow[6];
      default:
        return theme.colors.gray[6];
    }
  })();

  const visibleDetails = isExpanded ? diff.details : diff.details.slice(0, 2);
  const hiddenDetailsCount = diff.details.length - visibleDetails.length;

  const renderDetail = (detail: string): ReactNode => {
    const parsed = parseDiffDetail(detail);

    if (parsed.type === 'transition') {
      return (
        <Group gap={8} wrap="nowrap" align="flex-start">
          <Text
            size="xs"
            c="dimmed"
            ff={theme.fontFamilyMonospace}
            style={{ flex: 1, whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}
          >
            <OsuLink text={parsed.before} />
          </Text>
          <IconArrowsExchange size={14} color={theme.colors.yellow[6]} />
          <Text
            size="xs"
            ff={theme.fontFamilyMonospace}
            style={{ flex: 1, whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}
          >
            <OsuLink text={parsed.after} />
          </Text>
        </Group>
      );
    }

    if (parsed.type === 'keyValue') {
      return (
        <Group gap={8} wrap="nowrap" align="flex-start">
          <Text
            size="10px"
            tt="uppercase"
            fw={700}
            c="dimmed"
            style={{ letterSpacing: '0.04em', flexShrink: 0, paddingTop: 2 }}
          >
            {parsed.key}
          </Text>
          <Text size="xs" style={{ flex: 1, whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
            <OsuLink text={parsed.value} />
          </Text>
        </Group>
      );
    }

    return (
      <Text size="xs" style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
        <OsuLink text={parsed.text} />
      </Text>
    );
  };

  return (
    <Box
      p="sm"
      style={{
        borderRadius: theme.radius.sm,
        backgroundColor: theme.colors.dark[6],
        border: `1px solid ${theme.colors.dark[4]}`,
        boxShadow: `inset 3px 0 0 0 ${diffTypeColor}`,
        transition: 'border-color 120ms ease, background-color 120ms ease, box-shadow 120ms ease',
      }}
    >
      <Stack gap="sm">
        <Group justify="space-between" align="flex-start" gap="sm" wrap="nowrap">
          <Group gap="xs" wrap="nowrap" style={{ minWidth: 0, flex: 1 }}>
            <Box mt={2} style={{ display: 'flex', flexShrink: 0 }}>
              {getDiffTypeIcon(diff.diffType, 14)}
            </Box>
            <Text size="sm" fw={500} style={{ flex: 1, whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
              <OsuLink text={diff.message} />
            </Text>
          </Group>
        </Group>

        {visibleDetails.length > 0 && (
          <Stack gap={6} pl="lg">
            {visibleDetails.map((detail, index) => (
              <Box
                key={index}
                p={6}
                style={{
                  borderRadius: theme.radius.sm,
                  backgroundColor: theme.colors.dark[8],
                  border: `1px solid ${theme.colors.dark[4]}`,
                }}
              >
                {renderDetail(detail)}
              </Box>
            ))}

            {(hiddenDetailsCount > 0 || (isExpanded && diff.details.length > 2)) && (
              <UnstyledButton
                onClick={() => setIsExpanded((current) => !current)}
                style={{
                  width: 'fit-content',
                  color: theme.colors.blue[4],
                  fontSize: theme.fontSizes.xs,
                  fontWeight: 600,
                }}
              >
                {hiddenDetailsCount > 0
                  ? `Show ${hiddenDetailsCount} more detail${hiddenDetailsCount > 1 ? 's' : ''}`
                  : 'Show less'}
              </UnstyledButton>
            )}
          </Stack>
        )}
      </Stack>
    </Box>
  );
}

export default SnapshotDiffLine;

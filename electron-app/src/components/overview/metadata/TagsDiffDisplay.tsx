import {
  Text,
  Badge,
  Group,
  Paper,
  useMantineTheme,
  Stack,
  Box,
  Code,
} from '@mantine/core';
import { DifficultyMetadata } from '../../../Types';
import { countWord } from '../../../utils/countWord';

export interface TagsDiffDisplayProps {
  difficulties: DifficultyMetadata[];
}

interface TagGroupRow {
  tags: string;
  versions: string[];
}

/** Groups by exact `tags` string, then sorts by largest group first (ties keep first-seen order). */
function groupVersionsByTagsSorted(difficulties: DifficultyMetadata[]): TagGroupRow[] {
  const firstSeenOrder: string[] = [];
  const tagToVersions = new Map<string, string[]>();
  for (const d of difficulties) {
    const key = d.tags;
    if (!tagToVersions.has(key)) {
      firstSeenOrder.push(key);
      tagToVersions.set(key, []);
    }
    tagToVersions.get(key)!.push(d.version);
  }
  const tagIndex = new Map(firstSeenOrder.map((t, i) => [t, i]));
  const groups: TagGroupRow[] = firstSeenOrder.map((tags) => ({
    tags,
    versions: tagToVersions.get(tags)!,
  }));
  groups.sort((a, b) => {
    const byCount = b.versions.length - a.versions.length;
    if (byCount !== 0) {
      return byCount;
    }
    return (tagIndex.get(a.tags) ?? 0) - (tagIndex.get(b.tags) ?? 0);
  });
  return groups;
}

function tagTokenSet(tags: string): Set<string> {
  return new Set(
    tags
      .trim()
      .split(/\s+/)
      .filter(Boolean)
      .map((t) => t.toLowerCase())
  );
}

/** Keyword-level diff vs baseline; null when tokens match (e.g. reordering only). */
function tagTokenDiff(
  tags: string,
  baselineTags: string
): { onlyHere: string[]; missingHere: string[] } | null {
  const base = tagTokenSet(baselineTags);
  const mine = tagTokenSet(tags);
  const onlyHere = [...mine].filter((t) => !base.has(t)).sort();
  const missingHere = [...base].filter((t) => !mine.has(t)).sort();
  if (onlyHere.length === 0 && missingHere.length === 0) {
    return null;
  }
  return { onlyHere, missingHere };
}

export default function TagsDiffDisplay({ difficulties }: TagsDiffDisplayProps) {
  const theme = useMantineTheme();
  const tagGroups = groupVersionsByTagsSorted(difficulties);
  const maxCount = Math.max(...tagGroups.map((g) => g.versions.length));
  const numTiedMax = tagGroups.filter((g) => g.versions.length === maxCount).length;
  const hasClearMajority = numTiedMax === 1;
  const baselineTags = hasClearMajority ? (tagGroups[0]?.tags ?? '') : null;
  const minorityTotal = difficulties.length - maxCount;

  let summary: string;
  if (hasClearMajority && minorityTotal > 0) {
    summary =
      minorityTotal === 1
        ? `${countWord(maxCount, 'difficulty')} share these tags; 1 differs.`
        : `${countWord(maxCount, 'difficulty')} share these tags; ${countWord(minorityTotal, 'difficulty')} differ.`;
  } else {
    summary = `${tagGroups.length} different tag sets across ${countWord(difficulties.length, 'difficulty')}.`;
  }

  return (
    <Stack gap="sm">
      <Text size="sm" c="dimmed">
        {summary}
      </Text>
      {tagGroups.map(({ tags, versions }, groupIdx) => {
        const isMinority = hasClearMajority && versions.length < maxCount;
        const tokenHint =
          isMinority && baselineTags !== null ? tagTokenDiff(tags, baselineTags) : null;

        const statusBadge = hasClearMajority ? (
          isMinority ? (
            <Badge size="xs" color="yellow" variant="light">
              Differs
            </Badge>
          ) : (
            <Badge size="xs" color="gray" variant="light">
              Most difficulties
            </Badge>
          )
        ) : (
          <Badge size="xs" variant="light">
            {countWord(versions.length, 'difficulty')}
          </Badge>
        );

        const inner = (
          <Stack gap={4}>
            <Group gap="xs" wrap="wrap">
              {statusBadge}
              {versions.map((version, vi) => (
                <Badge key={`${groupIdx}-${version}-${vi}`} size="xs" variant="outline">
                  {version}
                </Badge>
              ))}
            </Group>
            <Code block fz="sm" style={{ wordBreak: 'break-word', whiteSpace: 'pre-wrap' }}>
              {tags || '(none)'}
            </Code>
            {tokenHint && (tokenHint.onlyHere.length > 0 || tokenHint.missingHere.length > 0) ? (
              <Stack gap={2}>
                {tokenHint.onlyHere.length > 0 ? (
                  <Text size="xs" c="dimmed">
                    Extra keywords vs shared set:{' '}
                    <Text span c="yellow.4" fz="xs">
                      {tokenHint.onlyHere.join(', ')}
                    </Text>
                  </Text>
                ) : null}
                {tokenHint.missingHere.length > 0 ? (
                  <Text size="xs" c="dimmed">
                    Missing vs shared set:{' '}
                    <Text span c="orange.4" fz="xs">
                      {tokenHint.missingHere.join(', ')}
                    </Text>
                  </Text>
                ) : null}
              </Stack>
            ) : null}
          </Stack>
        );

        return isMinority ? (
          <Paper
            key={groupIdx}
            p="sm"
            radius="sm"
            withBorder
            bg={theme.colors.dark[6]}
            style={{ borderColor: theme.colors.yellow[9] }}>
            {inner}
          </Paper>
        ) : (
          <Box key={groupIdx}>{inner}</Box>
        );
      })}
    </Stack>
  );
}

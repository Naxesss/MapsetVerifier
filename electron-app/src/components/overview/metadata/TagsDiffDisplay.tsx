import { Badge, Group, Stack } from '@mantine/core';
import TagChips from './TagChips.tsx';
import { DifficultyMetadata } from '../../../Types';

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

/**
 * Tags when they aren't the same in every difficulty: each tag set with the difficulties that use
 * it, most used first. Whether the difference matters is for the Inconsistent metadata check.
 */
export default function TagsDiffDisplay({ difficulties }: TagsDiffDisplayProps) {
  const tagGroups = groupVersionsByTagsSorted(difficulties);

  return (
    <Stack gap="md">
      {tagGroups.map(({ tags, versions }, groupIdx) => (
        <Stack key={groupIdx} gap="xs">
          <Group gap="xs" wrap="wrap">
            {versions.map((version, vi) => (
              <Badge key={`${groupIdx}-${version}-${vi}`}>{version}</Badge>
            ))}
          </Group>
          <TagChips tags={tags} />
        </Stack>
      ))}
    </Stack>
  );
}

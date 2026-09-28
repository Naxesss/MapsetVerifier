import { Badge, Collapse, Group, Stack, Text, UnstyledButton } from '@mantine/core';
import { IconChevronDown } from '@tabler/icons-react';
import { useState } from 'react';
import { CardTitle } from '../common/Headings.tsx';
import SectionCard from '../common/SectionCard.tsx';
import MantineMarkdown from '../documentation/MantineMarkdown.tsx';
import type { Components } from 'react-markdown';

/** Sub-headings inside an entry must not outrank the entry's own card title. */
const CHANGELOG_MARKDOWN_COMPONENTS: Components = {
  h3: ({ children }) => (
    <CardTitle component="h4" mt="md" mb="xs">
      {children}
    </CardTitle>
  ),
};

interface ChangelogEntry {
  version: string;
  title: string;
  body: string;
  previewLine: string | null;
}

function parseSemver(version: string): [number, number, number] {
  const [major, minor, patch] = version.split('.').map((part) => Number.parseInt(part, 10));
  return [major || 0, minor || 0, patch || 0];
}

function compareSemverDesc(a: string, b: string): number {
  const aParts = parseSemver(a);
  const bParts = parseSemver(b);

  for (let i = 0; i < 3; i += 1) {
    if (aParts[i] !== bParts[i]) {
      return bParts[i] - aParts[i];
    }
  }

  return 0;
}

function getPreviewLine(body: string): string | null {
  const firstContentLine = body
    .split('\n')
    .map((line) => line.trim())
    .find((line) => line.length > 0);

  return firstContentLine ?? null;
}

function parseEntry(raw: string, version: string): ChangelogEntry {
  const trimmed = raw.trim();
  if (!trimmed) {
    return { version, title: `[${version}]`, body: '', previewLine: null };
  }

  const lines = trimmed.split('\n');
  const firstLine = lines[0].trim();

  if (firstLine.startsWith('## ')) {
    const body = lines.slice(1).join('\n').trim();
    return {
      version,
      title: firstLine.replace(/^##\s+/, '').trim(),
      body,
      previewLine: getPreviewLine(body),
    };
  }

  return { version, title: `[${version}]`, body: trimmed, previewLine: getPreviewLine(trimmed) };
}

function loadEntries(): ChangelogEntry[] {
  const changelogFiles = import.meta.glob('../../content/changelog/*.md', {
    eager: true,
    import: 'default',
    query: '?raw',
  }) as Record<string, string>;

  return Object.entries(changelogFiles)
    .map(([path, raw]) => {
      const match = path.match(/[\\/](\d+\.\d+)\.md$/);
      if (!match) return null;
      return parseEntry(raw, match[1]);
    })
    .filter((entry): entry is ChangelogEntry => entry !== null)
    .sort((a, b) => compareSemverDesc(a.version, b.version));
}

export default function Changelog() {
  const entries = loadEntries();
  const [expandedVersions, setExpandedVersions] = useState<Set<string>>(
    () => new Set(entries[0] ? [entries[0].version] : [])
  );

  const toggleEntry = (version: string) => {
    setExpandedVersions((prev) => {
      const next = new Set(prev);
      if (next.has(version)) {
        next.delete(version);
      } else {
        next.add(version);
      }
      return next;
    });
  };

  return (
    <Stack gap="md">
      {entries.length === 0 ? <Text c="dimmed">No changelog entries yet.</Text> : null}

      {entries.map((entry, index) => {
        const isExpanded = expandedVersions.has(entry.version);
        const isLatest = index === 0;

        return (
          <SectionCard key={entry.version}>
            <Stack gap="sm">
              <UnstyledButton
                onClick={() => toggleEntry(entry.version)}
                aria-expanded={isExpanded}
                w="100%"
              >
                <Group justify="space-between" align="center" wrap="nowrap">
                  <Group gap="xs" align="center">
                    <CardTitle fz={22} fw={700}>
                      {entry.title}
                    </CardTitle>
                    {isLatest ? (
                      <Badge color="blue" size="sm">
                        Latest
                      </Badge>
                    ) : null}
                  </Group>
                  <IconChevronDown
                    size={18}
                    style={{
                      transform: isExpanded ? 'rotate(180deg)' : 'rotate(0deg)',
                      transition: 'transform 150ms ease',
                    }}
                  />
                </Group>
                {!isExpanded && entry.previewLine ? (
                  <Text c="dimmed" size="sm" mt="xs" lineClamp={2}>
                    {entry.previewLine}
                  </Text>
                ) : null}
              </UnstyledButton>

              <Collapse in={isExpanded}>
                {entry.body ? (
                  <MantineMarkdown components={CHANGELOG_MARKDOWN_COMPONENTS}>
                    {entry.body}
                  </MantineMarkdown>
                ) : null}
              </Collapse>
            </Stack>
          </SectionCard>
        );
      })}
    </Stack>
  );
}

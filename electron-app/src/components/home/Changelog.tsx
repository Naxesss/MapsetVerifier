import { Badge, Card, Collapse, Group, Stack, Text, Title } from '@mantine/core';
import { IconChevronDown } from '@tabler/icons-react';
import { useState } from 'react';
import MantineMarkdown from '../documentation/MantineMarkdown.tsx';

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
      const match = path.match(/[\\/](\d+\.\d+\.\d+)\.md$/);
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
      <Title order={2} fw={600}>
        Changelog
      </Title>

      {entries.length === 0 ? <Text c="dimmed">No changelog entries yet.</Text> : null}

      {entries.map((entry, index) => {
        const isExpanded = expandedVersions.has(entry.version);
        const isLatest = index === 0;

        return (
          <Card key={entry.version} withBorder padding="lg" radius="md" shadow="sm">
            <Stack gap="sm">
              <div
                role="button"
                tabIndex={0}
                onClick={() => toggleEntry(entry.version)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    toggleEntry(entry.version);
                  }
                }}
                style={{ width: '100%', textAlign: 'left', cursor: 'pointer' }}
              >
                <Group justify="space-between" align="center">
                  <Group gap="xs" align="center">
                    <Text component="h3" fw={700} fz="1.6rem" lh={1.15} m={0}>
                      {entry.title}
                    </Text>
                    {isLatest ? (
                      <Badge size="md" variant="light" color="blue">
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
              </div>

              <Collapse in={isExpanded}>
                {entry.body ? <MantineMarkdown>{entry.body}</MantineMarkdown> : null}
              </Collapse>
            </Stack>
          </Card>
        );
      })}
    </Stack>
  );
}

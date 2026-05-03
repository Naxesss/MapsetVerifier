import { Card, Stack, Title } from '@mantine/core';
import changelog from '../../content/CHANGELOG.md?raw';
import MantineMarkdown from '../documentation/MantineMarkdown.tsx';

interface ChangelogRelease {
  title: string;
  body: string;
}

function parseChangelog(raw: string): {
  pageHeading: string | null;
  intro: string | null;
  releases: ChangelogRelease[];
} {
  const parts = raw.trim().split(/\n(?=### )/);
  const first = parts[0]?.trim() ?? '';

  let pageHeading: string | null = null;
  let intro: string | null = null;

  const h2Match = first.match(/^##\s+(.+?)(?:\n|$)/);
  if (h2Match) {
    pageHeading = h2Match[1].trim();
    const afterH2 = first.slice(h2Match[0].length).trim();
    intro = afterH2 || null;
  } else if (first) {
    intro = first;
  }

  const releaseParts = h2Match ? parts.slice(1) : parts;
  const releases: ChangelogRelease[] = [];

  for (const block of releaseParts) {
    const trimmed = block.trim();
    if (!trimmed.startsWith('###')) continue;
    const lines = trimmed.split('\n');
    const title = lines[0].replace(/^###\s+/, '').trim();
    const body = lines.slice(1).join('\n').trim();
    releases.push({ title, body });
  }

  return { pageHeading, intro, releases };
}

export default function Changelog() {
  const { pageHeading, intro, releases } = parseChangelog(changelog);

  return (
    <Stack gap="md">
      {pageHeading && (
        <Title order={3} fw={600}>
          {pageHeading}
        </Title>
      )}
      {intro && <MantineMarkdown>{intro}</MantineMarkdown>}

      {releases.map((release, index) => (
        <Card key={`${release.title}-${index}`} withBorder padding="lg" radius="md" shadow="sm">
          <Stack gap="sm">
            <Title order={4} fw={600} fz="md">
              {release.title}
            </Title>
            {release.body ? <MantineMarkdown>{release.body}</MantineMarkdown> : null}
          </Stack>
        </Card>
      ))}
    </Stack>
  );
}

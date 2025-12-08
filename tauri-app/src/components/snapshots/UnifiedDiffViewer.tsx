import { Accordion, Badge, Box, Group, Stack, Text, Code, useMantineTheme } from '@mantine/core';
import { IconPlus, IconMinus, IconArrowsExchange } from '@tabler/icons-react';
import {useSettings} from "../../context/SettingsContext.tsx";
import { ApiSnapshotCommit, ApiSnapshotSection, ApiSnapshotDiff, DiffType } from '../../Types';
import OsuLink from "../common/OsuLink.tsx";

interface UnifiedDiffViewerProps {
  commit: ApiSnapshotCommit;
}

function getDiffTypeIcon(diffType: DiffType, size: number = 16) {
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

function DiffLine({ diff }: { diff: ApiSnapshotDiff }) {
  const theme = useMantineTheme();
  const { settings } = useSettings();

  return (
    <Box
      p="xs"
      style={{
        borderRadius: theme.radius.sm,
        backgroundColor: theme.colors.dark[8],
        fontFamily: 'monospace',
      }}
    >
      <Stack gap="xs">
        <Group gap="xs" wrap="nowrap">
          {getDiffTypeIcon(diff.diffType, 14)}
          <Text size="sm" style={{ flex: 1 }}>
            <OsuLink text={diff.message} />
          </Text>
        </Group>
        
        {settings.showSnapshotDiffView && (
          <>
            {/* Show unified diff format for changes */}
            {diff.diffType === 'Changed' && (diff.oldValue || diff.newValue) && (
              <Stack gap={2} pl="md">
                {diff.oldValue && (
                  <Code
                    block
                    style={{
                      backgroundColor: 'rgba(250, 82, 82, 0.15)',
                      borderLeft: `3px solid ${theme.colors.red[6]}`,
                      color: theme.colors.red[4],
                    }}
                  >
                    - <OsuLink text={diff.oldValue} />
                  </Code>
                )}
                {diff.newValue && (
                  <Code
                    block
                    style={{
                      backgroundColor: 'rgba(64, 192, 87, 0.15)',
                      borderLeft: `3px solid ${theme.colors.green[6]}`,
                      color: theme.colors.green[4],
                    }}
                  >
                    + <OsuLink text={diff.newValue} />
                  </Code>
                )}
              </Stack>
            )}

            {/* Show value for added items */}
            {diff.diffType === 'Added' && diff.newValue && (
              <Box pl="md">
                <Code
                  block
                  style={{
                    backgroundColor: 'rgba(64, 192, 87, 0.15)',
                    borderLeft: `3px solid ${theme.colors.green[6]}`,
                    color: theme.colors.green[4],
                  }}
                >
                  + <OsuLink text={diff.newValue} />
                </Code>
              </Box>
            )}

            {/* Show value for removed items */}
            {diff.diffType === 'Removed' && diff.oldValue && (
              <Box pl="md">
                <Code
                  block
                  style={{
                    backgroundColor: 'rgba(250, 82, 82, 0.15)',
                    borderLeft: `3px solid ${theme.colors.red[6]}`,
                    color: theme.colors.red[4],
                  }}
                >
                  - <OsuLink text={diff.oldValue} />
                </Code>
              </Box>
            )}
          </>
        )}

        {/* Show details if available */}
        {diff.details.length > 0 && (
          <Stack gap={2} pl="md">
            {diff.details.map((detail, index) => (
              <Text key={index} size="xs">
                <OsuLink text={detail} />
              </Text>
            ))}
          </Stack>
        )}
      </Stack>
    </Box>
  );
}

function SectionAccordion({ section }: { section: ApiSnapshotSection }) {
  return (
    <Accordion.Item value={section.name}>
      <Accordion.Control>
        <Group gap="sm">
          {getDiffTypeIcon(section.aggregatedDiffType, 18)}
          <Text fw={500}>{section.name}</Text>
          {section.additions > 0 && (
            <Badge size="sm" color="green" variant="light">
              {section.additions} Added
            </Badge>
          )}
          {section.removals > 0 && (
            <Badge size="sm" color="red" variant="light">
              {section.removals} Removed
            </Badge>
          )}
          {section.modifications > 0 && (
            <Badge size="sm" color="yellow" variant="light">
              {section.modifications} Changed
            </Badge>
          )}
        </Group>
      </Accordion.Control>
      <Accordion.Panel>
        <Stack gap="xs">
          {section.diffs.map((diff, index) => (
            <DiffLine key={`${diff.message}-${index}`} diff={diff} />
          ))}
        </Stack>
      </Accordion.Panel>
    </Accordion.Item>
  );
}

function UnifiedDiffViewer({ commit }: UnifiedDiffViewerProps) {
  const theme = useMantineTheme();

  if (commit.sections.length === 0) {
    return (
      <Text c="dimmed" size="sm" ta="center" py="md">
        No changes in this commit.
      </Text>
    );
  }

  return (
    <Accordion
      variant="separated"
      multiple
      defaultValue={commit.sections.map((s) => s.name)}
      styles={{
        item: {
          backgroundColor: theme.colors.dark[7],
          borderRadius: theme.radius.md,
        },
        control: {
          borderRadius: theme.radius.md,
        },
      }}
    >
      {commit.sections.map((section) => (
        <SectionAccordion key={section.name} section={section} />
      ))}
    </Accordion>
  );
}

export default UnifiedDiffViewer;


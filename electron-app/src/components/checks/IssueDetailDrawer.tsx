import {
  Alert,
  Anchor,
  Badge,
  Button,
  Divider,
  Drawer,
  Grid,
  Group,
  Loader,
  Paper,
  Stack,
  Text,
  Title,
} from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { IconAlertCircle, IconBook, IconCheck, IconCopy } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import DocumentationApi from '../../client/DocumentationApi';
import {
  ApiCheckResult,
  ApiDocumentationCheck,
  ApiDocumentationCheckDetails,
  Level,
} from '../../Types';
import { getLevelLabel } from '../../utils/levelLabel';
import OsuLink from '../common/OsuLink';
import { buildOsuEditHref, parseOsuLinkSegments } from '../common/osuLinkUtils';
import DocumentationOutcomeBlockquote from '../documentation/DocumentationOutcomeBlockquote';
import MantineMarkdown from '../documentation/MantineMarkdown';
import LevelIcon from '../icons/LevelIcon';

interface IssueDetailDrawerProps {
  opened: boolean;
  onClose: () => void;
  issue: ApiCheckResult | null;
  checkName?: string;
  documentationCheck?: ApiDocumentationCheck;
  onCopyIssue: () => void;
  groupCount?: number;
  onCopyAll?: () => void;
  sameSeverityCount?: number;
  onCopySameSeverity?: () => void;
}

export function normalizeLevel(level: Level): Exclude<Level, 'Check'> {
  return level === 'Check' ? 'Info' : level;
}

function getIssueTimestamps(issue: ApiCheckResult | null) {
  if (!issue) return [];

  return parseOsuLinkSegments(issue.message)
    .filter((segment) => segment.kind === 'timestamp')
    .map((segment) => segment.value);
}

export async function copyToClipboard(text: string, message: string) {
  try {
    await navigator.clipboard.writeText(text);
    notifications.show({
      message,
      color: 'green',
      icon: <IconCheck size={16} />,
    });
  } catch {
    notifications.show({
      message: 'Clipboard is unavailable.',
      color: 'red',
      icon: <IconAlertCircle size={16} />,
    });
  }
}

export default function IssueDetailDrawer({
  opened,
  onClose,
  issue,
  checkName,
  documentationCheck,
  onCopyIssue,
  groupCount,
  onCopyAll,
  sameSeverityCount,
  onCopySameSeverity,
}: IssueDetailDrawerProps) {
  const normalizedLevel = issue ? normalizeLevel(issue.level) : 'Info';
  const timestamps = getIssueTimestamps(issue);
  const visibleTimestamps = Array.from(new Set(timestamps));

  const { data, isLoading, error } = useQuery<ApiDocumentationCheckDetails, Error>({
    queryKey: ['documentationCheckDetails', documentationCheck?.id],
    queryFn: () => DocumentationApi.getCheckDetails(documentationCheck!.id.toString()),
    enabled: opened && Boolean(documentationCheck),
    staleTime: Infinity,
    refetchOnWindowFocus: false,
  });

  const relatedOutcomes =
    data?.outcomes.filter((outcome) => normalizeLevel(outcome.level) === normalizedLevel) ?? [];

  return (
    <Drawer
      opened={opened}
      onClose={onClose}
      position="right"
      size="lg"
      zIndex={1900}
      title={
        <Group>
          <LevelIcon level={normalizedLevel} />
          <Stack gap={2}>
            <Text size="sm" c="dimmed">
              Issue details
            </Text>
            <Text size="lg" fw={700}>
              {checkName ?? 'Check issue'}
            </Text>

            {documentationCheck && (
              <Group gap="xs">
                <Badge size="xs" variant="light">
                  {documentationCheck.category}
                </Badge>
                <Text size="xs" c="dimmed">
                  Created by {documentationCheck.author}
                </Text>
              </Group>
            )}
          </Stack>
        </Group>
      }
      styles={{
        overlay: {
          top: 'var(--mv-window-bar-height)',
          height: 'calc(100dvh - var(--mv-window-bar-height))',
        },
        content: {
          marginTop: 'var(--mv-window-bar-height)',
          height: 'calc(100dvh - var(--mv-window-bar-height))',
          display: 'flex',
          flexDirection: 'column',
          overflow: 'hidden',
        },
        body: {
          flex: 1,
          overflowY: 'auto',
          paddingTop: 0,
        },
      }}
    >
      {issue ? (
        <Stack gap="lg">
          <Grid grow>
            <Grid.Col span={4}>
              <Button
                w="100%"
                variant="light"
                leftSection={<IconCopy size={14} />}
                onClick={onCopyIssue}
              >
                Copy issue
              </Button>
            </Grid.Col>
            {onCopySameSeverity &&
              sameSeverityCount &&
              sameSeverityCount > 1 &&
              groupCount &&
              sameSeverityCount < groupCount && (
                <Grid.Col span={4}>
                  <Button
                    w="100%"
                    variant="light"
                    leftSection={<IconCopy size={14} />}
                    onClick={onCopySameSeverity}
                  >
                    Copy {getLevelLabel(normalizedLevel)} ({sameSeverityCount})
                  </Button>
                </Grid.Col>
              )}
            {onCopyAll && groupCount && groupCount > 1 && (
              <Grid.Col span={4}>
                <Button
                  w="100%"
                  variant="light"
                  leftSection={<IconCopy size={14} />}
                  onClick={onCopyAll}
                >
                  Copy all ({groupCount})
                </Button>
              </Grid.Col>
            )}
          </Grid>

          <Stack gap="xs">
            <Title order={3}>Full message</Title>
            <Paper p="sm" radius="sm" withBorder>
              <Text size="sm" style={{ overflowWrap: 'anywhere', wordBreak: 'break-word' }}>
                <OsuLink text={issue.message} />
              </Text>
            </Paper>
          </Stack>

          <Stack gap="xs">
            <Title order={3}>Timestamp links</Title>
            {visibleTimestamps.length > 0 ? (
              <Stack gap="xs">
                {visibleTimestamps.map((timestamp) => (
                  <Group key={timestamp} gap="xs" wrap="nowrap">
                    <Anchor
                      href={buildOsuEditHref(timestamp)}
                      size="sm"
                      style={{ fontFamily: 'var(--mantine-font-family-monospace)' }}
                    >
                      {timestamp}
                    </Anchor>
                    <Button
                      size="compact-xs"
                      variant="subtle"
                      leftSection={<IconCopy size={13} />}
                      onClick={() => copyToClipboard(timestamp, 'Timestamp copied.')}
                    >
                      Copy
                    </Button>
                  </Group>
                ))}
              </Stack>
            ) : (
              <Text size="sm" c="dimmed">
                This issue does not include a timestamp.
              </Text>
            )}
          </Stack>

          <Divider
            label={
              <Group>
                <IconBook />
                <Text>Check documentation</Text>
              </Group>
            }
          />

          <Stack gap="sm">
            {documentationCheck ? (
              <>
                {isLoading ? <Loader size="sm" /> : null}
                {error ? (
                  <Alert icon={<IconAlertCircle size={16} />} color="red">
                    Failed to load documentation details.
                  </Alert>
                ) : null}
                {data ? (
                  <Stack gap="md">
                    <MantineMarkdown notesForBlockquotes>{data.description}</MantineMarkdown>
                    {relatedOutcomes.length > 0 ? (
                      <Stack gap="sm">
                        {relatedOutcomes.map((outcome, index) => (
                          <DocumentationOutcomeBlockquote key={index} outcome={outcome} />
                        ))}
                      </Stack>
                    ) : null}
                  </Stack>
                ) : null}
              </>
            ) : (
              <Text size="sm" c="dimmed">
                No documentation entry is available for this check.
              </Text>
            )}
          </Stack>
        </Stack>
      ) : null}
    </Drawer>
  );
}

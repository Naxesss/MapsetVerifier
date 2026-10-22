import {
  Alert,
  Anchor,
  Badge,
  Button,
  Divider,
  Drawer,
  Group,
  Paper,
  Stack,
  Text,
} from '@mantine/core';
import { IconAlertCircle, IconCopy } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import DocumentationApi from '../../client/DocumentationApi';
import { useOpenOsuTimestamp } from '../../hooks/useOpenOsuTimestamp';
import { Z_INDEX } from '../../theme/layers';
import { ApiCheckResult, ApiDocumentationCheck, ApiDocumentationCheckDetails } from '../../Types';
import { getLevelLabel } from '../../utils/levelLabel';
import { notifyError, notifySuccess } from '../../utils/notify';
import { CardTitle, SectionTitle } from '../common/Headings';
import { TextSkeleton } from '../common/LoadingSkeletons';
import OsuLink from '../common/OsuLink';
import {
  buildOsuEditHref,
  parseOsuLinkSegments,
  shouldInterceptOsuOpen,
} from '../common/osuLinkUtils';
import DocumentationOutcomeBlockquote from '../documentation/DocumentationOutcomeBlockquote';
import MantineMarkdown from '../documentation/MantineMarkdown';
import LevelIcon from '../icons/LevelIcon';
import RuleReferences from '../rankingCriteria/RuleReferences';
import { normalizeLevel } from './utils/levelUtils';

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

function getIssueTimestamps(issue: ApiCheckResult | null) {
  if (!issue) return [];

  return parseOsuLinkSegments(issue.message)
    .filter((segment) => segment.kind === 'timestamp')
    .map((segment) => segment.value);
}

export async function copyToClipboard(text: string, message: string) {
  try {
    await navigator.clipboard.writeText(text);
    notifySuccess(message);
  } catch {
    notifyError('Clipboard is unavailable.');
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
  const openOsuTimestamp = useOpenOsuTimestamp();

  return (
    <Drawer
      opened={opened}
      onClose={onClose}
      position="right"
      size="lg"
      zIndex={Z_INDEX.drawer}
      title={
        <Group>
          <LevelIcon level={normalizedLevel} />
          <Stack gap="2xs">
            <Text size="sm" c="dimmed">
              Issue details
            </Text>
            <SectionTitle component="span">{checkName ?? 'Check issue'}</SectionTitle>

            {documentationCheck && (
              <Group gap="xs">
                <Badge>{documentationCheck.category}</Badge>
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
          <Group gap="xs">
            <Button variant="light" leftSection={<IconCopy size={14} />} onClick={onCopyIssue}>
              Copy issue
            </Button>
            {onCopySameSeverity &&
              sameSeverityCount &&
              sameSeverityCount > 1 &&
              groupCount &&
              sameSeverityCount < groupCount && (
                <Button
                  variant="light"
                  leftSection={<IconCopy size={14} />}
                  onClick={onCopySameSeverity}
                >
                  Copy {getLevelLabel(normalizedLevel)} ({sameSeverityCount})
                </Button>
              )}
            {onCopyAll && groupCount && groupCount > 1 && (
              <Button variant="light" leftSection={<IconCopy size={14} />} onClick={onCopyAll}>
                Copy all ({groupCount})
              </Button>
            )}
          </Group>

          <Stack gap="xs">
            <CardTitle>Full message</CardTitle>
            <Paper p="sm" radius="sm" withBorder>
              <Text size="sm" style={{ overflowWrap: 'anywhere', wordBreak: 'break-word' }}>
                <OsuLink text={issue.message} />
              </Text>
            </Paper>
          </Stack>

          {visibleTimestamps.length > 0 ? (
            <Stack gap="xs">
              <CardTitle>Timestamp links</CardTitle>
              <Stack gap="xs">
                {visibleTimestamps.map((timestamp) => (
                  <Group key={timestamp} gap="xs" wrap="nowrap">
                    <Anchor
                      href={buildOsuEditHref(timestamp)}
                      size="sm"
                      style={{ fontFamily: 'var(--mantine-font-family-monospace)' }}
                      onClick={(event) => {
                        if (!shouldInterceptOsuOpen(event)) return;
                        event.preventDefault();
                        void openOsuTimestamp(timestamp);
                      }}
                      onAuxClick={(event) => {
                        if (!shouldInterceptOsuOpen(event)) return;
                        event.preventDefault();
                        void openOsuTimestamp(timestamp);
                      }}
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
            </Stack>
          ) : null}

          {issue.ruleIds && issue.ruleIds.length > 0 ? (
            <Stack gap="xs">
              <CardTitle>Ranking criteria</CardTitle>
              <RuleReferences ruleIds={issue.ruleIds} />
            </Stack>
          ) : null}

          <Divider />

          <Stack gap="xs">
            <CardTitle>Check documentation</CardTitle>
            {documentationCheck ? (
              <>
                {isLoading ? <TextSkeleton lines={4} /> : null}
                {error ? (
                  <Alert icon={<IconAlertCircle size={16} />} color="red">
                    Couldn&apos;t load the check documentation.
                  </Alert>
                ) : null}
                {data ? (
                  <Stack gap="md">
                    <MantineMarkdown notesForBlockquotes>{data.description}</MantineMarkdown>
                    {relatedOutcomes.length > 0 ? (
                      <Stack gap="sm">
                        {relatedOutcomes.map((outcome, index) => (
                          <DocumentationOutcomeBlockquote
                            key={index}
                            outcome={outcome}
                            showRules={false}
                          />
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

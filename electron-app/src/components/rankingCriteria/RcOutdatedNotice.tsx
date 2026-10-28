import { Alert, Anchor, Button, Group, Text } from '@mantine/core';
import { IconAlertTriangle, IconCheck } from '@tabler/icons-react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { invalidateRankingCriteria, markReviewed } from './rcCurationApi';
import { wikiCompareUrl } from './rcUtils';
import { useRankingCriteriaOverview } from './useRankingCriteria';
import { openExternal } from '../../hooks/useOpenExternal';
import { ApiRcStatement } from '../../Types';
import { isDevBuild } from '../../utils/devSettings';

interface RcOutdatedNoticeProps {
  statement: ApiRcStatement;
  /** Called once the statement is marked as reviewed (dev builds only). */
  onReviewed?: () => void;
}

/**
 * Explains that a statement changed on the wiki since its linked checks were reviewed, with a link to
 * the wiki changes since then. Dev builds can mark it as reviewed from here, and also see changed
 * statements without checks so they can be cleared too.
 */
export default function RcOutdatedNotice({ statement, onReviewed }: RcOutdatedNoticeProps) {
  const overview = useRankingCriteriaOverview();
  const queryClient = useQueryClient();
  const review = useMutation({
    mutationFn: markReviewed,
    onSuccess: async () => {
      await invalidateRankingCriteria(queryClient);
      onReviewed?.();
    },
  });

  const lastReview = statement.lastReview;
  const linked = statement.links.length > 0;
  if (!lastReview || (!linked && !isDevBuild)) return null;

  const source = overview.data?.source;
  const compareUrl = source ? wikiCompareUrl(source, lastReview.commit) : null;

  return (
    <Alert
      variant="light"
      color={linked ? 'red' : 'gray'}
      icon={<IconAlertTriangle size={18} />}
      title={linked ? 'Changed since its checks were reviewed' : 'Changed on the osu! wiki'}
    >
      <Text size="sm">
        {lastReview.kind !== statement.kind
          ? `This was a ${lastReview.kind.toLowerCase()} and is now a ${statement.kind.toLowerCase()}. `
          : 'The wording changed on the osu! wiki. '}
        {linked
          ? 'The linked checks may no longer match it. '
          : 'No checks link to it, so there is nothing to update. '}
        {compareUrl && (
          <Anchor
            size="sm"
            href={compareUrl}
            onClick={(event) => {
              event.preventDefault();
              void openExternal(compareUrl);
            }}
          >
            See the wiki changes since{' '}
            <Text span ff="monospace" size="sm">
              {lastReview.commit.slice(0, 8)}
            </Text>
          </Anchor>
        )}
      </Text>
      {isDevBuild && (
        <Group gap="xs" mt="xs">
          <Button
            size="compact-sm"
            variant="light"
            color={linked ? 'red' : 'gray'}
            leftSection={<IconCheck size={14} />}
            loading={review.isPending}
            onClick={() => review.mutate(statement.id)}
          >
            Mark as reviewed
          </Button>
          {review.error && (
            <Text size="xs" c="red">
              {review.error.message}
            </Text>
          )}
        </Group>
      )}
    </Alert>
  );
}

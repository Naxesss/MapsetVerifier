import { Alert, Anchor, Text } from '@mantine/core';
import { IconAlertTriangle } from '@tabler/icons-react';
import { openExternal, wikiCompareUrl } from './rcUtils';
import { useRankingCriteriaOverview } from './useRankingCriteria';
import { ApiRcStatement } from '../../Types';

/**
 * Explains that a statement changed on the wiki since its linked checks were reviewed, with a link to
 * the wiki changes since then.
 */
export default function RcOutdatedNotice({ statement }: { statement: ApiRcStatement }) {
  const overview = useRankingCriteriaOverview();
  const review = statement.lastReview;
  if (!review || statement.links.length === 0) return null;

  const source = overview.data?.source;
  const compareUrl = source ? wikiCompareUrl(source, review.commit) : null;

  return (
    <Alert
      variant="light"
      color="red"
      icon={<IconAlertTriangle size={18} />}
      title="Changed since its checks were reviewed"
    >
      <Text size="sm">
        {review.kind !== statement.kind
          ? `This was a ${review.kind.toLowerCase()} and is now a ${statement.kind.toLowerCase()}. `
          : 'The wording changed on the osu! wiki. '}
        The linked checks may no longer match it.{' '}
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
              {review.commit.slice(0, 8)}
            </Text>
          </Anchor>
        )}
      </Text>
    </Alert>
  );
}

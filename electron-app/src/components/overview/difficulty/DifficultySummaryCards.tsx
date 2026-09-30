import { Badge } from '@mantine/core';
import StarRatingBadge, { STAR_BADGE_FONT_SIZE } from '../../common/StarRatingBadge.tsx';
import { StatLine } from '../../common/StatField.tsx';
import type { DifficultyOverviewDifficulty } from '../../../Types';

/** Coloured like everywhere else a difficulty's star rating shows; both badges share one width. */
const starBadge = (starRating: number, pair: number[]) => (
  <StarRatingBadge rating={starRating} size="sm" sizeTo={pair} />
);

/** A difference between two ratings, not a difficulty, so it takes the badge's shape in grey. */
const gapBadge = (gap: string) => (
  <Badge
    variant="light"
    color="gray"
    size="sm"
    style={{ fontFamily: 'Torus, sans-serif', fontSize: STAR_BADGE_FONT_SIZE.sm }}
  >
    {gap}
  </Badge>
);

/** The spread at a glance: easiest and hardest difficulty, and the biggest jump between neighbours. */
export function DifficultySpreadSummary({
  difficulties,
}: {
  difficulties: DifficultyOverviewDifficulty[];
}) {
  if (difficulties.length === 0) {
    return null;
  }

  if (difficulties.length === 1) {
    const only = difficulties[0];
    return (
      <StatLine
        items={[
          {
            label: 'Star rating',
            value: starBadge(only.starRating, []),
            note: only.version,
          },
        ]}
      />
    );
  }

  const sorted = [...difficulties].sort((a, b) => a.starRating - b.starRating);
  const lowest = sorted[0];
  const highest = sorted[sorted.length - 1];

  let largestGap = { from: sorted[0], to: sorted[1] };
  for (let i = 2; i < sorted.length; i++) {
    const gap = sorted[i].starRating - sorted[i - 1].starRating;
    if (gap > largestGap.to.starRating - largestGap.from.starRating) {
      largestGap = { from: sorted[i - 1], to: sorted[i] };
    }
  }

  return (
    <StatLine
      items={[
        {
          label: 'Lowest star rating',
          value: starBadge(lowest.starRating, [highest.starRating]),
          note: lowest.version,
        },
        {
          label: 'Highest star rating',
          value: starBadge(highest.starRating, [lowest.starRating]),
          note: highest.version,
        },
        {
          label: 'Largest gap',
          value: gapBadge(
            `★ ${(largestGap.to.starRating - largestGap.from.starRating).toFixed(2)}`
          ),
          note: `${largestGap.from.version} → ${largestGap.to.version}`,
        },
      ]}
    />
  );
}

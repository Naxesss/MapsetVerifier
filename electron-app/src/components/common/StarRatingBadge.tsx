import { Badge, type BadgeProps } from '@mantine/core';
import { getDifficultyColor, getDifficultyTextColor } from './DifficultyColor';

export type StarRatingBadgeProps = Omit<BadgeProps, 'color' | 'variant' | 'children' | 'styles'> & {
  rating: number;
  /** Ratings of sibling badges; the badge sizes itself to the widest of them so a list lines up. */
  sizeTo?: number[];
};

const formatRating = (rating: number) => `★ ${rating.toFixed(2)}`;

/** Text size per badge size, so a bigger badge doesn't leave its text looking lost inside it. */
export const STAR_BADGE_FONT_SIZE: Record<string, number> = {
  xs: 10,
  sm: 12,
  md: 13,
  lg: 15,
  xl: 17,
};

function StarRatingBadge({ rating, sizeTo, size = 'sm', style, ...props }: StarRatingBadgeProps) {
  const bg = getDifficultyColor(rating);
  const textColor = getDifficultyTextColor(rating);
  const label = formatRating(rating);
  // Hidden copies of the other labels share one grid cell with the real one, so the badge takes
  // the width of the widest label without measuring anything.
  const sizers = sizeTo ? [...new Set(sizeTo.map(formatRating))].filter((s) => s !== label) : [];

  return (
    <Badge
      {...props}
      variant="filled"
      size={size}
      style={{
        backgroundColor: bg,
        color: textColor,
        fontFamily: 'Torus, sans-serif',
        fontSize: STAR_BADGE_FONT_SIZE[String(size)] ?? 12,
        ...style,
      }}
    >
      {sizers.length === 0 ? (
        label
      ) : (
        <span style={{ display: 'inline-grid' }}>
          {sizers.map((s) => (
            <span key={s} aria-hidden style={{ gridArea: '1 / 1', visibility: 'hidden' }}>
              {s}
            </span>
          ))}
          <span style={{ gridArea: '1 / 1', justifySelf: 'center' }}>{label}</span>
        </span>
      )}
    </Badge>
  );
}

export default StarRatingBadge;

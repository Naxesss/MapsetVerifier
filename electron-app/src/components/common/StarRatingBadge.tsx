import { Badge, type BadgeProps } from '@mantine/core';
import { getDifficultyColor } from './DifficultyColor';

export type StarRatingBadgeProps = Omit<BadgeProps, 'color' | 'variant' | 'children' | 'styles'> & {
  rating: number;
};

function StarRatingBadge({ rating, ...props }: StarRatingBadgeProps) {
  const bg = getDifficultyColor(rating);
  const textColor = rating >= 6.5 ? 'var(--mantine-color-yellow-4)' : 'black';

  return (
    <Badge
      {...props}
      variant="filled"
      size="sm"
      style={{
          backgroundColor: bg,
          color: textColor,
          fontFamily: 'Torus, sans-serif',
          fontSize: 12,
      }}
    >
      ★ {rating.toFixed(2)}
    </Badge>
  );
}

export default StarRatingBadge;

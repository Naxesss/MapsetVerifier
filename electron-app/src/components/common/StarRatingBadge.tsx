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
      styles={{
        root: {
          backgroundColor: bg,
          color: textColor,
        },
        label: {
          fontWeight: 700,
          fontFamily: 'Comfortaa, Inter, sans-serif',
        },
      }}
    >
      ★ {rating.toFixed(2)}
    </Badge>
  );
}

export default StarRatingBadge;

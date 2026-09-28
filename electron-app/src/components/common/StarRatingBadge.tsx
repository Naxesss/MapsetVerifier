import { Badge, type BadgeProps } from '@mantine/core';
import { getDifficultyColor, getDifficultyTextColor } from './DifficultyColor';

export type StarRatingBadgeProps = Omit<BadgeProps, 'color' | 'variant' | 'children' | 'styles'> & {
  rating: number;
};

function StarRatingBadge({ rating, size = 'sm', style, ...props }: StarRatingBadgeProps) {
  const bg = getDifficultyColor(rating);
  const textColor = getDifficultyTextColor(rating);

  return (
    <Badge
      {...props}
      variant="filled"
      size={size}
      style={{
        backgroundColor: bg,
        color: textColor,
        fontFamily: 'Torus, sans-serif',
        fontSize: size === 'xs' ? 10 : 12,
        ...style,
      }}
    >
      ★ {rating.toFixed(2)}
    </Badge>
  );
}

export default StarRatingBadge;

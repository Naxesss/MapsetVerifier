interface DifficultyColorPillProps {
  color: string;
  height?: number;
}

export default function DifficultyColorPill({ color, height = 18 }: DifficultyColorPillProps) {
  return (
    <span
      aria-hidden
      style={{
        width: 4,
        height,
        borderRadius: 999,
        backgroundColor: color,
        flexShrink: 0,
        alignSelf: 'center',
      }}
    />
  );
}

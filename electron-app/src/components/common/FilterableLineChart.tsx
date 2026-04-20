import { LineChart } from '@mantine/charts';
import { ColorSwatch, getThemeColor, Group, Stack, Text, UnstyledButton, useMantineTheme } from '@mantine/core';
import { useEffect, useState, type ComponentProps } from 'react';

type MantineLineChartProps = ComponentProps<typeof LineChart>;

export type FilterableLineChartProps = Omit<MantineLineChartProps, 'series' | 'withLegend'> & {
  series: NonNullable<MantineLineChartProps['series']>;
};

/**
 * Wraps Mantine LineChart: with multiple series, legend is clickable to show one series;
 * click the active series again to show all. Single-series charts use the default Mantine legend.
 */
export function FilterableLineChart({ series, ...rest }: FilterableLineChartProps) {
  const theme = useMantineTheme();
  const [isolatedName, setIsolatedName] = useState<string | null>(null);

  useEffect(() => {
    if (isolatedName !== null && !series.some((s) => s.name === isolatedName)) {
      setIsolatedName(null);
    }
  }, [series, isolatedName]);

  if (series.length <= 1) {
    return <LineChart series={series} withLegend {...rest} />;
  }

  const displaySeries =
    isolatedName !== null ? series.filter((s) => s.name === isolatedName) : series;

  const toggle = (name: string) => {
    setIsolatedName((prev) => (prev === name ? null : name));
  };

  return (
    <Stack gap="sm">
      <LineChart series={displaySeries} withLegend={false} {...rest} />
      <Group gap="md" wrap="wrap" justify="flex-end">
        {series.map((s) => {
          const pressed = isolatedName === s.name;
          const swatchColor =
            typeof s.color === 'string' && !s.color.startsWith('var(')
              ? s.color
              : getThemeColor(s.color ?? 'blue', theme);

          return (
            <UnstyledButton
              key={s.name}
              type="button"
              onClick={() => toggle(s.name)}
              aria-pressed={pressed}
              style={{
                borderRadius: theme.radius.sm,
                padding: `${theme.spacing.xs} ${theme.spacing.sm}`,
                opacity: isolatedName !== null && !pressed ? 0.55 : 1,
                transition: 'opacity 120ms ease',
              }}
            >
              <Group gap={6} wrap="nowrap" align="flex-start">
                <ColorSwatch color={swatchColor} size={12} withShadow={false} style={{ flexShrink: 0, marginTop: 2 }} />
                <Text
                  size="sm"
                  style={{
                    whiteSpace: 'normal',
                    wordBreak: 'break-word',
                    lineHeight: 1.35,
                    textAlign: 'left',
                  }}
                >
                  {s.label ?? s.name}
                </Text>
              </Group>
            </UnstyledButton>
          );
        })}
      </Group>
    </Stack>
  );
}

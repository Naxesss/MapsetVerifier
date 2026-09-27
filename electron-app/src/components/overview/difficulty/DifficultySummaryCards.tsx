import { SimpleGrid, Stack, Text } from '@mantine/core';
import { MicroLabel } from '../../common/Headings.tsx';
import SectionCard from '../../common/SectionCard.tsx';
import type { DifficultyOverviewDifficulty } from '../../../Types';

export function SummaryCard({
  label,
  value,
  subValue,
}: {
  label: string;
  value: string;
  subValue?: string;
}) {
  return (
    <SectionCard>
      <Stack gap="xs">
        <MicroLabel>{label}</MicroLabel>
        <Text fw={700} size="lg">
          {value}
        </Text>
        {subValue && (
          <Text size="xs" c="dimmed" truncate>
            {subValue}
          </Text>
        )}
      </Stack>
    </SectionCard>
  );
}

const formatStars = (starRating: number) => `★ ${starRating.toFixed(2)}`;

/** The spread at a glance: easiest and hardest difficulty, and the biggest jump between neighbours. */
export function DifficultySpreadSummary({
  difficulties,
}: {
  difficulties: DifficultyOverviewDifficulty[];
}) {
  if (difficulties.length === 0) {
    return null;
  }

  const sorted = [...difficulties].sort((a, b) => a.starRating - b.starRating);
  const lowest = sorted[0];
  const highest = sorted[sorted.length - 1];

  let largestGap: { from: DifficultyOverviewDifficulty; to: DifficultyOverviewDifficulty } | null =
    null;
  for (let i = 1; i < sorted.length; i++) {
    const gap = sorted[i].starRating - sorted[i - 1].starRating;
    if (!largestGap || gap > largestGap.to.starRating - largestGap.from.starRating) {
      largestGap = { from: sorted[i - 1], to: sorted[i] };
    }
  }

  return (
    <SimpleGrid cols={{ base: 1, md: 3 }} spacing="md">
      <SummaryCard
        label="Lowest star rating"
        value={formatStars(lowest.starRating)}
        subValue={lowest.version}
      />
      <SummaryCard
        label="Highest star rating"
        value={formatStars(highest.starRating)}
        subValue={highest.version}
      />
      <SummaryCard
        label="Largest gap"
        value={
          largestGap
            ? `★ ${(largestGap.to.starRating - largestGap.from.starRating).toFixed(2)}`
            : '–'
        }
        subValue={
          largestGap
            ? `${largestGap.from.version} → ${largestGap.to.version}`
            : 'Only one difficulty'
        }
      />
    </SimpleGrid>
  );
}

import { Box, Group, Loader, SegmentedControl, Text } from '@mantine/core';
import { ApiCategoryCheckResult, DifficultyLevel } from '../../Types';
import DifficultyName from '../common/DifficultyName';

interface DifficultyLevelOverrideProps {
  selectedDifficulty: ApiCategoryCheckResult;
  currentOverrideLevel?: string;
  isLoading: boolean;
  onOverrideChange: (category: string, level: string | null) => void;
}

const SHOWING_DIFFICULTY_LEVELS: DifficultyLevel[] = ['Easy', 'Normal', 'Hard', 'Insane', 'Expert'];

function DifficultyLevelOverride({
  selectedDifficulty,
  currentOverrideLevel,
  isLoading,
  onOverrideChange,
}: DifficultyLevelOverrideProps) {
  const current = selectedDifficulty.difficultyLevel || 'Unknown';
  const selected = currentOverrideLevel || current;

  return (
    <Group gap="sm" align="center" wrap="nowrap">
      <Text size="sm" c="dimmed">
        Interpreted as
      </Text>
      <SegmentedControl
        radius="md"
        data={SHOWING_DIFFICULTY_LEVELS.map((lvl) => ({
          label: <DifficultyName difficulty={lvl} mode={selectedDifficulty.mode} />,
          value: lvl,
        }))}
        value={selected}
        onChange={(val) => {
          const isDefault = val === current || (current === 'Expert' && val === 'Ultra');
          if (isDefault) {
            onOverrideChange(selectedDifficulty.category, null);
          } else {
            onOverrideChange(selectedDifficulty.category, val);
          }
        }}
        fullWidth={false}
        // Exactly the height of the selected-difficulty row, so the row doesn't grow when it appears.
        styles={{ root: { maxWidth: '100%', height: 'var(--mv-control-height)' } }}
      />
      {/* Space for the loader is always kept, so the switch doesn't shift when it appears. */}
      <Box w={18} style={{ display: 'flex', justifyContent: 'center' }} aria-hidden={!isLoading}>
        {isLoading && <Loader size="xs" />}
      </Box>
    </Group>
  );
}

export default DifficultyLevelOverride;

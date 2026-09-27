import { ActionIcon, Button, Group, Menu, Text, Tooltip } from '@mantine/core';
import { IconArrowBackUp, IconCheck, IconChevronDown } from '@tabler/icons-react';
import { ApiCategoryCheckResult, DifficultyLevel } from '../../Types';
import DifficultyName from '../common/DifficultyName';

interface DifficultyLevelOverrideProps {
  selectedDifficulty: ApiCategoryCheckResult;
  currentOverrideLevel?: string;
  onOverrideChange: (category: string, level: string | null) => void;
}

const SHOWING_DIFFICULTY_LEVELS: DifficultyLevel[] = ['Easy', 'Normal', 'Hard', 'Insane', 'Expert'];

export const getDifficultyBadgeColor = (difficulty: string) => {
  switch (difficulty) {
    case 'Easy':
      return 'blue';
    case 'Normal':
      return 'green';
    case 'Hard':
      return 'yellow';
    case 'Insane':
      return 'red';
    case 'Expert':
      return 'grape';
    default:
      return 'grape';
  }
};

/** Shade of a level's colour that reads as text on the dark buttons and menus. */
const levelTextColor = (level: string) => `${getDifficultyBadgeColor(level)}.4`;

/**
 * "Interpreted as <level>" on the right of the selected difficulty's row: one button that both
 * shows the level the checks use and changes it, listing the levels with the detected one marked.
 * Once changed, a reset button appears to its left (so the button itself stays put) and goes back
 * to the detected level.
 */
function DifficultyLevelOverride({
  selectedDifficulty,
  currentOverrideLevel,
  onOverrideChange,
}: DifficultyLevelOverrideProps) {
  const detected = selectedDifficulty.difficultyLevel || 'Unknown';
  const selected = currentOverrideLevel || detected;
  const mode = selectedDifficulty.mode;

  const choose = (level: DifficultyLevel) => {
    const isDefault = level === detected || (detected === 'Expert' && level === 'Ultra');
    onOverrideChange(selectedDifficulty.category, isDefault ? null : level);
  };

  const isOverridden = selected !== detected;

  return (
    <Group gap="xs" wrap="nowrap">
      {isOverridden && (
        <Tooltip label="Back to the detected level">
          <ActionIcon
            variant="default"
            size="input-sm"
            aria-label="Back to the detected level"
            onClick={() => onOverrideChange(selectedDifficulty.category, null)}
          >
            <IconArrowBackUp size={16} stroke={1.5} />
          </ActionIcon>
        </Tooltip>
      )}
      <Menu position="bottom-end" withinPortal>
        <Menu.Target>
          <Button
            variant="default"
            size="sm"
            rightSection={<IconChevronDown size={14} stroke={1.5} />}
          >
            <Text span inherit c="dimmed" mr={4}>
              Interpreted as
            </Text>
            {/* The level in its own colour, as the level badge used to show it. */}
            <Text span inherit fw={700} c={levelTextColor(selected)}>
              <DifficultyName difficulty={selected as DifficultyLevel} mode={mode} />
            </Text>
          </Button>
        </Menu.Target>
        <Menu.Dropdown>
          <Menu.Label>Check this difficulty as</Menu.Label>
          {SHOWING_DIFFICULTY_LEVELS.map((level) => (
            <Menu.Item
              key={level}
              leftSection={<IconCheck size={14} style={{ opacity: level === selected ? 1 : 0 }} />}
              rightSection={
                level === detected && (
                  <Text size="xs" c="dimmed">
                    Detected
                  </Text>
                )
              }
              onClick={() => choose(level)}
            >
              <Text span inherit c={levelTextColor(level)}>
                <DifficultyName difficulty={level} mode={mode} />
              </Text>
            </Menu.Item>
          ))}
        </Menu.Dropdown>
      </Menu>
    </Group>
  );
}

export default DifficultyLevelOverride;

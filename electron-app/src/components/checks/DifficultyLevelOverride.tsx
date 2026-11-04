import { ActionIcon, Button, Divider, Group, Menu, Text, Tooltip } from '@mantine/core';
import { IconArrowBackUp, IconChevronDown } from '@tabler/icons-react';
import { useSettings } from '../../context/SettingsContext';
import { ApiCategoryCheckResult, DifficultyLevel } from '../../Types';
import { formatDifficultyName } from '../common/DifficultyName';
import GameModeIcon from '../icons/GameModeIcon';

interface DifficultyLevelOverrideProps {
  selectedDifficulty: ApiCategoryCheckResult;
  currentOverrideLevel?: string;
  onOverrideChange: (category: string, level: string | null) => void;
}

const SHOWING_DIFFICULTY_LEVELS: DifficultyLevel[] = ['Easy', 'Normal', 'Hard', 'Insane', 'Expert'];

const MODE_ICON_SIZE = 16;
const MODE_ICON_GAP = 6;

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
const levelColor = (level: string) => `var(--mantine-color-${getDifficultyBadgeColor(level)}-4)`;

/**
 * "Interpreted as <level>" on the right of the selected difficulty's row: one hover menu that both
 * shows the level the checks use and changes it. The chosen level is highlighted, and the
 * detected one is marked. Once changed, a reset button appears to its left (so the menu itself
 * stays put) and goes back to the detected level.
 */
function DifficultyLevelOverride({
  selectedDifficulty,
  currentOverrideLevel,
  onOverrideChange,
}: DifficultyLevelOverrideProps) {
  const { settings } = useSettings();
  const detected = selectedDifficulty.difficultyLevel || 'Unknown';
  const selected = currentOverrideLevel || detected;
  const mode = selectedDifficulty.mode;

  const nameOf = (level: string) =>
    formatDifficultyName(level, mode, settings.showGamemodeDifficultyNames);

  const levels = SHOWING_DIFFICULTY_LEVELS.includes(selected as DifficultyLevel)
    ? SHOWING_DIFFICULTY_LEVELS
    : [selected, ...SHOWING_DIFFICULTY_LEVELS];

  const choose = (level: string) => {
    if (level === selected) return;
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
      <Menu trigger="hover" position="bottom-end" width="target" withinPortal>
        <Menu.Target>
          <Button
            variant="default"
            size="sm"
            aria-label={`Interpreted as ${nameOf(selected)}`}
            leftSection={
              <Text span size="sm" c="dimmed" fw={500}>
                Interpreted as
              </Text>
            }
            rightSection={<IconChevronDown size={16} stroke={1.5} />}
          >
            {mode && (
              <GameModeIcon
                mode={mode}
                size={MODE_ICON_SIZE}
                color={levelColor(selected)}
                style={{ marginInlineEnd: MODE_ICON_GAP }}
              />
            )}
            <Text span inherit fw={700} c={levelTextColor(selected)}>
              {nameOf(selected)}
            </Text>
          </Button>
        </Menu.Target>
        <Menu.Dropdown>
          <Menu.Label>
            <Divider size="xs" label="Check this difficulty as" labelPosition="left" />
          </Menu.Label>
          {levels.map((level) => (
            <Menu.Item
              key={level}
              className="mv-level-option"
              data-checked={level === selected || undefined}
              onClick={() => choose(level)}
            >
              <Group gap={6} wrap="nowrap">
                {mode && (
                  <GameModeIcon mode={mode} size={MODE_ICON_SIZE} color={levelColor(level)} />
                )}
                <Text span inherit fw={700} c={levelTextColor(level)}>
                  {nameOf(level)}
                </Text>
                {level === detected && (
                  <Text size="xs" c="dimmed" fw={400}>
                    Detected
                  </Text>
                )}
              </Group>
            </Menu.Item>
          ))}
        </Menu.Dropdown>
      </Menu>
    </Group>
  );
}

export default DifficultyLevelOverride;

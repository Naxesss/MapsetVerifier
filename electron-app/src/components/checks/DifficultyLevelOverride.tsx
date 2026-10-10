import { ActionIcon, Box, Group, Select, Text, Tooltip } from '@mantine/core';
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

/** Room for "Interpreted as", so the selected level (icon and name) starts after it. */
const PREFIX_WIDTH = 112;
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
 * "Interpreted as <level>" on the right of the selected difficulty's row: one select that both
 * shows the level the checks use and changes it. The chosen level is highlighted, and the
 * detected one is marked. Once changed, a reset button appears to its left (so the select itself
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
      <Box pos="relative" w="fit-content">
        <Select
          aria-label={`Interpreted as ${nameOf(selected)}`}
          size="sm"
          w="auto"
          allowDeselect={false}
          withCheckIcon={false}
          comboboxProps={{ position: 'bottom-end', withinPortal: true, width: 'max-content' }}
          value={selected}
          data={[
            {
              group: 'Check this difficulty as',
              items: levels.map((level) => ({ value: level, label: nameOf(level) })),
            },
          ]}
          leftSectionWidth={PREFIX_WIDTH}
          leftSectionPointerEvents="none"
          leftSection={
            <Text size="sm" c="dimmed" fw={500}>
              Interpreted as
            </Text>
          }
          rightSection={<IconChevronDown size={14} stroke={1.5} />}
          rightSectionPointerEvents="none"
          classNames={{ option: 'mv-level-option' }}
          styles={{
            root: { width: 'auto' },
            wrapper: { width: 'auto' },
            input: {
              width: 'auto',
              fieldSizing: 'content',
              color: levelColor(selected),
              fontWeight: 700,
              paddingInlineStart: mode ? PREFIX_WIDTH + MODE_ICON_SIZE + MODE_ICON_GAP : undefined,
            },
          }}
          onChange={(level) => level && choose(level)}
          renderOption={({ option }) => (
            <Group gap={6} wrap="nowrap">
              {mode && (
                <GameModeIcon mode={mode} size={MODE_ICON_SIZE} color={levelColor(option.value)} />
              )}
              <Text span inherit fw={700} c={levelTextColor(option.value)}>
                {option.label}
              </Text>
              {option.value === detected && (
                <Text size="xs" c="dimmed" fw={400}>
                  Detected
                </Text>
              )}
            </Group>
          )}
        />
        {mode && (
          <GameModeIcon
            mode={mode}
            size={MODE_ICON_SIZE}
            color={levelColor(selected)}
            style={{
              position: 'absolute',
              left: PREFIX_WIDTH,
              top: '50%',
              transform: 'translateY(-50%)',
              pointerEvents: 'none',
            }}
          />
        )}
      </Box>
    </Group>
  );
}

export default DifficultyLevelOverride;

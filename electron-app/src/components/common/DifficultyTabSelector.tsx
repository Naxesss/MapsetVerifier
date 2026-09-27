import { alpha, Button, Flex, Group, Text, Tooltip, useMantineTheme } from '@mantine/core';
import { getDifficultyColor } from './DifficultyColor';
import DifficultyColorPill from './DifficultyColorPill';
import LevelIcon from '../icons/LevelIcon';
import type { Level } from '../../Types';
import type { ReactNode } from 'react';

export const GENERAL_TAB_ID = 'General';

export type DifficultyTab = {
  id: string;
  label: string;
  starRating?: number | null;
  level?: Level;
  levelLoading?: boolean;
  leading?: ReactNode;
  disabled?: boolean;
  disabledTooltip?: string;
};

export interface DifficultyTabSelectorProps {
  tabs: DifficultyTab[];
  selectedId?: string;
  onSelect: (id: string) => void;
  sortByStarRating?: boolean;
  /** When true, General shows the active border while nothing is selected. */
  highlightGeneralWhenIdle?: boolean;
  generalLevel?: Level;
  generalLeading?: ReactNode;
  showLevelIcons?: boolean;
  levelLoading?: boolean;
}

const labelStyle = {
  overflow: 'hidden',
  textOverflow: 'ellipsis',
  whiteSpace: 'nowrap',
  maxWidth: 400,
} as const;

function DifficultyTabSelector({
  tabs,
  selectedId,
  onSelect,
  sortByStarRating = false,
  highlightGeneralWhenIdle = false,
  generalLevel,
  generalLeading,
  showLevelIcons = false,
  levelLoading = false,
}: DifficultyTabSelectorProps) {
  const theme = useMantineTheme();
  const diffButtonBg = alpha(theme.colors.dark[5], 0.55);
  const diffButtonSelectedBorder = theme.colors.dark[2];
  const diffButtonHoverAlpha = 0.35;
  const generalButtonBg = alpha(theme.colors.dark[4], 0.6);
  const generalButtonHover = alpha(theme.colors.dark[3], 0.7);

  const displayTabs = sortByStarRating
    ? [...tabs].sort((a, b) => (a.starRating ?? 0) - (b.starRating ?? 0))
    : tabs;

  const isGeneralActive =
    selectedId === GENERAL_TAB_ID || (highlightGeneralWhenIdle && !selectedId);

  return (
    <Group gap="xs">
      <Group
        p="xs"
        gap="xs"
        bg="hsl(200deg 10% 10% / 50%)"
        style={{ borderRadius: theme.radius.md }}
      >
        <Button
          h="fit-content"
          p="xs"
          variant="light"
          style={{
            '--button-bg': generalButtonBg,
            '--button-hover': generalButtonHover,
          }}
          onClick={() => onSelect(GENERAL_TAB_ID)}
          bd={isGeneralActive ? `1px solid ${diffButtonSelectedBorder}` : '1px solid transparent'}
        >
          <Flex gap="xs" align="center">
            {showLevelIcons && (generalLevel != null || levelLoading) && (
              <LevelIcon level={generalLevel ?? 'Check'} size={24} loading={levelLoading} />
            )}
            {generalLeading}
            <Text c="white">General</Text>
          </Flex>
        </Button>
        {displayTabs.map((tab) => {
          const isActive = selectedId === tab.id;
          const srColor = getDifficultyColor(tab.starRating ?? 0);

          const button = (
            <Button
              key={tab.id}
              onClick={tab.disabled ? undefined : () => onSelect(tab.id)}
              disabled={tab.disabled}
              variant="light"
              style={{
                '--button-bg': diffButtonBg,
                '--button-hover': alpha(srColor, diffButtonHoverAlpha),
                opacity: tab.disabled ? 0.45 : undefined,
                filter: tab.disabled ? 'grayscale(70%)' : undefined,
                cursor: tab.disabled ? 'not-allowed' : undefined,
              }}
              size="compact-md"
              h="fit-content"
              p="xs"
              bd={
                isActive
                  ? `1px solid ${srColor}`
                  : tab.disabled
                    ? `1px dashed ${theme.colors.dark[2]}`
                    : '1px solid transparent'
              }
            >
              <Flex gap="xs" align="center">
                {showLevelIcons && (tab.level != null || tab.levelLoading) && (
                  <LevelIcon level={tab.level ?? 'Check'} size={24} loading={tab.levelLoading} />
                )}
                {tab.leading}
                <DifficultyColorPill color={srColor} />
                <Text c={tab.disabled ? 'dimmed' : 'white'} style={labelStyle}>
                  {tab.label}
                </Text>
              </Flex>
            </Button>
          );

          if (tab.disabled && tab.disabledTooltip) {
            return (
              <Tooltip key={tab.id} label={tab.disabledTooltip}>
                <div>{button}</div>
              </Tooltip>
            );
          }

          return button;
        })}
      </Group>
    </Group>
  );
}

export default DifficultyTabSelector;

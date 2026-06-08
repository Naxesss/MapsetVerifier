import { alpha, Button, Flex, Group, Text, useMantineTheme } from '@mantine/core';
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
};

export interface DifficultyTabSelectorProps {
  tabs: DifficultyTab[];
  selectedId?: string;
  onSelect: (id: string) => void;
  sortByStarRating?: boolean;
  activeOnHover?: boolean;
  hoveredId?: string;
  onHover?: (id: string | undefined) => void;
  hoverRestoreId?: string;
  /** When true, General shows active border while no difficulty tab is hovered. */
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
  activeOnHover = false,
  hoveredId,
  onHover,
  hoverRestoreId,
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
    selectedId === GENERAL_TAB_ID || (highlightGeneralWhenIdle && hoveredId === undefined && !selectedId);

  const handleHover = (id: string | undefined) => {
    onHover?.(id);
  };

  const handleHoverLeave = () => {
    onHover?.(hoverRestoreId);
  };

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
          onMouseEnter={activeOnHover ? () => handleHover(undefined) : undefined}
          onMouseLeave={activeOnHover ? handleHoverLeave : undefined}
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
          const isActive = selectedId === tab.id || (activeOnHover && hoveredId === tab.id);
          const srColor = getDifficultyColor(tab.starRating ?? 0);

          return (
            <Button
              key={tab.id}
              onClick={() => onSelect(tab.id)}
              variant="light"
              style={{
                '--button-bg': diffButtonBg,
                '--button-hover': alpha(srColor, diffButtonHoverAlpha),
              }}
              size="compact-md"
              h="fit-content"
              p="xs"
              onMouseEnter={activeOnHover ? () => handleHover(tab.id) : undefined}
              onMouseLeave={activeOnHover ? handleHoverLeave : undefined}
              bd={isActive ? `1px solid ${srColor}` : '1px solid transparent'}
            >
              <Flex gap="xs" align="center">
                {showLevelIcons && (tab.level != null || tab.levelLoading) && (
                  <LevelIcon level={tab.level ?? 'Check'} size={24} loading={tab.levelLoading} />
                )}
                {tab.leading}
                <DifficultyColorPill color={srColor} />
                <Text c="white" style={labelStyle}>
                  {tab.label}
                </Text>
              </Flex>
            </Button>
          );
        })}
      </Group>
    </Group>
  );
}

export default DifficultyTabSelector;

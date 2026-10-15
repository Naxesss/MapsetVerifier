import { Badge, Box, Flex, Group, Text, Tooltip, useMantineTheme } from '@mantine/core';
import {
  IconAlertTriangleFilled,
  IconCircleCheckFilled,
  IconCircleDashed,
  IconCircleHalf2,
  IconUser,
} from '@tabler/icons-react';
import { useState } from 'react';
import RcLeadText from './RcLeadText';
import { difficultyStarRating, KIND_COLOR, linkedCheckNames, pageMode } from './rcUtils';
import { ApiRcStatement } from '../../Types';
import { countWord } from '../../utils/countWord';
import GameModeIcon from '../icons/GameModeIcon';

interface RcStatementRowProps {
  statement: ApiRcStatement;
  /** Show which page the statement is on, for results spanning several pages. */
  showPage?: boolean;
  /** Hide the parent's lead, because the parent is shown right above this row. */
  hideParentLead?: boolean;
  /** Does not match the filter itself, only shown as the parent of statements that do. */
  muted?: boolean;
  onOpen: (statement: ApiRcStatement) => void;
}

const STATUS_ICON_SIZE = 22;
/** Line heights of the lead (size md), the parent's lead above it (size xs) and the status label (size sm). */
const TITLE_LINE = 'var(--mantine-font-size-md) * var(--mantine-line-height-md)';
const PARENT_LEAD_LINE = 'var(--mantine-font-size-xs) * var(--mantine-line-height-xs)';
const LABEL_LINE = 'var(--mantine-font-size-sm) * var(--mantine-line-height-sm)';

/** One glanceable status at the start of the row: covered, not covered, or manual. */
function StatusIcon({ statement }: { statement: ApiRcStatement }) {
  const theme = useMantineTheme();

  switch (statement.coverage) {
    case 'Partial':
      return (
        <Tooltip label="Partly covered by checks" withinPortal>
          <IconCircleHalf2
            size={STATUS_ICON_SIZE}
            color={theme.colors.yellow[6]}
            aria-label="Partly covered"
          />
        </Tooltip>
      );
    case 'Covered':
      return (
        <Tooltip label="Covered by a check" withinPortal>
          <IconCircleCheckFilled
            size={STATUS_ICON_SIZE}
            color={theme.colors.green[6]}
            aria-label="Covered"
          />
        </Tooltip>
      );
    case 'Outdated':
      return (
        <Tooltip label="Changed on the wiki since its checks were reviewed" withinPortal>
          <IconAlertTriangleFilled
            size={STATUS_ICON_SIZE}
            color={theme.colors.red[6]}
            aria-label="Outdated"
          />
        </Tooltip>
      );
    case 'Manual':
      return (
        <Tooltip label="Needs human judgement" withinPortal>
          <IconUser size={STATUS_ICON_SIZE} color={theme.colors.gray[6]} aria-label="Manual" />
        </Tooltip>
      );
    case 'Uncovered':
      return (
        <Tooltip label="No check yet" withinPortal>
          <IconCircleDashed
            size={STATUS_ICON_SIZE}
            color={theme.colors.gray[6]}
            aria-label="Not covered"
          />
        </Tooltip>
      );
    default:
      // Allowances have nothing to cover; keep the space so rows stay aligned.
      return <Box w={STATUS_ICON_SIZE} />;
  }
}

/** The status icon in words, plain so the icon stays the only colour on the row. */
function statusLabel(statement: ApiRcStatement) {
  if (statement.links.length > 0) return countWord(linkedCheckNames(statement).length, 'check');

  switch (statement.coverage) {
    // Covered through its nested statements rather than a check of its own.
    case 'Covered':
      return 'Via sub-rules';
    case 'Partial':
      return 'Partly via sub-rules';
    case 'Manual':
      return 'Manual';
    case 'Uncovered':
      return 'No check';
    default:
      return null;
  }
}

/**
 * An intro such as "The audio file of a beatmap must..." which only opens the sentence its nested
 * rules finish. Shaped like a rule row so it lines up, but muted and inert, as there is nothing to
 * open or cover.
 */
export function RcIntroRow({ statement }: { statement: ApiRcStatement }) {
  const theme = useMantineTheme();

  return (
    <Group
      p="sm"
      gap="sm"
      wrap="nowrap"
      ml={statement.parentId ? 'xl' : 0}
      aria-disabled
      style={{
        border: '1px dashed var(--mantine-color-default-border)',
        borderRadius: theme.defaultRadius,
        cursor: 'default',
      }}
    >
      <Box w={STATUS_ICON_SIZE} style={{ flexShrink: 0 }} />
      <Text fw="bold" c="dimmed" style={{ flex: 1, minWidth: 0 }}>
        <RcLeadText>{statement.lead}</RcLeadText>
      </Text>
      <Text size="xs" c="dimmed" style={{ flexShrink: 0 }}>
        Introduces the rules below
      </Text>
    </Group>
  );
}

/** A ranking criteria statement, laid out like a check on the documentation page. */
export default function RcStatementRow({
  statement,
  showPage,
  hideParentLead,
  muted,
  onOpen,
}: RcStatementRowProps) {
  const theme = useMantineTheme();
  const [hovered, setHovered] = useState(false);
  const background = hovered
    ? theme.variantColorResolver({ variant: 'light', theme, color: 'blue' }).background
    : theme.variantColorResolver({ variant: 'light', theme, color: 'gray' }).background;

  const mode = pageMode(statement.page);
  const showParentLead = !!statement.parentLead && !hideParentLead;
  const label = statusLabel(statement);

  // Centres an element of the given height on the first line of the lead, below the parent's lead
  // when that is shown, so the icon and label stay put however far the lead wraps.
  const titleLineOffset = (height: string) =>
    `calc(${showParentLead ? PARENT_LEAD_LINE : '0px'} + (${TITLE_LINE} - ${height}) / 2)`;

  return (
    <Group
      align="flex-start"
      style={{
        background,
        borderRadius: theme.defaultRadius,
        cursor: 'pointer',
        transition: 'background 0.2s, opacity 0.2s',
        // Shown only as context for the matching statements nested in it; full strength on hover.
        opacity: muted && !hovered ? 0.5 : 1,
      }}
      p="sm"
      gap="sm"
      wrap="nowrap"
      ml={statement.parentId ? 'xl' : 0}
      role="button"
      tabIndex={0}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
      onClick={() => onOpen(statement)}
      onKeyDown={(event) => {
        if (event.key === 'Enter' || event.key === ' ') {
          event.preventDefault();
          onOpen(statement);
        }
      }}
    >
      <Box
        style={{
          flexShrink: 0,
          lineHeight: 0,
          marginTop: titleLineOffset(`${STATUS_ICON_SIZE}px`),
        }}
      >
        <StatusIcon statement={statement} />
      </Box>
      <Flex direction="column" style={{ flex: 1, minWidth: 0 }}>
        {showParentLead && (
          <Text size="xs" c="dimmed" lineClamp={1}>
            <RcLeadText>{statement.parentLead!}</RcLeadText>
          </Text>
        )}
        <Text fw="bold">
          <RcLeadText>{statement.lead}</RcLeadText>
        </Text>
        <Group gap="xs">
          {mode &&
            (statement.difficulties.length > 0 ? (
              <GameModeIcon
                size={16}
                mode={mode}
                starRating={difficultyStarRating(statement.difficulties[0])}
              />
            ) : (
              <GameModeIcon size={16} mode={mode} color={theme.colors.gray[5]} />
            ))}
          <Badge size="xs" variant="light" color={KIND_COLOR[statement.kind]}>
            {statement.kind}
          </Badge>
          {showPage && (
            <Text size="xs" c="dimmed">
              {[statement.pageTitle, ...statement.path].join(' › ')}
            </Text>
          )}
        </Group>
      </Flex>
      {label && (
        <Text
          size="sm"
          c="dimmed"
          style={{ flexShrink: 0, marginTop: titleLineOffset(`(${LABEL_LINE})`) }}
        >
          {label}
        </Text>
      )}
    </Group>
  );
}

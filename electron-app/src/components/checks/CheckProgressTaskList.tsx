import { Box, Text } from '@mantine/core';
import { useCheckTaskStairway } from './hooks/useCheckTaskStairway';
import type { CheckProgress } from '../../Types';

const STEP_OFFSET_PX = 20;
const TRANSITION_MS = 100;
const TRANSITION_EASING = 'cubic-bezier(0.22, 1, 0.36, 1)';

interface CheckProgressTaskListProps {
  progress: CheckProgress | null;
}

function tierStyle(tier: number, isCurrent: boolean) {
  if (isCurrent) {
    return {
      fontSize: 'var(--mantine-font-size-sm)',
      fontWeight: 600,
      color: 'var(--mantine-color-gray-1)',
      opacity: 1,
    } as const;
  }

  const dimFactor = Math.min(tier, 3);
  return {
    fontSize:
      tier <= 1 ? 'var(--mantine-font-size-xs)' : 'calc(var(--mantine-font-size-xs) * 0.92)',
    fontWeight: 400,
    color: 'var(--mantine-color-dimmed)',
    opacity: Math.max(0.35, 1 - dimFactor * 0.22),
  } as const;
}

function CheckProgressTaskList({ progress }: CheckProgressTaskListProps) {
  const { steps, activeSet, primaryActiveLabel, placeholder } = useCheckTaskStairway(progress);

  const visibleCount = Math.max(steps.length, placeholder ? 1 : 0, 1);
  const containerHeight = STEP_OFFSET_PX * Math.min(visibleCount, 5) + 4;

  const showPlaceholder = !!placeholder && !primaryActiveLabel;
  const tierOffset = showPlaceholder ? 1 : 0;

  return (
    <Box
      pos="relative"
      mt={4}
      style={{
        height: containerHeight,
        transition: `height ${TRANSITION_MS}ms ${TRANSITION_EASING}`,
      }}
    >
      {steps.map((task, index) => {
        const tier = steps.length - 1 - index + tierOffset;
        const isCurrent =
          tier === 0 && activeSet.has(task.label) && task.label === primaryActiveLabel;
        const style = tierStyle(tier, isCurrent);

        return (
          <Text
            key={task.id}
            size="sm"
            lh={1.35}
            truncate
            pos="absolute"
            left={0}
            right={0}
            style={{
              bottom: tier * STEP_OFFSET_PX,
              ...style,
              transition: `bottom ${TRANSITION_MS}ms ${TRANSITION_EASING}, opacity ${TRANSITION_MS}ms ${TRANSITION_EASING}, color ${TRANSITION_MS}ms ${TRANSITION_EASING}`,
            }}
          >
            {task.label}
          </Text>
        );
      })}

      {showPlaceholder ? (
        <Text
          size="sm"
          fw={600}
          c="gray.1"
          lh={1.35}
          pos="absolute"
          left={0}
          right={0}
          bottom={0}
          style={{
            transition: `opacity ${TRANSITION_MS}ms ${TRANSITION_EASING}`,
          }}
        >
          {placeholder}
        </Text>
      ) : null}
    </Box>
  );
}

export default CheckProgressTaskList;

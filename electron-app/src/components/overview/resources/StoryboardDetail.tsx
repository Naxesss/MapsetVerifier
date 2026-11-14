import { Badge, Group, Stack, Text } from '@mantine/core';
import { countWord } from '../../../utils/countWord.ts';
import SectionCard from '../../common/SectionCard.tsx';
import { StatField } from '../../common/StatField.tsx';
import type { StoryboardInfo } from '../../../Types';

interface StoryboardDetailProps {
  storyboard: StoryboardInfo;
}

/** The set-wide .osb and each difficulty's own storyboard, by what they hold. */
function StoryboardDetail({ storyboard }: StoryboardDetailProps) {
  const perDifficulty = storyboard.difficultySpecificStoryboards.filter((d) => d.hasStoryboard);
  const hasAny = storyboard.hasOsb || perDifficulty.length > 0;

  return (
    <SectionCard title="Contents">
      {!hasAny ? (
        <Text size="sm" c="dimmed">
          No storyboard.
        </Text>
      ) : (
        <Stack gap="md">
          {storyboard.hasOsb && storyboard.osbFileName && (
            <StatField
              label=".osb file"
              value={storyboard.osbFileName}
              note={storyboard.osbIsUsed ? 'Has storyboard content' : 'Has no storyboard content'}
            />
          )}
          {perDifficulty.length > 0 && (
            <StatField
              label="Difficulty storyboards"
              value={
                <Stack gap="xs">
                  {perDifficulty.map((d) => (
                    <Group key={d.version} gap="sm" wrap="nowrap">
                      <Badge>{d.version}</Badge>
                      <Text size="sm" c="dimmed">
                        {countWord(d.spriteCount, 'sprite')},{' '}
                        {countWord(d.animationCount, 'animation')},{' '}
                        {countWord(d.sampleCount, 'sample')}
                      </Text>
                    </Group>
                  ))}
                </Stack>
              }
            />
          )}
        </Stack>
      )}
    </SectionCard>
  );
}

export default StoryboardDetail;

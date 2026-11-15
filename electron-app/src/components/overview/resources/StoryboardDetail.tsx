import { Badge, Group, Stack, Text } from '@mantine/core';
import { IconMovieOff } from '@tabler/icons-react';
import FileCard, { WideStat } from './FileCard.tsx';
import StoryboardImages from './storyboard/StoryboardImages.tsx';
import StoryboardSamples from './storyboard/StoryboardSamples.tsx';
import { useStoryboardDetail } from './storyboard/useStoryboardDetail.ts';
import { countWord } from '../../../utils/countWord.ts';
import EmptyState from '../../common/EmptyState.tsx';
import { StatField } from '../../common/StatField.tsx';
import AnalysisTab from '../AnalysisTab.tsx';
import UsedByField from '../UsedByField.tsx';
import type { StoryboardSource } from '../../../Types';

interface StoryboardDetailProps {
  beatmapFolderPath: string | undefined;
}

function sourceContents(source: StoryboardSource) {
  return [
    countWord(source.spriteCount, 'sprite'),
    countWord(source.animationCount, 'animation'),
    countWord(source.sampleCount, 'sample'),
  ].join(', ');
}

/**
 * The storyboard: what it's made of and who sees it, then the images it uses and the samples it
 * plays. Whether any of it breaks a ranking rule is for Checks to say.
 */
function StoryboardDetail({ beatmapFolderPath }: StoryboardDetailProps) {
  const { data, isLoading, isError, error } = useStoryboardDetail(beatmapFolderPath);

  return (
    <AnalysisTab
      data={data}
      isLoading={isLoading}
      isError={isError}
      error={error}
      subject="storyboard"
      padded={false}
    >
      {(data) => {
        const isEmpty = data.spriteCount + data.animationCount + data.sampleCount === 0;
        if (isEmpty) {
          return (
            <EmptyState
              icon={IconMovieOff}
              title="Empty storyboard"
              description="The storyboard file has no sprites, animations or samples."
            />
          );
        }

        const missingImages = data.images.reduce((sum, image) => sum + image.missingFileCount, 0);

        return (
          <>
            <FileCard
              title="Storyboard"
              info="Everything the storyboard shows and plays, from the .osb every difficulty shares and from each difficulty's own storyboard."
              stats={
                <>
                  <StatField label="Sprites" value={data.spriteCount.toLocaleString()} />
                  <StatField label="Animations" value={data.animationCount.toLocaleString()} />
                  <StatField label="Samples" value={data.sampleCount.toLocaleString()} />
                  <StatField
                    label="Image files"
                    value={data.imageFileCount.toLocaleString()}
                    note={missingImages > 0 ? `${missingImages} missing` : undefined}
                    noteColor="orange"
                  />
                  <StatField label="Image size" value={data.imageSizeFormatted} />
                  <StatField
                    label="Sample files"
                    value={data.sampleFiles.length.toLocaleString()}
                    note={data.sampleFiles.length > 0 ? data.sampleSizeFormatted : undefined}
                  />
                  {data.layers.length > 0 && (
                    <WideStat full>
                      <StatField
                        label="Sprites per layer"
                        value={data.layers
                          .map((layer) => `${layer.layer} ${layer.count.toLocaleString()}`)
                          .join(' · ')}
                      />
                    </WideStat>
                  )}
                  <WideStat full>
                    <StatField
                      label="Storyboards"
                      value={
                        <Stack gap="xs">
                          {data.sources.map((source) => (
                            <Group key={source.name} gap="sm" wrap="nowrap">
                              <Badge style={{ flexShrink: 0, maxWidth: '50%' }}>
                                {source.isOsb ? '.osb' : source.name}
                              </Badge>
                              <Text size="sm" c="dimmed" truncate>
                                {sourceContents(source)}
                              </Text>
                            </Group>
                          ))}
                        </Stack>
                      }
                    />
                  </WideStat>
                  <WideStat full>
                    <UsedByField difficulties={data.usedByDifficulties} />
                  </WideStat>
                </>
              }
            />

            {data.images.length > 0 && (
              <StoryboardImages images={data.images} beatmapFolderPath={beatmapFolderPath} />
            )}

            {data.samples.length > 0 && (
              <StoryboardSamples
                samples={data.samples}
                sampleFiles={data.sampleFiles}
                songDurationMs={data.songDurationMs}
                beatmapFolderPath={beatmapFolderPath}
              />
            )}
          </>
        );
      }}
    </AnalysisTab>
  );
}

export default StoryboardDetail;

import { Box, Group, Stack } from '@mantine/core';
import { useElementSize } from '@mantine/hooks';
import { IconFolderOff } from '@tabler/icons-react';
import { useMemo } from 'react';
import FileDetails from './FileDetails.tsx';
import FileList from './FileList.tsx';
import FilePicker from './FilePicker.tsx';
import { groupResourceFiles } from './resourceFiles.ts';
import { useBeatmap } from '../../../context/BeatmapContext.tsx';
import { useOverviewState } from '../../../context/OverviewContext.tsx';
import { useSettings } from '../../../context/SettingsContext.tsx';
import EmptyState from '../../common/EmptyState.tsx';
import AnalysisTab from '../AnalysisTab.tsx';
import { useMetadataAnalysis } from '../metadata/hooks/useMetadataAnalysis.ts';

/** Below this page width the file list moves from beside the details to a picker above them. */
const NARROW_WIDTH = 760;

/**
 * Every file the mapset uses: a list to pick one from, and that file's details beside it. The
 * list comes from the light metadata analysis; the song and video are only analyzed in depth once
 * picked, as that work is heavy.
 */
function ResourcesOverview() {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();
  const { data, isLoading, isError, error, beatmapFolderPath } = useMetadataAnalysis({
    folder,
    songFolder: settings.songFolder,
  });
  const { resourcesFile, setResourcesFile } = useOverviewState();

  const groups = useMemo(() => (data?.success ? groupResourceFiles(data.resources) : []), [data]);

  const { ref, width } = useElementSize();
  const narrow = width > 0 && width < NARROW_WIDTH;

  return (
    <Box ref={ref}>
      <AnalysisTab
        data={data}
        isLoading={isLoading}
        isError={isError}
        error={error}
        subject="files"
      >
        {(data) => {
          const files = groups.flatMap((group) => group.files);
          // The picked file, else the first: the song, in most mapsets. Also covers a mapset
          // without the file picked in the last one.
          const selected = files.find((file) => file.key === resourcesFile) ?? files[0];

          if (!selected) {
            return (
              <EmptyState
                icon={IconFolderOff}
                title="No files"
                description="This mapset doesn't use any audio, image or video files."
              />
            );
          }

          const details = (
            <FileDetails key={selected.key} file={selected} beatmapFolderPath={beatmapFolderPath} />
          );

          // Too narrow for the list beside the details: a picker above them instead.
          if (narrow) {
            return (
              <Stack gap="md">
                <FilePicker
                  groups={groups}
                  selectedKey={selected.key}
                  onSelect={setResourcesFile}
                  folderSize={data.resources.totalFolderSizeFormatted}
                />
                {details}
              </Stack>
            );
          }

          return (
            <Group align="flex-start" gap="md" wrap="nowrap">
              <FileList
                groups={groups}
                selectedKey={selected.key}
                onSelect={setResourcesFile}
                folderSize={data.resources.totalFolderSizeFormatted}
              />
              <Box style={{ flex: 1, minWidth: 0 }}>{details}</Box>
            </Group>
          );
        }}
      </AnalysisTab>
    </Box>
  );
}

export default ResourcesOverview;

import { ActionIcon, Box, Group, Slider, Tooltip } from '@mantine/core';
import { IconPlayerPauseFilled, IconPlayerPlayFilled, IconVolume } from '@tabler/icons-react';
import { memo, useEffect, useState } from 'react';
import { useSettings } from '../../../../context/SettingsContext.tsx';
import type { TimelinePlayback } from '../hooks/useTimelinePlayback.ts';

type TimelinePlaybackControlsProps = Pick<
  TimelinePlayback,
  'isPlaying' | 'isUnavailable' | 'toggle' | 'setVolume'
>;

function TimelinePlaybackControls({
  isPlaying,
  isUnavailable,
  toggle,
  setVolume,
}: TimelinePlaybackControlsProps) {
  const { settings, setSettings } = useSettings();
  const savedVolume = settings.timelineAudioVolume;
  // Only the audio follows the drag. Saving is left for the release, as settings re-render the app.
  const [draggedVolume, setDraggedVolume] = useState<number | null>(null);

  useEffect(() => {
    setVolume(savedVolume);
  }, [savedVolume, setVolume]);

  const label = isPlaying ? 'Pause' : 'Play';

  return (
    <Group gap="xs" wrap="nowrap" align="center">
      <Tooltip label={isUnavailable ? "This mapset's audio can't be played" : `${label} (Space)`}>
        <ActionIcon variant="filled" aria-label={label} disabled={isUnavailable} onClick={toggle}>
          {isPlaying ? <IconPlayerPauseFilled size={14} /> : <IconPlayerPlayFilled size={14} />}
        </ActionIcon>
      </Tooltip>
      <IconVolume size={16} color="var(--mantine-color-dimmed)" aria-hidden />
      <Box w={96}>
        <Slider
          aria-label="Audio volume"
          size="sm"
          min={0}
          max={100}
          step={1}
          value={draggedVolume ?? savedVolume}
          label={(value) => `${value}%`}
          onChange={(value) => {
            setDraggedVolume(value);
            setVolume(value);
          }}
          onChangeEnd={(value) => {
            setDraggedVolume(null);
            setSettings((prev) => ({ ...prev, timelineAudioVolume: value }));
          }}
        />
      </Box>
    </Group>
  );
}

export default memo(TimelinePlaybackControls);

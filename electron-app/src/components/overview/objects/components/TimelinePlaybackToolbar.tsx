import {
  ActionIcon,
  Box,
  Group,
  HoverCard,
  Slider,
  Stack,
  Switch,
  Text,
  Tooltip,
} from '@mantine/core';
import {
  IconAlertCircle,
  IconPlayerPause,
  IconPlayerPlay,
  IconVolume,
  IconVolumeOff,
} from '@tabler/icons-react';
import { memo } from 'react';
import type { TimelinePlaybackAudioStatus } from '../hooks/useTimelinePlayback.ts';
import { formatTime } from '../timelineUtils.ts';

export interface TimelinePlaybackToolbarProps {
  audioUrl: string | null;
  playbackMapTimeMs: number;
  startTimeMs: number;
  endTimeMs: number;
  muted: boolean;
  volume: number;
  audioStatus: TimelinePlaybackAudioStatus;
  isPlaying: boolean;
  seekSnapEnabled: boolean;
  showCenterPlayheadLine: boolean;
  volumeAccentColor: string;
  mutedAccentColor: string;
  onTogglePlaying: () => void;
  onPause: () => void;
  onScrubToTimeMs: (ms: number) => void;
  onCommitScrub: () => void;
  onSetMuted: (muted: boolean) => void;
  onSetVolume: (volume: number) => void;
  onSetSeekSnapEnabled: (enabled: boolean) => void;
  onShowCenterLineChange: (show: boolean) => void;
}

function TimelinePlaybackToolbarInner({
  audioUrl,
  playbackMapTimeMs,
  startTimeMs,
  endTimeMs,
  muted,
  volume,
  audioStatus,
  isPlaying,
  seekSnapEnabled,
  showCenterPlayheadLine,
  volumeAccentColor,
  mutedAccentColor,
  onTogglePlaying,
  onPause,
  onScrubToTimeMs,
  onCommitScrub,
  onSetMuted,
  onSetVolume,
  onSetSeekSnapEnabled,
  onShowCenterLineChange,
}: TimelinePlaybackToolbarProps) {
  return (
    <Group gap="xs" align="center" wrap="wrap" miw={{ base: '100%', sm: undefined }}>
      <ActionIcon
        variant="filled"
        onClick={() => onTogglePlaying()}
        disabled={audioStatus === 'error'}
        aria-label={isPlaying ? 'Pause' : 'Play'}
      >
        {isPlaying ? <IconPlayerPause size={16} /> : <IconPlayerPlay size={16} />}
      </ActionIcon>
      <HoverCard shadow="md" position="top" openDelay={180} closeDelay={400} withinPortal>
        <HoverCard.Target>
          <ActionIcon
            variant="default"
            disabled={!audioUrl || audioStatus === 'error'}
            aria-label="Volume"
          >
            {muted ? <IconVolumeOff size={16} /> : <IconVolume size={16} />}
          </ActionIcon>
        </HoverCard.Target>
        <HoverCard.Dropdown
          px={8}
          py={8}
          style={{
            overflow: 'visible',
          }}
        >
          <Stack gap="xs" align="center">
            <Box
              style={{
                width: 22,
                height: 132,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                overflow: 'visible',
              }}
            >
              <Box
                component="input"
                aria-label="Volume"
                type="range"
                min={0}
                max={100}
                step={1}
                value={Math.round(volume * 100)}
                onChange={(event) =>
                  onSetVolume(Number(event.currentTarget.value) / 100)
                }
                disabled={audioStatus !== 'ready'}
                style={{
                  WebkitAppearance: 'slider-vertical',
                  appearance: 'auto',
                  writingMode: 'vertical-lr',
                  width: 22,
                  height: 132,
                  transform: 'rotate(180deg)',
                  cursor: audioStatus === 'ready' ? 'pointer' : 'not-allowed',
                  accentColor: muted ? mutedAccentColor : volumeAccentColor,
                }}
              />
            </Box>
            <ActionIcon
              variant="default"
              size="sm"
              disabled={audioStatus !== 'ready'}
              onClick={(event) => {
                event.stopPropagation();
                onSetMuted(!muted);
              }}
              aria-label={muted ? 'Unmute' : 'Mute'}
            >
              {muted ? <IconVolumeOff size={18} /> : <IconVolume size={18} />}
            </ActionIcon>
          </Stack>
        </HoverCard.Dropdown>
      </HoverCard>
      <Text size="xs" ff="monospace" miw={84}>
        {formatTime(playbackMapTimeMs)}
      </Text>
      <Box miw={200} style={{ flex: '1 1 200px', maxWidth: 420 }}>
        <Slider
          min={startTimeMs}
          max={endTimeMs}
          value={playbackMapTimeMs}
          step={1}
          onPointerDown={() => onPause()}
          onChange={(v) => onScrubToTimeMs(v)}
          onChangeEnd={() => onCommitScrub()}
          label={(value) => formatTime(value)}
          size="sm"
        />
      </Box>
      <Tooltip
        multiline
        maw={320}
        label="When on, releasing the seek bar snaps to the nearest quarter-beat in the timing segment at that moment (matching osu!'s rhythm grid)."
      >
        <Box component="span" display="inline-block">
          <Switch
            size="xs"
            checked={seekSnapEnabled}
            onChange={(e) => onSetSeekSnapEnabled(e.currentTarget.checked)}
            label={<Text size="xs">Snap seeks (¼ beat)</Text>}
          />
        </Box>
      </Tooltip>
      <Switch
        size="xs"
        checked={showCenterPlayheadLine}
        onChange={(e) => onShowCenterLineChange(e.currentTarget.checked)}
        label={<Text size="xs">Show center line</Text>}
      />
      {audioStatus === 'loading' ? (
        <Text size="xs" c="dimmed">
          Loading audio…
        </Text>
      ) : audioStatus === 'error' ? (
        <Group gap={4} wrap="nowrap">
          <IconAlertCircle size={14} />
          <Text size="xs" c="dimmed">
            Audio unavailable
          </Text>
        </Group>
      ) : null}
    </Group>
  );
}

export const TimelinePlaybackToolbar = memo(TimelinePlaybackToolbarInner);

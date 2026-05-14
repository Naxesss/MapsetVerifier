import { ActionIcon, Box, Button, Group, Paper, Slider, Stack, Text, Title, useMantineTheme } from "@mantine/core";
import { IconEye, IconEyeOff, IconMinus, IconPlus } from "@tabler/icons-react";
import { useCallback, useMemo, useState, type MouseEvent as ReactMouseEvent } from "react";
import { buildBeatmapAudioUrl } from "../../../../utils/buildBeatmapFolderPath.ts";
import { LABEL_WIDTH } from "../constants.ts";
import { useDifficultyRowDnd } from "../hooks/useDifficultyRowDnd.ts";
import { useDifficultyRowVisibility } from "../hooks/useDifficultyRowVisibility.ts";
import { useHorizontalScrollPan } from "../hooks/useHorizontalScrollPan.ts";
import { usePerModeDifficultyOrder } from "../hooks/usePerModeDifficultyOrder.ts";
import { useTimelinePlayback } from "../hooks/useTimelinePlayback.ts";
import { useTimelineSeekDrag } from "../hooks/useTimelineSeekDrag.ts";
import { useTimelineZoom } from "../hooks/useTimelineZoom.ts";
import { getDifficultyKey } from "../timelineUtils.ts";
import ObjectsGameModeSelector from "./ObjectsGameModeSelector.tsx";
import { TimelinePlaybackToolbar } from "./TimelinePlaybackToolbar.tsx";
import { TimelineViewport } from "./TimelineViewport.tsx";
import type { Mode, ObjectsOverviewDifficulty, ObjectsTimingSegment } from "../../../../Types";
import type { ObjectsModeGroup } from "../types.ts";

interface ObjectsTimelineComparisonProps {
    playbackControlsEnabled: boolean;
    startTimeMs: number;
    endTimeMs: number;
    groupedDifficulties: ObjectsModeGroup[];
    difficulties: ObjectsOverviewDifficulty[];
    selectedMode?: Mode;
    onModeChange: (mode: Mode) => void;
    beatmapFolderPath?: string;
    songFolder?: string;
    playbackAudioFileName?: string;
    playbackTimingSegments?: ObjectsTimingSegment[];
}

export default function ObjectsTimelineComparison({
    playbackControlsEnabled,
    startTimeMs,
    endTimeMs,
    groupedDifficulties,
    difficulties,
    selectedMode,
    onModeChange,
    beatmapFolderPath,
    songFolder,
    playbackAudioFileName,
    playbackTimingSegments,
}: ObjectsTimelineComparisonProps) {
    const theme = useMantineTheme();
    const [showCenterPlayheadLine, setShowCenterPlayheadLine] = useState(true);
    const playbackActive = playbackControlsEnabled && Boolean(playbackAudioFileName?.trim());
    const durationMs = Math.max(1, endTimeMs - startTimeMs);
    const { zoom, setZoom, tickIntervalMs, timelineWidth, adjustZoom, formatZoomLabel } = useTimelineZoom(durationMs);
    const {
        scrollRef,
        isPanningTimeline,
        handleMouseDown: scrollPanMouseDown,
        handleMouseMove,
        stopDragging,
    } = useHorizontalScrollPan();

    const audioUrl = useMemo(() => {
        if (!playbackControlsEnabled) return null;
        const name = playbackAudioFileName?.trim();
        if (!beatmapFolderPath || !name) return null;
        return buildBeatmapAudioUrl(beatmapFolderPath, name, { songFolder });
    }, [playbackControlsEnabled, beatmapFolderPath, playbackAudioFileName, songFolder]);

    const playback = useTimelinePlayback({
        scrollRef,
        timelineWidth,
        startTimeMs,
        endTimeMs,
        audioUrl,
        timingSegments: playbackControlsEnabled ? (playbackTimingSegments ?? []) : [],
        seekSnapDivisor: 4,
    });

    const {
        playbackMapTimeMs,
        scrubToTimeMs,
        commitScrub,
        pause,
        togglePlaying,
        muted,
        setMuted,
        volume,
        setVolume,
        audioStatus,
        seekSnapEnabled,
        setSeekSnapEnabled,
        isPlaying,
        alignPlaybackFromViewportCenter,
    } = playback;

    const seekDrag = useTimelineSeekDrag({
        scrollRef,
        timelineWidth,
        startTimeMs,
        endTimeMs,
        playbackMapTimeMs,
        enabled: playbackActive,
        pause,
        scrubToTimeMs,
        commitScrub,
    });

    const { tryBeginSeekDrag, isSeekDragging } = seekDrag;

    const handleScrollMouseDown = useCallback(
        (event: ReactMouseEvent<HTMLDivElement>) => {
            if (playbackActive && tryBeginSeekDrag(event)) return;
            scrollPanMouseDown(event);
        },
        [playbackActive, tryBeginSeekDrag, scrollPanMouseDown],
    );

    const handleScrollMouseUp = useCallback(() => {
        const wasDragging = stopDragging();
        if (!wasDragging || !playbackControlsEnabled) return;
        alignPlaybackFromViewportCenter();
    }, [playbackControlsEnabled, stopDragging, alignPlaybackFromViewportCenter]);

    const handleScrollMouseLeave = useCallback(() => {
        const wasDragging = stopDragging();
        if (!wasDragging || !playbackControlsEnabled) return;
        alignPlaybackFromViewportCenter();
    }, [playbackControlsEnabled, stopDragging, alignPlaybackFromViewportCenter]);

    const { visibilityByDifficulty, toggleDifficultyVisibility, setManyVisible } =
        useDifficultyRowVisibility(groupedDifficulties);
    const contentWidth = timelineWidth + LABEL_WIDTH;
    const activeMode = selectedMode ?? groupedDifficulties[0]?.mode;
    const { orderedDifficulties, moveDifficulty } = usePerModeDifficultyOrder({
        groupedDifficulties,
        activeMode,
        difficulties,
    });
    const stopPanForDnd = useCallback(() => {
        stopDragging();
    }, [stopDragging]);

    const { sensors, collisionDetection, measuring, handleDragStart, handleDragEnd, handleDragCancel } =
        useDifficultyRowDnd({
            moveDifficulty,
            stopPanning: stopPanForDnd,
        });

    const visibleCount = orderedDifficulties.filter(
        (difficulty) => visibilityByDifficulty[getDifficultyKey(difficulty)] !== false,
    ).length;
    const allVisible = orderedDifficulties.length > 0 && visibleCount === orderedDifficulties.length;
    const allHidden = orderedDifficulties.length > 0 && visibleCount === 0;

    const setSelectedDifficultyVisibility = (visible: boolean) => {
        setManyVisible(orderedDifficulties, visible);
    };

    return (
        <Paper p="md" radius="md" withBorder>
            <Stack gap="md">
                <Group justify="space-between" align="flex-start">
                    <Stack gap={2}>
                        <Title order={4}>Timeline comparison</Title>
                        <Text size="sm" c="dimmed">
                            {playbackControlsEnabled
                                ? "Reorder rows with the grip. The orange line marks the viewport center; the map scrolls under it. Drag timelines or axes to scrub; drag empty strips to pan (when paused, playback syncs from the viewport center after a pan ends)."
                                : "Reorder rows with the grip. Drag timeline horizontally to scroll."}
                        </Text>
                    </Stack>
                    <Group gap="sm" align="center" wrap="wrap" justify="flex-end">
                        <ObjectsGameModeSelector
                            groupedDifficulties={groupedDifficulties}
                            selectedMode={selectedMode}
                            onModeChange={onModeChange}
                        />
                        <Group gap="xs" align="center" wrap="nowrap">
                            <Button
                                variant="default"
                                size="xs"
                                leftSection={<IconEye size={14} />}
                                disabled={orderedDifficulties.length === 0 || allVisible}
                                onClick={() => setSelectedDifficultyVisibility(true)}>
                                Show all
                            </Button>
                            <Button
                                variant="default"
                                size="xs"
                                leftSection={<IconEyeOff size={14} />}
                                disabled={orderedDifficulties.length === 0 || allHidden}
                                onClick={() => setSelectedDifficultyVisibility(false)}>
                                Hide all
                            </Button>
                        </Group>
                        <Group gap="xs" align="center" wrap="nowrap">
                            <ActionIcon variant="default" onClick={() => adjustZoom(-1)}>
                                <IconMinus size={16} />
                            </ActionIcon>
                            <Box miw={220}>
                                <Slider
                                    value={zoom}
                                    min={1}
                                    max={24}
                                    step={1.0}
                                    onChange={setZoom}
                                    label={(value) => `${formatZoomLabel(value)}x`}
                                />
                            </Box>
                            <ActionIcon variant="default" onClick={() => adjustZoom(1)}>
                                <IconPlus size={16} />
                            </ActionIcon>
                        </Group>
                        {playbackActive ? (
                            <TimelinePlaybackToolbar
                                audioUrl={audioUrl}
                                playbackMapTimeMs={playbackMapTimeMs}
                                startTimeMs={startTimeMs}
                                endTimeMs={endTimeMs}
                                muted={muted}
                                volume={volume}
                                audioStatus={audioStatus}
                                isPlaying={isPlaying}
                                seekSnapEnabled={seekSnapEnabled}
                                showCenterPlayheadLine={showCenterPlayheadLine}
                                volumeAccentColor={theme.colors.blue[5]}
                                mutedAccentColor={theme.colors.gray[5]}
                                onTogglePlaying={togglePlaying}
                                onPause={pause}
                                onScrubToTimeMs={scrubToTimeMs}
                                onCommitScrub={commitScrub}
                                onSetMuted={setMuted}
                                onSetVolume={setVolume}
                                onSetSeekSnapEnabled={setSeekSnapEnabled}
                                onShowCenterLineChange={setShowCenterPlayheadLine}
                            />
                        ) : null}
                    </Group>
                </Group>
                <TimelineViewport
                    showCenterPlayheadOverlay={playbackActive && showCenterPlayheadLine}
                    centerPlayheadBackground={theme.colors.yellow[6]}
                    centerPlayheadOpacity={0.5}
                    scrollRef={scrollRef}
                    onScrollMouseDown={handleScrollMouseDown}
                    onScrollMouseMove={handleMouseMove}
                    onScrollMouseUp={handleScrollMouseUp}
                    onScrollMouseLeave={handleScrollMouseLeave}
                    isPanningTimeline={isPanningTimeline}
                    isSeekDragging={isSeekDragging}
                    contentWidth={contentWidth}
                    startTimeMs={startTimeMs}
                    endTimeMs={endTimeMs}
                    timelineWidth={timelineWidth}
                    tickIntervalMs={tickIntervalMs}
                    orderedDifficulties={orderedDifficulties}
                    visibilityByDifficulty={visibilityByDifficulty}
                    sensors={sensors}
                    collisionDetection={collisionDetection}
                    measuring={measuring}
                    onDragStart={handleDragStart}
                    onDragEnd={handleDragEnd}
                    onDragCancel={handleDragCancel}
                    onToggleDifficultyVisibility={toggleDifficultyVisibility}
                />
            </Stack>
        </Paper>
    );
}

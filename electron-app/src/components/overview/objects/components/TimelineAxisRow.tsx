import { Box, useMantineTheme } from "@mantine/core";
import { AXIS_HEIGHT, LABEL_WIDTH } from "../constants.ts";
import TimelineAxis from "./TimelineAxis.tsx";

interface TimelineAxisRowProps {
    startTimeMs: number;
    endTimeMs: number;
    timelineWidth: number;
    tickIntervalMs: number;
    linePosition: "top" | "bottom";
}

export default function TimelineAxisRow({
    startTimeMs,
    endTimeMs,
    timelineWidth,
    tickIntervalMs,
    linePosition,
}: TimelineAxisRowProps) {
    const theme = useMantineTheme();

    return (
        <Box
            style={{
                display: "flex",
                alignItems: "stretch",
                width: LABEL_WIDTH + timelineWidth,
                minWidth: LABEL_WIDTH + timelineWidth,
                height: AXIS_HEIGHT,
            }}>
            <Box
                style={{
                    position: "sticky",
                    left: 0,
                    zIndex: 3,
                    flex: `0 0 ${LABEL_WIDTH}px`,
                    height: AXIS_HEIGHT,
                    background: theme.colors.dark[8],
                    borderRight: `1px solid ${theme.colors.dark[4]}`,
                    boxSizing: "border-box",
                }}
            />
            <Box
                data-timeline-seek-zone
                style={{ flex: `0 0 ${timelineWidth}px`, minWidth: timelineWidth, width: timelineWidth }}>
                <TimelineAxis
                    startTimeMs={startTimeMs}
                    endTimeMs={endTimeMs}
                    width={timelineWidth}
                    tickIntervalMs={tickIntervalMs}
                    linePosition={linePosition}
                />
            </Box>
        </Box>
    );
}

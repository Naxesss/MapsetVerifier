import { Box, useMantineTheme } from '@mantine/core';

interface TimeAxisProps {
  /** Duration in milliseconds */
  durationMs: number;
  /** Height of the axis in pixels (default: 20) */
  height?: number;
  /** Whether to show tick marks (default: true) */
  showTicks?: boolean;
  /** Custom interval in seconds (if not provided, will be calculated adaptively) */
  intervalSeconds?: number;
}

/**
 * Reusable time axis component for displaying time labels with tick marks.
 * Compatible with Mantine Charts and custom visualizations.
 * Automatically adapts label spacing based on duration to prevent overcrowding.
 */
function TimeAxis({ durationMs, height = 20, showTicks = true, intervalSeconds }: TimeAxisProps) {
  const theme = useMantineTheme();
  const durationSeconds = (durationMs / 1000) - 1;

  // Calculate adaptive interval if not provided
  const getAdaptiveInterval = (duration: number): number => {
    if (duration <= 30) return 5;        // 0-30s: every 5s
    if (duration <= 60) return 10;       // 30-60s: every 10s
    if (duration <= 120) return 15;      // 1-2min: every 15s
    if (duration <= 300) return 30;      // 2-5min: every 30s
    if (duration <= 600) return 60;      // 5-10min: every 1min
    if (duration <= 1800) return 120;    // 10-30min: every 2min
    return 300;                          // 30min+: every 5min
  };

  const interval = intervalSeconds ?? getAdaptiveInterval(durationSeconds);

  // Generate time labels at the calculated interval
  const timeLabels: number[] = [];
  for (let t = 0; t <= durationSeconds; t += interval) {
    timeLabels.push(t);
  }
  // Ensure the last label is included if not already
  if (timeLabels[timeLabels.length - 1] < durationSeconds) {
    timeLabels.push(Math.ceil(durationSeconds));
  }

  // Format time as mm:ss or h:mm:ss for longer durations
  const formatTime = (seconds: number): string => {
    const hours = Math.floor(seconds / 3600);
    const mins = Math.floor((seconds % 3600) / 60);
    const secs = Math.floor(seconds % 60);
    
    if (hours > 0) {
      return `${hours}:${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
    }
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  return (
    <Box style={{ position: 'relative', width: '100%', height, overflow: 'visible' }}>
      <svg width="100%" height={height} style={{ display: 'block', overflow: 'visible' }}>
        {timeLabels.map((timeSec, idx) => {
          const xPercent = (timeSec / durationSeconds) * 100;

          // Adjust textAnchor for edge labels to prevent clipping
          let anchor: string = "middle";
          if (xPercent <= 2) {
            anchor = "start"; // Left edge - align text to the right of the position
          } else if (xPercent >= 98) {
            anchor = "end"; // Right edge - align text to the left of the position
          }

          return (
            <g key={idx}>
              {/* Tick mark */}
              {showTicks && (
                <line
                  x1={`${xPercent}%`}
                  y1="0"
                  x2={`${xPercent}%`}
                  y2="6"
                  stroke={theme.colors.dark[3]}
                  strokeWidth="1"
                />
              )}
              {/* Time label */}
              <text
                x={`${xPercent}%`}
                y={showTicks ? 18 : 14}
                textAnchor={anchor}
                fill={theme.colors.dark[2]}
                fontSize="10"
                fontFamily={theme.fontFamily}
              >
                {formatTime(timeSec)}
              </text>
            </g>
          );
        })}
      </svg>
    </Box>
  );
}

export default TimeAxis;


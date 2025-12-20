import { Box, useMantineTheme } from '@mantine/core';

interface MagnitudeAxisProps {
  /** Minimum magnitude in dB (default: -120) */
  minDb?: number;
  /** Maximum magnitude in dB (default: 0) */
  maxDb?: number;
  /** Width of the axis in pixels (default: 50) */
  width?: number;
  /** Height of the axis as percentage (default: "100%") */
  height?: string;
  /** Whether to show tick marks (default: true) */
  showTicks?: boolean;
  /** Custom dB points to label (if not provided, will use standard intervals) */
  dbPoints?: number[];
}

/**
 * Reusable magnitude axis component for displaying dB labels with tick marks.
 * Compatible with Mantine Charts and custom visualizations.
 * Displays magnitude scale from 0 dB (top) to -120 dB (bottom).
 */
function MagnitudeAxis({
  minDb = -120,
  maxDb = 0,
  width = 50,
  height = "100%",
  showTicks = true,
  dbPoints
}: MagnitudeAxisProps) {
  const theme = useMantineTheme();
  const dbRange = maxDb - minDb;

  // Standard dB points if not provided
  const defaultDbPoints = [0, -20, -40, -60, -80, -100, -120];
  const dbLabels = dbPoints ?? defaultDbPoints.filter(db => db >= minDb && db <= maxDb);

  // Format dB value
  const formatDb = (db: number): string => {
    return db === 0 ? '0' : `${db}`;
  };

  return (
    <Box style={{ position: 'absolute', right: 0, top: 0, width, height, overflow: 'visible' }}>
      <svg width={width} height="100%" style={{ display: 'block', overflow: 'visible' }}>
        {dbLabels.map((db, idx) => {
          // Calculate Y position (0 dB at top, -120 dB at bottom)
          const normalizedDb = (maxDb - db) / dbRange;
          const yPercent = normalizedDb * 100;
          const yPos = `${yPercent}%`;

          // Adjust dominantBaseline for edge labels to prevent clipping
          let baseline: string = "middle";
          if (yPercent <= 2) {
            baseline = "hanging"; // Top edge - align text below the position
          } else if (yPercent >= 98) {
            baseline = "auto"; // Bottom edge - align text above the position
          }

          return (
            <g key={idx}>
              {/* Tick mark */}
              {showTicks && (
                <line
                  x1="0"
                  y1={yPos}
                  x2="6"
                  y2={yPos}
                  stroke={theme.colors.dark[3]}
                  strokeWidth="1"
                />
              )}
              {/* dB label */}
              <text
                x={showTicks ? 8 : 4}
                y={yPos}
                textAnchor="start"
                dominantBaseline={baseline}
                fill={theme.colors.dark[2]}
                fontSize="10"
                fontFamily={theme.fontFamily}
              >
                {formatDb(db)} dB
              </text>
            </g>
          );
        })}
      </svg>
    </Box>
  );
}

export default MagnitudeAxis;


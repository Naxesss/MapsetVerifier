import { Box, useMantineTheme } from '@mantine/core';

interface FrequencyAxisProps {
  /** Minimum frequency in Hz */
  minFreqHz: number;
  /** Maximum frequency in Hz */
  maxFreqHz: number;
  /** Width of the axis in pixels (default: 60) */
  width?: number;
  /** Height of the axis as percentage (default: "100%") */
  height?: string;
  /** Whether to show tick marks (default: true) */
  showTicks?: boolean;
  /** Custom frequency points to label (if not provided, will be calculated adaptively) */
  frequencyPoints?: number[];
}

/**
 * Reusable frequency axis component for displaying frequency labels with tick marks.
 * Compatible with Mantine Charts and custom visualizations.
 * Automatically adapts label spacing based on frequency range to prevent overcrowding.
 */
function FrequencyAxis({
  minFreqHz,
  maxFreqHz,
  width = 60,
  height = "100%",
  showTicks = true,
  frequencyPoints
}: FrequencyAxisProps) {
  const theme = useMantineTheme();
  const freqRange = maxFreqHz - minFreqHz;

  // Calculate adaptive frequency points if not provided
  const getAdaptiveFrequencyPoints = (min: number, max: number): number[] => {
    const points: number[] = [];
    
    // Always include min and max
    points.push(max);
    
    // Add intermediate points based on range
    if (max >= 20000) {
      points.push(20000, 15000, 10000, 5000, 2000);
    } else if (max >= 10000) {
      points.push(10000, 5000, 2000, 1000, 500);
    } else if (max >= 5000) {
      points.push(5000, 2000, 1000, 500);
    } else {
      points.push(max * 0.75, max * 0.5, max * 0.25);
    }
    
    // Filter to only include points within range and remove duplicates
    return [...new Set(points.filter(f => f >= min && f <= max))].sort((a, b) => b - a);
  };

  const freqLabels = frequencyPoints ?? getAdaptiveFrequencyPoints(minFreqHz, maxFreqHz);

  // Format frequency as Hz or kHz
  const formatFrequency = (hz: number): string => {
    if (!Number.isFinite(hz)) return "";

    if (hz >= 1000) {
      const khz = hz / 1000;
      return khz >= 10
        ? `${Math.round(khz)} kHz`
        : `${khz.toFixed(1)} kHz`;
    }

    return `${Math.round(hz)} Hz`;
  };

  return (
    <Box style={{ position: 'absolute', left: 0, top: 0, width, height, overflow: 'visible' }}>
      <svg width={width} height="100%" style={{ display: 'block', overflow: 'visible' }}>
        {freqLabels.map((freqHz, idx) => {
          // Calculate Y position (inverted because higher frequencies are at the top)
          const normalizedFreq = (freqHz - minFreqHz) / freqRange;
          const yPercent = (1 - normalizedFreq) * 100;
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
                  x1={width - 6}
                  y1={yPos}
                  x2={width}
                  y2={yPos}
                  stroke={theme.colors.dark[3]}
                  strokeWidth="1"
                />
              )}
              {/* Frequency label */}
              <text
                x={showTicks ? width - 8 : width - 4}
                y={yPos}
                textAnchor="end"
                dominantBaseline={baseline}
                fill={theme.colors.dark[2]}
                fontSize="10"
                fontFamily={theme.fontFamily}
              >
                {formatFrequency(freqHz)}
              </text>
            </g>
          );
        })}
      </svg>
    </Box>
  );
}

export default FrequencyAxis;


import {FunctionComponent, useCallback} from "react";
import {SpectralAnalysisResult} from "../../../Types.ts";
import * as d3 from 'd3';
import {Box, Flex, Paper, useMantineTheme} from "@mantine/core";
import AutoResizeCanvas from "../../common/AutoResizeCanvas.tsx";
import TimeAxis from "../../common/TimeAxis.tsx";
import FrequencyAxis from "../../common/FrequencyAxis.tsx";
import MagnitudeAxis from "../../common/MagnitudeAxis.tsx";

export type ColorScheme = 'viridis' | 'plasma' | 'inferno' | 'magma' | 'cividis';

interface SpectrogramCanvasProps {
  data: SpectralAnalysisResult
  avgFreq: number
  duration: number
  colorScheme?: ColorScheme
}

// Matplotlib color schemes (from dark to bright)
const COLOR_SCHEMES: Record<ColorScheme, Array<{ r: number; g: number; b: number }>> = {
  viridis: [
    { r: 253, g: 231, b: 37 },  // bright yellow (0dB)
    { r: 94, g: 201, b: 98 },   // green (-20dB)
    { r: 33, g: 145, b: 140 },  // teal (-40dB)
    { r: 59, g: 82, b: 139 },   // blue (-60dB)
    { r: 68, g: 58, b: 131 },   // dark blue (-80dB)
    { r: 49, g: 24, b: 75 },    // dark purple (-100dB)
    { r: 68, g: 1, b: 84 }      // very dark purple (-120dB)
  ],
  plasma: [
    { r: 240, g: 249, b: 33 },  // bright yellow (0dB)
    { r: 248, g: 149, b: 64 },  // orange (-20dB)
    { r: 221, g: 81, b: 130 },  // pink (-40dB)
    { r: 157, g: 56, b: 157 },  // purple (-60dB)
    { r: 99, g: 37, b: 155 },   // dark purple (-80dB)
    { r: 53, g: 19, b: 103 },   // very dark purple (-100dB)
    { r: 13, g: 8, b: 135 }     // near black (-120dB)
  ],
  inferno: [
    { r: 252, g: 255, b: 164 }, // bright yellow (0dB)
    { r: 249, g: 142, b: 9 },   // orange (-20dB)
    { r: 188, g: 55, b: 84 },   // red-pink (-40dB)
    { r: 87, g: 16, b: 110 },   // purple (-60dB)
    { r: 40, g: 11, b: 84 },    // dark purple (-80dB)
    { r: 13, g: 8, b: 35 },     // very dark purple (-100dB)
    { r: 0, g: 0, b: 4 }        // near black (-120dB)
  ],
  magma: [
    { r: 252, g: 253, b: 191 }, // bright yellow (0dB)
    { r: 254, g: 153, b: 139 }, // peach (-20dB)
    { r: 216, g: 73, b: 115 },  // pink-red (-40dB)
    { r: 140, g: 41, b: 129 },  // purple (-60dB)
    { r: 72, g: 26, b: 108 },   // dark purple (-80dB)
    { r: 25, g: 15, b: 53 },    // very dark purple (-100dB)
    { r: 0, g: 0, b: 4 }        // near black (-120dB)
  ],
  cividis: [
    { r: 255, g: 234, b: 70 },  // bright yellow (0dB)
    { r: 136, g: 177, b: 130 }, // light green (-20dB)
    { r: 94, g: 136, b: 144 },  // teal (-40dB)
    { r: 68, g: 100, b: 135 },  // blue (-60dB)
    { r: 53, g: 70, b: 110 },   // dark blue (-80dB)
    { r: 37, g: 43, b: 74 },    // very dark blue (-100dB)
    { r: 0, g: 32, b: 77 }      // near black (-120dB)
  ]
};

// Pre-compute color lookup tables for all schemes
const createColorLUT = (colorStops: Array<{ r: number; g: number; b: number }>) => {
  const colorScale = d3.scaleLinear<number, number, never>()
    .domain([0, -20, -40, -60, -80, -100, -120])
    .range([0, 1, 2, 3, 4, 5, 6])
    .clamp(true);

  const colorLUT = new Map<number, { r: number; g: number; b: number }>();
  for (let db = -120; db <= 0; db += 0.5) {
    const scaleValue = colorScale(db);
    const lowerIdx = Math.floor(scaleValue);
    const upperIdx = Math.ceil(scaleValue);
    const t = scaleValue - lowerIdx;

    const lower = colorStops[lowerIdx];
    const upper = colorStops[upperIdx];

    const r = Math.round(lower.r + (upper.r - lower.r) * t);
    const g = Math.round(lower.g + (upper.g - lower.g) * t);
    const b = Math.round(lower.b + (upper.b - lower.b) * t);

    colorLUT.set(db, { r, g, b });
  }
  return colorLUT;
};

const COLOR_LUTS: Record<ColorScheme, Map<number, { r: number; g: number; b: number }>> = {
  viridis: createColorLUT(COLOR_SCHEMES.viridis),
  plasma: createColorLUT(COLOR_SCHEMES.plasma),
  inferno: createColorLUT(COLOR_SCHEMES.inferno),
  magma: createColorLUT(COLOR_SCHEMES.magma),
  cividis: createColorLUT(COLOR_SCHEMES.cividis)
};

// Helper function to get color from LUT
const getColor = (magnitude: number, colorScheme: ColorScheme): { r: number; g: number; b: number } => {
  const clampedMag = Math.max(-120, Math.min(0, magnitude));
  const roundedMag = Math.round(clampedMag * 2) / 2;
  return COLOR_LUTS[colorScheme].get(roundedMag) || { r: 0, g: 0, b: 0 };
};

const SpectrogramCanvas: FunctionComponent<SpectrogramCanvasProps> = (props) => {
  const theme = useMantineTheme();
  const colorScheme = props.colorScheme || 'inferno';

  const draw = useCallback((ctx: CanvasRenderingContext2D, width: number, height: number) => {
    if (!props.data?.spectrogramData?.length) {
      return;
    }

    const frames = props.data.spectrogramData;
    const numBins = frames[0]?.magnitudes?.length || 0;
    if (numBins === 0) {
      return;
    }

    // Create ImageData for direct pixel manipulation (much faster than fillRect)
    const imageData = ctx.createImageData(width, height);
    const pixels = imageData.data;

    // Draw spectrogram using ImageData
    const cellWidth = width / frames.length;
    const cellHeight = height / numBins;

    frames.forEach((frame, timeIdx) => {
      const xStart = Math.floor(timeIdx * cellWidth);
      const xEnd = Math.floor((timeIdx + 1) * cellWidth);

      frame.magnitudes.forEach((magnitude, freqIdx) => {
        const yStart = Math.floor(height - (freqIdx + 1) * cellHeight);
        const yEnd = Math.floor(height - freqIdx * cellHeight);
        const color = getColor(magnitude, colorScheme);

        // Fill the cell in the ImageData buffer
        for (let y = yStart; y < yEnd && y < height; y++) {
          for (let x = xStart; x < xEnd && x < width; x++) {
            const pixelIndex = (y * width + x) * 4;
            pixels[pixelIndex] = color.r;
            pixels[pixelIndex + 1] = color.g;
            pixels[pixelIndex + 2] = color.b;
            pixels[pixelIndex + 3] = 255;
          }
        }
      });
    });

    // Single draw call - much faster than thousands of fillRect calls
    ctx.putImageData(imageData, 0, 0);

    // Convert average frequency (from props) to y position using actual frequency bins
    let avgFrequencyY = -1;

    if (props.avgFreq && props.avgFreq > 0 && props.data.frequencyBins && props.data.frequencyBins.length > 0) {
      // Find the bin index that corresponds to avgFreq
      let binIndex = -1;
      for (let i = 0; i < props.data.frequencyBins.length; i++) {
        if (props.data.frequencyBins[i] >= props.avgFreq) {
          binIndex = i;
          break;
        }
      }

      // If not found, it might be higher than all bins
      if (binIndex === -1 && props.avgFreq > props.data.frequencyBins[props.data.frequencyBins.length - 1]) {
        binIndex = props.data.frequencyBins.length - 1;
      }

      if (binIndex >= 0 && binIndex < numBins) {
        avgFrequencyY = height - (binIndex + 0.5) * cellHeight;
      }
    }

    // Draw the average frequency line as a horizontal line across the entire spectrogram
    if (avgFrequencyY >= 0) {
      ctx.save();

      ctx.strokeStyle = 'rgba(0, 200, 255, 1.0)'; // Cyan color
      ctx.lineWidth = 2;
      ctx.setLineDash([]);
      ctx.globalCompositeOperation = 'source-over';

      ctx.beginPath();
      ctx.moveTo(0, avgFrequencyY);
      ctx.lineTo(width, avgFrequencyY);
      ctx.stroke();

      ctx.restore();
    }
  }, [props.data, props.avgFreq, colorScheme]);

  // Draw color scale bar
  const drawColorScale = useCallback((ctx: CanvasRenderingContext2D, width: number, height: number) => {
    // Create gradient from 0 dB (top) to -120 dB (bottom)
    const imageData = ctx.createImageData(width, height);
    const pixels = imageData.data;

    for (let y = 0; y < height; y++) {
      // Map y position to dB value (0 at top, -120 at bottom)
      const db = -(y / height) * 120;
      const color = getColor(db, colorScheme);

      for (let x = 0; x < width; x++) {
        const pixelIndex = (y * width + x) * 4;
        pixels[pixelIndex] = color.r;
        pixels[pixelIndex + 1] = color.g;
        pixels[pixelIndex + 2] = color.b;
        pixels[pixelIndex + 3] = 255;
      }
    }

    ctx.putImageData(imageData, 0, 0);

    // Draw border around the color scale
    ctx.strokeStyle = '#888';
    ctx.lineWidth = 1;
    ctx.strokeRect(0, 0, width, height);
  }, [colorScheme]);

  // Get the actual frequency range from the data
  const minFreq = props.data?.frequencyBins?.[0] || 0;
  const maxFreq = props.data?.frequencyBins?.[props.data.frequencyBins.length - 1] || (props.data?.sampleRate ? props.data.sampleRate / 2 : 22050);

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
      <Flex direction="row" gap="md">
        {/* Container with relative positioning for axes */}
        <Box>
          <Box style={{ position: 'relative', paddingLeft: 60 }}>
            {/* Y-axis (Frequency) */}
            <FrequencyAxis
              minFreqHz={minFreq}
              maxFreqHz={maxFreq}
            />

            {/* Canvas visualization */}
            <AutoResizeCanvas
              draw={draw}
              fixedWidth={1600}
              fixedHeight={800}
            />
          </Box>

          {/* X-axis (Time) */}
          <Box style={{ paddingLeft: 60 }}>
            <TimeAxis durationMs={props.duration} />
          </Box>
        </Box>

        {/* Color scale bar with magnitude axis */}
        <Box style={{ position: 'relative', display: 'flex', alignItems: 'stretch', paddingRight: 50 }}>
          <AutoResizeCanvas
            draw={drawColorScale}
            fixedWidth={30}
            fixedHeight={400}
          />
          {/* Magnitude axis (dB scale) */}
          <MagnitudeAxis
            minDb={-120}
            maxDb={0}
            width={50}
          />
        </Box>
      </Flex>
    </Paper>
  );
}


export default SpectrogramCanvas

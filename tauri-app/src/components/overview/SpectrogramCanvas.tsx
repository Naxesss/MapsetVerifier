import {useCallback} from "react";
import {Box} from "@mantine/core";
import AutoResizeCanvas from "../common/AutoResizeCanvas.tsx";
import {SpectralAnalysisResult} from "../../Types.ts";
import * as d3 from 'd3';

interface SpectrogramCanvasProps {
  data: SpectralAnalysisResult;
  width: number | string;
  height: number | string;
  magnitudeThreshold: number;
}

// Pre-compute color lookup table once (outside component to avoid recreation)
const createColorLUT = () => {
  const colorScale = d3.scaleLinear<number, number, never>()
    .domain([0, -20, -40, -60, -80, -120])
    .range([0, 1, 2, 3, 4, 5])
    .clamp(true);

  const colorStops = [
    { r: 255, g: 255, b: 255 }, // white (0dB)
    { r: 255, g: 255, b: 0 },   // yellow (-20dB)
    { r: 255, g: 128, b: 0 },   // orange (-40dB)
    { r: 255, g: 0, b: 0 },     // red (-60dB)
    { r: 128, g: 0, b: 128 },   // purple (-80dB)
    { r: 0, g: 0, b: 0 }        // black (-120dB)
  ];

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

const COLOR_LUT = createColorLUT();

// Helper function to get color from LUT
const getColor = (magnitude: number): { r: number; g: number; b: number } => {
  const clampedMag = Math.max(-120, Math.min(0, magnitude));
  const roundedMag = Math.round(clampedMag * 2) / 2;
  return COLOR_LUT.get(roundedMag) || { r: 0, g: 0, b: 0 };
};

export function SpectrogramCanvas(props: SpectrogramCanvasProps) {
  // Memoize the draw function to prevent unnecessary re-renders
  const draw = useCallback((ctx: CanvasRenderingContext2D, width: number, height: number) => {
    if (!props.data?.spectrogramData?.length) return;
    if (!ctx) return;
    if (width === 0 || height === 0) return; // Prevent rendering with invalid dimensions

    const frames = props.data.spectrogramData;
    const numBins = frames[0]?.magnitudes?.length || 0;
    if (numBins === 0) return;

    // Create ImageData for direct pixel manipulation (much faster than fillRect)
    const imageData = ctx.createImageData(width, height);
    const pixels = imageData.data;

    // Fill with background color first
    const bgColor = {r: 26, g: 27, b: 30}; // theme.colors.dark[8] approximation
    for (let i = 0; i < pixels.length; i += 4) {
      pixels[i] = bgColor.r;
      pixels[i + 1] = bgColor.g;
      pixels[i + 2] = bgColor.b;
      pixels[i + 3] = 255;
    }

    // Draw spectrogram using ImageData
    const cellWidth = width / frames.length;
    const cellHeight = height / numBins;

    frames.forEach((frame, timeIdx) => {
      const xStart = Math.floor(timeIdx * cellWidth);
      const xEnd = Math.floor((timeIdx + 1) * cellWidth);

      frame.magnitudes.forEach((magnitude, freqIdx) => {
        const yStart = Math.floor(height - (freqIdx + 1) * cellHeight);
        const yEnd = Math.floor(height - freqIdx * cellHeight);
        const color = getColor(magnitude);

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

    // First pass: collect all observed Nyquist indices
    const nyquistIndices: number[] = [];
    frames.forEach((frame) => {
      let observedNyquistIndex = -1;
      for (let idx = frame.magnitudes.length - 1; idx >= 0; idx--) {
        if (frame.magnitudes[idx] > props.magnitudeThreshold) {
          observedNyquistIndex = idx;
          break;
        }
      }
      nyquistIndices.push(observedNyquistIndex);
    });

    // Second pass: apply moving average smoothing to reduce wobble
    const windowSize = 50; // Smoothing window (adjust for more/less smoothing)
    const smoothedIndices: number[] = [];

    for (let i = 0; i < nyquistIndices.length; i++) {
      const halfWindow = Math.floor(windowSize / 2);
      const start = Math.max(0, i - halfWindow);
      const end = Math.min(nyquistIndices.length, i + halfWindow + 1);

      // Calculate average of valid indices in the window
      let sum = 0;
      let count = 0;
      for (let j = start; j < end; j++) {
        if (nyquistIndices[j] >= 0) {
          sum += nyquistIndices[j];
          count++;
        }
      }

      smoothedIndices.push(count > 0 ? sum / count : -1);
    }

    // Third pass: draw the smoothed line
    ctx.strokeStyle = 'rgba(0, 200, 255, 1.0)'; // Cyan color
    ctx.lineWidth = 2;
    ctx.beginPath();

    let firstPoint = true;
    smoothedIndices.forEach((smoothedIndex, timeIdx) => {
      if (smoothedIndex >= 0) {
        const x = (timeIdx + 0.5) * cellWidth;
        const y = height - (smoothedIndex + 0.5) * cellHeight;

        if (firstPoint) {
          ctx.moveTo(x, y);
          firstPoint = false;
        } else {
          ctx.lineTo(x, y);
        }
      }
    });

    ctx.stroke();
    ctx.setLineDash([]);
  }, [props.data, props.magnitudeThreshold]);

  return (
    <Box w={props.width} h={props.height} style={{overflow: 'hidden'}}>
      <AutoResizeCanvas
        draw={draw}
        fixedWidth={1920}
        fixedHeight={400}
      />
    </Box>
  );
}

import {FunctionComponent, useEffect, useRef} from "react";
import {SpectralAnalysisResult} from "../../Types.ts";
import * as d3 from 'd3';

interface SpectrogramCanvasProps {
  data?: SpectralAnalysisResult;
  avgFreq?: number;
  width?: number;
  height?: number;
  xAxisMap?: any;
  yAxisMap?: any;
  offset?: {
    top?: number;
    bottom?: number;
    left?: number;
    right?: number;
    width?: number;
    height?: number;
  };
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

const SpectrogramCanvas: FunctionComponent<SpectrogramCanvasProps> = (props) => {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  // Use Recharts offset to position the canvas within the chart area
  const x = props.offset?.left || 0;
  const y = props.offset?.top || 0;
  const width = props.offset?.width || (typeof props.width === 'number' ? props.width : 800);
  const height = props.offset?.height || (typeof props.height === 'number' ? props.height : 400);
  
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    if (!props.data?.spectrogramData?.length) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

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

    // Get the actual frequency range from the data
    const minFreq = props.data.frequencyBins?.[0] || 0;
    const maxFreq = props.data.frequencyBins?.[props.data.frequencyBins.length - 1] || (props.data.sampleRate / 2);
    const freqRange = maxFreq - minFreq;

    // Draw horizontal grid lines at key frequency points
    ctx.save();
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.1)';
    ctx.lineWidth = 1;
    ctx.setLineDash([5, 5]);

    // Calculate grid line positions based on actual frequency range
    const gridFrequencies = [
      maxFreq,
      Math.min(20000, maxFreq * 0.9),
      maxFreq * 0.5,
      maxFreq * 0.2,
      maxFreq * 0.05,
      minFreq
    ].filter(f => f >= minFreq && f <= maxFreq);

    gridFrequencies.forEach(freq => {
      // Convert frequency to y position using the actual frequency range
      const normalizedFreq = (freq - minFreq) / freqRange;
      const y = height - (normalizedFreq * height);

      ctx.beginPath();
      ctx.moveTo(0, y);
      ctx.lineTo(width, y);
      ctx.stroke();
    });

    ctx.restore();

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
  }, [props.data, props.avgFreq, width, height, props.offset]);

  return (
    <foreignObject
      x={x}
      y={y}
      width={width}
      height={height}
    >
      <canvas
        ref={canvasRef}
        style={{
          display: 'block',
          width: '100%',
          height: '100%',
        }}
      />
    </foreignObject>
  );
}

export default SpectrogramCanvas

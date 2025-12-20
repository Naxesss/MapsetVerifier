import React, { useEffect, useRef } from "react";

interface RechartsCanvasProps {
  draw: (ctx: CanvasRenderingContext2D, width: number, height: number) => void;
  // These props are provided by Recharts' Customized component
  width?: number;
  height?: number;
  xAxisMap?: any;
  yAxisMap?: any;
  offset?: any;
}

/**
 * A canvas component designed to work with Recharts' Customized component.
 * Recharts' Customized component renders SVG foreignObject to embed HTML/Canvas.
 */
const RechartsCanvas: React.FC<RechartsCanvasProps> = ({
  draw,
  width = 0,
  height = 0,
}) => {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);

  useEffect(() => {
    const canvas = canvasRef.current;

    // Validate canvas element
    if (!canvas || !(canvas instanceof HTMLCanvasElement)) {
      console.error("Canvas ref is not an HTMLCanvasElement", canvas);
      return;
    }

    // Validate dimensions
    if (width <= 0 || height <= 0) {
      console.warn("Invalid canvas dimensions:", width, height);
      return;
    }

    const ctx = canvas.getContext("2d");
    if (!ctx) {
      console.error("Failed to get 2D context");
      return;
    }

    const ratio = window.devicePixelRatio || 1;

    // Set canvas internal resolution (high quality for retina displays)
    canvas.width = width * ratio;
    canvas.height = height * ratio;

    // Set CSS size to match Recharts dimensions
    canvas.style.width = `${width}px`;
    canvas.style.height = `${height}px`;

    // Scale context for device pixel ratio
    ctx.scale(ratio, ratio);

    // Draw the content
    try {
      draw(ctx, width, height);
    } catch (error) {
      console.error("Error drawing canvas:", error);
    }
  }, [draw, width, height]);

  // Recharts Customized expects an SVG element, so we use foreignObject to embed canvas
  return (
    <foreignObject x={0} y={0} width={width} height={height}>
      <canvas
        ref={canvasRef}
        style={{
          display: "block",
          pointerEvents: "none",
        }}
      />
    </foreignObject>
  );
};

export default RechartsCanvas;


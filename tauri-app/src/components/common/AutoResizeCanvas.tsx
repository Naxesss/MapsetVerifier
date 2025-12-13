import React, { useEffect, useRef } from "react";

interface AutoResizeCanvasProps {
  draw: (ctx: CanvasRenderingContext2D, width: number, height: number) => void;
  className?: string;
  style?: React.CSSProperties;
  // Optional: Use fixed resolution and scale with CSS instead of redrawing on resize
  fixedWidth?: number;
  fixedHeight?: number;
}

const AutoResizeCanvas: React.FC<AutoResizeCanvasProps> = ({
  draw,
  className,
  style,
  fixedWidth,
  fixedHeight,
}) => {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const resizeTimeoutRef = useRef<number | null>(null);
  const rafRef = useRef<number | null>(null);

  useEffect(() => {
    const container = containerRef.current;
    const canvas = canvasRef.current;

    if (!container || !canvas) return;

    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    // If fixed dimensions are provided, use them and scale with CSS
    if (fixedWidth && fixedHeight) {
      const ratio = window.devicePixelRatio || 1;

      // Set canvas internal resolution to fixed size (high quality)
      canvas.width = fixedWidth * ratio;
      canvas.height = fixedHeight * ratio;

      // CSS will scale to fit container
      canvas.style.width = "100%";
      canvas.style.height = "100%";

      // Normalize scaling
      ctx.resetTransform?.();
      ctx.scale(ratio, ratio);

      // Draw once at fixed resolution
      draw(ctx, fixedWidth, fixedHeight);

      // No need for ResizeObserver - canvas scales with CSS
      return;
    }

    // Otherwise, use dynamic resizing (original behavior)
    const resize = () => {
      const rect = container.getBoundingClientRect();
      const { width, height } = rect;

      // Skip if dimensions are invalid
      if (width === 0 || height === 0) return;

      const ratio = window.devicePixelRatio || 1;

      // Adjust canvas internal resolution (crisp on retina screens)
      canvas.width = width * ratio;
      canvas.height = height * ratio;

      // CSS size remains normal
      canvas.style.width = `${width}px`;
      canvas.style.height = `${height}px`;

      // Normalize scaling
      ctx.resetTransform?.(); // modern API
      ctx.scale(ratio, ratio);

      // Draw callback
      draw(ctx, width, height);
    };

    // Debounced resize handler to reduce lag during resizing
    const debouncedResize = () => {
      // Cancel any pending resize
      if (resizeTimeoutRef.current !== null) {
        window.clearTimeout(resizeTimeoutRef.current);
      }
      if (rafRef.current !== null) {
        window.cancelAnimationFrame(rafRef.current);
      }

      // Use requestAnimationFrame for immediate visual update
      rafRef.current = window.requestAnimationFrame(() => {
        // Then debounce the expensive draw operation
        resizeTimeoutRef.current = window.setTimeout(() => {
          resize();
        }, 100); // 100ms debounce
      });
    };

    const observer = new ResizeObserver(debouncedResize);
    observer.observe(container);

    // initial call
    resize();

    return () => {
      observer.disconnect();
      if (resizeTimeoutRef.current !== null) {
        window.clearTimeout(resizeTimeoutRef.current);
      }
      if (rafRef.current !== null) {
        window.cancelAnimationFrame(rafRef.current);
      }
    };
  }, [draw, fixedWidth, fixedHeight]);

  return (
    <div
      ref={containerRef}
      className={className}
      style={{ width: "100%", height: "100%", ...style }}
    >
      <canvas ref={canvasRef} />
    </div>
  );
};

export default AutoResizeCanvas

import React, { useEffect, useLayoutEffect, useRef } from 'react';

interface AutoResizeCanvasProps {
  draw: (ctx: CanvasRenderingContext2D, width: number, height: number) => void;
  className?: string;
  style?: React.CSSProperties;
  // Optional: Use fixed resolution and scale with CSS instead of redrawing on resize
  fixedWidth?: number;
  fixedHeight?: number;
  minHeight?: number | string;
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
  const drawRef = useRef(draw);
  drawRef.current = draw;

  const fixedDimsRef = useRef<{ w: number; h: number } | null>(null);

  // Fixed-size: only realloc backing store when fixedWidth/H change repainting whenever draw/size updates.
  // Avoid assigning canvas.width/height on every `draw` identity change (buffer wipe + realloc cost).
  useLayoutEffect(() => {
    if (!fixedWidth || !fixedHeight) return;

    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const ratio = window.devicePixelRatio || 1;
    const prev = fixedDimsRef.current;
    const needsBufferResize = !prev || prev.w !== fixedWidth || prev.h !== fixedHeight;

    if (needsBufferResize) {
      fixedDimsRef.current = { w: fixedWidth, h: fixedHeight };
      canvas.width = fixedWidth * ratio;
      canvas.height = fixedHeight * ratio;
      canvas.style.width = '100%';
      canvas.style.height = '100%';
      ctx.resetTransform?.();
      ctx.scale(ratio, ratio);
    }

    try {
      drawRef.current(ctx, fixedWidth, fixedHeight);
    } catch (error) {
      console.error('Error drawing fixed-size canvas:', error);
    }
  }, [draw, fixedWidth, fixedHeight]);

  useEffect(() => {
    const container = containerRef.current;
    const canvas = canvasRef.current;

    if (!container || !canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    if (fixedWidth && fixedHeight) {
      return;
    }

    fixedDimsRef.current = null;

    const resize = () => {
      const rect = container.getBoundingClientRect();
      const { width, height } = rect;

      if (width === 0 || height === 0) return;

      const ratio = window.devicePixelRatio || 1;

      canvas.width = width * ratio;
      canvas.height = height * ratio;

      canvas.style.width = `${width}px`;
      canvas.style.height = `${height}px`;

      ctx.resetTransform?.();
      ctx.scale(ratio, ratio);

      try {
        drawRef.current(ctx, width, height);
      } catch (error) {
        console.error('Error drawing auto-resize canvas:', error);
      }
    };

    const debouncedResize = () => {
      if (resizeTimeoutRef.current !== null) {
        window.clearTimeout(resizeTimeoutRef.current);
      }
      if (rafRef.current !== null) {
        window.cancelAnimationFrame(rafRef.current);
      }

      rafRef.current = window.requestAnimationFrame(() => {
        resizeTimeoutRef.current = window.setTimeout(() => {
          resize();
        }, 100);
      });
    };

    const observer = new ResizeObserver(debouncedResize);
    observer.observe(container);

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
  }, [fixedWidth, fixedHeight]);

  return (
    <div
      ref={containerRef}
      className={className}
      style={{ width: '100%', height: '100%', ...style }}
    >
      <canvas ref={canvasRef} />
    </div>
  );
};

export default AutoResizeCanvas;

import { withAlpha } from '../../../../utils/color.ts';
import type {
  AlphaFill,
  AlphaStroke,
  ColorRef,
  TimelineCircleTheme,
  TimelineHoldNoteTheme,
  TimelineLineBodyTheme,
  TimelineObjectMarkerTheme,
  TimelineReverseArrowTheme,
  TimelineSpinnerMarkerTheme,
} from './types.ts';

export function resolveColor(ref: ColorRef, objectColor: string): string {
  return ref.kind === 'object' ? objectColor : ref.color;
}

export function applyAlphaFill(
  ctx: CanvasRenderingContext2D,
  fill: AlphaFill,
  objectColor: string
): void {
  ctx.fillStyle = withAlpha(resolveColor(fill.color, objectColor), fill.alpha);
}

export function applyAlphaStroke(
  ctx: CanvasRenderingContext2D,
  stroke: AlphaStroke,
  objectColor: string
): void {
  ctx.strokeStyle = withAlpha(resolveColor(stroke.color, objectColor), stroke.alpha);
  ctx.lineWidth = stroke.width;
  if (stroke.lineCap) {
    ctx.lineCap = stroke.lineCap;
  }
}

export function drawThemedCircle(
  ctx: CanvasRenderingContext2D,
  x: number,
  centerY: number,
  radius: number,
  objectColor: string,
  circleTheme: TimelineCircleTheme
): void {
  ctx.beginPath();
  applyAlphaFill(ctx, circleTheme.fill, objectColor);
  applyAlphaStroke(ctx, circleTheme.border, objectColor);
  ctx.arc(x, centerY, radius, 0, Math.PI * 2);
  ctx.fill();
  ctx.stroke();
}

/** Slider head/tail: normal circle with a small white center dot. */
export function drawThemedSliderEndpoint(
  ctx: CanvasRenderingContext2D,
  x: number,
  centerY: number,
  radius: number,
  objectColor: string,
  circleTheme: TimelineCircleTheme
): void {
  drawThemedCircle(ctx, x, centerY, radius, objectColor, circleTheme);

  const dotRadius = Math.max(1.4, radius * 0.18);

  ctx.beginPath();
  ctx.fillStyle = withAlpha('#ffffff', 0.95);
  ctx.arc(x, centerY, dotRadius, 0, Math.PI * 2);
  ctx.fill();
}

export function drawThemedLineBody(
  ctx: CanvasRenderingContext2D,
  startX: number,
  endX: number,
  centerY: number,
  objectColor: string,
  bodyTheme: TimelineLineBodyTheme
): void {
  const { glow, core } = bodyTheme;

  applyAlphaStroke(ctx, glow, objectColor);
  ctx.beginPath();
  ctx.moveTo(startX, centerY);
  ctx.lineTo(endX, centerY);
  ctx.stroke();

  applyAlphaStroke(ctx, core, objectColor);
  ctx.beginPath();
  ctx.moveTo(startX, centerY);
  ctx.lineTo(endX, centerY);
  ctx.stroke();
}

export function drawThemedHoldNote(
  ctx: CanvasRenderingContext2D,
  startX: number,
  endX: number,
  centerY: number,
  objectColor: string,
  holdTheme: TimelineHoldNoteTheme
): void {
  const halfHeight = holdTheme.height / 2;
  const width = endX - startX;

  applyAlphaFill(ctx, holdTheme.fill, objectColor);
  ctx.fillRect(startX, centerY - halfHeight, width, holdTheme.height);

  applyAlphaStroke(ctx, holdTheme.stroke, objectColor);
  ctx.strokeRect(startX, centerY - halfHeight, width, holdTheme.height);
}

export function drawThemedSpinnerMarker(
  ctx: CanvasRenderingContext2D,
  x: number,
  centerY: number,
  objectColor: string,
  radius: number,
  markerTheme: TimelineSpinnerMarkerTheme
): void {
  const ringRadius = Math.max(
    radius * markerTheme.ringRadiusScale,
    radius - markerTheme.ringRadiusInset
  );
  const accentX = x + ringRadius * markerTheme.accentOffsetXScale;
  const accentY = centerY - ringRadius * markerTheme.accentOffsetYScale;

  ctx.save();
  ctx.beginPath();
  applyAlphaFill(ctx, markerTheme.ringFill, objectColor);
  ctx.strokeStyle = withAlpha(
    resolveColor(markerTheme.ringStroke.color, objectColor),
    markerTheme.ringStroke.alpha
  );
  ctx.lineWidth = Math.max(
    markerTheme.ringStrokeWidthMin,
    radius * markerTheme.ringStrokeWidthScale
  );
  ctx.arc(x, centerY, radius, 0, Math.PI * 2);
  ctx.fill();
  ctx.stroke();

  ctx.beginPath();
  ctx.strokeStyle = markerTheme.arcStroke;
  ctx.lineWidth = Math.max(markerTheme.arcStrokeWidthMin, radius * markerTheme.arcStrokeWidthScale);
  ctx.lineCap = 'round';
  ctx.arc(x, centerY, ringRadius, markerTheme.arcStartAngle, markerTheme.arcEndAngle);
  ctx.stroke();

  ctx.beginPath();
  ctx.fillStyle = markerTheme.accentFill;
  ctx.arc(
    accentX,
    accentY,
    Math.max(markerTheme.accentRadiusMin, radius * markerTheme.accentRadiusScale),
    0,
    Math.PI * 2
  );
  ctx.fill();

  ctx.beginPath();
  applyAlphaFill(ctx, markerTheme.centerFill, objectColor);
  ctx.arc(
    x,
    centerY,
    Math.max(markerTheme.centerRadiusMin, radius * markerTheme.centerRadiusScale),
    0,
    Math.PI * 2
  );
  ctx.fill();
  ctx.restore();
}

export function drawThemedObjectMarkerEdge(
  ctx: CanvasRenderingContext2D,
  objectColor: string,
  markerTheme: TimelineObjectMarkerTheme
): void {
  applyAlphaStroke(ctx, markerTheme.edgeStroke, objectColor);
  ctx.fillStyle = withAlpha(
    resolveColor(markerTheme.edgeStroke.color, objectColor),
    markerTheme.edgeStroke.alpha
  );
}

export function drawThemedTailSquare(
  ctx: CanvasRenderingContext2D,
  x: number,
  centerY: number,
  markerTheme: TimelineObjectMarkerTheme
): void {
  const half = markerTheme.tailSquareSize / 2;
  ctx.fillRect(x - half, centerY - half, markerTheme.tailSquareSize, markerTheme.tailSquareSize);
}

export function drawThemedReverseArrow(
  ctx: CanvasRenderingContext2D,
  x: number,
  centerY: number,
  size: number,
  arrowTheme: TimelineReverseArrowTheme
): void {
  const leftX = x - size;
  const middleX = x - size * 0.17;
  const rightX = x + size * 0.67;
  const yOffset = size * 0.89;

  ctx.save();
  ctx.strokeStyle = arrowTheme.stroke;
  ctx.lineWidth = Math.max(arrowTheme.lineWidthMin, size * arrowTheme.lineWidthScale);
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';

  ctx.beginPath();
  ctx.moveTo(leftX, centerY - yOffset);
  ctx.lineTo(middleX, centerY);
  ctx.lineTo(leftX, centerY + yOffset);
  ctx.stroke();

  ctx.beginPath();
  ctx.moveTo(middleX, centerY - yOffset);
  ctx.lineTo(rightX, centerY);
  ctx.lineTo(middleX, centerY + yOffset);
  ctx.stroke();

  ctx.restore();
}

import * as d3 from 'd3';

const difficultyColourSpectrum = d3
  .scaleLinear<string>()
  .domain([0.1, 1.25, 2, 2.5, 3.3, 4.2, 4.9, 5.8, 6.7, 7.7, 9])
  .clamp(true)
  .range([
    '#4290FB',
    '#4FC0FF',
    '#4FFFD5',
    '#7CFF4F',
    '#F6F05C',
    '#FF8068',
    '#FF4E6F',
    '#C645B8',
    '#6563DE',
    '#18158E',
    '#000000',
  ])
  .interpolate(d3.interpolateRgb.gamma(2.2));

// Text on a difficulty-coloured surface, from osu-web's getDiffTextColour.
const difficultyTextColourSpectrum = d3
  .scaleLinear<string>()
  .domain([9, 9.9, 10.6, 11.5, 12.4])
  .clamp(true)
  .range(['#F6F05C', '#FF8068', '#FF4E6F', '#C645B8', '#6563DE', '#18158E'])
  .interpolate(d3.interpolateRgb.gamma(2.2));

/**
 * Get the difficulty color for a given rating
 * @param rating The rating to get the color for
 * @returns The difficulty color
 */
export function getDifficultyColor(rating: number) {
  if (rating < 0.1) return '#AAAAAA';
  if (rating >= 9) return '#000000';
  return difficultyColourSpectrum(rating);
}

/** Text on a difficulty-coloured surface. Matches osu-web's getDiffTextColour. */
export function getDifficultyTextColor(rating: number) {
  if (rating < 6.5) return '#000000';
  if (rating < 9) return '#F6F05C';
  return difficultyTextColourSpectrum(rating);
}

/** Same hue as a difficulty color, at a low lightness. The fill behind a light-style badge. */
export function getDifficultyMutedColor(color: string) {
  const hsl = d3.hsl(color);
  // 9★ and above are already black. Lowering their lightness would turn that black into gray.
  if (Number.isNaN(hsl.l) || hsl.l <= 0.16) return color;
  hsl.l = 0.16;
  return hsl.formatHex();
}

/** Tooltip border: the difficulty color below 6.5★, then the same color as the text. */
export function getDifficultyBorderColor(rating: number) {
  if (rating < 6.5) return getDifficultyColor(rating);
  return getDifficultyTextColor(rating);
}

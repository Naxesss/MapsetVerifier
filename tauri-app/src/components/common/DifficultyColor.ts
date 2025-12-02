import * as d3 from "d3";

const difficultyColourSpectrum = d3
  .scaleLinear<string>()
  .domain([0.1, 1.25, 2, 2.5, 3.3, 4.2, 4.9, 5.8, 6.7, 7.7, 9])
  .clamp(true)
  .range([
    "#4290FB",
    "#4FC0FF",
    "#4FFFD5",
    "#7CFF4F",
    "#F6F05C",
    "#FF8068",
    "#FF4E6F",
    "#C645B8",
    "#6563DE",
    "#18158E",
    "#000000",
  ])
  .interpolate(d3.interpolateRgb.gamma(2.2));

/**
 * Get the difficulty color for a given rating
 * @param rating The rating to get the color for
 * @returns The difficulty color
 */
export function getDifficultyColor(rating: number) {
  if (rating < 0.1) return "#AAAAAA";
  if (rating >= 9) return "#000000";
  return difficultyColourSpectrum(rating);
}

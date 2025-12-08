import { useMantineTheme } from '@mantine/core';
import React from 'react';
import { Mode } from '../../Types';
import { getDifficultyColor } from '../common/DifficultyColor.ts';

export interface GameModeIconProps {
  mode: Mode | string;
  size?: number; // square size in px
  className?: string;
  color?: string;
  style?: React.CSSProperties;
  starRating?: number | null;
}

// Inline SVG components for each game mode
const OsuIcon: React.FC<{ size: number; color: string }> = ({ size, color }) => (
  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 666.667 666.667" width={size} height={size} style={{ color }}>
    <defs><clipPath id="osu-a"><path d="M0 500h500V0H0Z"/></clipPath></defs>
    <g clipPath="url(#osu-a)" transform="matrix(1.33333 0 0 -1.33333 0 666.667)">
      <path fill="currentColor" d="M0 0c-71.682 0-130 58.317-130 130 0 71.682 58.318 130 130 130 71.683 0 130-58.318 130-130C130 58.317 71.683 0 0 0" transform="translate(250 120)"/>
      <path fill="currentColor" d="M0 0c-82.843 0-150-67.157-150-150S-82.843-300 0-300s150 67.157 150 150S82.843 0 0 0m0-40c29.382 0 57.006-11.442 77.781-32.218C98.558-92.995 110-120.618 110-150c0-29.382-11.442-57.006-32.219-77.781C57.006-248.558 29.382-260 0-260c-29.382 0-57.005 11.442-77.782 32.219C-98.558-207.006-110-179.382-110-150c0 29.382 11.442 57.005 32.218 77.782C-57.005-51.442-29.382-40 0-40" transform="translate(250 400)"/>
      <path fill="currentColor" d="M0 0c-31.707 0-62.487-6.219-91.485-18.484-27.989-11.838-53.116-28.777-74.685-50.346-21.569-21.569-38.508-46.697-50.346-74.685C-228.781-172.513-235-203.293-235-235s6.219-62.487 18.484-91.484c11.838-27.99 28.777-53.117 50.346-74.686 21.569-21.569 46.696-38.508 74.685-50.347C-62.487-463.781-31.707-470 0-470s62.487 6.219 91.484 18.483c27.99 11.839 53.117 28.778 74.686 50.347 21.569 21.569 38.508 46.696 50.347 74.686C228.781-297.487 235-266.707 235-235c0 31.707-6.219 62.487-18.483 91.485-11.839 27.988-28.778 53.116-50.347 74.685-21.569 21.569-46.696 38.508-74.686 50.346C62.487-6.219 31.707 0 0 0m0-40c107.695 0 195-87.305 195-195S107.695-430 0-430c-107.696 0-195 87.305-195 195S-107.696-40 0-40" transform="translate(250 485)"/>
    </g>
  </svg>
);

const TaikoIcon: React.FC<{ size: number; color: string }> = ({ size, color }) => (
  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 666.667 666.667" width={size} height={size} style={{ color }}>
    <defs><clipPath id="taiko-a"><path d="M0 500h500V0H0Z"/></clipPath></defs>
    <g clipPath="url(#taiko-a)" transform="matrix(1.33333 0 0 -1.33333 0 666.667)">
      <path fill="currentColor" d="M0 0v193.672c43.082-11.129 75-50.325 75-96.836C75 50.325 43.082 11.129 0 0m-125 96.836c0 46.511 31.918 85.706 75 96.836V0c-43.082 11.129-75 50.325-75 96.836m100 150c-82.843 0-150-67.157-150-150s67.157-150 150-150 150 67.157 150 150-67.157 150-150 150" transform="translate(275 153.164)"/>
      <path fill="currentColor" d="M0 0c-31.707 0-62.487-6.219-91.485-18.484-27.989-11.838-53.116-28.777-74.685-50.346-21.569-21.569-38.508-46.697-50.346-74.685C-228.781-172.513-235-203.293-235-235s6.219-62.487 18.484-91.484c11.838-27.99 28.777-53.117 50.346-74.686 21.569-21.569 46.696-38.508 74.685-50.347C-62.487-463.781-31.707-470 0-470s62.487 6.219 91.484 18.483c27.99 11.839 53.117 28.778 74.686 50.347 21.569 21.569 38.508 46.696 50.347 74.686C228.781-297.487 235-266.707 235-235c0 31.707-6.219 62.487-18.483 91.485-11.839 27.988-28.778 53.116-50.347 74.685-21.569 21.569-46.696 38.508-74.686 50.346C62.487-6.219 31.707 0 0 0m0-40c107.695 0 195-87.305 195-195S107.695-430 0-430c-107.696 0-195 87.305-195 195S-107.696-40 0-40" transform="translate(250 485)"/>
    </g>
  </svg>
);

const CatchIcon: React.FC<{ size: number; color: string }> = ({ size, color }) => (
  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 666.67 666.67" width={size} height={size} style={{ color }}>
    <defs><clipPath id="catch-a"><path d="M0 500h500V0H0z"/></clipPath></defs>
    <g clipPath="url(#catch-a)" transform="matrix(1.3333 0 0 -1.3333 0 666.67)">
      <path fill="currentColor" d="M250 485c-31.707 0-62.487-6.219-91.485-18.484-27.989-11.838-53.116-28.777-74.685-50.346s-38.508-46.697-50.346-74.685C21.219 312.487 15 281.707 15 250s6.219-62.487 18.484-91.484c11.838-27.99 28.777-53.117 50.346-74.686s46.696-38.508 74.685-50.347C187.513 21.219 218.293 15 250 15s62.487 6.219 91.484 18.483c27.99 11.839 53.117 28.778 74.686 50.347s38.508 46.696 50.347 74.686C478.781 187.513 485 218.293 485 250s-6.219 62.487-18.483 91.485c-11.839 27.988-28.778 53.116-50.347 74.685s-46.696 38.508-74.686 50.346C312.487 478.781 281.707 485 250 485m0-40c107.7 0 195-87.305 195-195S357.695 55 250 55C142.3 55 55 142.305 55 250s87.304 195 195 195"/>
      <path fill="currentColor" d="M308.75 212.5c-20.711 0-37.5 16.789-37.5 37.5s16.789 37.5 37.5 37.5 37.5-16.789 37.5-37.5-16.79-37.5-37.5-37.5M221.25 288.75c-20.711 0-37.5 16.789-37.5 37.5s16.789 37.5 37.5 37.5 37.5-16.789 37.5-37.5-16.789-37.5-37.5-37.5M221.25 136.25c-20.711 0-37.5 16.789-37.5 37.5s16.789 37.5 37.5 37.5 37.5-16.789 37.5-37.5-16.789-37.5-37.5-37.5"/>
    </g>
  </svg>
);

const ManiaIcon: React.FC<{ size: number; color: string }> = ({ size, color }) => (
  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 666.667 666.667" width={size} height={size} style={{ color }}>
    <defs><clipPath id="mania-a"><path d="M0 500h500V0H0Z"/></clipPath></defs>
    <g clipPath="url(#mania-a)" transform="matrix(1.33333 0 0 -1.33333 0 666.667)">
      <path fill="currentColor" d="M0 0c-13.807 0-25 11.193-25 25v252c0 13.807 11.193 25 25 25s25-11.193 25-25V25C25 11.193 13.807 0 0 0" transform="translate(250 99)"/>
      <path fill="currentColor" d="M0 0c-13.807 0-25 11.192-25 25v110c0 13.807 11.193 25 25 25s25-11.193 25-25V25C25 11.192 13.807 0 0 0" transform="translate(170 170)"/>
      <path fill="currentColor" d="M0 0c-13.808 0-25 11.192-25 25v110c0 13.807 11.192 25 25 25s25-11.193 25-25V25C25 11.192 13.808 0 0 0" transform="translate(330 170)"/>
      <path fill="currentColor" d="M0 0c-31.707 0-62.487-6.219-91.485-18.484-27.989-11.838-53.116-28.777-74.685-50.346-21.569-21.569-38.508-46.697-50.346-74.685C-228.781-172.513-235-203.293-235-235s6.219-62.487 18.484-91.484c11.838-27.99 28.777-53.117 50.346-74.686 21.569-21.569 46.696-38.508 74.685-50.347C-62.487-463.781-31.707-470 0-470s62.487 6.219 91.484 18.483c27.99 11.839 53.117 28.778 74.686 50.347 21.569 21.569 38.508 46.696 50.347 74.686C228.781-297.487 235-266.707 235-235c0 31.707-6.219 62.487-18.483 91.485-11.839 27.988-28.778 53.116-50.347 74.685-21.569 21.569-46.696 38.508-74.686 50.346C62.487-6.219 31.707 0 0 0m0-40c107.695 0 195-87.305 195-195S107.695-430 0-430c-107.696 0-195 87.305-195 195S-107.696-40 0-40" transform="translate(250 485)"/>
    </g>
  </svg>
);

const iconComponents: Record<Mode, React.FC<{ size: number; color: string }>> = {
  Standard: OsuIcon,
  Taiko: TaikoIcon,
  Catch: CatchIcon,
  Mania: ManiaIcon,
};

export default function GameModeIcon({ mode, size = 32, className, style, starRating, color }: GameModeIconProps) {
  const theme = useMantineTheme();

  let customColor: string = color ?? theme.white;

  if (starRating) {
    customColor = getDifficultyColor(starRating);
  }

  const IconComponent = (iconComponents as Record<string, React.FC<{ size: number; color: string }>>)[mode] || iconComponents.Standard;

  return (
    <span
      className={className}
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        width: size,
        height: size,
        ...style,
      }}
    >
      <IconComponent size={size} color={customColor} />
    </span>
  );
}

import React from 'react';
import { Mode } from '../../Types';
import { Image } from '@mantine/core';
import { useMantineTheme } from '@mantine/core';
import osuSvg from '../../assets/osu.svg';
import taikoSvg from '../../assets/taiko.svg';
import catchSvg from '../../assets/catch.svg';
import maniaSvg from '../../assets/mania.svg';

const sources: Record<Mode, string> = {
  Standard: osuSvg,
  Taiko: taikoSvg,
  Catch: catchSvg,
  Mania: maniaSvg,
};

export interface GameModeIconProps {
  mode: Mode | string;
  size?: number; // square size in px
  className?: string;
  style?: React.CSSProperties;
}

export default function GameModeIcon({
  mode,
  size = 32,
  className,
  style
}: GameModeIconProps) {
  const theme = useMantineTheme();
  const src = (sources as Record<string, string>)[mode] || sources.Standard;
  // const background = theme.colors.dark[4];
  return (
    <Image
      src={src}
      width={size}
      height={size}
      className={className}
      style={{
        width: size,
        color: theme.white,
        ...style,
      }}
      fit="contain"
      radius={theme.radius.sm}
    />
  );
}

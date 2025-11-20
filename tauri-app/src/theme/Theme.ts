import {createTheme, DEFAULT_THEME, MantineColorsTuple, mergeMantineTheme} from "@mantine/core";

const red: MantineColorsTuple = [
  "#ffe8e9",
  "#ffd1d1",
  "#fba0a0",
  "#f76d6d",
  "#f44141",
  "#f22625",
  "#f21616",
  "#d8070b",
  "#c10007",
  "#a90003",
];

const blue: MantineColorsTuple = [
  "#e0fbff",
  "#cbf2ff",
  "#9ae2ff",
  "#64d2ff",
  "#3cc5fe",
  "#23bcfe",
  "#09b8ff",
  "#00a1e4",
  "#0090cd",
  "#007cb5",
];

const green: MantineColorsTuple = [
  "#e5feee",
  "#d2f9e0",
  "#a8f1c0",
  "#7aea9f",
  "#53e383",
  "#3bdf70",
  "#2bdd66",
  "#1ac455",
  "#0caf49",
  "#00963c",
];

const orange: MantineColorsTuple = [
  "#fff8e1",
  "#ffefcc",
  "#ffdd9b",
  "#ffca64",
  "#ffba38",
  "#ffb01b",
  "#ffab09",
  "#e39500",
  "#ca8500",
  "#af7100",
];

const themeOverride = createTheme({
  defaultRadius: 5,
  spacing: {
    xs: '0.25em',
    sm: '0.5em',
    md: '1em',
    lg: '2em',
    xl: '4em'
  },
  colors: {
    blue: blue,
    red: red,
    green: green,
    orange: orange,
  },
});

export const theme = mergeMantineTheme(DEFAULT_THEME, themeOverride);
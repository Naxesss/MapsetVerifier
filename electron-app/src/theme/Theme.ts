import { createTheme, DEFAULT_THEME, MantineColorsTuple, mergeMantineTheme } from '@mantine/core';

const red: MantineColorsTuple = [
  '#ffe8e9',
  '#ffd1d1',
  '#fba0a0',
  '#f76d6d',
  '#f44141',
  '#f22625',
  '#f21616',
  '#d8070b',
  '#c10007',
  '#a90003',
];

const blue: MantineColorsTuple = [
  '#e0fbff',
  '#cbf2ff',
  '#9ae2ff',
  '#64d2ff',
  '#3cc5fe',
  '#23bcfe',
  '#09b8ff',
  '#00a1e4',
  '#0090cd',
  '#007cb5',
];

const green: MantineColorsTuple = [
  '#e5feee',
  '#d2f9e0',
  '#a8f1c0',
  '#7aea9f',
  '#53e383',
  '#3bdf70',
  '#2bdd66',
  '#1ac455',
  '#0caf49',
  '#00963c',
];

const orange: MantineColorsTuple = [
  '#fff8e1',
  '#ffefcc',
  '#ffdd9b',
  '#ffca64',
  '#ffba38',
  '#ffb01b',
  '#ffab09',
  '#e39500',
  '#ca8500',
  '#af7100',
];

const dark: MantineColorsTuple = [
  '#c5cad3',
  '#a3adbd',
  '#8695ab',
  '#424D61',
  '#353F52',
  '#283243',
  '#212B39',
  '#1E2734',
  '#161D28',
  '#0D121A',
] as const;

const primary: MantineColorsTuple = [
  '#e1f8ff',
  '#cbedff',
  '#99ccff', // Main color
  '#64c1ff',
  '#3aaefe',
  '#20a2fe',
  '#099cff',
  '#0088e4',
  '#0079cd',
  '#0068b6',
] as const;

const themeOverride = createTheme({
  fontFamily: 'Inter, sans-serif',
  headings: {
    fontFamily: 'Comfortaa, Inter, sans-serif',
  },
  defaultRadius: 5,
  spacing: {
    xs: '0.25em',
    sm: '0.5em',
    md: '1em',
    lg: '2em',
    xl: '4em',
  },
  colors: {
    blue: blue,
    red: red,
    green: green,
    orange: orange,
    dark: dark,
    primary: primary,
  },
  components: {
    Modal: {
      styles: {
        title: {
          fontFamily: 'Comfortaa, Inter, sans-serif',
        },
      },
    },
    Title: {
      styles: {
        root: {
          fontFamily: 'Comfortaa, Inter, sans-serif',
        },
      },
    },
    ScrollArea: {
      styles: {
        thumb: {
          backgroundColor: 'var(--mantine-color-primary-2)',
        },
        scrollbar: {
          '&:hover > .mantine-ScrollArea-thumb': {
            backgroundColor: 'var(--mantine-color-primary-3)',
          },
        },
      },
    },
  },
});

export const theme = mergeMantineTheme(DEFAULT_THEME, themeOverride);

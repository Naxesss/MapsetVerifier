export type UiFontFamily = 'Nunito' | 'Noto Sans';

export const DEFAULT_UI_FONT_FAMILY: UiFontFamily = 'Nunito';

export const UI_FONT_FAMILY_OPTIONS: { value: UiFontFamily; label: string }[] = [
  { value: 'Nunito', label: 'Nunito (Default)' },
  { value: 'Noto Sans', label: 'Noto Sans (Legacy)' },
];

export function getFontFamilyStack(font: UiFontFamily): string {
  return `${font}, sans-serif`;
}

export function parseUiFontFamily(value: unknown): UiFontFamily {
  if (value === 'Noto Sans' || value === 'Nunito') {
    return value;
  }
  return DEFAULT_UI_FONT_FAMILY;
}

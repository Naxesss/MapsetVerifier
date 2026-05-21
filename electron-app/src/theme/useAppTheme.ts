import { useMemo } from 'react';
import { getFontFamilyStack, parseUiFontFamily } from './fonts';
import { createAppTheme } from './Theme';
import { useSettings } from '../context/SettingsContext';

export function useAppTheme() {
  const { settings } = useSettings();

  return useMemo(
    () => createAppTheme(getFontFamilyStack(parseUiFontFamily(settings.uiFontFamily))),
    [settings.uiFontFamily]
  );
}

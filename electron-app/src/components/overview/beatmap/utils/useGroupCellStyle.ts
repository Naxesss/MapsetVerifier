import { useMantineTheme } from '@mantine/core';
import { useMemo, type CSSProperties } from 'react';
import { groupValueLabel, groupValueStyle } from './consistencyTableStyles';
import {
  buildGroupColorLookup,
  itemKey,
  type InconsistencyField,
} from '../../../../utils/inconsistencies';

export type GroupCell = {
  style?: CSSProperties;
  label: string;
};

export function useGroupCellStyle<T extends { version: string; mode: string }>(
  items: T[],
  fields: InconsistencyField<T>[]
) {
  const theme = useMantineTheme();
  const lookup = useMemo(() => buildGroupColorLookup(items, fields), [items, fields]);

  return (item: T, fieldId: string): GroupCell | undefined => {
    const fieldLookup = lookup.get(fieldId);
    const key = itemKey(item);
    // No entry means every difficulty shares the value, so there is no group to name.
    if (!fieldLookup?.has(key)) {
      return undefined;
    }

    const colorIndex = fieldLookup.get(key) ?? null;
    return {
      style: groupValueStyle(theme, colorIndex),
      label: groupValueLabel(colorIndex),
    };
  };
}

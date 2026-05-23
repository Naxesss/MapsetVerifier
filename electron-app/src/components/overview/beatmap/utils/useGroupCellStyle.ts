import { useMantineTheme } from '@mantine/core';
import { useMemo } from 'react';
import { groupCellStyle } from './consistencyTableStyles';
import {
  buildGroupColorLookup,
  itemKey,
  type InconsistencyField,
} from '../../../../utils/inconsistencies';
export function useGroupCellStyle<T extends { version: string; mode: string }>(
  items: T[],
  fields: InconsistencyField<T>[]
) {
  const theme = useMantineTheme();
  const lookup = useMemo(() => buildGroupColorLookup(items, fields), [items, fields]);

  return (item: T, fieldId: string) => {
    const colorIndex = lookup.get(fieldId)?.get(itemKey(item));
    return groupCellStyle(theme, colorIndex === undefined ? null : colorIndex);
  };
}

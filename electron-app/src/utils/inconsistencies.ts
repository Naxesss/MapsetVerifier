export type ComparisonValue = string | number | boolean | null | undefined;

export type InconsistencyField<T> = {
  id: string;
  getValue: (item: T) => ComparisonValue;
};

function valueKey(value: ComparisonValue): string {
  return JSON.stringify(value ?? null);
}

export function groupItemsByMode<T extends { mode: string }>(items: T[]): Map<string, T[]> {
  const groups = new Map<string, T[]>();

  for (const item of items) {
    const group = groups.get(item.mode);
    if (group) {
      group.push(item);
    } else {
      groups.set(item.mode, [item]);
    }
  }

  return groups;
}

export function itemKey(item: { mode: string; version: string }): string {
  return `${item.mode}-${item.version}`;
}

/** Assigns a color index per value group. The dominant group (or first when tied) stays uncolored. */
export function assignGroupColorIndices<T extends { version: string; mode: string }>(
  items: T[],
  getValue: (item: T) => ComparisonValue
): Map<string, number | null> {
  const result = new Map<string, number | null>();

  if (items.length <= 1) {
    return result;
  }

  const valueGroups = new Map<string, { count: number; firstIndex: number }>();

  items.forEach((item, index) => {
    const key = valueKey(getValue(item));
    const existing = valueGroups.get(key);
    if (existing) {
      existing.count += 1;
    } else {
      valueGroups.set(key, { count: 1, firstIndex: index });
    }
  });

  if (valueGroups.size <= 1) {
    return result;
  }

  const sortedByFirstIndex = [...valueGroups.entries()].sort(
    (a, b) => a[1].firstIndex - b[1].firstIndex
  );
  const maxCount = Math.max(...sortedByFirstIndex.map(([, group]) => group.count));
  const dominantGroups = sortedByFirstIndex.filter(([, group]) => group.count === maxCount);

  const baselineKey = dominantGroups.length === 1 ? dominantGroups[0][0] : sortedByFirstIndex[0][0];

  const valueKeyToColor = new Map<string, number | null>();
  let colorIndex = 0;

  for (const [key] of sortedByFirstIndex) {
    if (key === baselineKey) {
      valueKeyToColor.set(key, null);
    } else {
      valueKeyToColor.set(key, colorIndex++);
    }
  }

  for (const item of items) {
    const key = valueKey(getValue(item));
    result.set(itemKey(item), valueKeyToColor.get(key) ?? null);
  }

  return result;
}

export function buildGroupColorLookup<T extends { version: string; mode: string }>(
  items: T[],
  fields: InconsistencyField<T>[]
): Map<string, Map<string, number | null>> {
  const lookup = new Map<string, Map<string, number | null>>();

  for (const field of fields) {
    lookup.set(field.id, new Map());
  }

  const byMode = groupItemsByMode(items);

  for (const field of fields) {
    for (const modeItems of byMode.values()) {
      const modeLookup = assignGroupColorIndices(modeItems, field.getValue);
      for (const [key, colorIndex] of modeLookup) {
        lookup.get(field.id)!.set(key, colorIndex);
      }
    }
  }

  return lookup;
}

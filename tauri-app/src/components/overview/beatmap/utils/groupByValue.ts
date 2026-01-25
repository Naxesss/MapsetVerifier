export interface ValueGroup<T> {
  key: string;
  difficulties: string[];
  value: T;
}

/**
 * Groups items by a computed key, collecting difficulties that share the same value.
 * @param items Array of items with a version property
 * @param getKey Function to compute a grouping key from an item
 * @returns Array of groups with difficulties and representative value
 */
export function groupByValue<T extends { version: string }>(
  items: T[],
  getKey: (item: T) => string
): ValueGroup<T>[] {
  const groups = new Map<string, ValueGroup<T>>();

  for (const item of items) {
    const key = getKey(item);
    const existing = groups.get(key);
    if (existing) {
      existing.difficulties.push(item.version);
    } else {
      groups.set(key, {
        key,
        difficulties: [item.version],
        value: item,
      });
    }
  }

  return Array.from(groups.values());
}


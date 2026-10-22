import { Table, Tooltip } from '@mantine/core';
import { Fragment, useMemo, type ReactNode } from 'react';
import { useGroupCellStyle } from './beatmap/utils/useGroupCellStyle';
import { itemKey, type ComparisonValue } from '../../utils/inconsistencies';
import AppTable from '../common/AppTable.tsx';
import { getDifficultyColor } from '../common/DifficultyColor.ts';
import SectionCard from '../common/SectionCard.tsx';

export interface ComparisonRow<T> {
  id: string;
  label: string;
  /** Heading over a run of rows, e.g. "Objects". */
  group?: string;
  /** What is compared between difficulties; null means the setting doesn't apply (N/A). */
  value: (item: T) => ComparisonValue;
  render: (item: T) => ReactNode;
  /** Mark values by group colour, so difficulties with the same value share a colour. */
  groupColours?: boolean;
  /**
   * Cells that open something (e.g. a list of timestamps). They lose their padding and highlight
   * on hover, so `render` should return a padded, full-cell target.
   */
  clickable?: (item: T) => boolean;
}

interface ComparisonTableProps<T extends { version: string; mode: string }> {
  title: string;
  /** Short explanation in the title's info tooltip. */
  info?: ReactNode;
  /** The difficulties to compare, easiest first; one column each. */
  items: T[];
  rows: ComparisonRow<T>[];
  /** Star rating per difficulty version, for the colour line under each column's name. */
  starRatings: Map<string, number>;
}

/**
 * The Overview's one per-difficulty table. Compares difficulties of one mode side by side: one row per setting, one column per difficulty,
 * so a setting reads across the spread. The setting names stay in view while the columns scroll.
 * A setting that doesn't apply to any of them (N/A throughout, e.g. approach rate in osu!mania)
 * is left out.
 */
export default function ComparisonTable<T extends { version: string; mode: string }>({
  title,
  info,
  items,
  rows,
  starRatings,
}: ComparisonTableProps<T>) {
  const shown = rows.filter((row) => items.some((item) => row.value(item) !== null));

  const groupFields = useMemo(
    () =>
      rows.filter((row) => row.groupColours).map((row) => ({ id: row.id, getValue: row.value })),
    [rows]
  );
  const groupCell = useGroupCellStyle(items, groupFields);

  if (items.length === 0 || shown.length === 0) {
    return null;
  }

  return (
    <SectionCard title={title} info={info}>
      <AppTable className="mv-table-transposed">
        <Table.Thead>
          <Table.Tr>
            <Table.Th className="mv-table-sticky" aria-label="Setting" />
            {items.map((item) => {
              const starRating = starRatings.get(item.version);
              return (
                <Table.Th
                  key={itemKey(item)}
                  scope="col"
                  title={
                    starRating != null
                      ? `${item.version} · ★ ${starRating.toFixed(2)}`
                      : item.version
                  }
                >
                  <span className="mv-table-column-name" lang="en">
                    {item.version}
                  </span>
                  <span
                    className="mv-table-column-rating"
                    style={{
                      backgroundColor:
                        starRating != null
                          ? getDifficultyColor(starRating)
                          : 'var(--mantine-color-dark-4)',
                    }}
                  />
                </Table.Th>
              );
            })}
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {shown.map((row, index) => (
            <Fragment key={row.id}>
              {/* A group's name heads its first row, spanning the table. */}
              {row.group && row.group !== shown[index - 1]?.group && (
                <Table.Tr className="mv-table-group-row">
                  <Table.Th className="mv-table-sticky" colSpan={items.length + 1} scope="rowgroup">
                    {row.group}
                  </Table.Th>
                </Table.Tr>
              )}
              <Table.Tr>
                <Table.Th scope="row" className="mv-table-sticky">
                  {row.label}
                </Table.Th>
                {items.map((item) => {
                  const group = row.groupColours ? groupCell(item, row.id) : undefined;
                  const cell = (
                    <Table.Td
                      className={row.clickable?.(item) ? 'mv-table-clickable' : undefined}
                      style={group?.style}
                    >
                      {row.render(item)}
                    </Table.Td>
                  );

                  return group ? (
                    <Tooltip
                      key={itemKey(item)}
                      label={group.label}
                      position="top-end"
                      // The cell's right padding, so the tooltip meets the right-aligned value.
                      offset={{ mainAxis: 4, alignmentAxis: 6 }}
                    >
                      {cell}
                    </Tooltip>
                  ) : (
                    <Fragment key={itemKey(item)}>{cell}</Fragment>
                  );
                })}
              </Table.Tr>
            </Fragment>
          ))}
        </Table.Tbody>
      </AppTable>
    </SectionCard>
  );
}

import { Box, Table } from '@mantine/core';
import { useState, type ComponentPropsWithoutRef, type CSSProperties, type ReactNode } from 'react';

type AppTableProps = Omit<ComponentPropsWithoutRef<typeof Table>, 'children'> & {
  children: ReactNode;
  containerStyle?: CSSProperties;
};

/**
 * The one table style (`.mv-table` in global.scss): the card is the only surface, with no header
 * fill, stripes or filled sticky column. Labels sit on a hairline, rows are split by fainter
 * hairlines, the first column is left-aligned and every other column right-aligned in tabular
 * figures, so numbers line up. The sticky first column only casts a shadow once the table is
 * scrolled sideways, when there is something underneath it. Mark the first cell of each row
 * `mv-table-sticky` to keep it in view (see ComparisonTable).
 */
function AppTable({
  children,
  containerStyle,
  className,
  highlightOnHover = true,
  horizontalSpacing = 'sm',
  verticalSpacing = 'sm',
  ...props
}: AppTableProps) {
  const [scrolled, setScrolled] = useState(false);

  return (
    <Box
      className="mv-table-scroll"
      data-scrolled={scrolled || undefined}
      onScroll={(event) => setScrolled(event.currentTarget.scrollLeft > 0)}
      style={{ overflowX: 'auto', maxWidth: '100%', ...containerStyle }}
    >
      <Table
        {...props}
        className={['mv-table', className].filter(Boolean).join(' ')}
        striped={false}
        withRowBorders
        highlightOnHover={highlightOnHover}
        horizontalSpacing={horizontalSpacing}
        verticalSpacing={verticalSpacing}
      >
        {children}
      </Table>
    </Box>
  );
}

export default AppTable;

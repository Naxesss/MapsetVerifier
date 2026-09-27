import { Box, Table, useMantineTheme } from '@mantine/core';
import type { ComponentPropsWithoutRef, CSSProperties, ReactNode } from 'react';

type AppTableProps = Omit<ComponentPropsWithoutRef<typeof Table>, 'children'> & {
  children: ReactNode;
  containerStyle?: CSSProperties;
};

export function DifficultyTableHeaderCell({
  children = 'Difficulty',
  style,
  ...props
}: ComponentPropsWithoutRef<typeof Table.Th>) {
  const theme = useMantineTheme();

  return (
    <Table.Th
      {...props}
      style={{
        position: 'sticky',
        left: 0,
        zIndex: 3,
        textAlign: 'left',
        backgroundColor: theme.colors.dark[5],
        borderRight: `1px solid ${theme.colors.dark[4]}`,
        boxShadow: `8px 0 12px -12px rgba(0, 0, 0, 0.8)`,
        ...style,
      }}
    >
      {children}
    </Table.Th>
  );
}

export function DifficultyTableCell({
  style,
  ...props
}: ComponentPropsWithoutRef<typeof Table.Td>) {
  const theme = useMantineTheme();

  return (
    <Table.Td
      {...props}
      style={{
        position: 'sticky',
        left: 0,
        zIndex: 2,
        textAlign: 'left',
        backgroundColor: theme.colors.dark[5],
        borderRight: `1px solid ${theme.colors.dark[4]}`,
        boxShadow: `8px 0 12px -12px rgba(0, 0, 0, 0.8)`,
        ...style,
      }}
    />
  );
}

function AppTable({
  children,
  containerStyle,
  style,
  styles,
  striped = true,
  highlightOnHover = true,
  horizontalSpacing = 'sm',
  verticalSpacing = 'xs',
  ...props
}: AppTableProps) {
  return (
    <Box style={{ overflowX: 'auto', maxWidth: '100%', ...containerStyle }}>
      <Table
        {...props}
        striped={striped}
        highlightOnHover={highlightOnHover}
        horizontalSpacing={horizontalSpacing}
        verticalSpacing={verticalSpacing}
        styles={
          styles ?? {
            // Headers use the shared uppercase label style (see MicroLabel).
            th: {
              textAlign: 'center',
              fontSize: 11,
              fontWeight: 600,
              textTransform: 'uppercase',
              letterSpacing: '0.04em',
              color: 'var(--mantine-color-dimmed)',
            },
            td: { textAlign: 'center' },
          }
        }
        style={{
          whiteSpace: 'nowrap',
          ...style,
        }}
      >
        {children}
      </Table>
    </Box>
  );
}

export default AppTable;

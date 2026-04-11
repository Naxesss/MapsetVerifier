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

export function DifficultyTableCell({ style, ...props }: ComponentPropsWithoutRef<typeof Table.Td>) {
  const theme = useMantineTheme();

  return (
    <Table.Td
      {...props}
      style={{
        position: 'sticky',
        left: 0,
        zIndex: 2,
        textAlign: 'left',
        backgroundColor: theme.colors.dark[7],
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
        style={{
          whiteSpace: 'nowrap',
          textAlign: 'center',
          ...style,
        }}
      >
        {children}
      </Table>
    </Box>
  );
}

export default AppTable;

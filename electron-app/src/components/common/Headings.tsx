import { Text, type TextProps } from '@mantine/core';
import type { ReactNode } from 'react';

type HeadingProps = Omit<TextProps, 'fz' | 'fw' | 'size'> & {
  children: ReactNode;
  /** Element to render; defaults to the heading level that fits. */
  component?: 'h2' | 'h3' | 'h4' | 'div' | 'span';
};

/**
 * The two heading levels below the page title. Use these instead of `Title` or ad-hoc bold text
 * so every page reads the same: sentence case, section 18px/700, card 16px/600.
 */
export function SectionTitle({ children, component = 'h2', ...props }: HeadingProps) {
  return (
    <Text component={component} fz={18} fw={700} lh={1.3} m={0} {...props}>
      {children}
    </Text>
  );
}

export function CardTitle({ children, component = 'h3', ...props }: HeadingProps) {
  return (
    <Text component={component} fz={16} fw={600} lh={1.3} m={0} {...props}>
      {children}
    </Text>
  );
}

/** Uppercase micro label for stat tiles, table headers and small field labels. */
export function MicroLabel({ children, component = 'div', style, ...props }: HeadingProps) {
  return (
    <Text
      component={component}
      fz={11}
      fw={600}
      tt="uppercase"
      c="dimmed"
      lh={1.3}
      {...props}
      style={[{ letterSpacing: '0.04em' }, style]}
    >
      {children}
    </Text>
  );
}

import { Group, Kbd } from '@mantine/core';
import { Fragment, type ReactNode } from 'react';

interface ShortcutLabelProps {
  label: ReactNode;
  /** Keys pressed together, e.g. ['Ctrl', 'K']; each is drawn as a key like in the page tips. */
  keys: string[];
}

/**
 * A tooltip label with its keyboard shortcut: the one way the app shows a shortcut next to an
 * action, with the same key style as the page tips (instead of "(F5)" in plain text).
 */
export default function ShortcutLabel({ label, keys }: ShortcutLabelProps) {
  return (
    <Group component="span" gap="xs" wrap="nowrap" justify="center">
      <span>{label}</span>
      <span>
        {keys.map((key, index) => (
          <Fragment key={key}>
            {index > 0 && ' + '}
            <Kbd size="xs">{key}</Kbd>
          </Fragment>
        ))}
      </span>
    </Group>
  );
}

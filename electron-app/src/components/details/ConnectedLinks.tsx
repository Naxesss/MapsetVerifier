import { Badge, Group, Stack, Text, UnstyledButton } from '@mantine/core';
import { SectionTitle } from '../common/Headings';
import type { Icon } from '@tabler/icons-react';
import type { ReactNode } from 'react';

export interface ConnectedLink {
  key: string;
  icon: Icon;
  label: ReactNode;
  /** Shown dimmed after the label, such as the levels a check reports for the rule. */
  meta?: ReactNode;
  /** Whether this is the rule or check the reader followed a link from. */
  cameFrom?: boolean;
  /** Opens it in place; without it the link is shown as plain text, e.g. a retired rule. */
  onOpen?: () => void;
  /** Said below the label, dimmed, such as why it can't be opened. */
  note?: string;
}

function ConnectedLinkRow({ link }: { link: ConnectedLink }) {
  const { icon: LinkIcon, label, meta, cameFrom, onOpen, note } = link;

  const content = (
    <Group gap="xs" wrap="nowrap" align="flex-start" w="100%">
      <LinkIcon size={16} style={{ flexShrink: 0, marginTop: 3 }} aria-hidden />
      <Stack gap={0} style={{ flex: 1, minWidth: 0 }}>
        <Text size="sm" inherit component="span">
          {label}
          {cameFrom && (
            <Badge color="gray" ml="xs" style={{ verticalAlign: 'middle' }}>
              You came from here
            </Badge>
          )}
        </Text>
        {note && (
          <Text size="xs" c="dimmed">
            {note}
          </Text>
        )}
      </Stack>
      {meta && (
        <Group gap="xs" wrap="nowrap" style={{ flexShrink: 0 }}>
          {meta}
        </Group>
      )}
    </Group>
  );

  if (!onOpen) {
    return (
      <div className="mv-connected-link" data-static>
        {content}
      </div>
    );
  }

  return (
    <UnstyledButton className="mv-connected-link" onClick={onOpen}>
      {content}
    </UnstyledButton>
  );
}

interface ConnectedLinksProps {
  links: ConnectedLink[];
  /** Said instead of the links when there are none. */
  empty?: ReactNode;
  /** Shown under the links, such as notes on how the rule is covered. */
  children?: ReactNode;
}

/**
 * The rules and checks a rule or check is connected to, as one compact list of links under a
 * "Connected to" heading. Each opens in place in the documentation panel.
 */
export default function ConnectedLinks({ links, empty, children }: ConnectedLinksProps) {
  return (
    <Stack gap="xs">
      <SectionTitle>Connected to</SectionTitle>
      {links.length > 0 ? (
        <Stack gap={2}>
          {links.map((link) => (
            <ConnectedLinkRow key={link.key} link={link} />
          ))}
        </Stack>
      ) : (
        empty
      )}
      {children}
    </Stack>
  );
}

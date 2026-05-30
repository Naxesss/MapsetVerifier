import {
  Title,
  Text,
  Code,
  Divider,
  List,
  Anchor,
  Alert,
  Blockquote,
} from '@mantine/core';
import { IconInfoCircleFilled } from '@tabler/icons-react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import type { ReactNode } from 'react';

interface MantineMarkdownProps {
  children: string;
  notesForBlockquotes?: boolean;
}

function hasLeadingContent(node: unknown): boolean {
  if (!node || typeof node !== 'object') return false;

  const start = (node as Record<string, unknown>).position as
    | { start?: { offset?: number } }
    | undefined;

  return (start?.start?.offset ?? 0) > 0;
}

export default function MantineMarkdown({
  children,
  notesForBlockquotes = false,
}: MantineMarkdownProps) {
  return (
    <div className="markdown-text">
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        components={{
          h1: ({ node, children }: { node?: unknown; children?: ReactNode }) => (
            <Title order={2} mt={hasLeadingContent(node) ? 'md' : 0} mb="xs">
              {children}
            </Title>
          ),
          h2: ({ node, children }: { node?: unknown; children?: ReactNode }) => (
            <Title order={3} mt={hasLeadingContent(node) ? 'md' : 0} mb="xs">
              {children}
            </Title>
          ),
          h3: ({ node, children }: { node?: unknown; children?: ReactNode }) => (
            <Title order={4} mt={hasLeadingContent(node) ? 'md' : 0} mb="xs">
              {children}
            </Title>
          ),
          p: ({ children }: { children?: ReactNode }) => (
            <Text size="md" mb="sm">
              {children}
            </Text>
          ),
          ul: ({ children }: { children?: ReactNode }) => (
            <List size="md" spacing="xs" mb="sm" withPadding>
              {children}
            </List>
          ),
          ol: ({ children }: { children?: ReactNode }) => (
            <List size="md" spacing="xs" type="ordered" mb="sm" withPadding>
              {children}
            </List>
          ),
          li: ({ children }: { children?: ReactNode }) => <List.Item>{children}</List.Item>,
          a: ({ href, children }: { href?: string; children?: ReactNode }) => {
            const handleClick = (e: React.MouseEvent<HTMLAnchorElement>) => {
              e.preventDefault();

              if (href) {
                if (window.electronAPI?.shell.openExternal) {
                  return window.electronAPI.shell.openExternal(href);
                }

                window.open(href, '_blank', 'noopener,noreferrer');
              }
            };

            return (
              <Anchor size="md" href={href} onClick={handleClick}>
                {children}
              </Anchor>
            );
          },
          code: ({ children }: { children?: ReactNode }) => (
            <Code c="primary.2" px="sm" fz="sm">
              {children}
            </Code>
          ),
          img: ({ src, alt, title }: { src?: string; alt?: string; title?: string }) => {
            if (!title) {
              return <img src={src} alt={alt} />;
            }

            return (
              <figure className="markdown-text-figure">
                <img src={src} alt={alt} />
                <figcaption>{title}</figcaption>
              </figure>
            );
          },
          blockquote: ({ node, children }: { node?: unknown; children?: ReactNode }) =>
            notesForBlockquotes ? (
              <Alert
                className="markdown-text-blockquote"
                color="blue"
                radius="md"
                title="Note"
                fz="xs"
                mt={hasLeadingContent(node) ? 'md' : 0}
                mb="md"
                icon={<IconInfoCircleFilled size={18} />}
              >
                {children}
              </Alert>
            ) : (
              <Blockquote mt={hasLeadingContent(node) ? 'md' : 0} mb="md" py="sm" px="md">
                {children}
              </Blockquote>
            ),
          hr: ({ node }: { node?: unknown }) => (
            <Divider mt={hasLeadingContent(node) ? 'xl' : 0} mb="xl" />
          ),
          table: ({ node, children }: { node?: unknown; children?: ReactNode }) => (
            <div
              className={`markdown-text-table-wrapper${hasLeadingContent(node) ? '' : ' markdown-text-table-wrapper-first'}`}
            >
              <table className="markdown-text-table">{children}</table>
            </div>
          ),
          thead: ({ children }: { children?: ReactNode }) => <thead>{children}</thead>,
          tr: ({ children }: { children?: ReactNode }) => <tr>{children}</tr>,
          th: ({ children }: { children?: ReactNode }) => <th>{children}</th>,
          td: ({ children }: { children?: ReactNode }) => <td>{children}</td>,
        }}
      >
        {children}
      </ReactMarkdown>
    </div>
  );
}

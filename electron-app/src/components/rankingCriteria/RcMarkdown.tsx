import { Anchor, Box, List, Text, Title } from '@mantine/core';
import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  headingAnchor,
  openExternal,
  parseDifficultyIcon,
  rankingCriteriaRoute,
  resolveWikiLink,
} from './rcUtils';
import { ApiRcPage } from '../../Types';
import MantineMarkdown from '../documentation/MantineMarkdown';
import GameModeIcon from '../icons/GameModeIcon';
import type { ReactNode } from 'react';
import type { Components } from 'react-markdown';

interface RcMarkdownProps {
  page: ApiRcPage;
  highlightedId?: string | null;
  /** Small text and headings, for showing a statement in context inside another view. */
  compact?: boolean;
}

type HastNode = {
  type?: string;
  value?: string;
  children?: HastNode[];
  position?: { start?: { line?: number; column?: number } };
};

function nodeText(node: HastNode | undefined): string {
  if (!node) return '';
  if (node.type === 'text') return node.value ?? '';
  return (node.children ?? []).map(nodeText).join('');
}

function startLine(node: unknown) {
  return (node as HastNode | undefined)?.position?.start?.line;
}

/** Wiki headings shifted one level down, with the anchors osu-web would give them. */
function RcHeading({
  order,
  node,
  compact,
  children,
}: {
  order: 1 | 2 | 3 | 4 | 5 | 6;
  node?: unknown;
  compact?: boolean;
  children?: ReactNode;
}) {
  if (compact) {
    return (
      <Text
        id={headingAnchor(nodeText(node as HastNode))}
        size="sm"
        fw={700}
        c={order <= 3 ? undefined : 'dimmed'}
        mt={order === 1 ? 0 : 'sm'}
        mb={4}
        className="rc-heading"
      >
        {children}
      </Text>
    );
  }

  return (
    <Title
      order={Math.min(order + 1, 6) as 2 | 3 | 4 | 5 | 6}
      id={headingAnchor(nodeText(node as HastNode))}
      mt={order === 1 ? 0 : 'lg'}
      mb="xs"
      className="rc-heading"
    >
      {children}
    </Title>
  );
}

/**
 * Renders a snapshotted ranking criteria page. Statements known to the catalogue are matched by
 * their line in the markdown, so one can be highlighted in place.
 */
export default function RcMarkdown({ page, highlightedId, compact }: RcMarkdownProps) {
  const navigate = useNavigate();

  const statementsByLine = useMemo(
    () => new Map(page.statements.map((statement) => [statement.startLine, statement])),
    [page.statements]
  );

  const components = useMemo((): Components => {
    const wrapStatement = (line: number | undefined, content: ReactNode) => {
      const statement = line !== undefined ? statementsByLine.get(line) : undefined;
      if (!statement) return null;

      const highlighted = statement.id === highlightedId;

      return (
        <Box
          className="rc-statement"
          data-rc-id={statement.id}
          data-highlighted={highlighted || undefined}
        >
          <Box className="rc-statement-content">{content}</Box>
        </Box>
      );
    };

    return {
      h1: ({ node, children }) => (
        <RcHeading order={1} node={node} compact={compact}>
          {children}
        </RcHeading>
      ),
      h2: ({ node, children }) => (
        <RcHeading order={2} node={node} compact={compact}>
          {children}
        </RcHeading>
      ),
      h3: ({ node, children }) => (
        <RcHeading order={3} node={node} compact={compact}>
          {children}
        </RcHeading>
      ),
      h4: ({ node, children }) => (
        <RcHeading order={4} node={node} compact={compact}>
          {children}
        </RcHeading>
      ),
      h5: ({ node, children }) => (
        <RcHeading order={5} node={node} compact={compact}>
          {children}
        </RcHeading>
      ),
      h6: ({ node, children }) => (
        <RcHeading order={6} node={node} compact={compact}>
          {children}
        </RcHeading>
      ),
      ...(compact && {
        ul: ({ children }) => (
          <List size="sm" spacing={2} mb="xs" withPadding>
            {children}
          </List>
        ),
        ol: ({ children }) => (
          <List size="sm" spacing={2} mb="xs" type="ordered" withPadding>
            {children}
          </List>
        ),
      }),
      li: ({ node, children }) => {
        const wrapped = wrapStatement(startLine(node), children);
        return <List.Item className="rc-list-item">{wrapped ?? children}</List.Item>;
      },
      p: ({ node, children }) => {
        // Only top-level paragraphs can be statements; paragraphs inside list items belong to the item.
        const column = (node as HastNode | undefined)?.position?.start?.column;
        const wrapped = column === 1 ? wrapStatement(startLine(node), children) : null;
        return (
          wrapped ?? (
            <Text size={compact ? 'sm' : 'md'} mb={compact ? 'xs' : 'sm'}>
              {children}
            </Text>
          )
        );
      },
      img: ({ src, alt }) => {
        const icon = parseDifficultyIcon(typeof src === 'string' ? src : undefined);
        if (icon) {
          return (
            <GameModeIcon
              mode={icon.mode}
              starRating={icon.starRating}
              size={compact ? 16 : 22}
              style={{ verticalAlign: 'middle', marginRight: 6 }}
            />
          );
        }

        const resolved =
          typeof src === 'string' && src.startsWith('/') ? 'https://osu.ppy.sh' + src : src;
        return <img src={resolved} alt={alt} />;
      },
      a: ({ href, children }) => {
        if (!href) return <>{children}</>;

        const link = resolveWikiLink(href, page.key);
        const handleClick = (event: React.MouseEvent<HTMLAnchorElement>) => {
          event.preventDefault();

          if (link.kind === 'external') {
            void openExternal(link.href);
          } else if (link.kind === 'anchor') {
            document.getElementById(link.anchor)?.scrollIntoView({ behavior: 'smooth' });
          } else {
            navigate(rankingCriteriaRoute(link.page));
          }
        };

        return (
          <Anchor size={compact ? 'sm' : 'md'} href={href} onClick={handleClick}>
            {children}
          </Anchor>
        );
      },
    };
  }, [statementsByLine, highlightedId, compact, page.key, navigate]);

  return (
    <Box className="rc-markdown">
      <MantineMarkdown components={components}>{page.markdown}</MantineMarkdown>
    </Box>
  );
}

import remarkGfm from 'remark-gfm';
import remarkParse from 'remark-parse';
import { unified } from 'unified';
import type { Root, RootContent } from 'mdast';

declare module 'mdast' {
  interface WikiAlert extends Parent {
    type: 'wikiAlert';
  }

  interface RootContentMap {
    wikiAlert: WikiAlert;
  }
}

/** osu-wiki `::: alert-notice` containers, turned into an element the renderer can style. */
export function remarkWikiAlerts() {
  return (tree: Root, file: { value?: string | Uint8Array }) => {
    const source = typeof file.value === 'string' ? file.value : '';
    const next: RootContent[] = [];

    for (let i = 0; i < tree.children.length; i++) {
      const node = tree.children[i];
      const start = node.position?.start.offset;
      if (node.type !== 'paragraph' || start == null || !source.startsWith('::: alert-', start)) {
        next.push(node);
        continue;
      }

      let end = i;
      let raw = '';
      for (; end < tree.children.length; end++) {
        const endOffset = tree.children[end].position?.end.offset;
        if (endOffset == null) break;
        raw = source.slice(start, endOffset);
        if (/\r?\n:::\s*$/.test(raw)) break;
      }

      const match = raw.match(/^::: alert-([a-z]+)\r?\n([\s\S]*?)\r?\n:::\s*$/);
      if (!match) {
        next.push(node);
        continue;
      }

      const lines = match[2].split(/\r?\n/);
      const title = lines[0]?.match(/^\*\*(.+)\*\*$/)?.[1];
      const bodySource = (title ? lines.slice(1).join('\n') : match[2]).trim();
      const body = bodySource
        ? unified().use(remarkParse).use(remarkGfm).parse(bodySource)
        : { children: [] as RootContent[] };

      next.push({
        type: 'wikiAlert',
        children: body.children,
        data: {
          hName: 'div',
          hProperties: {
            className: 'rc-wiki-alert',
            'data-kind': match[1],
            'data-title': title ?? '',
          },
        },
      });
      i = end;
    }

    tree.children = next;
  };
}

import {
  Title,
  Text,
  ListItem,
  Code,
  Blockquote,
  Image,
  Divider,
  List,
  useMantineTheme,
  useMantineColorScheme
} from "@mantine/core";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";

interface MantineMarkdownProps {
  children: string;
}

export default function MantineMarkdown({ children }: MantineMarkdownProps) {
  const theme = useMantineTheme();
  const { colorScheme } = useMantineColorScheme();
  
  return (
    <ReactMarkdown
      remarkPlugins={[remarkGfm]}
      components={{
        h1: ({ node, ...props }) => <Title order={1} {...props} />,
        h2: ({ node, ...props }) => <Title order={2} {...props} />,
        h3: ({ node, ...props }) => <Title order={3} {...props} />,
        p: ({ node, ...props }) => <Text component="p" {...props} />,
        strong: ({ node, ...props }) => <Text component="span" fw={700} {...props} />,
        em: ({ node, ...props }) => <Text component="span" fs="italic" {...props} />,
        blockquote: ({ node, ...props }) => <Blockquote p="sm" {...props} />,
        img: ({ node, ...props }) => <Image
          radius="md"
          mx="auto"
          p="md"
          style={{ maxHeight: 500, maxWidth: "100%", width: "auto" }}
          {...props} />,
        hr: () => <Divider my="md" />,
        ul: (props) => <List withPadding {...props} />,
        li: (props) => <ListItem {...props} />,
        code: ({ node, ...props }) => <Code {...props} />,
        table: ({ node, ...props }) => (
          <table
            style={{
              borderCollapse: "collapse",
              background: colorScheme === "dark" ? theme.colors.dark[6] : theme.white,
              overflow: "hidden"
            }}
            {...props}
          />
        ),
        thead: ({ node, ...props }) => (
          <thead
            style={{
              background: colorScheme === "dark" ? theme.colors.dark[5] : theme.colors.gray[1]
            }}
            {...props}
          />
        ),
        tr: ({ node, ...props }) => (
          <tr
            style={{
              borderBottom: `1px solid ${theme.colors.gray[3]}`
            }}
            {...props}
          />
        ),
        th: ({ node, ...props }) => (
          <th
            style={{
              padding: theme.spacing.sm,
              border: `1px solid ${theme.colors.gray[3]}`,
              fontWeight: 700,
              textAlign: "left",
              background: colorScheme === "dark" ? theme.colors.dark[5] : theme.colors.gray[0]
            }}
            {...props}
          />
        ),
        td: ({ node, ...props }) => (
          <td
            style={{
              padding: theme.spacing.sm,
              border: `1px solid ${theme.colors.gray[2]}`
            }}
            {...props}
          />
        ),
      }}
    >
      {children}
    </ReactMarkdown>
  );
}

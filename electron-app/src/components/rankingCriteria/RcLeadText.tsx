import { Code } from '@mantine/core';

/** A statement's lead with its `inline code`, such as file extensions, shown as code like on the wiki. */
export default function RcLeadText({ children }: { children: string }) {
  return (
    <>
      {children.split(/`([^`]+)`/).map((part, index) =>
        index % 2 === 1 ? (
          <Code key={index} fz="0.875em">
            {part}
          </Code>
        ) : (
          part
        )
      )}
    </>
  );
}

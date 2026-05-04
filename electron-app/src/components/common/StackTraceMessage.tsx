import { Button, Code, CopyButton, Flex, ScrollArea, Tooltip, Text } from '@mantine/core';
import { IconCheck, IconCopy } from '@tabler/icons-react';

interface Props {
  stackTrace: string;
}

function StackTraceMessage(props: Props) {
  return (
    <Flex direction="column" gap="xs">
      <Text mt="sm" size="xs" style={{ whiteSpace: 'pre-wrap' }}>
        Copy the below text and create a GitHub issue or contact Greaper so the issue can be solved.
      </Text>
      <ScrollArea.Autosize mah={200}>
        <Code block style={{ whiteSpace: 'pre-wrap', fontSize: 'var(--mantine-font-size-xs)' }}>
          {props.stackTrace}
        </Code>
      </ScrollArea.Autosize>
      <CopyButton value={props.stackTrace}>
        {({ copied, copy }) => (
          <Tooltip label={copied ? 'Copied' : 'Copy stack trace'} withArrow>
            <Button
              size="xs"
              variant="light"
              color={copied ? 'teal' : 'gray'}
              onClick={copy}
              leftSection={copied ? <IconCheck size={14} /> : <IconCopy size={14} />}
            >
              {copied ? 'Copied' : 'Copy to clipboard'}
            </Button>
          </Tooltip>
        )}
      </CopyButton>
    </Flex>
  );
}

export default StackTraceMessage;

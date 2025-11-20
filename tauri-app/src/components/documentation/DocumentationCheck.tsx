import {Flex, Group, Text, useMantineTheme} from "@mantine/core";
import {ApiDocumentationCheck} from "../../Types.ts";
import NoIssueIcon from "../icons/NoIssueIcon.tsx";
import ProblemIcon from "../icons/ProblemIcon.tsx";
import WarningIcon from "../icons/WarningIcon.tsx";
import MinorIcon from "../icons/MinorIcon.tsx";
import ErrorIcon from "../icons/ErrorIcon.tsx";

interface DocumentationCheckProps {
  check: ApiDocumentationCheck;
}

function DocumentationCheck({ check }: DocumentationCheckProps) {
  const theme = useMantineTheme();
  const background = theme.variantColorResolver({variant: "light", theme, color: "gray" }).background;
  
  return (
    <Group style={{ background: background, borderRadius: theme.defaultRadius }} p="sm" w="100%">
      <Flex direction="column" style={{ flex: 1 }}>
        <Text fw="bold">{check.description}</Text>
        <Text size="sm" c="dimmed">
          {check.category} {check.subCategory}
        </Text>
      </Flex>
      <Flex direction="column">
        <Group gap="xs" style={{ alignSelf: 'end' }}>
          {check.outcomes.map((outcome) => {
            if (outcome === "Info") {
              return <NoIssueIcon/>
            } else if (outcome === "Error") {
              return <ErrorIcon />
            } else if (outcome === "Problem") {
              return <ProblemIcon />
            } else if (outcome === "Warning") {
              return <WarningIcon />
            } else if (outcome === "Minor") {
              return <MinorIcon />
            } else {
              return <Text>Missing Icon</Text>
            }
          })}
        </Group>
        
        <Text size="sm" c="dimmed" style={{ alignSelf: 'end' }}>{check.author}</Text>
      </Flex>
    </Group>
  );
}

export default DocumentationCheck;


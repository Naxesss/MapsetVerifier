import {Alert, Flex, Text, SimpleGrid, Space, Tabs, useMantineTheme} from "@mantine/core";
import {
  IconCircleCheckFilled, IconCircleXFilled,
  IconExclamationCircleFilled
} from "@tabler/icons-react";
import React from "react";
import NoIssueIcon from "../icons/NoIssueIcon.tsx";
import MinorIcon from "../icons/MinorIcon.tsx";
import WarningIcon from "../icons/WarningIcon.tsx";
import ErrorIcon from "../icons/ErrorIcon.tsx";

function Documentation() {
  const theme = useMantineTheme();
  return (
    <>
      <Alert variant="light" color="gray" radius="md" title="Icons">
        <SimpleGrid
          cols={3}
          style={{
            gridTemplateColumns: "32px min-content auto",
            alignItems: "center"
          }}
        >
          <DocumentationIconExplanation
            icon={<NoIssueIcon />}
            title="Check"
            category="Checks"
            description="No issues were found."
          />
          <DocumentationIconExplanation
            icon={<MinorIcon />}
            title="Minor"
            category="Checks"
            description="One or more negligible issues were found."
          />
          <DocumentationIconExplanation
            icon={<WarningIcon />}
            title="Warning"
            category="Checks"
            description="One or more guideline breaking issues were found."
          />
          <DocumentationIconExplanation
            icon={<ErrorIcon />}
            title="Problem"
            category="Checks"
            description="One or more rule breaking issues were found."
          />
        </SimpleGrid>
      </Alert>
      <Space h="md" />
      <Tabs defaultValue="general">
        <Tabs.List grow>
          <Tabs.Tab value="general">General</Tabs.Tab>
          <Tabs.Tab value="osu">Standard</Tabs.Tab>
          <Tabs.Tab value="taiko">Taiko</Tabs.Tab>
          <Tabs.Tab value="catch">Catch</Tabs.Tab>
          <Tabs.Tab value="mania">Mania</Tabs.Tab>
        </Tabs.List>
        
        <Tabs.Panel value="general" pt="sm">
          This tab will contain all the general checks
          <Space h="md" />
          Check 1
          <Space h="md" />
          Check 2
          <Space h="md" />
          Check 3
          <Space h="md" />
          Check 4
          <Space h="md" />
          Check 5
        </Tabs.Panel>

        <Tabs.Panel value="osu" pt="sm">
          This tab will contain all the osu checks
        </Tabs.Panel>
        <Tabs.Panel value="taiko" pt="sm">
          This tab will contain all the taiko checks
        </Tabs.Panel>
        <Tabs.Panel value="catch" pt="sm">
          This tab will contain all the catch checks
        </Tabs.Panel>
        <Tabs.Panel value="mania" pt="sm">
          This tab will contain all the mania checks
        </Tabs.Panel>
      </Tabs>
    </>
  );
}

interface InfoIconExplanationProp {
  icon: React.ReactNode
  title: string
  category: string
  description: string
}

function DocumentationIconExplanation(props: InfoIconExplanationProp) {
  return (
    <>
      {props.icon}
      <Flex direction="column">
        <Text fw="bold">{props.title}</Text>
        <Text fs="italic">{props.category}</Text>
      </Flex>
      <Text>{props.description}</Text>
    </>
  )
}

export default Documentation;
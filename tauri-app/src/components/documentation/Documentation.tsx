import {Alert, Flex, Text, SimpleGrid, Space, Tabs, Group} from "@mantine/core";
import React from "react";
import NoIssueIcon from "../icons/NoIssueIcon.tsx";
import MinorIcon from "../icons/MinorIcon.tsx";
import WarningIcon from "../icons/WarningIcon.tsx";
import ErrorIcon from "../icons/ErrorIcon.tsx";
import GeneralChecks from "./GeneralChecks";
import BeatmapChecks from "./BeatmapChecks.tsx";
import ProblemIcon from "../icons/ProblemIcon.tsx";

function Documentation() {
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
            icon={<ProblemIcon />}
            title="Problem"
            category="Checks"
            description="One or more rule breaking issues were found."
          />
          <DocumentationIconExplanation
            icon={<ErrorIcon />}
            title="Error"
            category="Checks"
            description="An error occurred preventing a complete check."
          />
        </SimpleGrid>
      </Alert>
      <Space h="md" />
      <Tabs defaultValue="general">
        <Tabs.List grow>
          <Tabs.Tab value="general">General</Tabs.Tab>
          <Tabs.Tab value="standard">Standard</Tabs.Tab>
          <Tabs.Tab value="taiko">Taiko</Tabs.Tab>
          <Tabs.Tab value="catch">Catch</Tabs.Tab>
          <Tabs.Tab value="mania">Mania</Tabs.Tab>
        </Tabs.List>
        <Tabs.Panel value="general" pt="sm">
          <GeneralChecks />
        </Tabs.Panel>
        <Tabs.Panel value="standard" pt="sm">
          <Group gap="xs">
            <BeatmapChecks mode="Standard" />
          </Group>
        </Tabs.Panel>
        <Tabs.Panel value="taiko" pt="sm">
          <Group gap="xs">
            <BeatmapChecks mode="Taiko" />
          </Group>
        </Tabs.Panel>
        <Tabs.Panel value="catch" pt="sm">
          <Group gap="xs">
            <BeatmapChecks mode="Catch" />
          </Group>
        </Tabs.Panel>
        <Tabs.Panel value="mania" pt="sm">
          <Group gap="xs">
            <BeatmapChecks mode="Mania" />
          </Group>
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
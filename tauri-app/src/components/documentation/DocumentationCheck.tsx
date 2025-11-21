import {Flex, Group, Text, useMantineTheme} from "@mantine/core";
import {ApiDocumentationCheck} from "../../Types.ts";
import { useState } from "react";
import DocumentationCheckModal from "./DocumentationCheckModal";
import LevelIcon from "../icons/LevelIcon.tsx";

interface DocumentationCheckProps {
  check: ApiDocumentationCheck;
}

function DocumentationCheck({ check }: DocumentationCheckProps) {
  const theme = useMantineTheme();
  const [modalOpen, setModalOpen] = useState(false);
  const [hovered, setHovered] = useState(false);
  const background = hovered
    ? theme.variantColorResolver({variant: "light", theme, color: "blue" }).background
    : theme.variantColorResolver({variant: "light", theme, color: "gray" }).background;

  return (
    <>
      <Group
        style={{
          background: background,
          borderRadius: theme.defaultRadius,
          cursor: 'pointer',
          transition: 'background 0.2s',
        }}
        p="sm"
        w="100%"
        onMouseEnter={() => setHovered(true)}
        onMouseLeave={() => setHovered(false)}
        onClick={() => setModalOpen(true)}
      >
        <Flex direction="column" style={{ flex: 1 }}>
          <Text fw="bold">{check.description}</Text>
          <Text size="sm" c="dimmed">
            {`${check.category} > ${check.subCategory}`}
          </Text>
        </Flex>
        <Flex direction="column">
          <Group gap="xs" style={{ alignSelf: 'end' }}>
            {check.outcomes.map((level) => {
              return <LevelIcon level={level} />
            })}
          </Group>
          <Text size="sm" c="dimmed" style={{ alignSelf: 'end' }}>{check.author}</Text>
        </Flex>
      </Group>
      <DocumentationCheckModal
        opened={modalOpen}
        onClose={() => setModalOpen(false)}
        check={check}
      />
    </>
  );
}

export default DocumentationCheck;

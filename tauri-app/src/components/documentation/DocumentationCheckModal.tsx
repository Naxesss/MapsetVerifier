import { useEffect, useState } from "react";
import {Modal, Text, Loader, Flex, Alert} from "@mantine/core";
import DocumentationApi from "../../client/DocumentationApi";
import { ApiDocumentationCheck, ApiDocumentationCheckDetails } from "../../Types";
import LevelIcon from "../icons/LevelIcon.tsx";
import MantineMarkdown from "./MantineMarkdown";

interface DocumentationCheckModalProps {
  opened: boolean;
  onClose: () => void;
  check: ApiDocumentationCheck;
}

export default function DocumentationCheckModal({ opened, onClose, check }: DocumentationCheckModalProps) {
  const [details, setDetails] = useState<ApiDocumentationCheckDetails | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!opened) return;
    setLoading(true);
    setError(null);
    DocumentationApi().getCheckDetails(check.id.toString())
      .then(setDetails)
      .catch(() => setError("Failed to load details."))
      .finally(() => setLoading(false));
  }, [opened, check.id]);

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={<Text fw="bold" size="lg">{check.description}</Text>}
      size="80%"
      styles={{ content: { maxWidth: 1000 } }}
    >
      <Flex direction="column" gap="sm">
        <Flex justify="space-between">
          <Text size="sm">{`${check.category} > ${check.subCategory}`}</Text>
          <Text size="sm">Created by {check.author}</Text>
        </Flex>
        {loading && <Loader />}
        {error && <Text c="red">{error}</Text>}
        {details && (
          <>
            {details.outcomes.map((checkDetails, i) => (
              <Alert key={i}>
                <Flex direction="column" gap="xs">
                  <Flex align="center" gap="xs">
                    <LevelIcon level={checkDetails.level} />
                    <MantineMarkdown>{checkDetails.description}</MantineMarkdown>
                  </Flex>
                  {checkDetails.cause && <MantineMarkdown>{checkDetails.cause}</MantineMarkdown>}
                </Flex>
              </Alert>
            ))}
            <MantineMarkdown>{details.description}</MantineMarkdown>
          </>
        )}
      </Flex>
    </Modal>
  );
}

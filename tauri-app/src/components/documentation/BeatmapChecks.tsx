import { useEffect, useState } from "react";
import DocumentationApi from "../../client/DocumentationApi";
import DocumentationCheck from "./DocumentationCheck";
import {Group, Text} from "@mantine/core";
import {ApiDocumentationCheck, Mode} from "../../Types.ts";

interface BeatmapChecksProps {
  mode: Mode;
}

function BeatmapChecks({ mode }: BeatmapChecksProps) {
  const [checks, setChecks] = useState<ApiDocumentationCheck[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    DocumentationApi().getBeatmapDocumentation(mode)
      .then(data => setChecks(data))
      .finally(() => setLoading(false));
  }, [mode]);

  if (loading) return <div>Loading {mode} checks...</div>;
  if (!checks.length) return <div>No checks found for {mode}.</div>;

  return (
    <Group gap="xs">
      <Text>A total of {checks.length} {mode} checks exist.</Text>
      {checks.map(check => (
        <DocumentationCheck key={check.id} check={check} />
      ))}
    </Group>
  );
}

export default BeatmapChecks;

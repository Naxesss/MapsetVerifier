import { useQuery } from "@tanstack/react-query";
import DocumentationApi from "../../client/DocumentationApi";
import DocumentationCheck from "./DocumentationCheck";
import { Text } from "@mantine/core";
import { ApiDocumentationCheck, Mode } from "../../Types.ts";
import { FetchError } from "../../client/ApiHelper.ts";

interface BeatmapChecksProps {
  mode: Mode;
}

function BeatmapChecks({ mode }: BeatmapChecksProps) {
  const { data: checks, isLoading, error } = useQuery<ApiDocumentationCheck[], FetchError>({
    queryKey: ["documentationBeatmapChecks", mode],
    queryFn: () => DocumentationApi.getBeatmapDocumentation(mode),
    staleTime: Infinity
  });

  if (isLoading) return <Text>Loading...</Text>;
  if (error) return <Text>Error loading checks.</Text>;
  if (!checks || checks.length === 0) return <Text>No {mode} checks found.</Text>;

  return (
    <>
      <Text>A total of {checks.length} {mode} checks exist.</Text>
      {checks.map(check => (
        <DocumentationCheck key={check.id} check={check} />
      ))}
    </>
  );
}

export default BeatmapChecks;

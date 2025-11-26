import { Alert, Group, Loader, Text } from '@mantine/core';
import { useQuery, UseQueryOptions } from '@tanstack/react-query';
import DocumentationCheck from './DocumentationCheck';
import DocumentationApi from '../../client/DocumentationApi';
import { ApiDocumentationCheck } from '../../Types.ts';

function GeneralChecks() {
  const queryOptions: UseQueryOptions<ApiDocumentationCheck[], Error> = {
    queryKey: ['general-documentation'],
    queryFn: DocumentationApi.getGeneralDocumentation,
    staleTime: Infinity,
  };
  const { data, isLoading, isError } = useQuery<ApiDocumentationCheck[], Error>(queryOptions);

  if (isLoading) return <Loader size="sm" />;
  if (isError) return <Alert color="red">Failed to load general checks.</Alert>;
  if (!data || data.length === 0) return <Text>No general checks found.</Text>;

  return (
    <Group gap="xs">
      <Text>A total of {data.length} General checks exist.</Text>
      {data.map((check: ApiDocumentationCheck) => (
        <DocumentationCheck key={check.id} check={check} />
      ))}
    </Group>
  );
}

export default GeneralChecks;

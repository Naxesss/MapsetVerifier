import { useDocumentation } from '../../../context/DocumentationContext';

export function useDocumentationChecks() {
  const { status, error, generalChecks, beatmapChecks, allChecks, checksById, getCheckById } =
    useDocumentation();

  const isLoading = status === 'idle' || status === 'loading';
  const isError = status === 'error';

  return {
    allChecks,
    checksById,
    getCheckById,
    isLoading,
    isError,
    generalChecks,
    beatmapChecks: {
      Standard: beatmapChecks.Standard,
      Taiko: beatmapChecks.Taiko,
      Catch: beatmapChecks.Catch,
      Mania: beatmapChecks.Mania,
    },
    documentationError: error,
  };
}

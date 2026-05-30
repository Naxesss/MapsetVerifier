import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import DocumentationApi from '../client/DocumentationApi';
import type { ApiDocumentationCheck, Mode } from '../Types';

export type DocumentationStatus = 'idle' | 'loading' | 'success' | 'error';

const BEATMAP_MODES: Mode[] = ['Standard', 'Taiko', 'Catch', 'Mania'];

type DocumentationBeatmapChecks = Partial<Record<Mode, ApiDocumentationCheck[]>>;

interface DocumentationContextType {
  status: DocumentationStatus;
  error: Error | null;
  generalChecks?: ApiDocumentationCheck[];
  beatmapChecks: DocumentationBeatmapChecks;
  /** Merged lists: general, then modes in Standard → Taiko → Catch → Mania order */
  allChecks: ApiDocumentationCheck[];
  checksById: Map<number, ApiDocumentationCheck>;
  getCheckById: (id: number) => ApiDocumentationCheck | undefined;
}

const DocumentationContext = createContext<DocumentationContextType | undefined>(undefined);

const emptyChecksById = new Map<number, ApiDocumentationCheck>();

function mergeDocumentationChecks(params: {
  general: ApiDocumentationCheck[];
  byMode: DocumentationBeatmapChecks;
}): { allChecks: ApiDocumentationCheck[]; checksById: Map<number, ApiDocumentationCheck> } {
  const checks: ApiDocumentationCheck[] = [];
  checks.push(...params.general);

  for (const mode of BEATMAP_MODES) {
    const list = params.byMode[mode];
    if (list) checks.push(...list);
  }

  const checksById = new Map<number, ApiDocumentationCheck>();
  for (const check of checks) {
    checksById.set(check.id, check);
  }

  return { allChecks: checks, checksById };
}

export const DocumentationProvider = ({ children }: { children: ReactNode }) => {
  const [status, setStatus] = useState<DocumentationStatus>('idle');
  const [error, setError] = useState<Error | null>(null);
  const [generalChecks, setGeneralChecks] = useState<ApiDocumentationCheck[]>();
  const [beatmapChecks, setBeatmapChecks] = useState<DocumentationBeatmapChecks>({});

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setStatus('loading');
      setError(null);

      try {
        const [general, standard, taiko, catchMode, mania] = await Promise.all([
          DocumentationApi.getGeneralDocumentation(),
          DocumentationApi.getBeatmapDocumentation('Standard'),
          DocumentationApi.getBeatmapDocumentation('Taiko'),
          DocumentationApi.getBeatmapDocumentation('Catch'),
          DocumentationApi.getBeatmapDocumentation('Mania'),
        ]);

        if (cancelled) return;

        setGeneralChecks(general);
        setBeatmapChecks({
          Standard: standard,
          Taiko: taiko,
          Catch: catchMode,
          Mania: mania,
        });
        setStatus('success');
      } catch (e) {
        if (cancelled) return;

        console.error('[Documentation] Failed to load documentation', e);

        const err = e instanceof Error ? e : new Error(String(e));
        setGeneralChecks(undefined);
        setBeatmapChecks({});
        setError(err);
        setStatus('error');
      }
    }

    void load();

    return () => {
      cancelled = true;
    };
  }, []);

  const merged = useMemo(
    () =>
      generalChecks
        ? mergeDocumentationChecks({
            general: generalChecks,
            byMode: beatmapChecks,
          })
        : { allChecks: [] as ApiDocumentationCheck[], checksById: emptyChecksById },
    [generalChecks, beatmapChecks]
  );

  const getCheckById = useCallback((id: number) => merged.checksById.get(id), [merged.checksById]);

  const value = useMemo<DocumentationContextType>(
    () => ({
      status,
      error,
      generalChecks,
      beatmapChecks,
      allChecks: merged.allChecks,
      checksById: merged.checksById,
      getCheckById,
    }),
    [status, error, generalChecks, beatmapChecks, merged.allChecks, merged.checksById, getCheckById]
  );

  return <DocumentationContext.Provider value={value}>{children}</DocumentationContext.Provider>;
};

export const useDocumentation = () => {
  const ctx = useContext(DocumentationContext);
  if (!ctx) {
    throw new Error('useDocumentation must be used within DocumentationProvider');
  }
  return ctx;
};

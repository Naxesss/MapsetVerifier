import { useHotkeys, type HotkeyItem } from '@mantine/hooks';
import { notifications } from '@mantine/notifications';
import { IconCheck } from '@tabler/icons-react';
import { useQueryClient } from '@tanstack/react-query';
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  type ReactNode,
} from 'react';
import { useBeatmap } from './BeatmapContext.tsx';

/** Runs before all beatmap-scoped queries are invalidated (e.g. clear checks override state). */
export type BeatmapRefreshSideEffect = () => void | Promise<void>;

interface BeatmapReparseContextValue {
  registerReparse: (handler: BeatmapRefreshSideEffect) => () => void;
  triggerReparse: () => Promise<void>;
}

const BeatmapReparseContext = createContext<BeatmapReparseContextValue | null>(null);

const REPARSE_TOAST_ID = 'beatmap-reparse-result';

function queryKeyMatchesBeatmapFolderPath(queryKey: unknown, beatmapFolderPath: string): boolean {
  return Array.isArray(queryKey) && queryKey.length >= 2 && queryKey[1] === beatmapFolderPath;
}

export function BeatmapReparseProvider({ children }: { children: ReactNode }) {
  const { beatmapFolderPath } = useBeatmap();
  const queryClient = useQueryClient();
  const sideEffectHandlersRef = useRef(new Set<BeatmapRefreshSideEffect>());

  const registerReparse = useCallback((handler: BeatmapRefreshSideEffect) => {
    sideEffectHandlersRef.current.add(handler);
    return () => {
      sideEffectHandlersRef.current.delete(handler);
    };
  }, []);

  const triggerReparse = useCallback(async () => {
    if (!beatmapFolderPath) {
      return;
    }

    notifications.show({
      id: REPARSE_TOAST_ID,
      loading: true,
      message: 'Refreshing beatmap…',
      color: 'primary',
      autoClose: false,
      withCloseButton: false,
      loaderProps: { size: 18, color: 'white' },
    });

    try {
      const sideEffects = [...sideEffectHandlersRef.current];
      await Promise.all(sideEffects.map((fn) => Promise.resolve(fn())));

      await queryClient.invalidateQueries({
        predicate: (query) => queryKeyMatchesBeatmapFolderPath(query.queryKey, beatmapFolderPath),
        refetchType: 'all',
      });

      notifications.update({
        id: REPARSE_TOAST_ID,
        loading: false,
        color: 'green',
        message: 'Beatmap refreshed!',
        icon: <IconCheck size={18} />,
        autoClose: 1500,
        withCloseButton: false,
      });
    } catch (error) {
      console.error('Beatmap reparse failed:', error);
      notifications.update({
        id: REPARSE_TOAST_ID,
        loading: false,
        color: 'red',
        message: error instanceof Error ? error.message : 'Reparse failed.',
        autoClose: 5000,
        icon: undefined,
        withCloseButton: true,
      });
    }
  }, [beatmapFolderPath, queryClient]);

  const f5Hotkeys = useMemo<HotkeyItem[]>(
    () => [['F5', () => void triggerReparse(), { preventDefault: true }]],
    [triggerReparse]
  );
  useHotkeys(f5Hotkeys);

  const value = useMemo(
    () => ({ registerReparse, triggerReparse }),
    [registerReparse, triggerReparse]
  );

  return <BeatmapReparseContext.Provider value={value}>{children}</BeatmapReparseContext.Provider>;
}

export function useBeatmapReparse() {
  const ctx = useContext(BeatmapReparseContext);
  if (!ctx) {
    throw new Error('useBeatmapReparse must be used within BeatmapReparseProvider');
  }
  return ctx;
}

export function useRegisterBeatmapReparse(handler: BeatmapRefreshSideEffect) {
  const { registerReparse } = useBeatmapReparse();
  useEffect(() => registerReparse(handler), [handler, registerReparse]);
}

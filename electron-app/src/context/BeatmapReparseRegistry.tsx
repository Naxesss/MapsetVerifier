import { useHotkeys, type HotkeyItem } from '@mantine/hooks';
import { notifications } from '@mantine/notifications';
import { IconCheck } from '@tabler/icons-react';
import { createContext, useCallback, useContext, useEffect, useMemo, useRef, type ReactNode } from 'react';
import { useBeatmap } from './BeatmapContext.tsx';

type ReparseHandler = () => Promise<void | 'skipped'>;

interface BeatmapReparseContextValue {
  registerReparse: (handler: ReparseHandler) => () => void;
  triggerReparse: () => Promise<void>;
}

const BeatmapReparseContext = createContext<BeatmapReparseContextValue | null>(null);

const REPARSE_TOAST_ID = 'beatmap-reparse-result';

export function BeatmapReparseProvider({ children }: { children: ReactNode }) {
  const { beatmapFolderPath, refetchBeatmapInfo } = useBeatmap();
  const handlerRef = useRef<ReparseHandler | null>(null);

  const registerReparse = useCallback((handler: ReparseHandler) => {
    handlerRef.current = handler;
    return () => {
      if (handlerRef.current === handler) {
        handlerRef.current = null;
      }
    };
  }, []);

  const triggerReparse = useCallback(async () => {
    if (!handlerRef.current && !beatmapFolderPath) {
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
      let didWork = false;
      if (handlerRef.current) {
        const result = await handlerRef.current();
        if (result !== 'skipped') {
          didWork = true;
        }
      } else {
        await refetchBeatmapInfo();
        didWork = true;
      }

      if (!didWork) {
        notifications.hide(REPARSE_TOAST_ID);
        return;
      }

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
  }, [beatmapFolderPath, refetchBeatmapInfo]);

  const f5Hotkeys = useMemo<HotkeyItem[]>(
    () => [['F5', () => void triggerReparse(), { preventDefault: true }]],
    [triggerReparse],
  );
  useHotkeys(f5Hotkeys);

  const value = useMemo(
    () => ({ registerReparse, triggerReparse }),
    [registerReparse, triggerReparse],
  );

  return (
    <BeatmapReparseContext.Provider value={value}>{children}</BeatmapReparseContext.Provider>
  );
}

export function useBeatmapReparse() {
  const ctx = useContext(BeatmapReparseContext);
  if (!ctx) {
    throw new Error('useBeatmapReparse must be used within BeatmapReparseProvider');
  }
  return ctx;
}

export function useRegisterBeatmapReparse(handler: ReparseHandler) {
  const { registerReparse } = useBeatmapReparse();
  useEffect(() => registerReparse(handler), [handler, registerReparse]);
}

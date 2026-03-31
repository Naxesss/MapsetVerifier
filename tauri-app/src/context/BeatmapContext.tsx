import { useQuery, type QueryObserverResult } from '@tanstack/react-query';
import { createContext, useContext, useMemo, useState, ReactNode } from 'react';
import { useSettings } from './SettingsContext.tsx';
import { FetchError } from '../client/ApiHelper.ts';
import BeatmapApi from '../client/BeatmapApi.ts';
import type { ApiBeatmapInfo } from '../Types.ts';

interface BeatmapContextType {
  selectedFolder: string | undefined;
  setSelectedFolder: (folder: string | undefined) => void;
  beatmapFolderPath: string | undefined;
  beatmapInfo: ApiBeatmapInfo | undefined;
  isBeatmapInfoLoading: boolean;
  beatmapInfoError: FetchError | null;
  refetchBeatmapInfo: () => Promise<QueryObserverResult<ApiBeatmapInfo, FetchError>>;
}

const BeatmapContext = createContext<BeatmapContextType | undefined>(undefined);

export const BeatmapProvider = ({ children }: { children: ReactNode }) => {
  const { settings } = useSettings();
  const [selectedFolder, setSelectedFolder] = useState<string | undefined>(undefined);

  const beatmapFolderPath = useMemo(() => {
    return selectedFolder && settings.songFolder
      ? [settings.songFolder, selectedFolder].join('\\').replace(/\//g, '\\')
      : undefined;
  }, [selectedFolder, settings.songFolder]);

  const beatmapInfoQuery = useQuery<ApiBeatmapInfo, FetchError>({
    queryKey: ['beatmap-info', beatmapFolderPath || 'unavailable'],
    queryFn: () => {
      if (!beatmapFolderPath) throw new Error('Beatmap folder path unavailable');
      return BeatmapApi.getInfo(beatmapFolderPath);
    },
    enabled: !!beatmapFolderPath,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false,
  });

  return (
    <BeatmapContext.Provider
      value={{
        selectedFolder,
        setSelectedFolder,
        beatmapFolderPath,
        beatmapInfo: beatmapFolderPath ? beatmapInfoQuery.data : undefined,
        isBeatmapInfoLoading: !!beatmapFolderPath && beatmapInfoQuery.isLoading,
        beatmapInfoError: beatmapInfoQuery.error,
        refetchBeatmapInfo: beatmapInfoQuery.refetch,
      }}
    >
      {children}
    </BeatmapContext.Provider>
  );
};

export const useBeatmap = () => {
  const context = useContext(BeatmapContext);
  if (!context) throw new Error('useBeatmap must be used within BeatmapProvider');
  return context;
};


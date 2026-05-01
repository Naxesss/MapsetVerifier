import { useQuery, type QueryObserverResult } from '@tanstack/react-query';
import { createContext, useContext, useMemo, useState, ReactNode } from 'react';
import { useSettings } from './SettingsContext.tsx';
import { FetchError } from '../client/ApiHelper.ts';
import BeatmapApi from '../client/BeatmapApi.ts';
import { buildBeatmapFolderPath } from '../utils/buildBeatmapFolderPath.ts';
import type { ApiBeatmapInfo } from '../Types.ts';

interface BeatmapContextType {
  selectedFolder: string | undefined;
  setSelectedFolder: (folder: string | undefined) => void;
  selectedFolderPath: string | undefined;
  setSelectedFolderPath: (folderPath: string | undefined) => void;
  activeSongFolder: string | undefined;
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
  const [selectedFolderPath, setSelectedFolderPath] = useState<string | undefined>(undefined);
  const [selectedSongFolder, setSelectedSongFolder] = useState<string | undefined>(undefined);

  const activeSongFolder = selectedSongFolder ?? settings.songFolder;

  const beatmapFolderPath = useMemo(() => {
    if (selectedFolderPath)
      return selectedFolderPath;
    return buildBeatmapFolderPath(activeSongFolder, selectedFolder);
  }, [selectedFolderPath, activeSongFolder, selectedFolder]);

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
        setSelectedFolder: (folder) => {
          setSelectedFolder(folder);
          setSelectedFolderPath(undefined);
          setSelectedSongFolder(undefined);
        },
        selectedFolderPath,
        setSelectedFolderPath: (folderPath) => {
          setSelectedFolderPath(folderPath);
          if (folderPath)
          {
            setSelectedFolder(folderPath);
            setSelectedSongFolder(undefined);
            return;
          }
          setSelectedSongFolder(undefined);
        },
        activeSongFolder,
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


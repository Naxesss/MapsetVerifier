import { createContext, useContext, useState, ReactNode } from 'react';

interface BeatmapContextType {
  selectedFolder: string | undefined;
  setSelectedFolder: (folder: string | undefined) => void;
}

const BeatmapContext = createContext<BeatmapContextType | undefined>(undefined);

export const BeatmapProvider = ({ children }: { children: ReactNode }) => {
  const [selectedFolder, setSelectedFolder] = useState<string | undefined>(undefined);

  return (
    <BeatmapContext.Provider value={{ selectedFolder, setSelectedFolder }}>
      {children}
    </BeatmapContext.Provider>
  );
};

export const useBeatmap = () => {
  const context = useContext(BeatmapContext);
  if (!context) throw new Error('useBeatmap must be used within BeatmapProvider');
  return context;
};


import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import type { UpdateInfo } from '../electron-env';

export type UpdaterStatus =
  | 'idle'
  | 'checking'
  | 'up-to-date'
  | 'available'
  | 'downloading'
  | 'installing'
  | 'installed'
  | 'error';

interface CheckForUpdatesOptions {
  silent?: boolean;
  openModal?: boolean;
  openModalOnAvailable?: boolean;
}

interface UpdaterContextType {
  opened: boolean;
  currentVersion: string;
  status: UpdaterStatus;
  availableUpdate: UpdateInfo | null;
  errorMessage: string | null;
  downloadedBytes: number;
  totalBytes: number | null;
  progress: number;
  completedVersion: string | null;
  checkForUpdates: (options?: CheckForUpdatesOptions) => Promise<UpdateInfo | null>;
  openUpdater: () => Promise<void>;
  closeUpdater: () => void;
  installUpdate: () => Promise<void>;
}

const UpdaterContext = createContext<UpdaterContextType | undefined>(undefined);
let startupCheckTriggered = false;

function isElectronRuntime() {
  return typeof window !== 'undefined' && !!window.electronAPI;
}

export const UpdaterProvider = ({ children }: { children: React.ReactNode }) => {
  const [currentVersion, setCurrentVersion] = useState<string>('unknown');
  const [opened, setOpened] = useState(false);
  const [status, setStatus] = useState<UpdaterStatus>('idle');
  const [availableUpdate, setAvailableUpdate] = useState<UpdateInfo | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [downloadedBytes, setDownloadedBytes] = useState(0);
  const [totalBytes, setTotalBytes] = useState<number | null>(null);
  const [progress, setProgress] = useState(0);
  const [completedVersion, setCompletedVersion] = useState<string | null>(null);

  const pendingCheck = useRef<{
    resolve: (value: UpdateInfo | null) => void;
    silent: boolean;
    openModalOnAvailable: boolean;
  } | null>(null);
  const downloadingRef = useRef(false);

  const resetProgress = useCallback(() => {
    setDownloadedBytes(0);
    setTotalBytes(null);
    setProgress(0);
  }, []);

  // Fetch current version once from the main process.
  useEffect(() => {
    if (!isElectronRuntime()) return;
    window
      .electronAPI!.getVersion()
      .then(setCurrentVersion)
      .catch(() => setCurrentVersion('unknown'));
  }, []);

  // Subscribe to updater events from the main process.
  useEffect(() => {
    if (!isElectronRuntime()) return;
    const api = window.electronAPI!.updater;

    const offAvailable = api.onAvailable((info) => {
      setAvailableUpdate(info);
      if (downloadingRef.current) return;
      setStatus('available');
      const pending = pendingCheck.current;
      if (pending) {
        if (pending.openModalOnAvailable) setOpened(true);
        pending.resolve(info);
        pendingCheck.current = null;
      }
    });
    const offNotAvailable = api.onNotAvailable(() => {
      setAvailableUpdate(null);
      const pending = pendingCheck.current;
      const silent = pending?.silent ?? false;
      setStatus(silent ? 'idle' : 'up-to-date');
      if (pending) {
        pending.resolve(null);
        pendingCheck.current = null;
      }
    });
    const offProgress = api.onProgress((p) => {
      if (p.total && p.total > 0) setTotalBytes(p.total);
      setDownloadedBytes(p.transferred);
      setProgress(Math.min(100, Math.round(p.percent)));
    });
    const offDownloaded = api.onDownloaded((info) => {
      downloadingRef.current = false;
      setCompletedVersion(info.version);
      setStatus('installing');
      setProgress(100);
      // Give the user a moment to see the message; main will quit and install.
      window.setTimeout(() => {
        setStatus('installed');
        void window.electronAPI!.updater.quitAndInstall();
      }, 1500);
    });
    const offError = api.onError((message) => {
      const pending = pendingCheck.current;
      const silent = pending?.silent ?? false;
      downloadingRef.current = false;
      if (pending) {
        pending.resolve(null);
        pendingCheck.current = null;
      }
      if (silent) {
        setStatus('idle');
        setErrorMessage(null);
        return;
      }
      setStatus('error');
      setErrorMessage(message || 'Update failed.');
    });

    return () => {
      offAvailable();
      offNotAvailable();
      offProgress();
      offDownloaded();
      offError();
    };
  }, []);

  const checkForUpdates = useCallback(
    async ({
      silent = false,
      openModal = false,
      openModalOnAvailable = false,
    }: CheckForUpdatesOptions = {}) => {
      if (!isElectronRuntime()) {
        setStatus('idle');
        setErrorMessage(null);
        return null;
      }
      setAvailableUpdate(null);
      resetProgress();
      setCompletedVersion(null);
      setErrorMessage(null);
      if (openModal) setOpened(true);
      if (!silent) setStatus('checking');

      return new Promise<UpdateInfo | null>((resolve) => {
        pendingCheck.current = { resolve, silent, openModalOnAvailable };
        window.electronAPI!.updater.check().catch(() => {
          // Errors are delivered via the 'error' event listener.
        });
      });
    },
    [resetProgress]
  );

  const installUpdate = useCallback(async () => {
    if (!availableUpdate || !isElectronRuntime()) return;
    setOpened(true);
    setStatus('downloading');
    setErrorMessage(null);
    resetProgress();
    downloadingRef.current = true;
    const ok = await window.electronAPI!.updater.download();
    if (!ok) {
      downloadingRef.current = false;
    }
  }, [availableUpdate, resetProgress]);

  const closeUpdater = useCallback(() => {
    if (status === 'checking' || status === 'downloading' || status === 'installing') return;
    setOpened(false);
    if (status === 'available') setAvailableUpdate(null);
  }, [status]);

  const openUpdater = useCallback(async () => {
    await checkForUpdates({ silent: false, openModal: true });
  }, [checkForUpdates]);

  useEffect(() => {
    if (startupCheckTriggered) return;
    startupCheckTriggered = true;
    void checkForUpdates({ silent: true, openModalOnAvailable: true });
  }, [checkForUpdates]);

  const value = useMemo<UpdaterContextType>(
    () => ({
      opened,
      currentVersion,
      status,
      availableUpdate,
      errorMessage,
      downloadedBytes,
      totalBytes,
      progress,
      completedVersion,
      checkForUpdates,
      openUpdater,
      closeUpdater,
      installUpdate,
    }),
    [
      availableUpdate,
      checkForUpdates,
      closeUpdater,
      completedVersion,
      currentVersion,
      downloadedBytes,
      errorMessage,
      installUpdate,
      openUpdater,
      opened,
      progress,
      status,
      totalBytes,
    ]
  );

  return <UpdaterContext.Provider value={value}>{children}</UpdaterContext.Provider>;
};

export const useUpdater = () => {
  const context = useContext(UpdaterContext);
  if (!context) throw new Error('useUpdater must be used within UpdaterProvider');
  return context;
};

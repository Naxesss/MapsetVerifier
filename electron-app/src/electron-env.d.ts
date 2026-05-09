export interface UpdateInfo {
  version: string;
  date: string | null;
  body: string | null;
}

export interface UpdaterProgress {
  percent: number;
  transferred: number;
  total: number;
  bytesPerSecond: number;
}

export interface BackendStatus {
  running: boolean;
  port: number;
}

export type Unsubscribe = () => void;

export interface ElectronAPI {
  platform: NodeJS.Platform;
  getVersion(): Promise<string>;

  window: {
    minimize(): Promise<void>;
    maximize(): Promise<void>;
    toggleMaximize(): Promise<boolean>;
    close(): Promise<void>;
    isMaximized(): Promise<boolean>;
  };

  shell: {
    openPath(path: string): Promise<string>;
    openExternal(url: string): Promise<void>;
  };

  dialog: {
    openFolder(): Promise<string | null>;
  };

  settings: {
    exists(): Promise<boolean>;
    read(): Promise<string | null>;
    write(text: string): Promise<void>;
  };

  backend: {
    status(): Promise<BackendStatus>;
    start(): Promise<void>;
    onLog(cb: (line: string) => void): Unsubscribe;
  };

  updater: {
    check(): Promise<UpdateInfo | null>;
    download(): Promise<boolean>;
    quitAndInstall(): Promise<void>;
    onChecking(cb: () => void): Unsubscribe;
    onAvailable(cb: (info: UpdateInfo) => void): Unsubscribe;
    onNotAvailable(cb: (info: UpdateInfo | null) => void): Unsubscribe;
    onProgress(cb: (p: UpdaterProgress) => void): Unsubscribe;
    onDownloaded(cb: (info: UpdateInfo) => void): Unsubscribe;
    onError(cb: (message: string) => void): Unsubscribe;
  };
}

declare global {
  interface Window {
    electronAPI?: ElectronAPI;
  }
}

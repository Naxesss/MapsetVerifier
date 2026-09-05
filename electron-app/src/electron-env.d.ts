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

export interface UpdaterCheckOptions {
  allowPrerelease?: boolean;
}

export interface BackendStatus {
  running: boolean;
  port: number;
}

export interface BackendStartOptions {
  customChecksEnabled?: boolean;
}

export type Unsubscribe = () => void;

export type TimestampOpenTarget = 'current' | 'stable' | 'lazer' | 'custom';

export interface OpenOsuUrlOptions {
  url: string;
  target: TimestampOpenTarget;
  path?: string;
  stablePath?: string;
  lazerPath?: string;
  songsFolder?: string;
}

export interface OpenOsuUrlResult {
  ok: boolean;
  error?: string;
}

export interface ElectronAPI {
  platform: NodeJS.Platform;
  getVersion(): Promise<string>;

  app: {
    getAppFolderPath(): Promise<string>;
    getExternalsFolderPath(): Promise<string>;
    getSnapshotFolderPath(
      beatmapSetId: number | string,
      subfolder?: string | null
    ): Promise<string | null>;
  };

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
    openOsuUrl(options: OpenOsuUrlOptions): Promise<OpenOsuUrlResult>;
    detectOsuExecutable(options: {
      client: 'stable' | 'lazer';
      songsFolder?: string;
    }): Promise<string | null>;
  };

  dialog: {
    openFolder(): Promise<string | null>;
    openFile(): Promise<string | null>;
  };

  settings: {
    exists(): Promise<boolean>;
    read(): Promise<string | null>;
    write(text: string): Promise<void>;
  };

  backend: {
    status(): Promise<BackendStatus>;
    start(options?: BackendStartOptions): Promise<void>;
    onLog(cb: (line: string) => void): Unsubscribe;
  };

  updater: {
    check(options?: UpdaterCheckOptions): Promise<UpdateInfo | null>;
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

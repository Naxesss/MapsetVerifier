export type ApiDocumentationCheck = {
  id: number;
  description: string;
  category: string;
  subCategory: string;
  author: string;
  outcomes: Level[];
};

export type ApiDocumentationCheckDetails = {
  description: string;
  outcomes: ApiDocumentationCheckDetailsOutcome[];
};

export type ApiDocumentationCheckDetailsOutcome = {
  level: Level;
  description: string;
  cause?: string;
};

export type ApiBeatmapPage = {
  items: Beatmap[];
  page: number;
  pageSize: number;
  hasMore: boolean;
};

export type Beatmap = {
  folder: string;
  title: string;
  artist: string;
  creator: string;
  beatmapID: string;
  beatmapSetID: string;
};

export type ApiBeatmapSetCheckResult = {
  general: ApiCategoryCheckResult;
  difficulties: ApiCategoryCheckResult[];
  beatmapSetId?: number;
  checks: Record<number, ApiCheckDefinition>;
  title?: string;
  artist?: string;
  creator?: string;
};

export type ApiCategoryOverrideCheckResult = {
  categoryResult: ApiCategoryCheckResult;
  checks: Record<number, ApiCheckDefinition>;
};

export type ApiCategoryCheckResult = {
  category: string;
  beatmapId?: number;
  checkResults: ApiCheckResult[];
  mode?: Mode;
  difficultyLevel?: DifficultyLevel | null;
  starRating?: number | null;
};

export type ApiCheckDefinition = {
  id: number
  name: string
  difficulties: string[]
}

export type ApiCheckResult = {
  id: number;
  level: Level;
  message: string;
};

export type DifficultyLevel = 'Easy' | 'Normal' | 'Hard' | 'Insane' | 'Expert' | 'Ultra';
export type Mode = 'Standard' | 'Taiko' | 'Catch' | 'Mania';
export type Level = 'Info' | 'Check' | 'Error' | 'Minor' | 'Warning' | 'Problem';

// Snapshot types
export type DiffType = 'Added' | 'Removed' | 'Changed';

export type ApiSnapshotResult = {
  title: string | null;
  artist: string | null;
  creator: string | null;
  difficulties: ApiSnapshotDifficulty[];
  general: ApiSnapshotHistory | null;
  beatmapHistories: ApiSnapshotHistory[];
  errorMessage: string | null;
};

export type ApiSnapshotDifficulty = {
  name: string;
  isGeneral: boolean;
  starRating: number | null;
  mode: Mode | null;
};

export type ApiSnapshotHistory = {
  difficultyName: string;
  commits: ApiSnapshotCommit[];
};

export type ApiSnapshotCommit = {
  date: string;
  id: string;
  totalChanges: number;
  additions: number;
  removals: number;
  modifications: number;
  sections: ApiSnapshotSection[];
};

export type ApiSnapshotSection = {
  name: string;
  aggregatedDiffType: DiffType;
  additions: number;
  removals: number;
  modifications: number;
  diffs: ApiSnapshotDiff[];
};

export type ApiSnapshotDiff = {
  message: string;
  diffType: DiffType;
  oldValue: string | null;
  newValue: string | null;
  details: string[];
};

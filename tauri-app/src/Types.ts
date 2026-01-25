export type ApiDocumentationCheck = {
  id: number;
  description: string;
  category: string;
  modes: Mode[];
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

// Audio Analysis Types
export type AudioAnalysisResult = {
  success: boolean;
  errorMessage: string | null;
  audioFilePath: string;
  bitrateAnalysis: BitrateAnalysisResult | null;
  channelAnalysis: ChannelAnalysisResult | null;
  formatAnalysis: FormatAnalysisResult | null;
  dynamicRangeAnalysis: DynamicRangeResult | null;
  isCompliant: boolean;
  complianceIssues: string[];
};

export type BitrateAnalysisResult = {
  averageBitrate: number;
  isVbr: boolean;
  minBitrate: number | null;
  maxBitrate: number | null;
  bitrateOverTime: BitrateDataPoint[];
  isCompliant: boolean;
  complianceMessage: string;
  maxAllowedBitrate: number;
  minAllowedBitrate: number;
};

export type BitrateDataPoint = {
  timeMs: number;
  bitrate: number;
};

export type ChannelAnalysisResult = {
  channelCount: number;
  isMono: boolean;
  isStereo: boolean;
  leftChannelLevel: number;
  rightChannelLevel: number;
  balanceRatio: number;
  severity: ImbalanceSeverity;
  louderChannel: string;
  phaseCorrelation: number;
  stereoWidth: number;
  balanceOverTime: ChannelBalanceDataPoint[];
};

export type ImbalanceSeverity = 'None' | 'Minor' | 'Warning' | 'Severe';

export type ChannelBalanceDataPoint = {
  timeMs: number;
  leftLevel: number;
  rightLevel: number;
  balance: number;
};

export type FormatAnalysisResult = {
  format: string;
  rawFormat: string;
  sampleRate: number;
  bitDepth: number;
  codec: string;
  durationMs: number;
  durationFormatted: string;
  fileSizeBytes: number;
  fileSizeFormatted: string;
  channels: number;
  isCompliant: boolean;
  complianceIssues: string[];
  badgeType: string;
};

export type DynamicRangeResult = {
  loudnessRange: number;
  integratedLoudness: number;
  truePeak: number;
  peakLevel: number;
  rmsLevel: number;
  dynamicRange: number;
  crestFactor: number;
  compressionDetected: boolean;
  compressionSeverity: CompressionSeverity;
  clippingDetected: boolean;
  clippingCount: number;
  clippingMarkers: ClippingMarker[];
  loudnessOverTime: LoudnessDataPoint[];
};

export type CompressionSeverity = 'None' | 'Light' | 'Moderate' | 'Heavy';

export type ClippingMarker = {
  timeMs: number;
  durationMs: number;
  peakLevel: number;
  channel: string;
};

export type LoudnessDataPoint = {
  timeMs: number;
  momentaryLoudness: number;
  shortTermLoudness: number;
  peakLevel: number;
  rmsLevel: number;
};

export type SpectralAnalysisResult = {
  spectrogramData: SpectrogramFrame[];
  frequencyBins: number[];
  timePositions: number[];
  minDb: number;
  maxDb: number;
  fftSize: number;
  sampleRate: number;
  peakFrequencies: PeakFrequency[];
};

export type SpectrogramFrame = {
  timeMs: number;
  magnitudes: number[];
};

export type PeakFrequency = {
  frequencyHz: number;
  magnitudeDb: number;
  timeMs: number;
  noteName: string;
  centsDeviation: number;
};

export type FrequencyRange = {
  name: string;
  minHz: number;
  maxHz: number;
  color: string;
  averageEnergyDb: number;
};

export type FrequencyAnalysisResult = {
  fftData: FftDataPoint[];
  fftWindowSize: number;
  detectedNotes: DetectedNote[];
  harmonicAnalysis: HarmonicAnalysis;
  maskingResults: FrequencyMaskingResult[];
  dominantFrequency: number;
  fundamentalFrequency: number;
};

export type FftDataPoint = {
  frequencyHz: number;
  magnitudeDb: number;
};

export type DetectedNote = {
  frequencyHz: number;
  noteName: string;
  midiNote: number;
  centsDeviation: number;
  confidence: number;
};

export type HarmonicAnalysis = {
  harmonics: Harmonic[];
  fundamentalHz: number;
  harmonicToNoiseRatio: number;
  bassEnergy: number;
  midEnergy: number;
  highEnergy: number;
};

export type Harmonic = {
  harmonicNumber: number;
  frequencyHz: number;
  magnitudeDb: number;
};

export type FrequencyMaskingResult = {
  centerFrequencyHz: number;
  bandwidthHz: number;
  severity: number;
  description: string;
};

export type HitSoundBatchResult = {
  totalFiles: number;
  compliantFiles: number;
  nonCompliantFiles: number;
  results: HitSoundAnalysisResult[];
};

export type HitSoundAnalysisResult = {
  filePath: string;
  format: string;
  bitrate: number;
  durationMs: number;
  channels: number;
  sampleRate: number;
  isCompliant: boolean;
  issues: string[];
  channelBalanceRatio: number;
  hasImbalance: boolean;
};

// Metadata Analysis Types
export type MetadataAnalysisResult = {
  success: boolean;
  errorMessage: string | null;
  difficulties: DifficultyMetadata[];
  resources: ResourcesInfo;
  colourSettings: DifficultyColourSettings[];
};

export type DifficultyMetadata = {
  version: string;
  artist: string;
  artistUnicode: string;
  title: string;
  titleUnicode: string;
  creator: string;
  source: string;
  tags: string;
  beatmapId: number | null;
  beatmapSetId: number | null;
  mode: string;
  starRating: number | null;
};

export type ResourcesInfo = {
  hitSounds: HitSoundUsage[];
  backgrounds: BackgroundInfo[];
  videos: VideoInfo[];
  storyboard: StoryboardInfo;
  audioFile: AudioFileInfo | null;
  totalFolderSizeBytes: number;
  totalFolderSizeFormatted: string;
};

export type HitSoundUsage = {
  fileName: string;
  format: string;
  fileSizeBytes: number;
  fileSizeFormatted: string;
  durationMs: number;
  totalUsageCount: number;
  usagePerDifficulty: DifficultyHitSoundUsage[];
};

export type DifficultyHitSoundUsage = {
  version: string;
  usageCount: number;
  timestamps: string[];
};

export type BackgroundInfo = {
  fileName: string;
  fileSizeBytes: number;
  fileSizeFormatted: string;
  width: number;
  height: number;
  resolution: string;
  usedByDifficulties: string[];
};

export type VideoInfo = {
  fileName: string;
  fileSizeBytes: number;
  fileSizeFormatted: string;
  width: number;
  height: number;
  resolution: string;
  durationMs: number;
  durationFormatted: string;
  offsetMs: number;
  usedByDifficulties: string[];
};

export type StoryboardInfo = {
  hasOsb: boolean;
  osbFileName: string | null;
  osbIsUsed: boolean;
  difficultySpecificStoryboards: DifficultyStoryboardInfo[];
};

export type DifficultyStoryboardInfo = {
  version: string;
  hasStoryboard: boolean;
  spriteCount: number;
  animationCount: number;
  sampleCount: number;
};

export type AudioFileInfo = {
  fileName: string;
  fileSizeBytes: number;
  fileSizeFormatted: string;
  durationMs: number;
  durationFormatted: string;
  format: string;
  averageBitrate: number;
};

export type DifficultyColourSettings = {
  version: string;
  mode: string;
  isApplicable: boolean;
  comboColours: ComboColourInfo[];
  sliderBorder: ColourInfo | null;
  sliderTrack: ColourInfo | null;
};

export type ComboColourInfo = {
  index: number;
  r: number;
  g: number;
  b: number;
  hex: string;
  hspLuminosity: number;
  luminosityWarning: string;
};

export type ColourInfo = {
  r: number;
  g: number;
  b: number;
  hex: string;
  hspLuminosity: number;
  luminosityWarning: string;
};

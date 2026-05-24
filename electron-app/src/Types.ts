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

export type LazerLookupStatus =
  | 'unsupported_platform'
  | 'no_process'
  | 'ambiguous_client'
  | 'no_editor_title'
  | 'songs_folder_not_found'
  | 'metadata_detected'
  | 'folder_found';

export type ApiLazerLookupResult = {
  status: LazerLookupStatus;
  message: string | null;
  detectedMetadata: string | null;
  folderPath: string | null;
  lookupRoot: string | null;
  beatmap: Beatmap | null;
};

export type ApiStableLookupResult = ApiLazerLookupResult;

export type ApiBeatmapInfo = {
  title: string | null;
  artist: string | null;
  creator: string | null;
  beatmapSetId: number | null;
};

export type ApiBeatmapSetCheckResult = {
  general: ApiCategoryCheckResult;
  difficulties: ApiCategoryCheckResult[];
  checks: Record<number, ApiCheckDefinition>;
};

export type ApiBeatmapStructureDifficulty = {
  category: string;
  beatmapId?: number | null;
  mode: Mode;
  starRating?: number | null;
};

export type ApiBeatmapStructure = {
  difficulties: ApiBeatmapStructureDifficulty[];
};

export type CheckProgress = {
  completed: number;
  total: number;
  label: string | null;
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
  id: number;
  name: string;
  difficulties: string[];
};

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

// Beatmap Analysis Types
export type BeatmapAnalysisResult = {
  success: boolean;
  errorMessage: string | null;
  statistics: DifficultyStatistics[];
  generalSettings: DifficultyGeneralSettings[];
  difficultySettings: DifficultyDifficultySettings[];
};

export type DifficultyOverviewResult = {
  success: boolean;
  errorMessage: string | null;
  msPerPeak: number;
  difficulties: DifficultyOverviewDifficulty[];
};

export type DifficultySamplePoint = {
  timeMs: number;
  value: number;
};

export type DifficultyOverviewDifficulty = {
  label: string;
  version: string;
  mode: Mode;
  difficultyLevel: DifficultyLevel;
  starRating: number;
  starRatingSamples: DifficultySamplePoint[];
  sliderVelocitySamples: DifficultySamplePoint[];
  volumeSamples: DifficultySamplePoint[];
  skills: DifficultySkillData[];
};

export type DifficultySkillData = {
  skillName: string;
  strainSamples: DifficultySamplePoint[];
};

export type DifficultyChartDataPoint = {
  timeMs: number;
  timeSeconds: number;
  value: number;
};

export type DifficultyChartSeries = {
  skillName: string;
  label: string;
  mode: Mode;
  difficultyLevel: DifficultyLevel;
  starRating: number;
  points: DifficultyChartDataPoint[];
};

export type DifficultyStatistics = {
  version: string;
  mode: string;
  starRating: number | null;
  circleCount: number;
  sliderCount: number | null;
  spinnerCount: number | null;
  holdNoteCount: number | null;
  objectsPerColumn: number[] | null;
  columnCount: number;
  newComboCount: number;
  breakCount: number;
  uninheritedLineCount: number;
  inheritedLineCount: number;
  kiaiTimeMs: number;
  kiaiTimeFormatted: string;
  drainTimeMs: number;
  drainTimeFormatted: string;
  playTimeMs: number;
  playTimeFormatted: string;
};

export type DifficultyGeneralSettings = {
  version: string;
  mode: string;
  audioFileName: string;
  audioLeadIn: number;
  stackLeniency: string | null;
  hasCountdown: boolean;
  countdownInsufficientTime: boolean;
  countdownSpeed: string | null;
  countdownOffset: number | null;
  letterboxInBreaks: boolean;
  widescreenStoryboard: boolean;
  previewTime: number;
  previewTimeFormatted: string;
  useSkinSprites: string | null;
  skinPreference: string;
  epilepsyWarning: string | null;
};

export type DifficultyDifficultySettings = {
  version: string;
  mode: string;
  hpDrain: number;
  circleSize: string | null;
  overallDifficulty: number;
  approachRate: string | null;
  sliderTickRate: string | null;
  sliderVelocity: string | null;
};

export type ObjectsOverviewResult = {
  success: boolean;
  errorMessage: string | null;
  startTimeMs: number;
  endTimeMs: number;
  difficulties: ObjectsOverviewDifficulty[];
};

export type ObjectsOverviewDifficulty = {
  version: string;
  mode: string;
  starRating: number | null;
  objectCount: number;
  edgeCount: number;
  unsnappedCount: number;
  unsnappedPercentage: number;
  breakPeriods: ObjectsBreakPeriod[];
  timelineObjects: ObjectsTimelineObject[];
  timingSegments: ObjectsTimingSegment[];
  snappings: ObjectsSnappingBucket[];
  /** Populated by server analysis; omit if using an older API. */
  unsnappedEdgeTimesMs?: number[];
  /** Populated by server analysis; omit if using an older API. */
  timelineSamples?: ObjectsTimelineSample[];
  /** Populated by server analysis; omit if using an older API. */
  hitsoundGapPeriods?: ObjectsHitsoundGapPeriod[];
};

export type ObjectsBreakPeriod = {
  startTimeMs: number;
  endTimeMs: number;
};

export type ObjectsTimelineObject = {
  startTimeMs: number;
  endTimeMs: number;
  objectType: string;
  hasFinishHitSound: boolean;
  /** Bitfield: Normal=1, Whistle=2, Finish=4, Clap=8 */
  hitSoundFlags?: number;
  comboColourIndex: number | null;
  comboColourHex: string | null;
  edges: ObjectsTimelineEdge[];
};

export type ObjectsTimelineEdge = {
  timeMs: number;
  partName: string;
  /** Bitfield: Normal=1, Whistle=2, Finish=4, Clap=8 */
  hitSoundFlags?: number;
};

export type ObjectsTimelineSample = {
  timeMs: number;
  source: 'Edge' | 'Body' | 'Tick' | string;
  hitSound: string | null;
  sampleset: string;
  customIndex: number;
  partName: string | null;
  objectType: string | null;
};

export type ObjectsHitsoundGapPeriod = {
  startTimeMs: number;
  endTimeMs: number;
};

export type ObjectsTimingSegment = {
  startTimeMs: number;
  endTimeMs: number;
  offsetMs: number;
  msPerBeat: number;
  bpm: number;
  meter: number;
  /** Populated by server analysis; omit if using an older API. */
  sampleset?: string;
  /** Populated by server analysis; omit if using an older API. */
  customIndex?: number;
};

export type ObjectsSnappingBucket = {
  divisor: number;
  label: string;
  count: number;
  percentage: number;
  /** Edge timestamps for this snap column; omit if using an older API. */
  edgeTimesMs?: number[];
};

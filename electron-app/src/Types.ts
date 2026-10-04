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
  /** Ranking criteria statements this outcome enforces. */
  ruleIds?: string[];
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
  /** Relative lazer/stable image path from the API; may include a cache-bust `v` query. */
  backgroundPath?: string;
};

export type LazerLookupStatus =
  | 'unsupported_platform'
  | 'no_process'
  | 'ambiguous_client'
  | 'no_editor_title'
  | 'songs_folder_not_found'
  | 'lazer_data_dir_not_found'
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

export type ApiLazerMaterializeResult = {
  success: boolean;
  folderPath: string | null;
  errorMessage: string | null;
  /** Realm set id actually materialized; may differ after delete+redownload. */
  beatmapSetId?: string | null;
};

export type ApiBeatmapInfo = {
  title: string | null;
  artist: string | null;
  creator: string | null;
  beatmapId: number | null;
  beatmapSetId: number | null;
};

export type ApiBeatmapSetCheckResult = {
  general: ApiCategoryCheckResult;
  difficulties: ApiCategoryCheckResult[];
  checks: Record<number, ApiCheckDefinition>;
  checkRunDelta?: ApiCheckRunDelta | null;
  /** Populated only when the request opts in via `includeCheckTimings`. */
  checkTimings?: ApiCheckTimingReport | null;
};

export type ApiCheckTimingReport = {
  checks: ApiCheckTiming[];
  /** Actual wall-clock duration of the whole check run; checks run in parallel, so the sum of
   *  individual `elapsedMs` values will generally exceed this. */
  totalElapsedMs: number;
};

export type ApiCheckTiming = {
  checkName: string;
  /** Null for general/beatmapset-wide checks that aren't tied to one difficulty. */
  difficulty: string | null;
  elapsedMs: number;
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
  activeLabels: string[];
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
  /** Ranking criteria statements the issue's template enforces. */
  ruleIds?: string[];
};

export type ApiCheckRunDelta = {
  previousRunAt: string;
  currentRunAt: string;
  newIssues: ApiCheckDeltaIssue[];
  resolvedIssues: ApiCheckDeltaIssue[];
  worsenedIssues: ApiCheckDeltaIssue[];
  improvedIssues: ApiCheckDeltaIssue[];
  unchangedIssues: ApiCheckDeltaIssue[];
};

export type ApiCheckDeltaIssue = {
  category: string;
  id: number;
  checkName: string;
  level: Level;
  previousLevel?: Level | null;
  message: string;
};

export type DifficultyLevel = 'Easy' | 'Normal' | 'Hard' | 'Insane' | 'Expert' | 'Ultra';
export type Mode = 'Standard' | 'Taiko' | 'Catch' | 'Mania';
export type Level = 'Info' | 'Check' | 'Error' | 'Minor' | 'Warning' | 'Problem';

export type ApiPluginReport = {
  directoryPath: string;
  customChecksEnabled: boolean;
  loadedPlugins: ApiLoadedPlugin[];
  failedPlugins: ApiFailedPlugin[];
};

export type ApiLoadedPlugin = {
  fileName: string;
  filePath: string;
  assemblyName: string;
  version: string | null;
  authors: string[];
  checkCount: number;
  generalCheckCount: number;
  beatmapCheckCount: number;
  beatmapSetCheckCount: number;
  checkNames: string[];
};

export type ApiFailedPlugin = {
  fileName: string;
  filePath: string;
  message: string;
  details: string;
};

// Snapshot types
export type DiffType = 'Added' | 'Removed' | 'Changed';

/** What a change is about, in the words a mapper uses; the tracks of the change map. */
export type SnapshotChangeKind = 'Rhythm' | 'Placement' | 'Hitsound' | 'Timing';

export type ApiSnapshotObjectRef = {
  time: number;
  /** Circle, Slider, Spinner or Hold note. */
  type: string;
  /** The osu! timestamp including the object, e.g. "00:41:210 (1,2) - ". */
  stamp: string;
};

/** One structured difference between two versions of a difficulty. */
export type ApiSnapshotChange = {
  kind: SnapshotChangeKind;
  op: DiffType;
  time: number;
  endTime: number | null;
  /** What changed: Time, Position, Column, NewCombo, Hitsound, Sv, Bpm, Kiai, ... */
  field: string | null;
  before: string | null;
  after: string | null;
  magnitude: number | null;
  object: ApiSnapshotObjectRef | null;
  minor: boolean;
};

export type ApiSnapshotRollup = {
  kind: 'Offset';
  amount: number;
  absorbed: number;
};

export type ApiSnapshotSettingChange = {
  section: string;
  key: string;
  op: DiffType;
  before: string | null;
  after: string | null;
  added: string[] | null;
  removed: string[] | null;
  /** Above 0 when the same change happened in this many difficulties (shown once, in General). */
  appliesTo: number;
};

export type ApiSnapshotFileChange = {
  name: string;
  category: string;
  op: DiffType;
  sizeBefore: number | null;
  sizeAfter: number | null;
};

export type ApiSnapshotTimeRange = { start: number; end: number };

export type ApiSnapshotMark = {
  kind: SnapshotChangeKind;
  start: number;
  end: number;
  minor: boolean;
};

export type ApiSnapshotCounts = {
  added: number;
  removed: number;
  changed: number;
  total: number;
};

export type ApiSnapshotVisualObject = {
  time: number;
  endTime: number | null;
  type: string;
  x: number;
  y: number;
  column: number | null;
  hitSound: string;
  path: number[][] | null;
  /** Where a repeating slider turns around (not its head or tail). */
  edges: number[] | null;
  /** Catch: the times of this object's fruits that start a hyperdash (they glow red in the game). */
  hyperTimes: number[] | null;
  /** osu!: the number the object shows in the editor (1 at each new combo). */
  combo: number | null;
  /** When the slider's ticks are, following the tick rate (osu! and catch). */
  ticks: number[] | null;
  /** The volume it plays at: its own, or the timing line's it inherits. */
  volume: number | null;
  /** A slider's sounds (osu! and catch): head, reverses, tail, and the body's whistle. */
  sounds: ApiSnapshotSoundPart[] | null;
};

export type ApiSnapshotSoundPart = {
  time: number;
  kind: 'Head' | 'Repeat' | 'Tail' | 'Body';
  hitSound: string;
  /** Normal, Soft or Drum: where the sound comes from. */
  sampleset: string;
  /** Where the whistle, finish and clap come from. */
  addition: string;
};

/** A red line: where a beat grid starts and how long a beat is. */
export type ApiSnapshotTimingMark = {
  offset: number;
  beatLength: number;
  meter: number;
};

export type ApiSnapshotHunkVisual = {
  before: ApiSnapshotVisualObject[];
  after: ApiSnapshotVisualObject[];
  /** Too many objects to draw. */
  truncated: boolean;
  /** The beat grid under the objects, per side (it can differ after a retime). */
  beforeTiming: ApiSnapshotTimingMark[];
  afterTiming: ApiSnapshotTimingMark[];
};

/**
 * A few objects of one difficulty around a change, before and after: some leading up to it, the
 * changed ones, and some after, like the objects a player sees coming.
 */
export type ApiSnapshotWindow = {
  from: number;
  to: number;
  /** How many objects the window shows. */
  objects: number;
  hasEarlier: boolean;
  hasLater: boolean;
  visual: ApiSnapshotHunkVisual;
};

export type SnapshotHunkLabel =
  | 'Remapped'
  | 'Rhythm'
  | 'Placement'
  | 'Hitsounding'
  | 'Sv'
  | 'Mixed';

/** Changes close together in the song, shown as one row. */
export type ApiSnapshotHunk = {
  start: number;
  end: number;
  label: SnapshotHunkLabel;
  kinds: SnapshotChangeKind[];
  changes: ApiSnapshotChange[];
  counts: ApiSnapshotCounts;
};

export type SnapshotDifficultyStatus = 'Unchanged' | 'Changed' | 'Added' | 'Removed';

export type ApiSnapshotDifficultyComparison = {
  key: string;
  name: string;
  mode: Mode;
  beatmapId: number | null;
  status: SnapshotDifficultyStatus;
  starsBefore: number | null;
  starsAfter: number | null;
  objectsBefore: number;
  objectsAfter: number;
  counts: ApiSnapshotCounts;
  minorCount: number;
  hunks: ApiSnapshotHunk[];
  minor: ApiSnapshotChange[];
  settings: ApiSnapshotSettingChange[];
  rollups: ApiSnapshotRollup[];
  marks: ApiSnapshotMark[];
  kiai: ApiSnapshotTimeRange[];
  breaks: ApiSnapshotTimeRange[];
  lengthMs: number;
};

export type ApiSnapshotGeneralComparison = {
  rollups: ApiSnapshotRollup[];
  settings: ApiSnapshotSettingChange[];
  files: ApiSnapshotFileChange[];
  counts: ApiSnapshotCounts;
};

export type ApiSnapshotInfo = {
  id: string;
  time: string;
  trigger: string;
  pin: string | null;
};

export type ApiSnapshotComparison = {
  base: ApiSnapshotInfo;
  target: ApiSnapshotInfo;
  general: ApiSnapshotGeneralComparison;
  difficulties: ApiSnapshotDifficultyComparison[];
};

export type ApiSnapshotChecks = {
  problems: number;
  warnings: number;
  minor: number;
};

export type ApiSnapshotHistoryEntry = {
  id: string;
  time: string;
  /** checkRun, pageOpen, manual or import. */
  trigger: string;
  pin: string | null;
  checks: ApiSnapshotChecks | null;
  previousChecks: ApiSnapshotChecks | null;
  /** Keys of the difficulties that changed since the snapshot before. */
  changedDifficulties: string[];
  counts: ApiSnapshotCounts;
  fileChanges: number;
  generalChanges: number;
  isFirst: boolean;
};

export type ApiSnapshotHistoryDifficulty = {
  key: string;
  name: string;
  mode: Mode;
  beatmapId: number | null;
  starRating: number | null;
};

export type ApiSnapshotHistory = {
  setKey: string;
  difficulties: ApiSnapshotHistoryDifficulty[];
  /** Newest first. */
  entries: ApiSnapshotHistoryEntry[];
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
};

export type BitrateAnalysisResult = {
  averageBitrate: number;
  isVbr: boolean;
  minBitrate: number | null;
  maxBitrate: number | null;
  bitrateOverTime: BitrateDataPoint[];
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
  codec: string | null;
  frameRate: number | null;
  hasAudioTrack: boolean;
  bitrateKbps: number;
  usedByDifficulties: string[];
};

export type VideoAnalysisResult = {
  success: boolean;
  errorMessage: string | null;
  videos: VideoAnalysisEntry[];
};

export type VideoAnalysisEntry = {
  fileName: string;
  exists: boolean;
  fileSizeBytes: number;
  fileSizeFormatted: string;
  container: string;
  videoCodec: string | null;
  videoCodecProfile: string | null;
  width: number;
  height: number;
  resolution: string;
  frameRate: number | null;
  isVariableFrameRate: boolean;
  videoBitrateKbps: number | null;
  overallBitrateKbps: number;
  hasAudioTrack: boolean;
  audioCodec: string | null;
  audioChannels: number;
  audioSampleRate: number;
  durationMs: number;
  durationFormatted: string;
  offsetMs: number;
  usedByDifficulties: string[];
  canPreview: boolean;
  warnings: string[];
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
  /** This skill's own aggregate difficulty contribution for the map (pre-Star-Rating-curve) -
   *  how much it actually matters to the map's real difficulty. */
  difficultyValue: number;
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
  /** Populated by server analysis; omit if using an older API. */
  objectTypes?: ObjectsTypeBucket[];
  /** Mania only; null or omitted for other modes. */
  columnUsage?: ObjectsColumnUsage[] | null;
};

export type ObjectsColumnUsage = {
  column: number;
  noteCount: number;
  holdNoteCount: number;
  totalCount: number;
  percentage: number;
};

export type ObjectsTypeBucket = {
  label: string;
  count: number;
  percentage: number;
  /** Populated by server analysis; omit if using an older API. */
  entries?: ObjectsTypeEntry[];
};

export type ObjectsTypeEntry = {
  timeMs: number;
  detail: string;
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
  /** Slider body hitsound bitfield; 0 for non-sliders. */
  sliderBodyHitSoundFlags?: number;
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
  isBaseHitNormal?: boolean;
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

export type RcKind = 'Rule' | 'Guideline' | 'Allowance';
export type RcAutomation = 'Unknown' | 'Automatable' | 'Partial' | 'Manual';
export type RcCoverage =
  | 'Covered'
  | 'Partial'
  | 'Outdated'
  | 'Uncovered'
  | 'Manual'
  | 'Informational';

/** How a statement read when its linked checks were last reviewed, if it changed since. */
export type ApiRcReview = {
  commit: string;
  kind: RcKind;
};

export type ApiRcSource = {
  repository: string;
  commit: string;
  commitDate?: string | null;
};

export type ApiRcPageSummary = {
  key: string;
  title: string;
  wikiUrl: string;
  modes: Mode[];
  hasStatements: boolean;
  coverage: Record<RcCoverage, number>;
};

export type ApiRcOverview = {
  source: ApiRcSource;
  pages: ApiRcPageSummary[];
};

export type ApiRcCheckLink = {
  checkId: number;
  checkName: string;
  templateKey: string;
  level: Level;
};

export type ApiRcStatement = {
  id: string;
  page: string;
  pageTitle: string;
  kind: RcKind;
  lead: string;
  path: string[];
  wikiUrl: string;
  startLine: number;
  endLine: number;
  parentId?: string | null;
  parentLead?: string | null;
  /** Only opens a sentence its nested statements finish, e.g. "The audio file of a beatmap must...". */
  intro: boolean;
  difficulties: DifficultyLevel[];
  automation: RcAutomation;
  notes?: string | null;
  retired: boolean;
  lastReview?: ApiRcReview | null;
  coverage: RcCoverage;
  links: ApiRcCheckLink[];
};

export type ApiRcPage = {
  key: string;
  title: string;
  wikiUrl: string;
  markdown: string;
  statements: ApiRcStatement[];
};

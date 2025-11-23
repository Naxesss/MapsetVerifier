export type ApiDocumentationCheck = {
  id: number
  description: string
  category: string
  subCategory: string
  author: string
  outcomes: Level[]
}

export type ApiDocumentationCheckDetails = {
  description: string
  outcomes: ApiDocumentationCheckDetailsOutcome[]
}

export type ApiDocumentationCheckDetailsOutcome = {
  level: Level
  description: string
  cause?: string
}

export type ApiBeatmapPage = {
  items: Beatmap[]
  page: number
  pageSize: number
  hasMore: boolean
}

export type Beatmap = {
  folder: string
  title: string
  artist: string
  creator: string
  beatmapID: string
  beatmapSetID: string
}

export type ApiBeatmapSetCheckResult = {
  general: ApiCategoryCheckResult
  difficulties: ApiCategoryCheckResult[]
  beatmapSetId?: number
  checks: Record<number, { id: number; name: string; difficulties: string[] }>
  title?: string
  artist?: string
  creator?: string
}

export type ApiCategoryCheckResult = {
  category: string
  beatmapId?: number
  checkResults: ApiCheckResult[]
  mode?: Mode
  difficultyLevel?: string | null
}

export type ApiCheckResult = {
  id: number
  level: Level
  message: string
}

export type Mode = 'Standard' | 'Taiko' | 'Catch' | 'Mania'
export type Level = 'Info' | 'Check' | 'Error' | 'Minor' | 'Warning' | 'Problem';
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
  backgroundPath: string
}

export type Mode = 'Standard' | 'Taiko' | 'Catch' | 'Mania'
export type Level = 'Info' | 'Check' | 'Error' | 'Minor' | 'Warning' | 'Problem';
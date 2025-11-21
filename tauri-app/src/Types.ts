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

export type Mode = 'Standard' | 'Taiko' | 'Catch' | 'Mania'
export type Level = 'Info' | 'Check' | 'Error' | 'Minor' | 'Warning' | 'Problem';
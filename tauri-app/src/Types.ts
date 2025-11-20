export type ApiDocumentationCheck = {
  id: number
  description: string
  category: string
  subCategory: string
  author: string
  outcomes: Level[]
}

export type Mode = 'Standard' | 'Taiko' | 'Catch' | 'Mania'
export type Level = 'Info' | 'Check' | 'Error' | 'Minor' | 'Warning' | 'Problem';
import type { DateTime } from 'luxon'

export type DishRole = 'Main' | 'Side' | 'Dessert' | 'Other'
export type EffortLevel = 'Quick' | 'Medium' | 'Elaborate'
export type SeasonAffinity = 'Summer' | 'Winter' | 'Spring' | 'Autumn' | 'AllYear'

export interface DishMetadata {
  roles: DishRole[]
  rolesConfirmed: boolean
  effortLevel: EffortLevel | null
  effortLevelConfirmed: boolean
  seasonAffinity: SeasonAffinity | null
  seasonAffinityConfirmed: boolean
  cuisine: string | null
  cuisineConfirmed: boolean
}

export interface Tag {
  value: string
  color: string
}

export interface MenuItem {
  dishId: string
  dishName: string
}

export interface Dinner {
  description: string
  date: DateTime
  menu: MenuItem[]
  tags: Tag[]
  isPlanned: boolean
  isOptedOut: boolean
  optOutReason: string | null
  isResolved: boolean
}

export interface DinnerDate {
  date: string
  daysSinceLast: number
}

export interface DishStats {
  dishId: string
  lastUsed: DateTime | undefined
  timesUsed: number
}

export interface Rating {
  familyMemberId: string
  rating: number
}

export interface Dish {
  name: string
  id: string
  url: string
  tags: Tag[]
  notes: string
  rating: number
  dates: DinnerDate[]
  dishStats: DishStats
  ratings: Rating[]
  isArchived: boolean
  roles?: DishRole[]
  rolesConfirmed?: boolean
  effortLevel?: EffortLevel | null
  effortLevelConfirmed?: boolean
  seasonAffinity?: SeasonAffinity | null
  seasonAffinityConfirmed?: boolean
  cuisine?: string | null
  cuisineConfirmed?: boolean
}

export interface DishSelector {
  name: string
  id: string
  url: string
  tags: Tag[]
  rating: number
}

export interface FamilyMember {
  id: string
  name: string
  isOwner: boolean
  hasAutonomy: boolean
}

export interface Family {
  id: string
  name: string
  familyMembers: FamilyMember[]
}

export interface FamilySelect {
  id: string
  name: string
}

export interface DishSuggestion {
  dishId: string
  dishName: string
  rating: number
  daysSinceLast: number
}

export interface DaySuggestion {
  date: string
  suggestion: DishSuggestion | null
}

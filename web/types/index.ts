import type { DateTime } from 'luxon'

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

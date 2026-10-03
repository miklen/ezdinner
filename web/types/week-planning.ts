import { z } from 'zod'
import type { Dish, DishRole, EffortLevel, SeasonAffinity, DishStats } from '~/types'
import type { RecommendedDish, UndoRequest } from './dish-recommendations'

export const planningDinnerSchema = z.object({ date: z.iso.date(), menu: z.array(z.object({ dishId: z.string(), dishName: z.string().optional() })), optOutReason: z.string().nullable().optional() })
export type PlanningDinner = z.infer<typeof planningDinnerSchema>
export interface PlanningDay { date: string; menu: { dishId: string; dishName: string }[]; optOutReason: string | null }
export interface PlanningDish { dish: Dish; stats?: DishStats; wishVotes?: number; recommendation?: RecommendedDish }
export interface ExplorationPreferences { search: string; role: DishRole | 'All'; effort: EffortLevel | 'All'; season: SeasonAffinity | 'All'; cuisine: string; wishesOnly: boolean; sort: 'name' | 'rating' | 'usage' | 'lastUsed' }
export interface UndoFeedback { familyId: string; date: string; dishId: string; kind: 'added' | 'removed'; inverse: UndoRequest }

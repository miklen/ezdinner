import { z } from 'zod'

const date = z.iso.date()
const role = z.enum(['Main', 'Side', 'Dessert', 'Other'])
export const recommendationRequestSchema = z.object({
  selectedMonday: date,
  targetDate: date.nullable(),
  mode: z.enum(['automatic', 'request', 'more']),
  locale: z.enum(['en', 'da']),
  turns: z.array(z.string().min(1)).max(12),
  constraints: z.array(z.string().min(1)).max(12),
  excludedDishIds: z.array(z.uuid()).max(1000),
  role: role.nullable(),
  nameFilter: z.string().trim().max(100).optional(),
})
export type RecommendationRequest = z.infer<typeof recommendationRequestSchema>

export const recommendedDishSchema = z.object({
  dishId: z.uuid(), name: z.string(), rating: z.number().nullable(), roles: z.array(role),
  isUnclassified: z.boolean(), isWished: z.boolean(), wishVotes: z.number().int().nonnegative(),
  lastServed: date.nullable(), servingCount: z.number().int().nonnegative(), neverUsed: z.boolean(),
  typicalSpacingDays: z.number().nullable(), assignedDates: z.array(date),
  historicalReasons: z.array(z.object({ kind: z.enum(['Rating', 'Wish', 'NeverUsed', 'ObservedRotation', 'ForgottenFavourite', 'FormerRegular', 'RecentlyServed']), value: z.number().nullable() })),
  explanations: z.array(z.object({ text: z.string(), kind: z.enum(['Fact', 'Inference']), sourceReferences: z.array(z.string()) })),
  limitations: z.array(z.string()),
})
export type RecommendedDish = z.infer<typeof recommendedDishSchema>
const resultFields = {
  activeConstraints: z.array(z.string()), contextSummary: z.string(), message: z.string().nullable(),
}
export const recommendationResultSchema = z.discriminatedUnion('outcome', [
  z.object({ outcome: z.literal('Matches'), dishes: z.array(recommendedDishSchema).min(1), ...resultFields }),
  z.object({ outcome: z.literal('NoMatch'), dishes: z.array(recommendedDishSchema).max(0), ...resultFields }),
  z.object({ outcome: z.literal('Exhausted'), dishes: z.array(recommendedDishSchema).max(0), ...resultFields }),
  z.object({ outcome: z.literal('NeedsClarification'), dishes: z.array(recommendedDishSchema).max(0), ...resultFields }),
])
export type RecommendationResult = z.infer<typeof recommendationResultSchema>
export type RecommendationState =
  | { status: 'idle'; result: RecommendationResult | null }
  | { status: 'loading'; result: RecommendationResult | null }
  | { status: 'ready'; result: RecommendationResult }
  | { status: 'failed'; result: RecommendationResult | null; error: string }

export const dinnerStateSchema = z.object({ dishIds: z.array(z.uuid()).max(100), optOutReason: z.string().max(500).nullable(),
  changeId: z.uuid().optional(), dishChangeId: z.uuid().optional(), optOutChangeId: z.uuid().optional() })
export const menuChangeRequestSchema = z.object({ dishId: z.uuid(), expectedState: dinnerStateSchema })
export type MenuChangeRequest = z.infer<typeof menuChangeRequestSchema>
export const menuChangeResultSchema = z.discriminatedUnion('outcome', [
  z.object({ outcome: z.literal('Changed'), before: dinnerStateSchema, after: dinnerStateSchema }),
  z.object({ outcome: z.literal('NoOp') }),
  z.object({ outcome: z.literal('Conflict') }),
])
export const undoRequestSchema = z.object({ dishId: z.uuid(), before: dinnerStateSchema, after: dinnerStateSchema })
export type UndoRequest = z.infer<typeof undoRequestSchema>
export const undoResultSchema = z.discriminatedUnion('outcome', [
  z.object({ outcome: z.literal('Restored') }), z.object({ outcome: z.literal('Conflict') }),
])

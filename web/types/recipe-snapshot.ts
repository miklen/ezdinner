import { z } from 'zod'

export const recipeSnapshotSchema = z.object({
  content: z.string().min(1).max(32000),
  sourceUrl: z.url().refine(url => ['http:', 'https:'].includes(new URL(url).protocol)),
  capturedAt: z.iso.datetime({ offset: true }),
  sourceHash: z.string().regex(/^[a-fA-F0-9]{64}$/),
}).strict()

export const recipePreviewSchema = z.object({
  candidate: recipeSnapshotSchema,
  warnings: z.array(z.string()),
  sourceChanged: z.boolean(),
}).strict()

export type RecipeSnapshot = z.infer<typeof recipeSnapshotSchema>
export type RecipeCandidate = RecipeSnapshot
export type RecipePreview = z.infer<typeof recipePreviewSchema>

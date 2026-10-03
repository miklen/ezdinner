import { describe, expect, it, vi } from 'vitest'
import { DishesRepository } from '~/repository/dishes-repository'

const candidate = { content: '- Salt\n\n1. Boil', sourceUrl: 'https://example.com/soup', capturedAt: '2026-10-03T10:00:00+00:00', sourceHash: 'a'.repeat(64) }
const preview = { candidate, warnings: ['RECIPE_LLM_EXTRACTED'], sourceChanged: true }

describe('recipe snapshot repository', () => {
  it('posts a preview request and maps its validated response', async () => {
    const fetch = vi.fn().mockResolvedValue(preview)
    const signal = new AbortController().signal
    expect(await new DishesRepository(fetch).previewRecipe('family', 'dish', signal)).toEqual(preview)
    expect(fetch).toHaveBeenCalledWith('/api/dishes/dish/recipe/preview/family/family', { method: 'POST', signal })
  })

  it('puts only the confirmed candidate without user notes', async () => {
    const fetch = vi.fn().mockResolvedValue(undefined)
    await new DishesRepository(fetch).confirmRecipe('family', 'dish', candidate)
    expect(fetch).toHaveBeenCalledWith('/api/dishes/dish/recipe/family/family', { method: 'PUT', body: candidate })
  })

  it('deletes the snapshot at its dedicated endpoint', async () => {
    const fetch = vi.fn().mockResolvedValue(undefined)
    await new DishesRepository(fetch).removeRecipe('family', 'dish')
    expect(fetch).toHaveBeenCalledWith('/api/dishes/dish/recipe/family/family', { method: 'DELETE' })
  })

  it('propagates API failures', async () => {
    const error = new Error('network failed')
    const fetch = vi.fn().mockRejectedValue(error)
    const repository = new DishesRepository(fetch)
    await expect(repository.previewRecipe('family', 'dish')).rejects.toBe(error)
    await expect(repository.confirmRecipe('family', 'dish', candidate)).rejects.toBe(error)
    await expect(repository.removeRecipe('family', 'dish')).rejects.toBe(error)
  })

  it('rejects malformed previews', async () => {
    const fetch = vi.fn().mockResolvedValue({ candidate: { ...candidate, sourceHash: 'invalid' }, warnings: [], sourceChanged: true })
    await expect(new DishesRepository(fetch).previewRecipe('family', 'dish')).rejects.toThrow()
  })

  it('reads optional snapshot data while leaving legacy dishes compatible', async () => {
    const fetch = vi.fn().mockResolvedValueOnce({ notes: 'My notes', url: candidate.sourceUrl, recipeSnapshot: candidate }).mockResolvedValueOnce({ notes: 'Legacy', url: '' })
    const repository = new DishesRepository(fetch)
    expect((await repository.getFull('dish', 'family')).recipeSnapshot).toEqual(candidate)
    const legacy = await repository.getFull('legacy', 'family')
    expect(legacy.recipeSnapshot).toBeUndefined()
    expect(legacy.notes).toBe('Legacy')
  })
})

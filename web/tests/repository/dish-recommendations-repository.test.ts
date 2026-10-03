import { describe, expect, it, vi } from 'vitest'
import { DishRecommendationsRepository } from '~/repository/dish-recommendations-repository'
import type { RecommendationRequest } from '~/types/dish-recommendations'

const dishId = '00000000-0000-4000-8000-000000000001'
const request: RecommendationRequest = { selectedMonday: '2026-10-05', targetDate: null, mode: 'automatic', locale: 'en', turns: [], constraints: [], excludedDishIds: [], role: 'Main' }
const empty = { outcome: 'NoMatch', dishes: [], activeConstraints: [], contextSummary: '', message: null }

describe('dish recommendations repository', () => {
  it('preserves undo provenance through mutation responses and undo requests', async () => {
    const nil = '00000000-0000-0000-0000-000000000000'
    const before = { dishIds: [], optOutReason: null, changeId: nil, dishChangeId: nil, optOutChangeId: nil }
    const after = { dishIds: [dishId], optOutReason: null, changeId: dishId, dishChangeId: dishId, optOutChangeId: nil }
    const fetch = vi.fn().mockResolvedValueOnce({ outcome: 'Changed', before, after }).mockResolvedValueOnce({ outcome: 'Restored' })
    const repository = new DishRecommendationsRepository(fetch)
    const changed = await repository.changeMenu('family', '2026-10-05', 'added', { dishId, expectedState: { dishIds: [], optOutReason: null } })
    if (changed.outcome !== 'Changed') throw new Error('Expected successful assignment')
    await repository.undo('family', '2026-10-05', { dishId, before: changed.before, after: changed.after })
    expect(fetch.mock.calls[1][1].body).toEqual({ dishId, before, after })
  })
  it.each(['added', 'removed'])('opts into server-confirmed %s mutations', async kind => {
    const fetch = vi.fn().mockResolvedValue({ outcome: 'NoOp' })
    const expectedState = { dishIds: [], optOutReason: null }
    const action = kind === 'added' ? 'added' : 'removed'
    expect(await new DishRecommendationsRepository(fetch).changeMenu('family', '2026-10-05', action, { dishId, expectedState })).toEqual({ outcome: 'NoOp' })
    expect(fetch).toHaveBeenCalledWith(action === 'added' ? '/api/dinners/menuitem' : '/api/dinners/menuitem/remove', {
      method: 'PUT', headers: { 'X-EzDinner-Mutation': 'conditional' }, body: { familyId: 'family', date: '2026-10-05', dishId, expectedState }, ignoreResponseError: true,
    })
  })

  it('rejects changed mutations without canonical states', async () => {
    await expect(new DishRecommendationsRepository(vi.fn().mockResolvedValue({ outcome: 'Changed' }))
      .changeMenu('family', '2026-10-05', 'added', { dishId, expectedState: { dishIds: [], optOutReason: null } })).rejects.toThrow()
  })

  const modes: RecommendationRequest['mode'][] = ['automatic', 'request', 'more']
  it.each(modes)('posts %s intent with cancellation', async (mode) => {
    const fetch = vi.fn().mockResolvedValue(empty)
    const signal = new AbortController().signal
    const intent = { ...request, mode, turns: mode === 'request' ? ['Potato pairing'] : [] }
    expect(await new DishRecommendationsRepository(fetch).recommend('family', intent, signal)).toEqual(empty)
    expect(fetch).toHaveBeenCalledWith('/api/families/family/dish-recommendations', { method: 'POST', body: intent, signal })
  })

  it.each(['NoMatch', 'Exhausted', 'NeedsClarification'])('preserves the %s outcome', async (outcome) => {
    const result = { ...empty, outcome }
    expect(await new DishRecommendationsRepository(vi.fn().mockResolvedValue(result)).recommend('family', request)).toEqual(result)
  })

  it('rejects malformed success responses', async () => {
    await expect(new DishRecommendationsRepository(vi.fn().mockResolvedValue({ ...empty, outcome: 'Matches' })).recommend('family', request)).rejects.toThrow()
  })

  it('propagates provider failure without converting it to no-match', async () => {
    const failure = new Error('PROVIDER_UNAVAILABLE')
    await expect(new DishRecommendationsRepository(vi.fn().mockRejectedValue(failure)).recommend('family', request)).rejects.toBe(failure)
  })

  it.each(['Restored', 'Conflict'])('validates conditional undo %s', async (outcome) => {
    const fetch = vi.fn().mockResolvedValue({ outcome })
    const inverse = { dishId, before: { dishIds: [], optOutReason: null }, after: { dishIds: [dishId], optOutReason: null } }
    expect(await new DishRecommendationsRepository(fetch).undo('family', '2026-10-05', inverse)).toEqual({ outcome })
    expect(fetch).toHaveBeenCalledWith('/api/families/family/dinners/2026-10-05/undo-menu-change', { method: 'POST', body: inverse, ignoreResponseError: true })
  })
})

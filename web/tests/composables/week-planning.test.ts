import { afterEach, describe, expect, it, vi } from 'vitest'
import { effectScope, nextTick, shallowRef } from 'vue'
import type { EffectScope } from 'vue'
import { flushPromises } from '@vue/test-utils'
import { DateTime } from 'luxon'
import { useWeekPlanning, planningWindow } from '~/composables/useWeekPlanning'
import { useDishRecommendations } from '~/composables/useDishRecommendations'
import { useDishExploration } from '~/composables/useDishExploration'
import { DinnerRepository } from '~/repository/dinner-repository'
import { DishRecommendationsRepository } from '~/repository/dish-recommendations-repository'
import type { Dish } from '~/types'
import type { PlanningDish } from '~/types/week-planning'
import { menuChangeRequestSchema } from '~/types/dish-recommendations'

const scopes: EffectScope[] = []
afterEach(() => { scopes.splice(0).forEach(scope => scope.stop()) })
function scoped<Value>(create: () => Value): Value {
  const scope = effectScope()
  scopes.push(scope)
  const result = scope.run(create)
  if (!result) throw new Error('Scope did not run')
  return result
}
const dishId = '00000000-0000-4000-8000-000000000001'
const otherId = '00000000-0000-4000-8000-000000000002'
const noMatch = { outcome: 'NoMatch', dishes: [], activeConstraints: [], contextSummary: '', message: null }

function dinnerWorkspace(fetch = vi.fn().mockResolvedValue([]), writes = vi.fn().mockImplementation((path: string, options: { body: unknown }) => {
  const { dishId: changedDish, expectedState: before } = menuChangeRequestSchema.parse(options.body)
  const adding = path.endsWith('/menuitem')
  return { outcome: 'Changed', before, after: { dishIds: adding ? [...before.dishIds, changedDish] : before.dishIds.filter(id => id !== changedDish), optOutReason: null } }
})) {
  const family = shallowRef('family')
  const account = shallowRef('account')
  const recommendations = new DishRecommendationsRepository(writes)
  const planning = scoped(() => useWeekPlanning({ family, account, dishes: shallowRef([]), unavailableDishLabel: shallowRef('Dish unavailable'), dinners: new DinnerRepository(fetch), recommendations, initialMonday: '2026-10-12' }))
  return { planning, family, account, fetch, writes }
}

describe('locally owned planning window', () => {
  it.each(['added', 'removed'])('never offers undo for a server %s no-op', async kind => {
    const action = kind === 'added' ? 'added' : 'removed'
    const fetch = vi.fn().mockResolvedValue(action === 'removed' ? [{ date: '2026-10-12', menu: [{ dishId }], optOutReason: null }] : [])
    const { planning } = dinnerWorkspace(fetch, vi.fn().mockResolvedValue({ outcome: 'NoOp' }))
    await flushPromises()
    await planning.mutate(action, dishId, '2026-10-12')
    expect(planning.feedback.value).toBeNull()
    expect(planning.notice.value).toBe(action === 'added' ? 'duplicate' : 'unchanged')
  })

  it('refreshes a conflicting server change without manufacturing undo feedback', async () => {
    const fetch = vi.fn().mockResolvedValueOnce([]).mockResolvedValue([{ date: '2026-10-12', menu: [{ dishId: otherId }], optOutReason: null }])
    const { planning } = dinnerWorkspace(fetch, vi.fn().mockResolvedValue({ outcome: 'Conflict' }))
    await flushPromises()
    await planning.mutate('added', dishId, '2026-10-12')
    expect(planning.feedback.value).toBeNull()
    expect(planning.notice.value).toBe('conflict')
    expect(planning.days.value[2].menu.map(item => item.dishId)).toEqual([otherId])
  })
  it('blocks writes until the current window has loaded, including after navigation', async () => {
    let resolveRead: (value: unknown) => void = () => { throw new Error('No pending read') }
    const fetch = vi.fn().mockImplementation(() => new Promise(resolve => { resolveRead = resolve }))
    const { planning, writes } = dinnerWorkspace(fetch)
    await planning.mutate('added', dishId, '2026-10-12')
    expect(fetch).toHaveBeenCalledTimes(1)
    expect(writes).not.toHaveBeenCalled()
    resolveRead([])
    await flushPromises()
    expect(planning.canMutate.value).toBe(true)
    planning.navigate(1)
    await planning.mutate('added', dishId, '2026-10-19')
    expect(writes).not.toHaveBeenCalled()
    resolveRead([])
    await flushPromises()
  })

  it('blocks writes after a failed dinner read instead of treating an unknown day as empty', async () => {
    const { planning, fetch } = dinnerWorkspace(vi.fn().mockRejectedValue(new Error('Unavailable')))
    await flushPromises()
    await planning.mutate('added', dishId, '2026-10-12')
    expect(fetch).toHaveBeenCalledTimes(1)
    expect(planning.error.value).toBe('readFailed')
    expect(planning.feedback.value).toBeNull()
  })

  it('covers exactly both weekends around Monday', () => {
    expect(planningWindow('2026-10-12')).toEqual(['2026-10-10', '2026-10-11', '2026-10-12', '2026-10-13', '2026-10-14', '2026-10-15', '2026-10-16', '2026-10-17', '2026-10-18'])
  })

  it('navigates historical windows and clears only out-of-window targets', async () => {
    const { planning } = dinnerWorkspace()
    await flushPromises()
    planning.selectedDate.value = '2026-10-17'
    planning.navigate(1)
    expect(planning.monday.value).toBe('2026-10-19')
    expect(planning.selectedDate.value).toBe('2026-10-17')
    planning.navigate(-2)
    expect(planning.monday.value).toBe('2026-10-05')
    expect(planning.selectedDate.value).toBeNull()
    expect(planning.days.value.map(day => day.date)).toEqual(planning.dates.value)
  })

  it('uses existing additive persistence for occupied days and avoids duplicate undo', async () => {
    const fetch = vi.fn().mockResolvedValue([{ date: '2026-10-12', menu: [{ dishId: otherId }], optOutReason: null }])
    const { planning, writes } = dinnerWorkspace(fetch)
    await flushPromises()
    await planning.mutate('added', otherId, '2026-10-12')
    expect(planning.feedback.value).toBeNull()
    expect(planning.notice.value).toBe('duplicate')
    await planning.mutate('added', dishId, '2026-10-12')
    expect(writes).toHaveBeenCalledWith('/api/dinners/menuitem', expect.objectContaining({ method: 'PUT', body: { date: '2026-10-12', dishId, familyId: 'family', expectedState: { dishIds: [otherId], optOutReason: null } } }))
    expect(planning.feedback.value?.inverse.before.dishIds).toEqual([otherId])
    expect(planning.feedback.value?.inverse.after.dishIds).toEqual([otherId, dishId])
  })

  it('requires explicit confirmation before clearing opt-outs', async () => {
    const { planning, fetch } = dinnerWorkspace(vi.fn().mockResolvedValue([{ date: '2026-10-12', menu: [], optOutReason: 'Eating out' }]))
    await flushPromises()
    await expect(planning.mutate('added', dishId, '2026-10-12')).rejects.toThrow('OPT_OUT_CONFIRMATION_REQUIRED')
    expect(fetch).toHaveBeenCalledTimes(1)
    await planning.mutate('added', dishId, '2026-10-12', true)
    expect(planning.feedback.value?.inverse.before.optOutReason).toBe('Eating out')
  })

  it('ignores a late dinner response from another family', async () => {
    let resolveOld: (value: unknown) => void = () => { throw new Error('No pending request') }
    const pending = new Promise(resolve => { resolveOld = resolve })
    const fetch = vi.fn().mockReturnValueOnce(pending).mockResolvedValue([])
    const { planning, family } = dinnerWorkspace(fetch)
    family.value = 'new-family'
    await nextTick()
    await flushPromises()
    resolveOld([{ date: '2026-10-12', menu: [{ dishId }], optOutReason: null }])
    await flushPromises()
    expect(planning.days.value.every(day => day.menu.length === 0)).toBe(true)
    expect(planning.feedback.value).toBeNull()
  })

  it('reports save failure without claiming success', async () => {
    const fetch = vi.fn().mockResolvedValueOnce([]).mockRejectedValue(new Error('Storage failed'))
    const { planning } = dinnerWorkspace(fetch, vi.fn().mockRejectedValue(new Error('Storage failed')))
    await flushPromises()
    await planning.mutate('added', dishId, '2026-10-12')
    expect(planning.error.value).toBe('saveFailed')
    expect(planning.feedback.value).toBeNull()
  })
})

function requestWorkspace(fetch = vi.fn().mockResolvedValue(noMatch)) {
  const family = shallowRef('family')
  const account = shallowRef('account')
  const target = shallowRef<string | null>(null)
  const monday = shallowRef('2026-10-12')
  const locale = shallowRef('en')
  const recommendation = scoped(() => useDishRecommendations({ family, account, target, monday, locale, repository: new DishRecommendationsRepository(fetch) }))
  return { recommendation, family, account, target, monday, locale, fetch }
}

describe('recommendation conversation continuity', () => {
  it('clears explanations in the previous language while retaining editable preferences', async () => {
    const { recommendation, locale, fetch } = requestWorkspace()
    await recommendation.submit('request', 'With potatoes')
    expect(recommendation.state.value.result).not.toBeNull()
    locale.value = 'da'
    await nextTick()
    expect(recommendation.state.value).toEqual({ status: 'idle', result: null })
    expect(recommendation.constraints.value).toEqual(['With potatoes'])
    await recommendation.submit('request')
    expect(fetch.mock.calls[1][1].body).toMatchObject({ locale: 'da', constraints: ['With potatoes'] })
  })

  it('retries with the accepted day, current language and edited preferences', async () => {
    const fetch = vi.fn().mockRejectedValueOnce(new Error('Unavailable')).mockResolvedValue(noMatch)
    const { recommendation, target, locale } = requestWorkspace(fetch)
    target.value = '2026-10-13'
    await nextTick()
    await recommendation.submit('request', 'With potatoes')
    target.value = '2026-10-15'
    locale.value = 'da'
    await nextTick()
    await recommendation.retry()
    expect(fetch).toHaveBeenCalledTimes(1)
    recommendation.decideScope('keep')
    recommendation.constraints.value = ['No fish']
    recommendation.role.value = 'Side'
    await recommendation.retry()
    expect(fetch.mock.calls[1][1].body).toMatchObject({ targetDate: '2026-10-15', locale: 'da', constraints: ['No fish'], role: 'Side', mode: 'request' })
  })

  it('retains cumulative editable constraints and independent role scope', async () => {
    const { recommendation, fetch } = requestWorkspace()
    await recommendation.submit('request', 'With potatoes')
    await recommendation.submit('request', 'No fish')
    recommendation.role.value = 'Side'
    await recommendation.submit('more')
    expect(recommendation.constraints.value).toEqual(['With potatoes', 'No fish'])
    expect(fetch.mock.calls[2][1].body).toMatchObject({ constraints: ['With potatoes', 'No fish'], role: 'Side', mode: 'more', targetDate: null })
    recommendation.constraints.value = ['No fish']
    await recommendation.submit('request')
    expect(fetch.mock.calls[3][1].body.constraints).toEqual(['No fish'])
  })

  it('requires keep or new before another day-scoped request', async () => {
    const { recommendation, target, fetch } = requestWorkspace()
    target.value = '2026-10-13'
    await nextTick()
    await recommendation.submit('request', 'With potatoes')
    target.value = '2026-10-15'
    await nextTick()
    await recommendation.submit('request', 'No fish')
    expect(fetch).toHaveBeenCalledTimes(1)
    recommendation.decideScope('keep')
    await recommendation.submit('request', 'No fish')
    expect(fetch.mock.calls[1][1].body).toMatchObject({ targetDate: '2026-10-15', constraints: ['With potatoes', 'No fish'] })
  })

  it('retains previous results and intent on provider failure', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(noMatch).mockRejectedValue(new Error('Unavailable'))
    const { recommendation } = requestWorkspace(fetch)
    await recommendation.submit('request', 'With potatoes')
    await recommendation.submit('request', 'No fish')
    expect(recommendation.state.value).toMatchObject({ status: 'failed', result: noMatch })
    expect(recommendation.constraints.value).toEqual(['With potatoes', 'No fish'])
  })

  it('discards pending requests when family or account changes', async () => {
    let resolveOld: (value: unknown) => void = () => { throw new Error('No pending request') }
    const { recommendation, family, account } = requestWorkspace(vi.fn().mockImplementation(() => new Promise(resolve => { resolveOld = resolve })))
    const pending = recommendation.submit('request', 'No fish')
    family.value = 'new-family'
    account.value = 'new-account'
    await nextTick()
    resolveOld(noMatch)
    await pending
    expect(recommendation.state.value).toEqual({ status: 'idle', result: null })
    expect(recommendation.constraints.value).toEqual([])
  })
})

function dish(id: string, roles: Dish['roles'], effortLevel: Dish['effortLevel'] = null): Dish {
  return { id, name: id, roles, effortLevel, isArchived: false, url: '', notes: '', rating: 0, ratings: [], dates: [], dishStats: { dishId: id, lastUsed: undefined, timesUsed: 0 } }
}

describe('dish exploration', () => {
  it('orders rating and usage descending with stable alphabetical ties', () => {
    const entries = shallowRef<PlanningDish[]>([
      { dish: { ...dish('Soup C', ['Main']), rating: 4 }, stats: { dishId: 'Soup C', timesUsed: 8, lastUsed: undefined } },
      { dish: { ...dish('Soup B', ['Main']), rating: 9 }, stats: { dishId: 'Soup B', timesUsed: 2, lastUsed: undefined } },
      { dish: { ...dish('Soup A', ['Main']), rating: 9 }, stats: { dishId: 'Soup A', timesUsed: 2, lastUsed: undefined } },
    ])
    const exploration = useDishExploration(entries, shallowRef('en'))
    exploration.update({ ...exploration.preferences.value, sort: 'rating' })
    expect(exploration.visible.value.map(entry => entry.dish.id)).toEqual(['Soup A', 'Soup B', 'Soup C'])
    exploration.update({ ...exploration.preferences.value, sort: 'usage' })
    expect(exploration.visible.value.map(entry => entry.dish.id)).toEqual(['Soup C', 'Soup A', 'Soup B'])
  })

  it('combines filters without treating unknown metadata as a match', () => {
    const entries = shallowRef<PlanningDish[]>([{ dish: dish('Main', ['Main'], 'Quick') }, { dish: dish('Unknown', []) }, { dish: dish('Side', ['Side'], 'Quick') }])
    const exploration = useDishExploration(entries, shallowRef('en'))
    expect(exploration.visible.value.map(entry => entry.dish.id)).toEqual(['Main', 'Unknown'])
    exploration.update({ ...exploration.preferences.value, effort: 'Quick' })
    expect(exploration.visible.value.map(entry => entry.dish.id)).toEqual(['Main'])
    exploration.update({ ...exploration.preferences.value, role: 'Side' })
    expect(exploration.visible.value.map(entry => entry.dish.id)).toEqual(['Side'])
  })

  it('sorts last-served dates without resetting search or wishes', () => {
    const entries = shallowRef<PlanningDish[]>([
      { dish: dish('Soup B', ['Main']), wishVotes: 0, stats: { dishId: 'Soup B', timesUsed: 1, lastUsed: DateTime.fromISO('2026-09-01') } },
      { dish: dish('Soup A', []), wishVotes: 2, stats: { dishId: 'Soup A', timesUsed: 3, lastUsed: DateTime.fromISO('2026-01-01') } },
    ])
    const exploration = useDishExploration(entries, shallowRef('en'))
    exploration.update({ ...exploration.preferences.value, sort: 'lastUsed', search: 'Soup', wishesOnly: true })
    expect(exploration.visible.value.map(entry => entry.dish.id)).toEqual(['Soup A', 'Soup B'])
    expect(exploration.preferences.value.search).toBe('Soup')
  })
})

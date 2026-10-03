import { computed, onScopeDispose, shallowRef, watch } from 'vue'
import { DateTime } from 'luxon'
import { z } from 'zod'
import type { DinnerRepository } from '~/repository/dinner-repository'
import type { DishRecommendationsRepository } from '~/repository/dish-recommendations-repository'
import { planningDinnerSchema } from '~/types/week-planning'
import type { PlanningDinner, PlanningDay, UndoFeedback } from '~/types/week-planning'
import type { Dish } from '~/types'
import type { Ref } from 'vue'

export function planningWindow(monday: string): string[] {
  const date = DateTime.fromISO(monday)
  if (!date.isValid || date.weekday !== 1) throw new Error('INVALID_SELECTED_MONDAY')
  return Array.from({ length: 9 }, (_, index) => date.plus({ days: index - 2 }).toFormat('yyyy-MM-dd'))
}

export function useWeekPlanning(options: { family: Ref<string>; account: Ref<string>; dishes: Ref<Dish[]>; unavailableDishLabel: Ref<string>; dinners: DinnerRepository; recommendations: DishRecommendationsRepository; initialMonday?: string }) {
  const monday = shallowRef(options.initialMonday ?? DateTime.local().startOf('week').toFormat('yyyy-MM-dd'))
  const selectedDate = shallowRef<string | null>(null)
  const saved = shallowRef<PlanningDinner[]>([])
  const loading = shallowRef(false)
  const busy = shallowRef(false)
  const loadedScope = shallowRef<string | null>(null)
  const currentScope = computed(() => JSON.stringify([options.family.value, options.account.value, monday.value]))
  const canMutate = computed(() => loadedScope.value === currentScope.value && !loading.value && !busy.value)
  const error = shallowRef<string | null>(null)
  const feedback = shallowRef<UndoFeedback | null>(null)
  const notice = shallowRef<'duplicate' | 'unchanged' | 'conflict' | 'restored' | null>(null)
  let generation = 0
  let readGeneration = 0
  const dates = computed(() => planningWindow(monday.value))
  const days = computed<PlanningDay[]>(() => dates.value.map((date) => {
    const dinner = saved.value.find(item => item.date === date)
    return { date, menu: (dinner?.menu ?? []).map(item => ({ dishId: item.dishId, dishName: options.dishes.value.find(dish => dish.id === item.dishId)?.name ?? item.dishName ?? options.unavailableDishLabel.value })), optOutReason: dinner?.optOutReason ?? null }
  }))

  async function refresh() {
    const family = options.family.value
    const window = dates.value
    const token = ++readGeneration
    const session = generation
    const scope = currentScope.value
    loadedScope.value = null
    if (!family) return
    loading.value = true
    try {
      const response: unknown = await options.dinners.getRange(family, DateTime.fromISO(window[0]), DateTime.fromISO(window[8]))
      if (token !== readGeneration || session !== generation) return
      saved.value = z.array(planningDinnerSchema).parse(response)
      loadedScope.value = scope
      error.value = null
    }
    catch { if (token === readGeneration && session === generation) error.value = 'readFailed' }
    finally { if (token === readGeneration && session === generation) loading.value = false }
  }

  function navigate(offset: number) {
    monday.value = DateTime.fromISO(monday.value).plus({ weeks: offset }).toFormat('yyyy-MM-dd')
    if (selectedDate.value && !dates.value.includes(selectedDate.value)) selectedDate.value = null
  }

  async function mutate(kind: 'added' | 'removed', dishId: string, date: string, confirmedOptOut = false) {
    if (!canMutate.value || !dates.value.includes(date)) return
    const day = days.value.find(day => day.date === date)
    if (!day) throw new Error('DAY_OUTSIDE_WINDOW')
    const present = day.menu.some(item => item.dishId === dishId)
    if ((kind === 'added') === present) { feedback.value = null; notice.value = kind === 'added' ? 'duplicate' : 'unchanged'; return 'NoOp' }
    if (kind === 'added' && day.optOutReason && !confirmedOptOut) throw new Error('OPT_OUT_CONFIRMATION_REQUIRED')
    const family = options.family.value
    const token = generation
    const before = { dishIds: day.menu.map(item => item.dishId), optOutReason: day.optOutReason }
    busy.value = true
    feedback.value = null
    error.value = null
    try {
      const result = await options.recommendations.changeMenu(family, date, kind, { dishId, expectedState: before })
      if (token !== generation) return
      if (result.outcome === 'Changed') {
        feedback.value = { familyId: family, date, dishId, kind, inverse: { dishId, before: result.before, after: result.after } }
        notice.value = null
      }
      else notice.value = result.outcome === 'Conflict' ? 'conflict' : kind === 'added' ? 'duplicate' : 'unchanged'
      await refresh()
      return result.outcome
    }
    catch {
      if (token === generation) { await refresh(); if (token === generation) error.value = 'saveFailed' }
    }
    finally { if (token === generation) busy.value = false }
  }

  async function undo() {
    const action = feedback.value
    if (!action || busy.value) return
    const token = generation
    busy.value = true
    try {
      const result = await options.recommendations.undo(action.familyId, action.date, action.inverse)
      if (token !== generation) return
      notice.value = result.outcome === 'Restored' ? 'restored' : 'conflict'
      feedback.value = null
      await refresh()
    }
    catch { if (token === generation) error.value = 'undoFailed' }
    finally { if (token === generation) busy.value = false }
  }

  watch([options.family, options.account], () => {
    generation++
    readGeneration++
    saved.value = []
    loadedScope.value = null
    selectedDate.value = null
    feedback.value = null
    notice.value = null
    error.value = null
    busy.value = false
    loading.value = false
    void refresh()
  }, { immediate: true })
  watch(monday, () => { readGeneration++; saved.value = []; void refresh() })
  onScopeDispose(() => { generation++; readGeneration++ })
  return { monday, dates, days, selectedDate, loading, busy, canMutate, error, feedback, notice, navigate, refresh, mutate, undo }
}

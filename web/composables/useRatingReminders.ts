import { computed, onScopeDispose, readonly, shallowRef, toValue, watch } from 'vue'
import type { MaybeRefOrGetter } from 'vue'
import { DateTime } from 'luxon'
import type { RatingReminder, ReminderRequestState, ReminderActionState } from '~/types/rating-reminders'
import type { RatingRemindersRepository } from '~/repository/rating-reminders-repository'
import type { DishesRepository } from '~/repository/dishes-repository'

export function useRatingReminders(options: {
  family: MaybeRefOrGetter<string>
  account: MaybeRefOrGetter<string>
  repository: Pick<RatingRemindersRepository, 'get' | 'dismiss'>
  dishes: Pick<DishesRepository, 'updateRating'>
}) {
  const queue = shallowRef<RatingReminder[]>([])
  const today = shallowRef('')
  const request = shallowRef<ReminderRequestState>({ status: 'idle' })
  const action = shallowRef<ReminderActionState>({ status: 'idle' })
  const current = computed(() => queue.value[0] ?? null)
  const pending = computed(() => action.value.status === 'rating' || action.value.status === 'dismissing')
  let generation = 0
  let readGeneration = 0
  let controller: AbortController | null = null
  let timer: ReturnType<typeof setTimeout> | undefined
  let disposed = false

  function retireExpired() {
    const now = DateTime.now().setZone('Europe/Copenhagen').toFormat('yyyy-MM-dd')
    if (today.value && now > today.value) {
      const cutoff = DateTime.fromISO(now).minus({ days: 7 }).toFormat('yyyy-MM-dd')
      queue.value = queue.value.filter(reminder => reminder.dinnerDate >= cutoff && reminder.dinnerDate < now)
    }
  }

  function scheduleMidnight() {
    clearTimeout(timer)
    if (disposed || typeof window === 'undefined') return
    const now = DateTime.now().setZone('Europe/Copenhagen')
    const delay = now.plus({ days: 1 }).startOf('day').diff(now).as('milliseconds') + 50
    timer = setTimeout(() => { retireExpired(); void refresh() }, delay)
  }

  async function refresh() {
    if (pending.value) return
    controller?.abort()
    const token = ++readGeneration
    const identity = generation
    const family = toValue(options.family)
    const account = toValue(options.account)
    if (!family || !account || disposed) return
    retireExpired()
    controller = new AbortController()
    request.value = { status: 'loading' }
    try {
      const result = await options.repository.get(family, controller.signal)
      if (token !== readGeneration || identity !== generation || disposed) return
      queue.value = result.reminders
      today.value = result.today
      request.value = { status: 'ready' }
    } catch {
      if (token === readGeneration && identity === generation && !disposed) request.value = { status: 'failed', error: 'loadFailed' }
    } finally {
      if (token === readGeneration && identity === generation) scheduleMidnight()
    }
  }

  async function resolve(kind: 'rating' | 'dismissing', value?: number): Promise<'saved' | 'failed' | 'stale' | 'ignored'> {
    const reminder = current.value
    if (!reminder || pending.value) return 'ignored'
    const family = toValue(options.family)
    const account = toValue(options.account)
    if (!family || !account) return 'ignored'
    const identity = generation
    controller?.abort()
    readGeneration++
    action.value = { status: kind }
    try {
      if (kind === 'rating') {
        if (value === undefined || !Number.isFinite(value) || value < 0 || value > 5 || value * 2 % 1 !== 0) throw new Error('INVALID_RATING')
        await options.dishes.updateRating(reminder.dishId, value, account)
      } else {
        await options.repository.dismiss(family, reminder.dishId, reminder.dinnerDate)
      }
    } catch {
      if (identity !== generation || disposed) return 'stale'
      action.value = { status: 'failed', action: kind }
      return 'failed'
    }
    if (identity !== generation || disposed) return 'stale'
    queue.value = queue.value.filter(item => item.dishId !== reminder.dishId)
    action.value = { status: 'idle' }
    await refresh()
    return identity === generation && !disposed ? 'saved' : 'stale'
  }

  watch([() => toValue(options.family), () => toValue(options.account)], () => {
    generation++
    readGeneration++
    controller?.abort()
    queue.value = []
    today.value = ''
    action.value = { status: 'idle' }
    request.value = { status: 'idle' }
    void refresh()
  }, { immediate: true, flush: 'sync' })

  function activate() { if (typeof document === 'undefined' || document.visibilityState !== 'hidden') { retireExpired(); void refresh() } }
  if (typeof window !== 'undefined') window.addEventListener('focus', activate)
  if (typeof document !== 'undefined') document.addEventListener('visibilitychange', activate)
  onScopeDispose(() => {
    disposed = true
    generation++
    controller?.abort()
    clearTimeout(timer)
    if (typeof window !== 'undefined') window.removeEventListener('focus', activate)
    if (typeof document !== 'undefined') document.removeEventListener('visibilitychange', activate)
  })
  return { queue: readonly(queue), today: readonly(today), request: readonly(request), action: readonly(action), current, pending,
    refresh, rate: (value: number) => resolve('rating', value), dismiss: () => resolve('dismissing') }
}

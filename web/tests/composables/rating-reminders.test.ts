import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, nextTick, shallowRef } from 'vue'
import { useRatingReminders } from '~/composables/useRatingReminders'
import type { RatingReminder } from '~/types/rating-reminders'

const first: RatingReminder = { dishId: '00000001-0000-0000-0000-000000000000', dishName: 'Lasagne', dinnerDate: '2026-10-11' }
const second: RatingReminder = { dishId: '00000002-0000-0000-0000-000000000000', dishName: 'Tacos', dinnerDate: '2026-10-10' }
const scopes: ReturnType<typeof effectScope>[] = []
function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason: Error) => void
  const promise = new Promise<T>((yes, no) => { resolve = yes; reject = no })
  return { promise, resolve, reject }
}
function setup() {
  const family = shallowRef('family')
  const account = shallowRef('account')
  const get = vi.fn().mockResolvedValue({ today: '2026-10-12', reminders: [first, second] })
  const dismiss = vi.fn().mockResolvedValue(undefined)
  const updateRating = vi.fn().mockResolvedValue(undefined)
  const scope = effectScope()
  scopes.push(scope)
  const reminders = scope.run(() => useRatingReminders({ family, account, repository: { get, dismiss }, dishes: { updateRating } }))!
  return { family, account, get, dismiss, updateRating, reminders, scope }
}
async function settled() { for (let index = 0; index < 8; index++) await Promise.resolve(); await nextTick() }
beforeEach(() => { vi.useFakeTimers(); vi.setSystemTime(new Date('2026-10-12T12:00:00Z')) })
afterEach(() => { scopes.splice(0).forEach(scope => scope.stop()); vi.useRealTimers() })

describe('personal reminder queue', () => {
  it('rates through the existing dish endpoint with the current account', async () => {
    const { reminders, get, updateRating } = setup()
    await settled()
    get.mockResolvedValue({ today: '2026-10-12', reminders: [second] })
    expect(await reminders.rate(3.5)).toBe('saved')
    expect(updateRating).toHaveBeenCalledWith(first.dishId, 3.5, 'account')
    expect(reminders.current.value).toEqual(second)
  })

  it('advances dismissal only after successful persistence', async () => {
    const { reminders, dismiss, get } = setup()
    await settled()
    const save = deferred<unknown>()
    dismiss.mockReturnValue(save.promise)
    get.mockResolvedValue({ today: '2026-10-12', reminders: [second] })
    const pending = reminders.dismiss()
    expect(reminders.current.value).toEqual(first)
    expect(reminders.pending.value).toBe(true)
    expect(await reminders.rate(4)).toBe('ignored')
    save.resolve(undefined)
    expect(await pending).toBe('saved')
    expect(dismiss).toHaveBeenCalledWith('family', first.dishId, first.dinnerDate)
    expect(reminders.current.value).toEqual(second)
  })

  it.each(['rating', 'dismissing'])('retains a candidate when %s fails', async (kind) => {
    const { reminders, dismiss, updateRating } = setup()
    await settled()
    dismiss.mockRejectedValue(new Error('Unavailable'))
    updateRating.mockRejectedValue(new Error('Unavailable'))
    expect(await (kind === 'rating' ? reminders.rate(3.5) : reminders.dismiss())).toBe('failed')
    expect(reminders.current.value).toEqual(first)
    expect(reminders.action.value).toEqual({ status: 'failed', action: kind })
    expect(reminders.pending.value).toBe(false)
  })

  it('reports saved action even when follow-up refresh fails', async () => {
    const { reminders, get } = setup()
    await settled()
    get.mockRejectedValue(new Error('Refresh failed'))
    expect(await reminders.rate(4)).toBe('saved')
    expect(reminders.current.value).toEqual(second)
    expect(reminders.action.value).toEqual({ status: 'idle' })
    expect(reminders.request.value.status).toBe('failed')
  })

  it.each(['family', 'account'])('discards stale read on %s switch', async (identity) => {
    const { reminders, get, family, account } = setup()
    await settled()
    const previous = deferred<{ today: string; reminders: RatingReminder[] }>()
    get.mockReturnValueOnce(previous.promise)
    const refresh = reminders.refresh()
    get.mockResolvedValue({ today: '2026-10-12', reminders: [second] })
    if (identity === 'family') family.value = 'other'
    else account.value = 'other'
    await settled()
    previous.resolve({ today: '2026-10-12', reminders: [first] })
    await refresh
    expect(reminders.current.value).toEqual(second)
  })

  it.each(['family', 'account'])('does not mutate the new queue when a write finishes after %s switch', async (identity) => {
    const { reminders, get, updateRating, family, account } = setup()
    await settled()
    const save = deferred<unknown>()
    updateRating.mockReturnValue(save.promise)
    const action = reminders.rate(3)
    get.mockResolvedValue({ today: '2026-10-12', reminders: [first, second] })
    if (identity === 'family') family.value = 'other'
    else account.value = 'other'
    await settled()
    save.resolve(undefined)
    expect(await action).toBe('stale')
    expect(reminders.current.value).toEqual(first)
    expect(reminders.pending.value).toBe(false)
  })

  it('clears identity state on sign-out and keeps instances independent', async () => {
    const a = setup()
    const b = setup()
    await settled()
    a.account.value = ''
    expect(a.reminders.current.value).toBeNull()
    expect(b.reminders.current.value).toEqual(first)
    a.scope.stop()
    expect(b.reminders.current.value).toEqual(first)
  })
})

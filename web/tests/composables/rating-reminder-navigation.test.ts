import { afterEach, describe, expect, it, vi } from 'vitest'
import { effectScope, shallowRef } from 'vue'
import { parseRatingReminderDestination, rememberRatingReminderReturn, consumeRatingReminderReturn, clearRatingReminderReturn } from '~/utils/rating-reminder-navigation'
import { useReminderDishEntry } from '~/composables/useReminderDishEntry'
import { useRatingReminderPreference } from '~/composables/useRatingReminderPreference'

const dishId = '00000001-0000-0000-0000-000000000000'
const familyId = '00000002-0000-0000-0000-000000000000'
const destination = `/dishes/${dishId}?familyId=${familyId}&ratingReminderDate=2026-10-11#my-rating`
const scopes: ReturnType<typeof effectScope>[] = []
afterEach(() => scopes.splice(0).forEach(scope => scope.stop()))
async function settle() { for (let i = 0; i < 10; i++) await Promise.resolve() }
function scoped<T>(create: () => T): T { const scope = effectScope(); scopes.push(scope); return scope.run(create)! }
function deferred<T>() { let resolve!: (value: T) => void; const promise = new Promise<T>(yes => { resolve = yes }); return { promise, resolve } }

describe('notification return intent', () => {
  it('preserves and consumes only the recognized relative dish route', () => {
    const entries = new Map<string, string>()
    const storage = { setItem: (key: string, value: string) => { entries.set(key, value) }, getItem: (key: string) => entries.get(key) ?? null, removeItem: (key: string) => { entries.delete(key) } }
    rememberRatingReminderReturn(destination, storage)
    expect(consumeRatingReminderReturn(storage)).toBe(destination)
    expect(consumeRatingReminderReturn(storage)).toBe('/home')
    rememberRatingReminderReturn(destination, storage)
    clearRatingReminderReturn(storage)
    expect(consumeRatingReminderReturn(storage)).toBe('/home')
  })
  it.each(['https://evil.test' + destination, '//' + destination, '/plan', destination.replace('2026-10-11', '2026-02-30'), destination + '&extra=1', destination.replace(familyId, 'bad')])('rejects unsafe or malformed destination %s', (value) => {
    expect(parseRatingReminderDestination(value)).toBeNull()
  })
})

describe('reminder dish entry', () => {
  function setup(initialFamily = 'previous') {
    const route = shallowRef({ id: dishId, fullPath: destination })
    const account = shallowRef('account')
    const family = shallowRef(initialFamily)
    const selectors = vi.fn().mockResolvedValue([{ id: familyId, name: 'Family' }])
    const getFamily = vi.fn().mockResolvedValue({ id: familyId, name: 'Family', familyMembers: [{ id: 'account', name: 'Me', isOwner: true, hasAutonomy: true }] })
    const getFull = vi.fn().mockResolvedValue({ id: dishId, name: 'Lasagne' })
    const catalog = vi.fn()
    const entry = scoped(() => useReminderDishEntry({ route, account, family, selectFamily: id => { family.value = id }, families: { familySelectors: selectors, get: getFamily }, dishes: { getFull }, catalog }))
    return { entry, route, account, family, selectors, getFamily, getFull, catalog }
  }
  it('resolves accessible family before loading and avoids the ordinary family redirect', async () => {
    const { entry, family, getFull, catalog } = setup()
    await settle()
    expect(family.value).toBe(familyId)
    expect(getFull).toHaveBeenCalledWith(dishId, familyId)
    expect(entry.focusKey.value).toBe(destination)
    expect(catalog).not.toHaveBeenCalled()
    family.value = 'manual-other'
    expect(catalog).toHaveBeenCalledOnce()
    expect(entry.dish.value).toBeNull()
  })
  it('preserves cold-entry intent while the layout auto-selects its first family', async () => {
    const { entry, family, selectors, getFull, catalog } = setup('')
    const pending = deferred<{ id: string; name: string }[]>()
    selectors.mockReturnValue(pending.promise)
    const loading = entry.load()
    family.value = 'layout-default'
    pending.resolve([{ id: familyId, name: 'Target' }])
    await loading
    expect(family.value).toBe(familyId)
    expect(getFull).toHaveBeenCalledWith(dishId, familyId)
    expect(catalog).not.toHaveBeenCalled()
  })
  it('does not select or load an inaccessible family', async () => {
    const { entry, family, selectors, getFull } = setup()
    selectors.mockResolvedValue([])
    await entry.load()
    expect(family.value).toBe('previous')
    expect(getFull).not.toHaveBeenCalled()
    expect(entry.unavailable.value).toBe(true)
  })
  it('reacts to another dish route and ignores stale loading', async () => {
    const { entry, route, getFull } = setup()
    await settle()
    const previous = deferred<{ id: string; name: string }>()
    getFull.mockReturnValueOnce(previous.promise)
    const pending = entry.load()
    await settle()
    getFull.mockResolvedValue({ id: 'next', name: 'Tacos' })
    route.value = { id: 'next', fullPath: '/dishes/next' }
    await settle()
    previous.resolve({ id: dishId, name: 'Old' })
    await pending
    expect(entry.dish.value?.name).toBe('Tacos')
    expect(entry.focusKey.value).toBeUndefined()
  })
  it('clears loaded dish on sign-out', async () => {
    const { entry, account } = setup()
    await settle()
    account.value = ''
    await settle()
    expect(entry.dish.value).toBeNull()
  })
})

describe('independent push preference', () => {
  function setup() {
    const account = shallowRef('account')
    const subscribed = shallowRef(true)
    const preferences = vi.fn().mockResolvedValue({ pushEnabled: false })
    const setPreference = vi.fn().mockResolvedValue({ pushEnabled: true })
    const preference = scoped(() => useRatingReminderPreference({ account, subscribed, repository: { preferences, setPreference } }))
    return { account, subscribed, preferences, setPreference, preference }
  }
  it('defaults existing subscribers off and uses confirmed persistence across reload', async () => {
    const { preference, preferences } = setup()
    await settle()
    expect(preference.enabled.value).toBe(false)
    await preference.setEnabled(true)
    expect(preference.enabled.value).toBe(true)
    preferences.mockResolvedValue({ pushEnabled: true })
    await preference.refresh()
    expect(preference.enabled.value).toBe(true)
  })
  it('leaves confirmed state unchanged after save failure', async () => {
    const { preference, setPreference } = setup()
    await settle()
    setPreference.mockRejectedValue(new Error('Failed'))
    await preference.setEnabled(true)
    expect(preference.enabled.value).toBe(false)
    expect(preference.state.value).toBe('saveFailed')
  })
  it('does not save or prompt without a subscription', async () => {
    const { preference, subscribed, setPreference } = setup()
    await settle()
    subscribed.value = false
    expect(preference.active.value).toBe(false)
    await preference.setEnabled(true)
    expect(setPreference).not.toHaveBeenCalled()
  })
  it('clears account-bound state and discards stale updates', async () => {
    const { preference, account, preferences, setPreference } = setup()
    await settle()
    const pending = deferred<{ pushEnabled: boolean }>()
    setPreference.mockReturnValue(pending.promise)
    const save = preference.setEnabled(true)
    preferences.mockResolvedValue({ pushEnabled: false })
    account.value = 'other'
    await settle()
    pending.resolve({ pushEnabled: true })
    await save
    expect(preference.enabled.value).toBe(false)
    expect(preference.state.value).toBe('ready')
  })
})

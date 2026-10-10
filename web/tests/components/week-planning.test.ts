import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import type { VueWrapper } from '@vue/test-utils'
import { defineComponent, h, KeepAlive, onScopeDispose, reactive, shallowRef, watch } from 'vue'
import { DateTime } from 'luxon'
import { createPinia, setActivePinia } from 'pinia'
import { z } from 'zod'
import Workspace from '~/components/WeekPlanning/Workspace.vue'
import type { Dish, WishlistItem } from '~/types'
import type { PlanningDinner } from '~/types/week-planning'
import type { MenuChangeRequest } from '~/types/dish-recommendations'
import english from '~/i18n/locales/en.json'
import danish from '~/i18n/locales/da.json'

vi.mock('vuetify', () => ({ useDisplay: () => ({ smAndDown: shallowRef(false), height: shallowRef(800) }) }))

enableAutoUnmount(afterEach)
const favouriteId = '00000000-0000-4000-8000-000000000001'
const sideId = '00000000-0000-4000-8000-000000000002'
const unknownId = '00000000-0000-4000-8000-000000000003'
const monday = DateTime.fromISO('2026-10-12')
const date = (offset: number) => monday.plus({ days: offset }).toFormat('yyyy-MM-dd')
const locale = shallowRef('en')
const account = shallowRef('account')
const app = reactive({ activeFamilyId: 'family' })
let saved: PlanningDinner[] = []
let providerFailure = false
let undoConflict = false
let catalog: Dish[] = []
let wishes: WishlistItem[] = []
const readDinners = vi.fn(async () => structuredClone(saved))

function dish(id: string, name: string, roles: Dish['roles']): Dish {
  return { id, name, roles, notes: '## Family notes\nUse frozen vegetables. <img src=x onerror=alert(1)>', url: '', rating: 8, ratings: [{ familyMemberId: 'member', rating: 4 }], dates: [], dishStats: { dishId: id, timesUsed: 5, lastUsed: undefined }, isArchived: false, effortLevel: id === favouriteId ? 'Quick' : null }
}

function match(id = favouriteId) {
  const selected = catalog.find(dish => dish.id === id)
  if (!selected) throw new Error('Missing fixture dish')
  return { dishId: id, name: selected.name, rating: 8, roles: selected.roles ?? [], isUnclassified: !selected.roles?.length,
    isWished: false, wishVotes: 0, lastServed: '2026-01-01', servingCount: 5, neverUsed: false, typicalSpacingDays: 35, assignedDates: [],
    historicalReasons: [{ kind: 'ForgottenFavourite', value: 70 }], explanations: [], limitations: [] }
}

const recommend = vi.fn(async (_family: string, intent: { mode: string; constraints: string[]; excludedDishIds: string[]; role: string | null }) => {
  if (providerFailure) throw new Error('Unavailable')
  const id = intent.role === 'Side' || intent.constraints.some(text => /side/i.test(text)) ? sideId : favouriteId
  if (intent.excludedDishIds.includes(id)) return { outcome: 'Exhausted', dishes: [], activeConstraints: intent.constraints, contextSummary: intent.constraints.join('; '), message: null }
  return { outcome: 'Matches', dishes: [match(id)], activeConstraints: intent.constraints, contextSummary: intent.constraints.join('; '), message: null }
})
const add = vi.fn(async (_family: string, target: DateTime, id: string) => {
  const chosenDate = target.toFormat('yyyy-MM-dd')
  let day = saved.find(day => day.date === chosenDate)
  if (!day) { day = { date: chosenDate, menu: [], optOutReason: null }; saved.push(day) }
  if (!day.menu.some(item => item.dishId === id)) day.menu.push({ dishId: id })
  day.optOutReason = null
})
const remove = vi.fn(async (_family: string, target: DateTime, id: string) => {
  const day = saved.find(day => day.date === target.toFormat('yyyy-MM-dd'))
  if (day) day.menu = day.menu.filter(item => item.dishId !== id)
})
async function persistMenu(family: string, target: string, kind: 'added' | 'removed', request: MenuChangeRequest) {
  const original = saved.find(day => day.date === target)
  const before = { dishIds: original?.menu.map(item => item.dishId) ?? [], optOutReason: original?.optOutReason ?? null }
  if (before.dishIds.includes(request.dishId) === (kind === 'added')) return { outcome: 'NoOp' }
  await (kind === 'added' ? add : remove)(family, DateTime.fromISO(target), request.dishId)
  const changed = saved.find(day => day.date === target)
  return { outcome: 'Changed', before, after: { dishIds: changed?.menu.map(item => item.dishId) ?? [], optOutReason: changed?.optOutReason ?? null } }
}
const changeMenu = vi.fn(persistMenu)
const undo = vi.fn(async (_family: string, target: string, intent: { before: { dishIds: string[]; optOutReason: string | null } }) => {
  if (undoConflict) return { outcome: 'Conflict' }
  saved = saved.filter(day => day.date !== target)
  saved.push({ date: target, menu: intent.before.dishIds.map(dishId => ({ dishId })), optOutReason: intent.before.optOutReason })
  return { outcome: 'Restored' }
})

const Block = defineComponent({ setup: (_, { slots }) => () => h('div', slots.default?.()) })
const Button = defineComponent({ props: { disabled: Boolean, loading: Boolean }, setup: (props, { slots }) => () => h('button', { disabled: props.disabled || props.loading }, slots.default?.()) })
const Field = defineComponent({ props: { modelValue: String, label: String }, emits: ['update:modelValue'], setup: (props, { emit }) => () => h('input', { value: props.modelValue, 'aria-label': props.label, onInput: (event: Event) => { if (event.target instanceof HTMLInputElement) emit('update:modelValue', event.target.value) } }) })
const Select = defineComponent({ props: { modelValue: String, label: String, items: Array }, emits: ['update:modelValue'], setup: (props, { emit }) => () => h('select', { 'aria-label': props.label, value: props.modelValue ?? 'NULL', onChange: (event: Event) => { if (event.target instanceof HTMLSelectElement) emit('update:modelValue', event.target.value === 'NULL' ? null : event.target.value) } }, z.array(z.object({ title: z.string(), value: z.string().nullable() })).parse(props.items).map(item => h('option', { value: item.value ?? 'NULL' }, item.title))) })
const Checkbox = defineComponent({ props: { modelValue: Boolean, label: String }, emits: ['update:modelValue'], setup: (props, { emit }) => () => h('input', { type: 'checkbox', checked: props.modelValue, 'aria-label': props.label, onChange: (event: Event) => { if (event.target instanceof HTMLInputElement) emit('update:modelValue', event.target.checked) } }) })
const Dialog = defineComponent({ props: { modelValue: Boolean }, emits: ['afterLeave'], setup: (props, { emit, slots }) => { watch(() => props.modelValue, open => { if (!open) emit('afterLeave') }); return () => props.modelValue ? h('div', { role: 'dialog' }, slots.default?.()) : null } })
const Link = defineComponent({ props: { to: String }, setup: (props, { slots }) => () => h('a', { href: props.to, onClick: (event: Event) => event.preventDefault() }, slots.default?.()) })
const global = { mocks: { $t: translate }, stubs: { NuxtLink: Link, DishOverflowMenu: Block, DishRating: Block, DishPill: false, VIcon: Block, PlanPlannedDinnerDetails: Block, VChip: Button, VRating: Block, VProgressCircular: Block, VCardTitle: Block, VSpacer: Block, VAutocomplete: Block, VBtn: Button, VSelect: Select, VTextField: Field, VTextarea: Field, VCheckbox: Checkbox, VDialog: Dialog, VCard: Block, VCardText: Block, VCardActions: Block, VAlert: Block, VProgressLinear: Block } }

function translate(key: string, parameters: Record<string, unknown> = {}): string {
  let value: unknown = locale.value === 'da' ? danish : english
  for (const part of key.split('.')) {
    if (typeof value !== 'object' || value === null) return key
    value = Reflect.get(value, part)
  }
  if (typeof value !== 'string') return key
  return Object.entries(parameters).reduce((text, [key, replacement]) => text.replace(`{${key}}`, String(replacement)), value)
}

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.clearAllMocks()
  changeMenu.mockReset().mockImplementation(persistMenu)
  readDinners.mockReset().mockImplementation(async () => structuredClone(saved))
  setActivePinia(createPinia())
  locale.value = 'en'
  app.activeFamilyId = 'family'
  account.value = 'account'
  providerFailure = false
  undoConflict = false
  wishes = []
  catalog = [dish(favouriteId, 'Forgotten favourite', ['Main']), dish(sideId, 'Potato side', ['Side']), dish(unknownId, 'Unclassified soup', [])]
  saved = [{ date: date(0), menu: [{ dishId: sideId }], optOutReason: null }, { date: date(1), menu: [], optOutReason: 'Eating out' }]
  vi.stubGlobal('useI18n', () => ({ t: translate, locale }))
  vi.stubGlobal('useAppStore', () => app)
  vi.stubGlobal('useDishesStore', () => ({ dishes: catalog }))
  vi.stubGlobal('onScopeDispose', onScopeDispose)
  vi.stubGlobal('useNuxtApp', () => ({ $msal: { isAuthenticated: shallowRef(true), getObjectId: () => account.value } }))
  vi.stubGlobal('useRepositories', () => ({
    dishes: { all: async () => catalog, allUsageStats: async () => ({}), getFull: async (id: string) => catalog.find(dish => dish.id === id) },
    wishlist: { getWishlist: async () => wishes },
    dinners: { getRange: readDinners, addDishToMenu: add, removeDishFromMenu: remove },
    dishRecommendations: { recommend, undo, changeMenu },
  }))
  vi.spyOn(window, 'scrollTo').mockImplementation(() => {})
  Object.defineProperty(window, 'scrollY', { value: 0, configurable: true })
})

function button(wrapper: VueWrapper, name: string) {
  const found = wrapper.findAll('button').find(button => button.text() === name || button.attributes('aria-label') === name)
  if (!found) throw new Error(`Missing button: ${name}`)
  return found
}
async function workspace() { const wrapper = mount(Workspace, { global, attachTo: document.body }); await flushPromises(); return wrapper }

afterEach(() => { vi.useRealTimers() })

describe.each(['2026-10-12', '2026-10-10'])('planning workspace public interactions on %s', today => {
  beforeEach(() => { vi.setSystemTime(DateTime.fromISO(today).toJSDate()) })
  it('keeps the same dish open and synchronizes targets through previous, next and current week', async () => {
    const wrapper = await workspace()
    await button(wrapper, 'Plan dish').trigger('click')
    const nextWeek = () => wrapper.findAll('.plan-picker .week-nav__arrow')[1]
    await nextWeek().trigger('click')
    await flushPromises()
    expect(wrapper.find('.plan-picker__title').text()).toContain('Forgotten favourite')
    expect(wrapper.findAll('.plan-picker button.plan-day-row')[2].text()).toContain('19 Oct')
    expect(wrapper.findAll('.dinner-card__header')[2].text()).toContain('Oct 19')
    await wrapper.findAll('.plan-picker .week-nav__arrow')[0].trigger('click')
    await flushPromises()
    await wrapper.findAll('.plan-picker .week-nav__arrow')[0].trigger('click')
    await flushPromises()
    expect(wrapper.findAll('.plan-picker button.plan-day-row')[0].text()).toContain('3 Oct')
    await wrapper.find('.plan-picker .week-nav__center').trigger('click')
    await flushPromises()
    const currentMonday = DateTime.fromISO(today).startOf('week')
    expect(wrapper.findAll('.plan-picker button.plan-day-row')[2].text()).toContain(currentMonday.setLocale('en').toFormat('d MMM'))
    expect(wrapper.findAll('.dinner-card__header')[2].text()).toContain(currentMonday.setLocale('en').toFormat('MMM d'))
    expect(wrapper.find('.plan-picker__title').text()).toContain('Forgotten favourite')
    await wrapper.findAll('.plan-picker button.plan-day-row')[2].trigger('click')
    await flushPromises()
    expect(changeMenu.mock.calls[0][1]).toBe(currentMonday.toFormat('yyyy-MM-dd'))
  })

  it('keeps navigation available during reads, discards late reads and retries the selected week', async () => {
    const wrapper = await workspace()
    await button(wrapper, 'Plan dish').trigger('click')
    let resolveOld: (value: PlanningDinner[]) => void = () => { throw new Error('No pending read') }
    readDinners.mockReturnValueOnce(new Promise(resolve => { resolveOld = resolve }))
    await wrapper.findAll('.plan-picker .week-nav__arrow')[1].trigger('click')
    await flushPromises()
    expect(wrapper.findAll('.plan-picker .plan-day-row--skeleton')).toHaveLength(9)
    expect(wrapper.findAll('.plan-picker button.plan-day-row')).toHaveLength(0)
    expect(wrapper.findAll('.plan-picker .week-nav__arrow')[1].attributes('disabled')).toBeUndefined()
    readDinners.mockRejectedValueOnce(new Error('Unavailable'))
    await wrapper.findAll('.plan-picker .week-nav__arrow')[1].trigger('click')
    await flushPromises()
    expect(wrapper.find('.plan-picker [role="alert"]').text()).toContain(translate('weekPlanning.readFailed'))
    expect(wrapper.findAll('.plan-picker button.plan-day-row')).toHaveLength(0)
    resolveOld([{ date: '2026-10-26', menu: [{ dishId: favouriteId }], optOutReason: null }])
    await flushPromises()
    expect(wrapper.find('.plan-picker [role="alert"]').exists()).toBe(true)
    await button(wrapper, translate('weekPlanning.retry')).trigger('click')
    await flushPromises()
    expect(wrapper.find('.plan-picker [role="alert"]').exists()).toBe(false)
    expect(wrapper.findAll('.plan-picker button.plan-day-row')[2].text()).toContain('26 Oct')
    await wrapper.findAll('.plan-picker button.plan-day-row')[2].trigger('click')
    await flushPromises()
    expect(changeMenu.mock.calls[0][1]).toBe('2026-10-26')
  })

  it('blocks both week navigations throughout a save and enables them after refresh', async () => {
    const wrapper = await workspace()
    await button(wrapper, 'Plan dish').trigger('click')
    let finishSave: () => void = () => { throw new Error('No pending save') }
    const save = new Promise<void>(resolve => { finishSave = resolve })
    const implementation = changeMenu.getMockImplementation()
    if (!implementation) throw new Error('Missing save implementation')
    changeMenu.mockImplementationOnce(async (...args) => { await save; return implementation(...args) })
    await wrapper.findAll('.plan-picker button.plan-day-row')[2].trigger('click')
    for (const control of wrapper.findAll('.week-nav button')) expect(control.attributes('disabled')).toBeDefined()
    await wrapper.findAll('.plan-picker .week-nav__arrow')[1].trigger('click')
    expect(wrapper.findAll('.plan-picker button.plan-day-row')[2].text()).toContain('12 Oct')
    finishSave()
    await flushPromises()
    for (const control of wrapper.findAll('.week-nav button')) expect(control.attributes('disabled')).toBeUndefined()
  })

  it('clears obsolete opt-out confirmation on navigation while retaining the dish', async () => {
    const wrapper = await workspace()
    await button(wrapper, 'Plan dish').trigger('click')
    await wrapper.findAll('.assignment-day')[3].find('button').trigger('click')
    expect(wrapper.findAll('[role="dialog"]')).toHaveLength(2)
    await wrapper.findAll('.plan-picker .week-nav__arrow')[1].trigger('click')
    await flushPromises()
    expect(wrapper.findAll('[role="dialog"]')).toHaveLength(1)
    expect(wrapper.find('.plan-picker__title').text()).toContain('Forgotten favourite')
    expect(changeMenu).not.toHaveBeenCalled()
  })

  it.each(['family', 'account'])('closes the picker and clears its pending confirmation on %s change', async scope => {
    const wrapper = await workspace()
    await button(wrapper, 'Plan dish').trigger('click')
    await wrapper.findAll('.assignment-day')[3].find('button').trigger('click')
    if (scope === 'family') app.activeFamilyId = 'another-family'
    if (scope === 'account') account.value = 'another-account'
    await flushPromises()
    expect(wrapper.find('.plan-picker').exists()).toBe(false)
    expect(wrapper.findAll('[role="dialog"]')).toHaveLength(0)
    expect(changeMenu).not.toHaveBeenCalled()
  })

  it('retains explorer preferences, scroll position and conversation through picker navigation and reopening', async () => {
    const wrapper = await workspace()
    await wrapper.find('input[aria-label="Search dishes"]').setValue('Forgotten')
    await wrapper.find('.catalog__filter-toggle').trigger('click')
    await button(wrapper, translate('weekPlanning.efforts.Quick')).trigger('click')
    await button(wrapper, translate('weekPlanning.sorts.rating')).trigger('click')
    await wrapper.find('input[aria-label="What would you like?"]').setValue('With potatoes')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    Object.defineProperty(window, 'scrollY', { value: 420, configurable: true })
    await button(wrapper, 'Plan dish').trigger('click')
    await wrapper.findAll('.plan-picker .week-nav__arrow')[1].trigger('click')
    await flushPromises()
    expect(wrapper.find<HTMLInputElement>('input[aria-label="Search dishes"]').element.value).toBe('Forgotten')
    expect(button(wrapper, translate('weekPlanning.efforts.Quick')).attributes('aria-pressed')).toBe('true')
    expect(button(wrapper, translate('weekPlanning.sorts.rating')).attributes('color')).toBe('primary')
    expect(wrapper.find('input[aria-label="Edit preference"]').element).toHaveProperty('value', 'With potatoes')
    expect(wrapper.find('.recommendation-grid').text()).toContain('Forgotten favourite')
    expect(window.scrollY).toBe(420)
    expect(recommend).toHaveBeenCalledTimes(2)
    await button(wrapper, 'Close').trigger('click')
    await button(wrapper, 'Plan dish').trigger('click')
    expect(wrapper.findAll('.plan-picker button.plan-day-row')[2].text()).toContain('19 Oct')
  })

  it('retains overlapping selected days and clears only targets outside the new window', async () => {
    const wrapper = await workspace()
    await wrapper.findAll('.dinner-card__header')[7].trigger('click')
    await button(wrapper, translate('plan.nextWeekAriaLabel')).trigger('click')
    await flushPromises()
    expect(wrapper.findAll('.dinner-card__header')[0].attributes('aria-expanded')).toBe('true')
    await button(wrapper, translate('plan.nextWeekAriaLabel')).trigger('click')
    await flushPromises()
    expect(wrapper.findAll('.dinner-card__header').every(row => row.attributes('aria-expanded') === 'false')).toBe(true)
  })

  it('retains the chosen week when returning to the cached workspace and reopening the picker', async () => {
    const visible = shallowRef(true)
    const Host = defineComponent({ setup: () => () => h(KeepAlive, null, { default: () => visible.value ? h(Workspace) : h('div') }) })
    const wrapper = mount(Host, { global, attachTo: document.body })
    await flushPromises()
    await button(wrapper, 'Plan dish').trigger('click')
    await wrapper.findAll('.plan-picker .week-nav__arrow')[1].trigger('click')
    await flushPromises()
    await button(wrapper, 'Close').trigger('click')
    visible.value = false
    await flushPromises()
    visible.value = true
    await flushPromises()
    await button(wrapper, 'Plan dish').trigger('click')
    expect(wrapper.findAll('.plan-picker button.plan-day-row')[2].text()).toContain('19 Oct')
    expect(wrapper.findAll('.dinner-card__header')[2].text()).toContain('Oct 19')
    expect(recommend).toHaveBeenCalledTimes(1)
  })

  it('safely undoes the additive assignment in its original date after picker navigation', async () => {
    const wrapper = await workspace()
    await button(wrapper, 'Plan dish').trigger('click')
    await wrapper.findAll('.assignment-day')[2].find('button').trigger('click')
    await flushPromises()
    expect(wrapper.findAll('.assignment-day')[2].find('button').attributes('disabled')).toBeDefined()
    expect(saved[0].menu.map(item => item.dishId)).toEqual([sideId, favouriteId])
    await wrapper.findAll('.plan-picker .week-nav__arrow')[1].trigger('click')
    await flushPromises()
    await button(wrapper, 'Close').trigger('click')
    await button(wrapper, 'Undo').trigger('click')
    await flushPromises()
    expect(undo.mock.calls[0][1]).toBe(date(0))
    expect(saved.find(day => day.date === date(0))?.menu.map(item => item.dishId)).toEqual([sideId])
    expect(wrapper.findAll('.dinner-card__header')[2].text()).toContain('Oct 19')
  })

  it('clears an unsubmitted request when the family changes', async () => {
    const wrapper = await workspace()
    await wrapper.find('input[aria-label="What would you like?"]').setValue('Private family request')
    app.activeFamilyId = 'other-family'
    await flushPromises()
    expect(wrapper.find<HTMLInputElement>('input[aria-label="What would you like?"]').element.value).toBe('')
  })

  it('clears an unsubmitted request when the account changes', async () => {
    const wrapper = await workspace()
    await wrapper.find('input[aria-label="What would you like?"]').setValue('Private account request')
    account.value = 'another-account'
    await flushPromises()
    expect(wrapper.find<HTMLInputElement>('input[aria-label="What would you like?"]').element.value).toBe('')
  })

  it('submits an explicit dish-name filter and resets it with a new request', async () => {
    const wrapper = await workspace()
    const filterLabel = translate('weekPlanning.recommendationNameFilter')
    await wrapper.find(`input[aria-label="${filterLabel}"]`).setValue('Potato')
    await wrapper.find('input[aria-label="What would you like?"]').setValue('Works with fish')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(recommend.mock.calls[1][1]).toMatchObject({ nameFilter: 'Potato' })
    await wrapper.find('input[aria-label="What would you like?"]').setValue('Unsubmitted request')
    await button(wrapper, 'Start new request').trigger('click')
    await flushPromises()
    expect(wrapper.find<HTMLInputElement>(`input[aria-label="${filterLabel}"]`).element.value).toBe('')
    expect(wrapper.find<HTMLInputElement>('input[aria-label="What would you like?"]').element.value).toBe('')
  })

  it('refreshes saved data on return without losing search or reranking recommendations', async () => {
    const visible = shallowRef(true)
    const Host = defineComponent({ setup: () => () => h(KeepAlive, null, { default: () => visible.value ? h(Workspace) : h('div') }) })
    const wrapper = mount(Host, { global, attachTo: document.body })
    await flushPromises()
    await wrapper.find('input[aria-label="Search dishes"]').setValue('Forgotten')
    visible.value = false
    await flushPromises()
    saved[0].menu.push({ dishId: favouriteId })
    catalog = catalog.map(item => item.id === favouriteId ? { ...item, name: 'Updated favourite' } : item)
    visible.value = true
    await flushPromises()
    expect(wrapper.findAll('.week-day')[2].text()).toContain('Updated favourite')
    expect(wrapper.find<HTMLInputElement>('input[aria-label="Search dishes"]').element.value).toBe('Forgotten')
    expect(recommend).toHaveBeenCalledTimes(1)
    expect(readDinners).toHaveBeenCalledTimes(2)
  })

  it('restores focus to the recipe link after returning to the cached workspace', async () => {
    const visible = shallowRef(true)
    const Host = defineComponent({ setup: () => () => h(KeepAlive, null, { default: () => visible.value ? h(Workspace) : h('div', { tabindex: 0, id: 'detail' }) }) })
    const wrapper = mount(Host, { global, attachTo: document.body })
    await flushPromises()
    const recipeLink = wrapper.find<HTMLAnchorElement>('.dish-grid .planning-card__name')
    await recipeLink.trigger('click')
    visible.value = false
    await flushPromises()
    wrapper.find<HTMLElement>('#detail').element.focus()
    visible.value = true
    await flushPromises()
    expect(document.activeElement).toBe(recipeLink.element)
  })

  it.each(['NoOp', 'Conflict', 'Changed'])('only clears wish feedback for a server-confirmed change: %s', async (outcome) => {
    wishes = [{ wishId: 'wish', dishId: favouriteId, dishName: 'Forgotten favourite', addedById: 'member', addedByName: 'Member', voteCount: 2, voterIds: [], expiresAt: '2099-01-01', isVotedByCurrentUser: false }]
    const wrapper = await workspace()
    expect(wrapper.find('.dish-grid .planning-card').text()).toContain('Family wish · 2 votes')
    if (outcome !== 'Changed') changeMenu.mockResolvedValueOnce({ outcome })
    await button(wrapper, 'Plan dish').trigger('click')
    await wrapper.findAll('.assignment-day')[2].find('button').trigger('click')
    await flushPromises()
    expect(wrapper.find('.dish-grid .planning-card').text().includes('Family wish · 2 votes')).toBe(outcome !== 'Changed')
  })

  it('does not undo a dish another member already assigned before a stale save', async () => {
    const wrapper = await workspace()
    saved[0].menu.push({ dishId: favouriteId })
    await button(wrapper, 'Plan dish').trigger('click')
    await wrapper.findAll('.assignment-day')[2].find('button').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('This dish is already planned')
    expect(wrapper.findAll('button').some(button => button.text() === 'Undo')).toBe(false)
    expect(saved[0].menu.map(item => item.dishId)).toContain(favouriteId)
    expect(add).not.toHaveBeenCalled()
  })

  it('disables assignment until the dinner window is known, while keeping inspection available', async () => {
    let resolveRead: (value: PlanningDinner[]) => void = () => { throw new Error('No pending read') }
    readDinners.mockReturnValueOnce(new Promise(resolve => { resolveRead = resolve }))
    const wrapper = await workspace()
    expect(button(wrapper, 'Plan dish').attributes('disabled')).toBeDefined()
    expect(wrapper.find('.planning-card__name').attributes('href')).toBe(`/dishes/${favouriteId}`)
    resolveRead(structuredClone(saved))
    await flushPromises()
    expect(button(wrapper, 'Plan dish').attributes('disabled')).toBeUndefined()
    expect(wrapper.text()).toContain('Eating out')
  })

  it('shows catalog ratings from the list contract on their five-point scale', async () => {
    Object.assign(catalog[0], { ratingCount: 1, rating: 5 })
    Reflect.deleteProperty(catalog[0], 'ratings')
    const wrapper = await workspace()
    expect(wrapper.find('.dish-grid .planning-card [aria-label="Rating 5/5"]').exists()).toBe(true)
  })

  it('dismisses a suggestion, excludes it from more ideas and resets the request', async () => {
    const wrapper = await workspace()
    await button(wrapper, 'Dismiss Forgotten favourite').trigger('click')
    expect(wrapper.findAll('.recommendation-grid .planning-card__name')).toHaveLength(0)
    await button(wrapper, 'More ideas').trigger('click')
    await flushPromises()
    expect(recommend.mock.calls[1][1].excludedDishIds).toContain(favouriteId)
    expect(wrapper.findAll('.recommendation-grid .planning-card__name')).toHaveLength(0)
    await button(wrapper, 'Start new request').trigger('click')
    await flushPromises()
    await wrapper.find('input[aria-label="What would you like?"]').setValue('With potatoes')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(recommend.mock.calls[2][1].excludedDishIds).toEqual([])
    expect(wrapper.findAll('.recommendation-grid .planning-card__name').map(button => button.text())).toEqual(['Forgotten favourite'])
  })

  it('restores weekly explorer search, filters and sorting after leaving and remounting the page', async () => {
    const first = await workspace()
    await first.find('input[aria-label="Search dishes"]').setValue('Forgotten')
    await first.find('.catalog__filter-toggle').trigger('click')
    await button(first, translate('weekPlanning.efforts.Quick')).trigger('click')
    await button(first, translate('weekPlanning.sorts.rating')).trigger('click')
    first.unmount()
    const returned = await workspace()
    expect(returned.find<HTMLInputElement>('input[aria-label="Search dishes"]').element.value).toBe('Forgotten')
    expect(button(returned, translate('weekPlanning.efforts.Quick')).attributes('aria-pressed')).toBe('true')
    expect(button(returned, translate('weekPlanning.sorts.rating')).attributes('color')).toBe('primary')
    expect(returned.findAll('.dish-grid .planning-card__name').map(card => card.text())).toEqual(['Forgotten favourite'])
    app.activeFamilyId = 'other'
    await flushPromises()
    expect(returned.find<HTMLInputElement>('input[aria-label="Search dishes"]').element.value).toBe('')
    app.activeFamilyId = 'family'
    await flushPromises()
    expect(returned.find<HTMLInputElement>('input[aria-label="Search dishes"]').element.value).toBe('Forgotten')
    expect(button(returned, translate('weekPlanning.efforts.Quick')).attributes('aria-pressed')).toBe('true')
  })

  it('changes catalog roles without changing recommendation scope', async () => {
    const wrapper = await workspace()
    expect(wrapper.findAll('.workspace-exploration .dish-grid .planning-card__name').map(button => button.text())).toEqual(['Forgotten favourite', 'Unclassified soup'])
    await wrapper.find('.catalog__filter-toggle').trigger('click')
    await button(wrapper, translate('weekPlanning.roles.Side')).trigger('click')
    expect(wrapper.findAll('.dish-grid .planning-card__name').map(button => button.text())).toEqual(['Potato side'])
    expect(recommend).toHaveBeenCalledTimes(1)
  })

  it('links catalog and recommendation dishes to their detail page without a recipe dialog', async () => {
    const wrapper = await workspace()
    await wrapper.find('input[aria-label="Search dishes"]').setValue('Forgotten')
    for (const link of wrapper.findAll('.planning-card__name')) {
      expect(link.element.tagName).toBe('A')
      expect(link.attributes('href')).toBe(`/dishes/${favouriteId}`)
      await link.trigger('click')
    }
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
    expect(wrapper.find('input[aria-label="Search dishes"]').element).toHaveProperty('value', 'Forgotten')
  })

  it('assigns to occupied days, repeats for leftovers, removes and undoes without reranking', async () => {
    const wrapper = await workspace()
    await wrapper.find('input[aria-label="Search dishes"]').setValue('Forgotten')
    await wrapper.find('input[aria-label="What would you like?"]').setValue('Works with potatoes')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    await button(wrapper, 'Plan dish').trigger('click')
    expect(wrapper.findAll('[role="dialog"] .assignment-day')).toHaveLength(9)
    expect(wrapper.find('[role="dialog"]').text()).toContain('Potato side')
    const row = wrapper.findAll('.assignment-day')[2]
    await row.find('button').trigger('click')
    await flushPromises()
    expect(saved.find(day => day.date === date(0))?.menu.map(item => item.dishId)).toEqual([sideId, favouriteId])
    await wrapper.findAll('.assignment-day')[4].find('button').trigger('click')
    await flushPromises()
    expect(saved.find(day => day.date === date(2))?.menu.map(item => item.dishId)).toEqual([favouriteId])
    await button(wrapper, 'Close').trigger('click')
    await flushPromises()
    await button(wrapper, 'Remove Forgotten favourite').trigger('click')
    await flushPromises()
    await button(wrapper, 'Undo').trigger('click')
    await flushPromises()
    expect(saved.find(day => day.date === date(0))?.menu.map(item => item.dishId)).toEqual([sideId, favouriteId])
    expect(wrapper.find('input[aria-label="Search dishes"]').element).toHaveProperty('value', 'Forgotten')
    expect(recommend).toHaveBeenCalledTimes(2)
    expect(wrapper.find('input[aria-label="Edit preference"]').element).toHaveProperty('value', 'Works with potatoes')
  })

  it('requires confirmation before clearing an opt-out', async () => {
    const wrapper = await workspace()
    await button(wrapper, 'Plan dish').trigger('click')
    await wrapper.findAll('.assignment-day')[3].find('button').trigger('click')
    await flushPromises()
    expect(add).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Adding a dish will clear “Eating out”')
    await button(wrapper, 'Clear decision and add').trigger('click')
    await flushPromises()
    expect(saved.find(day => day.date === date(1))?.optOutReason).toBeNull()
    await button(wrapper, 'Close').trigger('click')
    await button(wrapper, 'Undo').trigger('click')
    await flushPromises()
    expect(saved.find(day => day.date === date(1))?.optOutReason).toBe('Eating out')
  })

  it('keeps cumulative requests and prior results through provider failure', async () => {
    const wrapper = await workspace()
    await wrapper.find('input[aria-label="What would you like?"]').setValue('Works with potatoes')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    providerFailure = true
    await wrapper.find('input[aria-label="What would you like?"]').setValue('No fish')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(wrapper.text()).toContain('Your previous results are still available')
    expect(wrapper.find('.recommendation-grid').text()).toContain('Forgotten favourite')
    expect(recommend.mock.calls[2][1].constraints).toEqual(['Works with potatoes', 'No fish'])
  })

  it('shows sides requested conversationally while catalog remains on mains', async () => {
    const wrapper = await workspace()
    await wrapper.find('input[aria-label="What would you like?"]').setValue('Potato side dishes')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(wrapper.find('.recommendation-grid').text()).toContain('Potato side')
    expect(wrapper.findAll('.dish-grid .planning-card__name').map(button => button.text())).toEqual(['Forgotten favourite', 'Unclassified soup'])
    await button(wrapper, 'More ideas').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('No more matching dishes remain')
  })

  it('shows nine dates with multiple dishes, opt-outs and empty days in both locales', async () => {
    saved[0].menu.push({ dishId: favouriteId })
    const wrapper = await workspace()
    expect(wrapper.findAll('.week-day')).toHaveLength(9)
    expect(wrapper.findAll('.week-day')[2].text()).toContain('Potato side')
    expect(wrapper.findAll('.week-day')[2].text()).toContain('Forgotten favourite')
    expect(wrapper.findAll('.week-day')[3].text()).toContain('Eating out')
    expect(wrapper.findAll('.week-day')[4].text()).toContain('No dinner planned')
    locale.value = 'da'
    await flushPromises()
    expect(wrapper.text()).toContain('Planlæg din uge')
    expect(wrapper.findAll('.week-day')[4].text()).toContain('Ingen aftensmad planlagt')
  })

  it('names archived saved dishes without offering them in exploration', async () => {
    catalog[1].isArchived = true
    const wrapper = await workspace()
    expect(wrapper.findAll('.week-day')[2].text()).toContain('Potato side')
    await wrapper.find('.catalog__filter-toggle').trigger('click')
    await button(wrapper, translate('weekPlanning.roles.All')).trigger('click')
    expect(wrapper.findAll('.dish-grid .planning-card__name').map(button => button.text())).not.toContain('Potato side')
  })

  it.each(['en', 'da'])('keeps saved dinners visible with an empty catalog in %s', async language => {
    locale.value = language
    catalog = []
    recommend.mockResolvedValueOnce({ outcome: 'NoMatch', dishes: [], activeConstraints: [], contextSummary: '', message: null })
    const wrapper = await workspace()
    expect(wrapper.text()).toContain(translate('weekPlanning.emptyCatalog'))
    expect(wrapper.findAll('.planning-card__name')).toHaveLength(0)
    expect(wrapper.findAll('.week-day')).toHaveLength(9)
    expect(wrapper.findAll('.week-day')[2].text()).toContain(translate('weekPlanning.dishUnavailable'))
    expect(add).not.toHaveBeenCalled()
    expect(remove).not.toHaveBeenCalled()
  })

  it('shows undo conflict and keeps exploration open', async () => {
    const wrapper = await workspace()
    await button(wrapper, 'Plan dish').trigger('click')
    await wrapper.findAll('.assignment-day')[2].find('button').trigger('click')
    await flushPromises()
    await button(wrapper, 'Close').trigger('click')
    undoConflict = true
    await button(wrapper, 'Undo').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('The day has changed')
    expect(wrapper.find('.dish-grid').exists()).toBe(true)
  })

  it('links saved dishes to their detail pages without selecting the day', async () => {
    const wrapper = await workspace()
    const link = wrapper.findAll('.week-day')[2].find('a')
    expect(link.attributes('href')).toBe(`/dishes/${sideId}`)
    await link.trigger('click')
    expect(wrapper.findAll('.dinner-card__header')[2].attributes('aria-expanded')).toBe('false')
  })

  it('requires an explicit day-scope decision and never automatically advances the selected day', async () => {
    const wrapper = await workspace()
    const dayButtons = wrapper.findAll('.dinner-card__header')
    await dayButtons[2].trigger('click')
    await wrapper.find('input[aria-label="What would you like?"]').setValue('Works with potatoes')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(recommend.mock.calls[1][1]).toMatchObject({ targetDate: date(0) })
    const addButton = wrapper.findAll('.planning-card__actions button').find(button => button.text().startsWith('Add to'))
    if (!addButton) throw new Error('Selected-day action missing')
    await addButton.trigger('click')
    await flushPromises()
    expect(dayButtons[2].attributes('aria-expanded')).toBe('true')
    await dayButtons[4].trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('You selected another day')
    await button(wrapper, 'Keep preferences').trigger('click')
    await wrapper.find('input[aria-label="What would you like?"]').setValue('No fish')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(recommend.mock.calls[2][1]).toMatchObject({ targetDate: date(2), constraints: ['Works with potatoes', 'No fish'] })
    await button(wrapper, 'Start new request').trigger('click')
    await flushPromises()
    expect(wrapper.findAll('input[aria-label="Edit preference"]')).toHaveLength(0)
  })

  it('keeps both weekend targets visible after navigation and exposes the mobile overview in place', async () => {
    const wrapper = await workspace()
    const toggle = wrapper.find('.mobile-week-control')
    expect(toggle.attributes('aria-expanded')).toBe('false')
    await toggle.trigger('click')
    expect(toggle.attributes('aria-expanded')).toBe('true')
    expect(wrapper.find('.workspace-week').classes()).toContain('workspace-week--open')
    await button(wrapper, translate('plan.nextWeekAriaLabel')).trigger('click')
    await flushPromises()
    expect(wrapper.findAll('.dinner-card__header')[0].text()).toContain(monday.plus({ days: 5 }).setLocale('en').toFormat('MMM d'))
    expect(wrapper.findAll('.dinner-card__header')[8].text()).toContain(monday.plus({ days: 13 }).setLocale('en').toFormat('MMM d'))
    await button(wrapper, translate('plan.previousWeekAriaLabel')).trigger('click')
    await flushPromises()
    expect(wrapper.findAll('.dinner-card__header')[0].text()).toContain(monday.plus({ days: -2 }).setLocale('en').toFormat('MMM d'))
  })
})

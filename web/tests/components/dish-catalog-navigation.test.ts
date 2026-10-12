import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { computed, defineComponent, h, onMounted, onScopeDispose, reactive, ref, shallowRef, watch } from 'vue'
import { DateTime } from 'luxon'
import Catalog from '~/pages/dishes/index.vue'
import type { Dish } from '~/types'
import { catalogPreferencesKey, defaultCatalogPreferences } from '~/utils/dish-catalog-preferences'
import english from '~/i18n/locales/en.json'
import danish from '~/i18n/locales/da.json'

enableAutoUnmount(afterEach)
afterEach(() => vi.unstubAllGlobals())
const app = reactive({ activeFamilyId: 'family' })
const account = shallowRef('account')
const locale = shallowRef('en')
const read = vi.fn()
const usage = vi.fn().mockResolvedValue({})
const populateDishes = vi.fn().mockResolvedValue(undefined)
const Block = defineComponent({ setup: (_, { slots }) => () => h('div', slots.default?.()) })
const Card = defineComponent({ props: ['dish', 'dishStats'], setup: props => () => h('article', props.dish.name) })
const Field = defineComponent({ props: ['modelValue'], emits: ['update:modelValue'], setup: (props, { emit }) => () => h('input', { value: props.modelValue, onInput: (event: Event) => emit('update:modelValue', (event.target as HTMLInputElement).value) }) })
const Chip = defineComponent({ setup: (_, { slots }) => () => h('button', slots.default?.()) })

function translate(key: string): string {
  let value: unknown = locale.value === 'da' ? danish : english
  for (const part of key.split('.')) {
    if (typeof value !== 'object' || value === null) return key
    value = Reflect.get(value, part)
  }
  return typeof value === 'string' ? value : key
}

function dish(id: string, rating: number, archived = false): Dish {
  return { id, name: `Rice ${id}`, rating, isArchived: archived, roles: ['Main'], effortLevel: 'Quick', seasonAffinity: 'AllYear', cuisine: 'Italian', dates: [], ratings: [], notes: '', url: '', dishStats: { dishId: id, timesUsed: 0, lastUsed: undefined } }
}

function page() {
  return mount(Catalog, { global: { mocks: { $t: (key: string) => key }, stubs: {
    Content: Block, VBtn: Chip, VChip: Chip, VTextField: Field, DishCard: Card,
    VDialog: true, VCard: Block, VCardTitle: Block, VCardText: Block, VCardActions: Block,
    VSpacer: Block, VSheet: Block, VBottomSheet: true,
    VSkeletonLoader: Block, VIcon: Block, EmptyState: Block,
    PlanAssistantPanel: true, PlanAssistantMobileSheet: true,
  } } })
}

beforeEach(() => {
  localStorage.clear()
  setActivePinia(createPinia())
  app.activeFamilyId = 'family'
  account.value = 'account'
  locale.value = 'en'
  read.mockReset().mockResolvedValue([dish('a', 2), dish('b', 4)])
  usage.mockReset().mockResolvedValue({})
  populateDishes.mockClear()
  const boundaries = {
    computed, ref, shallowRef, watch, onMounted, onScopeDispose,
    useHead: () => {},
    useAppStore: () => app,
    useDishesStore: () => ({ populateDishes, populateStats: async () => {} }),
    useDinnersStore: () => ({ populateDinners: async () => {} }),
    useWishlistStore: () => ({ fetchWishes: async () => {} }),
    useRepositories: () => ({ dishes: { all: read, allUsageStats: usage } }),
    useSnackbar: () => ({ show: () => {} }),
    useI18n: () => ({ t: translate, locale }),
    useWeekNav: () => ({ weekStart: shallowRef(DateTime.local().startOf('week')) }),
    useDisplay: () => ({ mdAndUp: shallowRef(false) }),
    useNuxtApp: () => ({ $msal: { isAuthenticated: shallowRef(true), getObjectId: () => account.value } }),
  }
  Object.entries(boundaries).forEach(([name, value]) => vi.stubGlobal(name, value))
})

describe('catalog navigation uses remembered controls with fresh data', () => {
  it('preserves multiple selected roles through the shared filter controls', async () => {
    read.mockResolvedValue([dish('a', 2), dish('b', 4), { ...dish('side', 3), roles: ['Side'] }])
    const wrapper = page()
    await flushPromises()
    await wrapper.find('.catalog__filter-toggle').trigger('click')
    await wrapper.find('input').setValue('Rice')
    await wrapper.findAll('.sort-chip')[1].trigger('click')
    const roles = wrapper.findAll('.filter-tag--role')
    await roles[0].trigger('click')
    expect(wrapper.findAll('article').map(card => card.text())).toEqual(['Rice b', 'Rice a'])
    await roles[1].trigger('click')
    expect(roles.map(role => role.attributes('aria-pressed'))).toEqual(['true', 'true'])
    expect(wrapper.findAll('article').map(card => card.text())).toEqual(['Rice b', 'Rice side', 'Rice a'])
    await wrapper.find('.filter-clear').trigger('click')
    expect(roles.map(role => role.attributes('aria-pressed'))).toEqual(['false', 'false'])
    expect(wrapper.find('input').element.value).toBe('Rice')
    expect(wrapper.findAll('.sort-chip')[1].text()).toContain('↓')
  })

  it('keeps dishes without serving history last in both last-used sort directions', async () => {
    read.mockResolvedValue([dish('unknown', 2), dish('recent', 4), dish('old', 3)])
    usage.mockResolvedValue({
      recent: { dishId: 'recent', timesUsed: 1, lastUsed: DateTime.fromISO('2026-09-01') },
      old: { dishId: 'old', timesUsed: 2, lastUsed: DateTime.fromISO('2026-01-01') },
    })
    const wrapper = page()
    await flushPromises()
    await wrapper.findAll('.sort-chip')[3].trigger('click')
    expect(wrapper.findAll('article').map(card => card.text())).toEqual(['Rice recent', 'Rice old', 'Rice unknown'])
    await wrapper.findAll('.sort-chip')[3].trigger('click')
    expect(wrapper.findAll('article').map(card => card.text())).toEqual(['Rice old', 'Rice recent', 'Rice unknown'])
  })

  it('updates shared control labels when the locale changes', async () => {
    const wrapper = page()
    await flushPromises()
    expect(wrapper.find('.catalog__sort').attributes('aria-label')).toBe('Sort by')
    expect(wrapper.find('input').attributes('aria-label')).toBe('Find a dish…')
    locale.value = 'da'
    await flushPromises()
    expect(wrapper.find('.catalog__sort').attributes('aria-label')).toBe('Sortér efter')
    expect(wrapper.find('.catalog__filter-body').attributes('aria-label')).toBe('Filtrér efter')
    expect(wrapper.findAll('.sort-chip')[0].text()).toContain(translate('dishes.sortName'))
    expect(wrapper.find('input').attributes('aria-label')).toBe(translate('dishes.findADish'))
  })

  it('restores search/filter/sort after route remount and sorts newly fetched dishes', async () => {
    const first = page()
    await flushPromises()
    await first.find('input').setValue('Rice')
    await first.find('.filter-tag--role').trigger('click')
    await first.findAll('.sort-chip')[1].trigger('click')
    expect(first.findAll('article').map(card => card.text())).toEqual(['Rice b', 'Rice a'])
    first.unmount()
    read.mockResolvedValue([dish('a', 5), dish('b', 1)])
    const returned = page()
    await flushPromises()
    expect(returned.find('input').element.value).toBe('Rice')
    expect(returned.find('.filter-tag--role').attributes('aria-pressed')).toBe('true')
    expect(returned.findAll('.sort-chip')[1].text()).toContain('\u2193')
    expect(returned.findAll('article').map(card => card.text())).toEqual(['Rice a', 'Rice b'])
    expect(read).toHaveBeenCalledTimes(2)
  })

  it('restores archive mode before the first fetch and keeps the shared dish store active-only', async () => {
    localStorage.setItem(catalogPreferencesKey('account', 'family')!, JSON.stringify({ version: 1, ...defaultCatalogPreferences(), showArchived: true, roles: ['Main'] }))
    read.mockResolvedValue([dish('active', 5), dish('archived', 1, true)])
    const wrapper = page()
    await flushPromises()
    expect(read).toHaveBeenCalledWith('family', true)
    expect(populateDishes).toHaveBeenCalledWith()
    expect(wrapper.findAll('article').map(card => card.text())).toEqual(['Rice archived'])
    expect(wrapper.find('.filter-tag--role').attributes('aria-pressed')).toBe('true')
    await wrapper.find('.filter-clear').trigger('click')
    await flushPromises()
    expect(read).toHaveBeenLastCalledWith('family', false)
    expect(wrapper.find('.filter-tag--archive').attributes('aria-pressed')).toBe('false')
  })

  it('rejects a response from the previous family instead of pruning the current filters', async () => {
    let finishPrevious!: (dishes: Dish[]) => void
    read.mockImplementationOnce(() => new Promise<Dish[]>(resolve => { finishPrevious = resolve }))
    const wrapper = page()
    app.activeFamilyId = 'other'
    await flushPromises()
    await wrapper.find('.filter-tag--role').trigger('click')
    finishPrevious([])
    await flushPromises()
    expect(wrapper.findAll('article')).toHaveLength(2)
    expect(wrapper.find('.filter-tag--role').attributes('aria-pressed')).toBe('true')
  })
})

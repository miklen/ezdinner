import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { computed, defineComponent, h, onMounted, onScopeDispose, reactive, ref, shallowRef, watch } from 'vue'
import { DateTime } from 'luxon'
import Catalog from '~/pages/dishes/index.vue'
import type { Dish } from '~/types'
import { catalogPreferencesKey, defaultCatalogPreferences } from '~/utils/dish-catalog-preferences'

enableAutoUnmount(afterEach)
afterEach(() => vi.unstubAllGlobals())
const app = reactive({ activeFamilyId: 'family' })
const account = shallowRef('account')
const read = vi.fn()
const usage = vi.fn().mockResolvedValue({})
const populateDishes = vi.fn().mockResolvedValue(undefined)
const Block = defineComponent({ setup: (_, { slots }) => () => h('div', slots.default?.()) })
const Card = defineComponent({ props: ['dish', 'dishStats'], setup: props => () => h('article', props.dish.name) })
const Field = defineComponent({ props: ['modelValue'], emits: ['update:modelValue'], setup: (props, { emit }) => () => h('input', { value: props.modelValue, onInput: (event: Event) => emit('update:modelValue', (event.target as HTMLInputElement).value) }) })
const Chip = defineComponent({ setup: (_, { slots }) => () => h('button', slots.default?.()) })

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
  read.mockReset().mockResolvedValue([dish('a', 2), dish('b', 4)])
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
    useI18n: () => ({ t: (key: string) => key }),
    useWeekNav: () => ({ weekStart: shallowRef(DateTime.local().startOf('week')) }),
    useDisplay: () => ({ mdAndUp: shallowRef(false) }),
    useNuxtApp: () => ({ $msal: { isAuthenticated: shallowRef(true), getObjectId: () => account.value } }),
  }
  Object.entries(boundaries).forEach(([name, value]) => vi.stubGlobal(name, value))
})

describe('catalog navigation uses remembered controls with fresh data', () => {
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

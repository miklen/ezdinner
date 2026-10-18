import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { computed, defineComponent, h, nextTick, ref, watch } from 'vue'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import { createI18n, useI18n } from 'vue-i18n'
import { DateTime } from 'luxon'
import PlannedDinnerDetails from '~/components/Plan/PlannedDinnerDetails.vue'
import PlanDishRow from '~/components/Plan/DishRow.vue'
import type { Dinner, Dish } from '~/types'
import english from '~/i18n/locales/en.json'
import danish from '~/i18n/locales/da.json'

vi.mock('vuetify', async importOriginal => {
  const actual = await importOriginal<typeof import('vuetify')>()
  return { ...actual, useDisplay: () => ({ smAndDown: mobile }) }
})

const mobile = ref(false)
const create = vi.fn(async () => 'new-dish')
const enrich = vi.fn(async () => {})
const updateDish = vi.fn(async () => {})
const addDishToMenu = vi.fn(async () => {})
const catalog = ref<Dish[]>([])
const date = DateTime.fromISO('2026-10-18')
const scrollIntoView = HTMLElement.prototype.scrollIntoView
const dinner: Dinner = { date, menu: [], description: '', isPlanned: false, isOptedOut: false, optOutReason: null, isResolved: false }
const Assignment = defineComponent({ setup: (_, { slots }) => () => h('div', slots.assignment?.()) })
const translations = () => createI18n({ legacy: false, locale: 'en', messages: { en: english, da: danish } })
let i18n = translations()

function dish(id: string, name: string): Dish {
  return { id, name, rating: 0, ratings: [], dates: [], notes: '', url: '', isArchived: false, dishStats: { dishId: id, timesUsed: 0, lastUsed: undefined } }
}

beforeEach(() => {
  vi.clearAllMocks()
  mobile.value = false
  i18n = translations()
  catalog.value = [dish('lasagne', 'Lasagne'), dish('soup', 'Tomato soup')]
  vi.stubGlobal('ResizeObserver', class { observe() {} unobserve() {} disconnect() {} })
  vi.stubGlobal('IntersectionObserver', class { observe() {} unobserve() {} disconnect() {} })
  vi.stubGlobal('visualViewport', new EventTarget())
  HTMLElement.prototype.scrollIntoView = vi.fn()
  vi.spyOn(HTMLElement.prototype, 'offsetParent', 'get').mockImplementation(function (this: HTMLElement) { return this.parentElement })
  for (const [name, value] of Object.entries({ ref, computed, watch, nextTick, useI18n,
    useAppStore: () => ({ activeFamilyId: 'family' }),
    useDishesStore: () => ({ get dishes() { return catalog.value }, updateDish }),
    useDinnersStore: () => ({ previousOptOutReasons: [] }),
    useWishlistStore: () => ({ wishes: [] }),
    useSnackbar: () => ({ show: vi.fn() }),
    useRepositories: () => ({ dishes: { create, enrich }, dinners: { addDishToMenu } }),
  })) vi.stubGlobal(name, value)
})

afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks(); HTMLElement.prototype.scrollIntoView = scrollIntoView })
enableAutoUnmount(afterEach)

async function picker(query: string) {
  const wrapper = mount(PlannedDinnerDetails, { props: { dinner }, attachTo: document.body, global: {
    plugins: [i18n, createVuetify({ components })],
    components: { PlanDishRow },
    stubs: { DinnerDayDetails: Assignment },
  } })
  if (mobile.value) await wrapper.get('button').trigger('click')
  await flushPromises()
  const input = mobile.value ? document.querySelector('.sheet-search input') : wrapper.get('input').element
  if (!(input instanceof HTMLInputElement)) throw new Error('Missing dish search input')
  input.focus()
  await flushPromises()
  input.value = query
  input.dispatchEvent(new Event('input', { bubbles: true }))
  await flushPromises()
  return { wrapper, input }
}

async function enter(input: HTMLInputElement) {
  input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }))
  input.dispatchEvent(new KeyboardEvent('keyup', { key: 'Enter', bubbles: true }))
  await flushPromises()
}

function options() {
  return [...document.querySelectorAll<HTMLElement>('[role="option"]')]
}

describe('planning dish picker', () => {
  it('assigns the matching dish when Enter is pressed without navigating the results', async () => {
    const { wrapper, input } = await picker('Lasa')
    await enter(input)
    expect(create).not.toHaveBeenCalled()
    expect(addDishToMenu).toHaveBeenCalledTimes(1)
    expect(addDishToMenu).toHaveBeenCalledWith('family', date, 'lasagne')
    expect(wrapper.emitted('dinner:menuupdated')).toEqual([[{ date, dishId: 'lasagne', dishName: 'Lasagne' }]])
  })

  it('does not create a dish when tabbing away from an unmatched search', async () => {
    const { input } = await picker('Risotto')
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true }))
    await flushPromises()
    expect(create).not.toHaveBeenCalled()
    expect(addDishToMenu).not.toHaveBeenCalled()
  })

  it('restores the mobile list when the search is cleared', async () => {
    mobile.value = true
    await picker('Risotto')
    const clear = document.querySelector<HTMLElement>('[aria-label^="Clear"]')
    if (!clear) throw new Error('Missing clear search control')
    clear.click()
    await flushPromises()
    expect(options().map(option => option.textContent?.trim())).toEqual(['Lasagne —', 'Tomato soup —'])
    expect(create).not.toHaveBeenCalled()
  })

  describe.each([false, true])('on mobile=%s', isMobile => {
    beforeEach(() => { mobile.value = isMobile })

    it('highlights the first matching dish while typing', async () => {
      await picker('Lasa')
      expect(options()[0]?.textContent).toContain('Lasagne')
      expect(options()[0]?.classList.contains('v-list-item--active')).toBe(true)
    })

    it('assigns the first match without creating a dish', async () => {
      const { input } = await picker('Lasa')
      await enter(input)
      expect(create).not.toHaveBeenCalled()
      expect(addDishToMenu).toHaveBeenCalledTimes(1)
      expect(addDishToMenu).toHaveBeenCalledWith('family', date, 'lasagne')
    })

    it('offers a single highlighted create choice when nothing matches', async () => {
      const { input } = await picker('  Risotto  ')
      expect(options()).toHaveLength(1)
      expect(options()[0]?.textContent).toContain('Risotto')
      expect(options()[0]?.classList.contains('v-list-item--active')).toBe(true)
      await enter(input)
      expect(create).toHaveBeenCalledTimes(1)
      expect(create).toHaveBeenCalledWith('family', 'Risotto')
      expect(enrich).toHaveBeenCalledWith('family', 'new-dish')
      expect(updateDish).toHaveBeenCalledWith('new-dish')
      expect(addDishToMenu).toHaveBeenCalledTimes(1)
      expect(addDishToMenu).toHaveBeenCalledWith('family', date, 'new-dish')
    })

    it('hides creation for an existing name regardless of case and whitespace', async () => {
      const { input } = await picker('  lASAGNE  ')
      expect(options()).toHaveLength(1)
      expect(options()[0]?.textContent).toContain('Lasagne')
      await enter(input)
      expect(create).not.toHaveBeenCalled()
      expect(addDishToMenu).toHaveBeenCalledWith('family', date, 'lasagne')
    })

    it('does not offer creation for whitespace', async () => {
      await picker('   ')
      expect(options()).toHaveLength(2)
      expect(options().some(option => option.classList.contains('create-dish-item'))).toBe(false)
    })

    it('assigns the clicked result rather than the first result', async () => {
      await picker('a')
      const soup = options().find(option => option.textContent?.includes('Tomato soup'))
      if (!soup) throw new Error('Missing soup option')
      soup.click()
      await flushPromises()
      expect(addDishToMenu).toHaveBeenCalledTimes(1)
      expect(addDishToMenu).toHaveBeenCalledWith('family', date, 'soup')
      expect(create).not.toHaveBeenCalled()
    })

    it('shows the create choice in Danish', async () => {
      i18n.global.locale.value = 'da'
      await picker('Risotto')
      expect(options()[0]?.textContent?.trim()).toBe(i18n.global.t('plan.createDish', { name: 'Risotto' }))
    })

    it('assigns the result reached with ArrowDown', async () => {
      const { input } = await picker('a')
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true }))
      await flushPromises()
      if (!isMobile) {
        document.activeElement?.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true }))
        await flushPromises()
      }
      const focused = document.activeElement
      if (!(focused instanceof HTMLElement)) throw new Error('Missing focused choice')
      focused.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }))
      focused.dispatchEvent(new KeyboardEvent('keyup', { key: 'Enter', bubbles: true }))
      await flushPromises()
      expect(addDishToMenu).toHaveBeenCalledWith('family', date, 'soup')
      expect(create).not.toHaveBeenCalled()
    })

    it('does not create a dish while confirming composed text', async () => {
      const { input } = await picker('Risotto')
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', isComposing: true, bubbles: true, cancelable: true }))
      await flushPromises()
      expect(create).not.toHaveBeenCalled()
    })
  })
})

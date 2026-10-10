import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { computed, defineComponent, h, ref, shallowRef, watch } from 'vue'
import { DateTime } from 'luxon'
import AssignmentPicker from '~/components/WeekPlanning/PlanningAssignmentPicker.vue'
import DetailPicker from '~/components/Dish/PlanDishDialog.vue'
import PlanDayRow from '~/components/Dish/PlanDayRow.vue'
import WeekNav from '~/components/Plan/WeekNav.vue'
import PickerShell from '~/components/Plan/PickerShell.vue'
import { useWeekNav } from '~/composables/useWeekNav'
import { planningWindow } from '~/composables/useWeekPlanning'
import english from '~/i18n/locales/en.json'
import danish from '~/i18n/locales/da.json'

const display = vi.hoisted(() => ({ mobile: false, height: 800 }))
let swipeEnd: (event: Event, direction: string) => void
vi.mock('@vueuse/core', () => ({ useSwipe: (_target: unknown, options: { onSwipeEnd: typeof swipeEnd }) => { swipeEnd = options.onSwipeEnd } }))
vi.mock('vuetify', () => ({ useDisplay: () => ({ smAndDown: computed(() => display.mobile), height: computed(() => display.height) }) }))
enableAutoUnmount(afterEach)
const locale = shallowRef('en')
const monday = '2026-10-12'
const name = 'Very long family favourite with vegetables and a homemade tomato sauce'
const dishId = 'dish'
const getRange = vi.fn().mockResolvedValue([])
const add = vi.fn().mockResolvedValue(undefined)
const remove = vi.fn().mockResolvedValue(undefined)
const show = vi.fn()
const Block = defineComponent({ setup: (_, { slots }) => () => h('div', slots.default?.()) })
const Overlay = defineComponent({ props: { modelValue: Boolean }, emits: ['update:modelValue', 'afterLeave'], setup: (props, { slots, emit }) => { watch(() => props.modelValue, open => { if (!open) emit('afterLeave') }); return () => props.modelValue ? h('div', { role: 'dialog' }, slots.default?.()) : null } })
const global = { mocks: { $t: translate }, components: { PlanWeekNav: WeekNav, DishPlanDayRow: PlanDayRow }, stubs: { NuxtLink: Block, VDialog: Overlay, VBottomSheet: Overlay, VCard: Block, VIcon: Block, VProgressCircular: Block, VAlert: Block, VBtn: Block } }

function translate(key: string, parameters: Record<string, unknown> = {}) {
  let value: unknown = locale.value === 'en' ? english : danish
  for (const part of key.split('.')) {
    if (typeof value !== 'object' || value === null) return key
    value = Reflect.get(value, part)
  }
  if (typeof value !== 'string') return key
  return Object.entries(parameters).reduce((text, [key, replacement]) => text.replace(`{${key}}`, String(replacement)), value)
}

beforeEach(() => {
  vi.clearAllMocks()
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(DateTime.fromISO('2026-10-10').toJSDate())
  locale.value = 'en'
  display.mobile = false
  display.height = 800
  vi.stubGlobal('useI18n', () => ({ t: translate, locale }))
  vi.stubGlobal('useWeekNav', useWeekNav)
  vi.stubGlobal('computed', computed)
  vi.stubGlobal('ref', ref)
  vi.stubGlobal('shallowRef', shallowRef)
  vi.stubGlobal('watch', watch)
  vi.stubGlobal('useRepositories', () => ({ dinners: { getRange, addDishToMenu: add, removeDishFromMenu: remove } }))
  vi.stubGlobal('useDishesStore', () => ({ populateDishes: async () => {}, dishMap: {} }))
  vi.stubGlobal('useSnackbar', () => ({ show }))
})
afterEach(() => { vi.useRealTimers() })

describe.each([false, true])('shared planning presentation with mobile=%s', mobile => {
  beforeEach(() => { display.mobile = mobile })

  it('restores focus to the initiating action after closing and renders optional footer content', async () => {
    const Host = defineComponent({ setup() {
      const open = shallowRef(false)
      return () => h('div', [h('button', { id: 'open-picker', onClick: () => { open.value = true } }, 'Open'), h(PickerShell, { open: open.value, title: name, closeLabel: 'Close', 'onUpdate:open': (value: boolean) => { open.value = value } }, { default: () => h('button', { id: 'day-action' }, 'Day'), navigation: () => h('div', 'Week'), footer: () => h('div', 'Footer') })])
    } })
    const wrapper = mount(Host, { global, attachTo: document.body })
    const opener = wrapper.find<HTMLButtonElement>('#open-picker')
    opener.element.focus()
    await opener.trigger('click')
    expect(wrapper.find('.plan-picker__footer').text()).toBe('Footer')
    wrapper.find<HTMLButtonElement>('#day-action').element.focus()
    await wrapper.find('.plan-picker__close').trigger('click')
    await flushPromises()
    expect(document.activeElement).toBe(opener.element)
  })

  it('shows all nine workspace dates, saved pills, opt-outs and a reachable close control', async () => {
    const days = planningWindow(monday).map(date => ({ date, menu: date === monday ? [{ dishId, dishName: name }] : [], optOutReason: date === '2026-10-13' ? 'Eating out' : null }))
    const wrapper = mount(AssignmentPicker, { props: { open: true, monday, dishId, dishName: name, days, busy: false, loading: false, canMutate: true }, global })
    expect(wrapper.find('.plan-picker').classes()).toContain(mobile ? 'plan-picker--sheet' : 'plan-picker--dialog')
    expect(wrapper.find('.plan-picker__title').text()).toContain(name)
    expect(wrapper.findAll('.plan-day-row')).toHaveLength(9)
    expect(wrapper.findAll('.plan-day-row--weekend')).toHaveLength(4)
    expect(wrapper.find('.plan-picker__divider').exists()).toBe(true)
    expect(wrapper.text()).toContain('Eating out')
    expect(wrapper.text()).toContain(name)
    await wrapper.findAll('button.plan-day-row')[4].trigger('click')
    expect(wrapper.emitted('assign')).toEqual([['2026-10-14']])
    expect(wrapper.findAll('button.plan-day-row')[2].attributes('disabled')).toBeDefined()
    await wrapper.find('.plan-picker__close').trigger('click')
    expect(wrapper.emitted('close')).toEqual([[]])
    await wrapper.setProps({ open: false })
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
  })

  it('preserves the detail picker date navigation and additive toggle/remove actions', async () => {
    const wrapper = mount(DetailPicker, { props: { modelValue: false, dishId, dishName: name, familyId: 'family' }, global })
    await wrapper.setProps({ modelValue: true })
    await flushPromises()
    expect(wrapper.find('.plan-picker').classes()).toContain(mobile ? 'plan-picker--sheet' : 'plan-picker--dialog')
    expect(wrapper.findAll('button.plan-day-row')).toHaveLength(9)
    await wrapper.findAll('button.plan-day-row')[2].trigger('click')
    await flushPromises()
    expect(add.mock.calls[0][1].toFormat('yyyy-MM-dd')).toBe(monday)
    const plannedDate = wrapper.emitted('planned')?.[0][0]
    expect(DateTime.isDateTime(plannedDate) && plannedDate.toFormat('yyyy-MM-dd')).toBe(monday)
    await wrapper.findAll('button.plan-day-row')[2].trigger('click')
    await flushPromises()
    expect(remove.mock.calls[0][1].toFormat('yyyy-MM-dd')).toBe(monday)
    await wrapper.findAll('.week-nav__arrow')[1].trigger('click')
    await flushPromises()
    expect(wrapper.findAll('button.plan-day-row')[2].text()).toContain('19 Oct')
    await wrapper.find('.plan-picker__close').trigger('click')
    expect(wrapper.emitted('update:modelValue')).toEqual([[false]])
  })
})

describe('planning day action labels', () => {
  it('shows an assigned indicator without offering to remove a workspace duplicate', () => {
    const wrapper = mount(PlanDayRow, { props: { date: DateTime.fromISO(monday), dishName: name, isPlanned: true, alreadyAssigned: true, disabled: true }, global })
    expect(wrapper.attributes('aria-label')).toBe(translate('weekPlanning.alreadyAssignedDay', { dish: name, day: 'Monday 12 Oct' }))
    expect(wrapper.text()).toContain('mdi-check-circle')
    expect(wrapper.text()).not.toContain('mdi-close-circle')
  })

  it('keeps the existing detail toggle removal label by default', async () => {
    const wrapper = mount(PlanDayRow, { props: { date: DateTime.fromISO(monday), dishName: name, isPlanned: true }, global })
    expect(wrapper.attributes('aria-label')).toBe(translate('dishes.planDish.removeDayAriaLabel', { dish: name, day: 'Monday 12 Oct' }))
    expect(wrapper.text()).toContain('mdi-close-circle')
    await wrapper.trigger('click')
    expect(wrapper.emitted('toggle')).toEqual([[]])
  })
})

describe('picker week navigation and read feedback', () => {
  const days = planningWindow(monday).map(date => ({ date, menu: [], optOutReason: null }))

  it('emits navigation offsets without changing the selected dish', async () => {
    const wrapper = mount(AssignmentPicker, { props: { open: true, monday, dishId, dishName: name, days, busy: false, loading: false, canMutate: true }, global })
    await wrapper.findAll('.week-nav__arrow')[0].trigger('click')
    await wrapper.findAll('.week-nav__arrow')[1].trigger('click')
    await wrapper.find('.week-nav__center').trigger('click')
    expect(wrapper.emitted('navigate')).toEqual([[-1], [1], [-1]])
    expect(wrapper.find('.plan-picker__title').text()).toContain(name)
  })

  it('replaces unknown days with skeletons and localized loading feedback', async () => {
    const wrapper = mount(AssignmentPicker, { props: { open: true, monday, dishId, dishName: name, days, busy: false, loading: true, canMutate: false }, global })
    expect(wrapper.findAll('.plan-day-row--skeleton')).toHaveLength(9)
    expect(wrapper.findAll('button.plan-day-row')).toHaveLength(0)
    expect(wrapper.find('[role="status"]').attributes('aria-label')).toBe(translate('weekPlanning.loading'))
    locale.value = 'da'
    await flushPromises()
    expect(wrapper.find('.plan-picker__title').text()).toBe(translate('weekPlanning.assignTitle', { name }))
    expect(wrapper.find('.plan-picker__close').attributes('aria-label')).toBe(translate('weekPlanning.close'))
    expect(wrapper.find('[role="status"]').attributes('aria-label')).toBe(translate('weekPlanning.loading'))
    expect(wrapper.find('.week-nav__label').text()).toBe(translate('plan.nextWeek'))
    expect(wrapper.findAll('.week-nav__arrow')[0].attributes('aria-label')).toBe(translate('plan.previousWeekAriaLabel'))
    await wrapper.setProps({ loading: false, error: 'readFailed' })
    expect(wrapper.find('[role="alert"]').text()).toContain(translate('weekPlanning.readFailed'))
    expect(wrapper.findAll('button.plan-day-row')).toHaveLength(0)
    await wrapper.find('[data-testid="picker-retry"]').trigger('click')
    expect(wrapper.emitted('retry')).toEqual([[]])
    locale.value = 'en'
    await flushPromises()
    expect(wrapper.find('[role="alert"]').text()).toContain(translate('weekPlanning.readFailed'))
    expect(wrapper.find('[data-testid="picker-retry"]').text()).toBe(translate('weekPlanning.retry'))
    await wrapper.setProps({ canMutate: true, error: null })
    expect(wrapper.findAll('button.plan-day-row')[2].text()).toContain('Mon12 Oct')
    locale.value = 'da'
    await flushPromises()
    expect(wrapper.findAll('button.plan-day-row')[2].text()).toContain('man.12 okt.')
    expect(wrapper.props('dishName')).toBe(name)
    expect(wrapper.props('monday')).toBe(monday)
  })
})

describe('shared week navigation mutation guard', () => {
  it('disables arrows, center and swipe only when requested', async () => {
    const wrapper = mount(WeekNav, { props: { modelValue: DateTime.fromISO(monday), disabled: true }, global })
    for (const button of wrapper.findAll('button')) {
      expect(button.attributes('disabled')).toBeDefined()
      await button.trigger('click')
    }
    swipeEnd(new Event('touchend'), 'left')
    swipeEnd(new Event('touchend'), 'right')
    expect(wrapper.emitted('update:modelValue')).toBeUndefined()
    await wrapper.setProps({ disabled: false })
    swipeEnd(new Event('touchend'), 'left')
    const nextWeek = wrapper.emitted('update:modelValue')?.[0][0]
    expect(DateTime.isDateTime(nextWeek) && nextWeek.toFormat('yyyy-MM-dd')).toBe('2026-10-19')
  })

  it('leaves Plan and detail navigation enabled by default', async () => {
    const wrapper = mount(WeekNav, { props: { modelValue: DateTime.fromISO(monday) }, global })
    for (const button of wrapper.findAll('button')) expect(button.attributes('disabled')).toBeUndefined()
    await wrapper.find('.week-nav__center').trigger('click')
    const currentWeek = wrapper.emitted('update:modelValue')?.[0][0]
    expect(DateTime.isDateTime(currentWeek) && currentWeek.toFormat('yyyy-MM-dd')).toBe('2026-10-05')
  })
})

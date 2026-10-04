import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { computed, defineComponent, h, ref, shallowRef, watch } from 'vue'
import { createVuetify } from 'vuetify'
import { createVueI18nAdapter } from 'vuetify/locale/adapters/vue-i18n'
import { createI18n, useI18n } from 'vue-i18n'
import english from '../../i18n/locales/en.json'
import danish from '../../i18n/locales/da.json'
import { VRating, VBtn, VIcon } from 'vuetify/components'
import DishRating from '~/components/Dish/DishRating.vue'
import FamilyRatings from '~/components/Dish/DishFamilyRatings.vue'
import Reminder from '~/components/Home/RatingReminder.vue'
import ReminderCard from '~/components/Home/RatingReminderCard.vue'
import DishPill from '~/components/Dish/DishPill.vue'
import Preference from '~/components/RatingReminderPreference.vue'
import type { Dish } from '~/types'

enableAutoUnmount(afterEach)
const account = shallowRef('account')
const family = shallowRef('family')
const get = vi.fn()
const dismiss = vi.fn()
const updateRating = vi.fn()
const preferences = vi.fn()
const setPreference = vi.fn()
const snackbar = vi.fn()
const first = { dishId: '00000001-0000-0000-0000-000000000000', dishName: 'Lasagne', dinnerDate: '2026-10-11' }
const second = { dishId: '00000002-0000-0000-0000-000000000000', dishName: 'Tacos', dinnerDate: '2026-10-10' }
const Block = defineComponent({ setup: (_, { slots }) => () => h('div', slots.default?.()) })
const Link = defineComponent({ props: ['to'], setup: props => () => h('a', { href: props.to }, props.to) })
const i18n = createI18n({ legacy: false, locale: 'en', messages: { en: english, da: danish } })
const vuetify = createVuetify({ components: { VRating, VBtn, VIcon }, locale: { adapter: createVueI18nAdapter({ i18n, useI18n }) } })
function globals() { return { plugins: [vuetify], mocks: { $t: (key: string) => key }, stubs: { VCard: Block, VCardText: Block, VSkeletonLoader: Block, VAvatar: Block, NuxtLink: Link },
  components: { DishRating, DishPill, HomeRatingReminderCard: ReminderCard } } }
beforeEach(() => {
  vi.stubGlobal('ResizeObserver', class { observe() {} unobserve() {} disconnect() {} })
  vi.stubGlobal('IntersectionObserver', class { observe() {} unobserve() {} disconnect() {} })
  get.mockReset().mockResolvedValue({ today: '2026-10-12', reminders: [first, second] })
  dismiss.mockReset().mockResolvedValue(undefined)
  updateRating.mockReset().mockResolvedValue(undefined)
  snackbar.mockReset()
  preferences.mockReset().mockResolvedValue({ pushEnabled: false })
  setPreference.mockReset().mockResolvedValue({ pushEnabled: true })
  account.value = 'account'; family.value = 'family'
  i18n.global.locale.value = 'en'
  for (const [name, value] of Object.entries({ computed, ref, shallowRef, watch,
    useI18n: () => ({ t: (key: string) => key, locale: shallowRef('en') }),
    useNuxtApp: () => ({ $msal: { getObjectId: () => account.value } }),
    useAppStore: () => ({ get activeFamilyId() { return family.value } }),
    useRepositories: () => ({ ratingReminders: { get, dismiss, preferences, setPreference }, dishes: { updateRating } }),
    useSnackbar: () => ({ show: snackbar }),
  })) vi.stubGlobal(name, value)
})
afterEach(() => { vi.unstubAllGlobals(); vi.useRealTimers() })

describe('shared heart control', () => {
  it('announces localized half-heart values through the app Vue i18n adapter', async () => {
    const wrapper = mount(DishRating, { props: { modelValue: 3.5, editable: true }, global: globals() })
    expect(wrapper.find('button[aria-label="3.5 of 5 hearts"]').exists()).toBe(true)
    i18n.global.locale.value = 'da'
    await flushPromises()
    expect(wrapper.find('button[aria-label="3.5 af 5 hjerter"]').exists()).toBe(true)
    expect(wrapper.findAll('button').some(button => button.attributes('aria-label')?.includes('{0}'))).toBe(false)
  })
  it('focuses the own editable row after asynchronous readiness without changing a rating', async () => {
    const dish: Dish = { id: first.dishId, name: first.dishName, rating: 3, ratings: [{ familyMemberId: 'account', rating: 3 }], dates: [], notes: '', url: '', isArchived: false, dishStats: { dishId: first.dishId, timesUsed: 0, lastUsed: undefined } }
    const scroll = vi.fn()
    const prior = HTMLElement.prototype.scrollIntoView
    HTMLElement.prototype.scrollIntoView = scroll
    const wrapper = mount(FamilyRatings, { props: { userId: 'account', familyMembers: [], loading: true, focusKey: 'reminder' }, attachTo: document.body, global: globals() })
    await wrapper.setProps({ dish, loading: false, familyMembers: [{ id: 'account', name: 'Me', isOwner: true, hasAutonomy: true }] })
    await flushPromises()
    expect(document.activeElement?.id).toBe('my-rating')
    expect(scroll).toHaveBeenCalledOnce()
    expect(updateRating).not.toHaveBeenCalled()
    expect(wrapper.find('#my-rating').classes()).toContain('ratings-card__row--highlighted')
    wrapper.unmount()
    HTMLElement.prototype.scrollIntoView = prior
  })
  it('keeps five hearts read-only by default', async () => {
    const wrapper = mount(DishRating, { props: { modelValue: 3.5 }, global: globals() })
    expect(wrapper.findAll('.v-rating__wrapper')).toHaveLength(5)
    await wrapper.find('button').trigger('keydown', { key: 'ArrowRight' })
    expect(wrapper.emitted('update:modelValue')).toBeUndefined()
  })
  it('supports keyboard half increments and disables pending actions', async () => {
    const wrapper = mount(DishRating, { props: { modelValue: 3, editable: true }, global: globals() })
    await wrapper.find('button[tabindex="0"]').trigger('keydown', { key: 'ArrowRight' })
    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([3.5])
    await wrapper.setProps({ disabled: true })
    expect(wrapper.findAll('button').every(button => button.attributes('disabled') !== undefined)).toBe(true)
    await wrapper.find('button').trigger('keydown', { key: 'ArrowRight' })
    expect(wrapper.emitted('update:modelValue')).toHaveLength(1)
  })
  it('permits self and dependent-member rows and blocks autonomous peers', async () => {
    const dish: Dish = { id: first.dishId, name: first.dishName, rating: 0, ratings: [], dates: [], notes: '', url: '', isArchived: false, dishStats: { dishId: first.dishId, timesUsed: 0, lastUsed: undefined } }
    const wrapper = mount(FamilyRatings, { props: { dish, userId: 'account', familyMembers: [
      { id: 'account', name: 'Me', isOwner: true, hasAutonomy: true },
      { id: 'child', name: 'Child', isOwner: false, hasAutonomy: false },
      { id: 'peer', name: 'Peer', isOwner: false, hasAutonomy: true },
    ] }, global: globals() })
    const hearts = wrapper.findAllComponents(DishRating)
    expect(hearts.map(control => control.props('editable'))).toEqual([true, true, false])
    await hearts[2].find('button').trigger('keydown', { key: 'ArrowRight' })
    expect(updateRating).not.toHaveBeenCalled()
    await hearts[1].find('button[tabindex="0"]').trigger('keydown', { key: 'ArrowRight' })
    await flushPromises()
    expect(updateRating).toHaveBeenCalledWith(first.dishId, 0.5, 'child')
    expect(snackbar).toHaveBeenCalledWith('ratingReminders.rated', { type: 'success' })
  })
})

describe('Home quick rating', () => {
  it('restores focus after progressing without stealing focus from unrelated activity', async () => {
    const wrapper = mount(Reminder, { attachTo: document.body, global: globals() })
    await flushPromises()
    const button = wrapper.find('.rating-reminder__actions > button').element
    if (!(button instanceof HTMLElement)) throw new Error('Button missing')
    button.focus()
    get.mockResolvedValue({ today: '2026-10-12', reminders: [second] })
    await wrapper.find('.rating-reminder__actions > button').trigger('click')
    await flushPromises()
    expect(document.activeElement).toBe(wrapper.element)
  })
  it('shows one card and navigates only from the dish name', async () => {
    vi.useFakeTimers()
    const wrapper = mount(Reminder, { global: globals() })
    await flushPromises()
    expect(wrapper.findAllComponents(ReminderCard)).toHaveLength(1)
    expect(wrapper.find('a').attributes('href')).toBe(`/dishes/${first.dishId}`)
    get.mockResolvedValue({ today: '2026-10-12', reminders: [second] })
    await wrapper.findComponent(DishRating).find('button[tabindex="0"]').trigger('keydown', { key: 'ArrowRight' })
    await flushPromises()
    expect(updateRating).toHaveBeenCalledWith(first.dishId, 0.5, 'account')
    expect(wrapper.findComponent(ReminderCard).props('reminder')).toEqual(first)
    expect(wrapper.findComponent(DishRating).props('modelValue')).toBe(0.5)
    expect(wrapper.find('.rating-reminder__confirmation').text()).toContain('ratingReminders.rated')
    expect(wrapper.findComponent(DishRating).findAll('button').every(button => button.attributes('disabled') !== undefined)).toBe(true)
    await wrapper.findComponent(ReminderCard).vm.$emit('rate', 4)
    expect(updateRating).toHaveBeenCalledOnce()
    await vi.advanceTimersByTimeAsync(1199)
    expect(wrapper.findComponent(ReminderCard).props('reminder')).toEqual(first)
    await vi.advanceTimersByTimeAsync(1)
    expect(wrapper.findComponent(ReminderCard).props('reminder')).toEqual(second)
    expect(wrapper.findComponent(DishRating).props('modelValue')).toBe(0)
    expect(wrapper.find('[role="status"]').text()).toContain('ratingReminders.rated')
  })
  it('retains selection and card on failed persistence then retries', async () => {
    vi.useFakeTimers()
    const wrapper = mount(Reminder, { global: globals() })
    await flushPromises()
    expect(wrapper.find('.rating-reminder__confirmation').exists()).toBe(false)
    updateRating.mockRejectedValueOnce(new Error('Failed'))
    await wrapper.findComponent(DishRating).find('button[tabindex="0"]').trigger('keydown', { key: 'ArrowRight' })
    await flushPromises()
    expect(wrapper.findComponent(ReminderCard).props('reminder')).toEqual(first)
    expect(wrapper.findComponent(ReminderCard).props('selection')).toBe(0.5)
    expect(wrapper.find('[role="alert"]').text()).toContain('ratingReminders.rateFailed')
    get.mockResolvedValue({ today: '2026-10-12', reminders: [second] })
    await wrapper.find('[role="alert"] button').trigger('click')
    await flushPromises()
    expect(wrapper.findComponent(ReminderCard).props('reminder')).toEqual(first)
    await vi.advanceTimersByTimeAsync(1200)
    expect(wrapper.findComponent(ReminderCard).props('reminder')).toEqual(second)
  })
  it('keeps the voted card while refreshing and delays the last card only after persistence succeeds', async () => {
    vi.useFakeTimers()
    let confirmSave: (() => void) | undefined
    updateRating.mockImplementationOnce(() => new Promise<void>(resolve => { confirmSave = resolve }))
    const wrapper = mount(Reminder, { global: globals() })
    await flushPromises()
    get.mockResolvedValue({ today: '2026-10-12', reminders: [] })
    await wrapper.findComponent(DishRating).find('button[tabindex="0"]').trigger('keydown', { key: 'ArrowRight' })
    await vi.advanceTimersByTimeAsync(2000)
    expect(wrapper.findComponent(ReminderCard).props('reminder')).toEqual(first)
    expect(wrapper.find('.rating-reminder__confirmation').exists()).toBe(false)
    confirmSave?.()
    await flushPromises()
    expect(wrapper.find('.rating-reminder__confirmation').exists()).toBe(true)
    await vi.advanceTimersByTimeAsync(1199)
    expect(wrapper.findComponent(ReminderCard).exists()).toBe(true)
    await vi.advanceTimersByTimeAsync(1)
    expect(wrapper.findComponent(ReminderCard).exists()).toBe(false)
  })
  it('cancels saved feedback when the family changes and on unmount', async () => {
    vi.useFakeTimers()
    const wrapper = mount(Reminder, { global: globals() })
    await flushPromises()
    get.mockResolvedValue({ today: '2026-10-12', reminders: [second] })
    await wrapper.findComponent(DishRating).find('button[tabindex="0"]').trigger('keydown', { key: 'ArrowRight' })
    await flushPromises()
    expect(wrapper.find('.rating-reminder__confirmation').exists()).toBe(true)
    family.value = 'another-family'
    await flushPromises()
    expect(wrapper.findComponent(ReminderCard).props('reminder')).toEqual(second)
    expect(wrapper.findComponent(DishRating).props('modelValue')).toBe(0)
    expect(wrapper.find('.rating-reminder__confirmation').exists()).toBe(false)
    await wrapper.findComponent(DishRating).find('button[tabindex="0"]').trigger('keydown', { key: 'ArrowRight' })
    await flushPromises()
    wrapper.unmount()
    expect(vi.getTimerCount()).toBe(0)
  })
  it('does not steal focus when the user moves away during the confirmation delay', async () => {
    vi.useFakeTimers()
    const wrapper = mount(Reminder, { attachTo: document.body, global: globals() })
    await flushPromises()
    get.mockResolvedValue({ today: '2026-10-12', reminders: [second] })
    const heart = wrapper.findComponent(DishRating).find('button[tabindex="0"]')
    if (!(heart.element instanceof HTMLElement)) throw new Error('Heart missing')
    heart.element.focus()
    await heart.trigger('keydown', { key: 'ArrowRight' })
    await flushPromises()
    const outside = document.createElement('button')
    document.body.append(outside)
    outside.focus()
    await vi.advanceTimersByTimeAsync(1200)
    expect(document.activeElement).toBe(outside)
    outside.remove()
  })
  it('dismisses without navigating and hides the last card', async () => {
    const wrapper = mount(Reminder, { global: globals() })
    await flushPromises()
    get.mockResolvedValue({ today: '2026-10-12', reminders: [] })
    await wrapper.find('.rating-reminder__actions > button').trigger('click')
    await flushPromises()
    expect(dismiss).toHaveBeenCalledWith('family', first.dishId, first.dinnerDate)
    expect(wrapper.findComponent(ReminderCard).exists()).toBe(false)
    expect(updateRating).not.toHaveBeenCalled()
  })
  it('retires expired dates at Copenhagen midnight even if refresh fails and cleans up', async () => {
    vi.useFakeTimers(); vi.setSystemTime(new Date('2026-10-12T21:59:50Z'))
    get.mockResolvedValue({ today: '2026-10-12', reminders: [{ ...first, dinnerDate: '2026-10-05' }] })
    const wrapper = mount(Reminder, { global: globals() })
    await Promise.resolve(); await Promise.resolve(); await Promise.resolve()
    get.mockRejectedValue(new Error('Offline'))
    await vi.advanceTimersByTimeAsync(10100)
    expect(wrapper.findComponent(ReminderCard).exists()).toBe(false)
    expect(wrapper.find('[role="alert"]').text()).toContain('ratingReminders.loadFailed')
    wrapper.unmount()
    const count = get.mock.calls.length
    window.dispatchEvent(new Event('focus'))
    document.dispatchEvent(new Event('visibilitychange'))
    await vi.advanceTimersByTimeAsync(86400000)
    expect(get).toHaveBeenCalledTimes(count)
  })
})

describe('profile reminder control', () => {
  it('remains inactive without a subscription and displays localized guidance', async () => {
    const Switch = defineComponent({ props: ['disabled', 'modelValue'], emits: ['update:modelValue'], setup: (props, { emit }) => () => h('button', { disabled: props.disabled, onClick: () => emit('update:modelValue', !props.modelValue) }, String(props.modelValue)) })
    const List = defineComponent({ props: ['title'], setup: (props, { slots }) => () => h('div', [props.title, slots.append?.()]) })
    const wrapper = mount(Preference, { props: { subscribed: false }, global: { ...globals(), stubs: { VListItem: List, VSwitch: Switch, VBtn: Block } } })
    await flushPromises()
    expect(wrapper.find('button').attributes('disabled')).toBeDefined()
    expect(wrapper.text()).toContain('ratingReminders.subscriptionRequired')
    expect(setPreference).not.toHaveBeenCalled()
    await wrapper.setProps({ subscribed: true })
    await wrapper.find('button').trigger('click')
    await flushPromises()
    expect(setPreference).toHaveBeenCalledWith(true)
    expect(wrapper.find('button').text()).toBe('true')
    setPreference.mockRejectedValueOnce(new Error('Failed'))
    await wrapper.find('button').trigger('click')
    await flushPromises()
    expect(wrapper.find('button').text()).toBe('true')
    expect(wrapper.find('[role="alert"]').text()).toContain('ratingReminders.preferenceFailed')
  })
})

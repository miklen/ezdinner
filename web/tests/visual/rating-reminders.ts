import { computed, createApp, defineComponent, h, onMounted, ref, shallowRef, watch } from 'vue'
import { createVuetify } from 'vuetify'
import { createVueI18nAdapter } from 'vuetify/locale/adapters/vue-i18n'
import { createI18n, useI18n } from 'vue-i18n'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import { DateTime } from 'luxon'
import 'vuetify/styles'
import '../../assets/global.scss'
import Hero from '../../components/Home/DinnerHeroCard.vue'
import Reminder from '../../components/Home/RatingReminder.vue'
import ReminderCard from '../../components/Home/RatingReminderCard.vue'
import DishRating from '../../components/Dish/DishRating.vue'
import DishPill from '../../components/Dish/DishPill.vue'
import FamilyRatings from '../../components/Dish/DishFamilyRatings.vue'
import NotificationsToggle from '../../components/NotificationsToggle.vue'
import Preference from '../../components/RatingReminderPreference.vue'
import english from '../../i18n/locales/en.json'
import danish from '../../i18n/locales/da.json'
import type { RatingReminder } from '../../types/rating-reminders'
import type { Dish } from '../../types'

const query = new URLSearchParams(location.search)
const mobile = query.has('mobile')
const today = DateTime.now().setZone('Europe/Copenhagen').toFormat('yyyy-MM-dd')
const locale = shallowRef(query.get('lang') === 'da' ? 'da' : 'en')
const i18n = createI18n({ legacy: false, locale: locale.value, messages: { en: english, da: danish } })
watch(locale, value => { i18n.global.locale.value = value })
const subscribed = shallowRef(true)
const failed = shallowRef(query.has('fail'))
const focusKey = shallowRef<string | undefined>(undefined)
const account = '00000003-0000-0000-0000-000000000000'
const family = '00000004-0000-0000-0000-000000000000'
const longName = 'Lasagne with roasted seasonal vegetables and a very long family recipe name'
const candidates: RatingReminder[] = [
  { dishId: '00000001-0000-0000-0000-000000000000', dishName: longName, dinnerDate: DateTime.fromISO(today).minus({ days: 1 }).toFormat('yyyy-MM-dd') },
  { dishId: '00000002-0000-0000-0000-000000000000', dishName: 'Tacos', dinnerDate: DateTime.fromISO(today).minus({ days: 3 }).toFormat('yyyy-MM-dd') },
]
const resolved = new Set<string>()
const waitForReview = () => query.has('slow') ? new Promise<void>(resolve => setTimeout(resolve, 8000)) : Promise.resolve()
const status = shallowRef('')
let pushEnabled = false
const dish = shallowRef<Dish>({ id: candidates[0].dishId, name: longName, rating: 0, ratings: [], dates: [], notes: '', url: '', isArchived: false,
  dishStats: { dishId: candidates[0].dishId, timesUsed: 0, lastUsed: undefined } })

function translate(key: string, parameters: Record<string, string> = {}): string {
  let value: unknown = locale.value === 'da' ? danish : english
  for (const part of key.split('.')) {
    if (typeof value !== 'object' || value === null || !(part in value)) return key
    value = Reflect.get(value, part)
  }
  if (typeof value !== 'string') return key
  return Object.entries(parameters).reduce((text, [name, replacement]) => text.replace(`{${name}}`, replacement), value)
}

Object.assign(globalThis, {
  computed, ref, shallowRef, watch, onMounted,
  useNuxtApp: () => ({ $msal: { getObjectId: () => account } }),
  useAppStore: () => ({ activeFamilyId: family }),
  useI18n: () => ({ t: translate, locale }),
  useSnackbar: () => ({ show: (message: string) => { status.value = message } }),
  navigateTo: (path: string) => { status.value = path },
  usePushNotifications: () => ({ isSupported: shallowRef(true), isIosSafariWithoutPwa: shallowRef(false), isSubscribed: subscribed, init: async () => {},
    subscribe: async () => { subscribed.value = true; return 'ok' }, unsubscribe: async () => { subscribed.value = false; return 'ok' } }),
  useRepositories: () => ({ ratingReminders: {
    get: async () => { await waitForReview(); if (failed.value) throw new Error('Controlled failure'); return { today, reminders: candidates.filter(item => !resolved.has(item.dishId)) } },
    dismiss: async (_family: string, id: string) => { if (failed.value) throw new Error('Controlled failure'); resolved.add(id) },
    preferences: async () => ({ pushEnabled }),
    setPreference: async (enabled: boolean) => { if (failed.value) throw new Error('Controlled failure'); pushEnabled = enabled; return { pushEnabled } },
  }, dishes: { updateRating: async (id: string, rating: number, member: string) => {
    if (failed.value) throw new Error('Controlled failure')
    resolved.add(id)
    dish.value = { ...dish.value, ratings: [{ familyMemberId: member, rating }] }
  } } }),
})

const App = defineComponent({ setup: () => () => {
  if (mobile) return h('iframe', { title: '375px mobile viewport', src: '/tests/visual/rating-reminders.html?embedded=1&lang=' + locale.value, style: 'width:375px;height:1000px;border:0;display:block;margin:0 auto;' })
  return h(components.VApp, {}, { default: () => h(components.VMain, {}, { default: () => h('main', { style: 'max-width:780px;margin:24px auto;padding:16px;display:flex;flex-direction:column;gap:24px;' }, [
    h(Hero, { loading: false, firstName: 'Alex', dinner: { date: DateTime.now(), description: '', menu: [{ dishId: candidates[1].dishId, dishName: 'Tacos' }], isPlanned: true, isOptedOut: false, optOutReason: null, isResolved: true } }),
    h(Reminder),
    h(components.VCard, { style: 'padding:16px;background:var(--color-surface-variant)' }, { default: () => translate('home.tomorrow') + ': Pasta' }),
    h(FamilyRatings, { dish: dish.value, userId: account, focusKey: focusKey.value, familyMembers: [
      { id: account, name: 'Alex', hasAutonomy: true, isOwner: true }, { id: 'child', name: 'Liv', hasAutonomy: false, isOwner: false },
    ] }),
    h(components.VCard, {}, { default: () => h(components.VList, {}, { default: () => h(NotificationsToggle) }) }),
    h('p', { role: 'status' }, status.value),
    h('aside', { style: 'display:flex;flex-wrap:wrap;gap:16px;' }, [
      h('button', { onClick: () => { locale.value = locale.value === 'en' ? 'da' : 'en' } }, 'English / Dansk'),
      h('button', { onClick: () => { failed.value = !failed.value } }, failed.value ? 'Enable successful requests' : 'Simulate failure'),
      h('button', { onClick: () => { focusKey.value = 'preview-entry' } }, 'Focus my rating row'),
      h('a', { href: '?mobile=1&lang=da' }, '375px mobile review'),
    ]),
  ]) }) })
} })

const app = createApp(App).use(i18n).use(createVuetify({ components, directives, locale: { adapter: createVueI18nAdapter({ i18n, useI18n }) }, theme: {
  defaultTheme: 'light', themes: { light: { colors: {
    primary: '#6FAF7A', background: '#FAF7F4', surface: '#FFFFFF', 'surface-variant': '#F5F0EB',
  } } },
} }))
app.config.globalProperties.$t = translate
app.component('DishRating', DishRating).component('DishPill', DishPill).component('HomeRatingReminderCard', ReminderCard).component('RatingReminderPreference', Preference)
app.component('NuxtLink', defineComponent({ props: ['to'], setup: (props, { slots }) => () => h('a', { href: props.to, onClick: (event: Event) => { event.preventDefault(); status.value = props.to } }, slots.default?.()) }))
app.mount('#app')

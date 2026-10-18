import { computed, createApp, defineComponent, h, shallowRef } from 'vue'
import { createVuetify } from 'vuetify'
import { createVueI18nAdapter } from 'vuetify/locale/adapters/vue-i18n'
import { createI18n, useI18n } from 'vue-i18n'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import 'vuetify/styles'
import '../../assets/global.scss'
import GroceryStores from '../../components/Family/GroceryStores.vue'
import Explorer from '../../components/WeekPlanning/RecommendationExplorer.vue'
import DishRating from '../../components/Dish/DishRating.vue'
import { recommendationResultSchema } from '../../types/dish-recommendations'
import type { RecommendationState } from '../../types/dish-recommendations'
import type { Family } from '../../types'
import en from '../../i18n/locales/en.json'
import da from '../../i18n/locales/da.json'

const query = new URLSearchParams(location.search)
const locale = shallowRef(query.get('lang') === 'da' ? 'da' : 'en')
const i18n = createI18n({ legacy: false, locale: locale.value, messages: { en, da } })
const family = shallowRef<Family>({ id: 'family', name: 'Family', familyMembers: [], groceryStores: ['netto'], supportedGroceryStores: ['Netto', 'REMA 1000', 'Nemlig', 'Lidl', 'Føtex', 'Bilka', 'SuperBrugsen', '365discount', 'MENY', 'Spar', 'Min Købmand', 'Løvbjerg', 'ABC Lavpris', 'Aldi', 'Dagli’Brugsen', 'Let-Køb', 'Fleggaard', 'Calle', 'Otto Duborg', 'Poetzsch Padborg', 'Scandinavian Park'].map(name => ({ id: name.toLowerCase(), name, isAvailable: true, isBorderTrade: ['Fleggaard', 'Calle'].includes(name), isFlyerOnly: name === 'Aldi' })) })
const status = shallowRef('')
Object.assign(globalThis, {
  computed, useI18n: () => ({ t: i18n.global.t, locale }),
  useAppStore: () => ({ activeFamilyId: 'family' }), useDishesStore: () => ({ dishes: [] }),
  useRepositories: () => ({ families: { saveGroceryStores: async (_familyId: string, stores: string[]) => { if (query.has('fail')) throw new Error('Controlled failure'); return [...stores].sort() } } }),
  useFamiliesStore: () => ({ updateGroceryStores: () => {} }),
})
const result = recommendationResultSchema.parse({ outcome: 'Matches', activeConstraints: [], contextSummary: '', message: null,
  groceryCoverage: { status: query.get('coverage') ?? 'Partial', evaluatedAt: new Date().toISOString(), storeIds: ['lidl', 'nemlig', 'netto'], limitations: ['source-unavailable'], stores: [
    { storeId: 'lidl', storeName: 'Lidl', status: 'Fresh', usableOfferCount: 0 },
    { storeId: 'nemlig', storeName: 'Nemlig', status: 'Unavailable', usableOfferCount: 0 },
    { storeId: 'netto', storeName: 'Netto', status: 'Fresh', usableOfferCount: 1 },
  ] },
  dishes: [{ dishId: '00000000-0000-4000-8000-000000000001', name: 'Lasagne with roasted seasonal vegetables', rating: 8, roles: ['Main'], isUnclassified: false, isWished: false, wishVotes: 0, lastServed: '2026-10-01', servingCount: 5, neverUsed: false, typicalSpacingDays: null, assignedDates: [], historicalReasons: [], explanations: [], limitations: [], groceryOffers: [{ offerId: '1', ingredient: 'minced-beef', storeId: 'netto', storeName: 'Netto', productName: locale.value === 'da' ? 'Hakket oksekød 400 g' : 'Minced beef 400 g', price: 27, referencePrice: 30, currency: 'DKK', discountFraction: 0.1, conditions: ['chain-price'], freshness: 'Fresh', validFrom: null, validThrough: '2026-10-20', observedAt: new Date(Date.now() - 3600000).toISOString(), usableUntil: new Date(Date.now() + (query.has('expire') ? 15000 : 3600000)).toISOString(), sourceUrl: 'https://example.com/offer', relationship: 'Inference' }] }] })
const recommendation = result.dishes[0]
const requestState: RecommendationState = query.has('request-error') ? { status: 'failed', result, error: 'recommendationFailed' } : { status: 'ready', result }
const dish = { id: recommendation.dishId, name: recommendation.name, rating: 8, ratings: [], dates: [], notes: '', url: '', isArchived: false, roles: recommendation.roles }
const App = defineComponent({ setup: () => () => {
  if (query.has('mobile')) return h('iframe', { title: '375px mobile review', src: '/tests/visual/grocery-offers.html?embedded=1&lang=' + locale.value, style: 'width:375px;height:1600px;border:0;display:block;margin:auto;' })
  return h(components.VApp, {}, { default: () => h(components.VMain, {}, { default: () => h('main', { style: 'max-width:1100px;margin:24px auto;padding:16px;display:grid;gap:24px;' }, [
    h(Explorer, { state: requestState, entries: [{ dish, recommendation }], constraints: [], role: null, scopeDecisionRequired: false, stale: false, scopedDay: null, planned: {}, target: '2026-10-25', busy: false, coverage: result.groceryCoverage, onAssign: () => { status.value = 'Assignment selected' }, onDismiss: () => { status.value = 'Dismiss selected' } }),
    h('p', { role: 'status' }, status.value),
    h(components.VCard, { rounded: 'lg' }, { default: () => [h(components.VCardTitle, {}, { default: () => 'Family' }), h(GroceryStores, { family: family.value, owner: !query.has('member'), account: 'owner', onSaved: stores => { family.value = { ...family.value, groceryStores: stores } } })] }),
  ]) }) })
} })
const app = createApp(App).use(i18n).use(createVuetify({ components, directives, locale: { adapter: createVueI18nAdapter({ i18n, useI18n }) }, theme: { defaultTheme: 'light', themes: { light: { colors: { primary: '#6FAF7A', background: '#FAF7F4', surface: '#FFFFFF' } } } } }))
app.component('DishRating', DishRating)
app.component('NuxtLink', defineComponent({ props: { to: String }, setup: (props, { slots }) => () => h('a', { href: props.to, onClick: (event: Event) => { event.preventDefault(); status.value = String(props.to) } }, slots.default?.()) }))
app.mount('#app')

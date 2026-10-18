<script setup lang="ts">
import { computed } from 'vue'
import type { GroceryCoverage } from '~/types/grocery-offers'

const props = defineProps<{
  coverage?: GroceryCoverage
  status: GroceryCoverage['status']
  offerCount: number
  loading: boolean
  refreshRequired?: boolean
  staleLocale?: boolean
}>()
const emit = defineEmits<{ refresh: [] }>()
const { t } = useI18n()
const mode = computed(() => {
  if (props.loading) return 'Loading'
  if (props.refreshRequired) return props.staleLocale ? 'Locale' : 'Selection'
  if (props.status === 'Disabled' && !props.coverage?.storeIds.length) return 'NoStores'
  if (props.status === 'Fresh' && !props.offerCount) return 'NoMatch'
  return props.status
})
const needsRefresh = computed(() => ['Selection', 'Locale', 'Stale'].includes(mode.value))
const icon = computed(() => needsRefresh.value ? 'mdi-refresh' : mode.value === 'Unavailable' ? 'mdi-basket-outline' : 'mdi-sprout-outline')
const details = computed(() => [...new Set(props.coverage?.limitations ?? [])].map(code => {
  const known = ['source-unavailable', 'source-stale', 'source-partial', 'profiles-partial', 'enrichment-timeout', 'source-disabled', 'coverage-partial', 'coverage-unknown']
  return t(`grocery.limitations.${known.includes(code) ? code : 'unknown'}`)
}))
const showDetails = computed(() => !props.loading && !props.refreshRequired && details.value.length > 0)
const storesToCheck = computed(() => (props.coverage?.stores ?? []).filter(store =>
  props.coverage?.storeIds.includes(store.storeId) && store.usableOfferCount === 0
  && ['Fresh', 'Partial', 'Unavailable', 'Stale'].includes(store.status)))
</script>

<template>
  <div class="grocery-status" :class="{ 'grocery-status--action': needsRefresh }" :aria-busy="loading">
    <div class="grocery-status__main" role="status" aria-live="polite">
      <span class="grocery-status__icon" aria-hidden="true"><v-icon size="22">{{ icon }}</v-icon></span>
      <div class="grocery-status__copy">
        <div class="grocery-status__eyebrow">{{ t('grocery.status.label') }}<span class="grocery-status__badge">{{ t(`grocery.status.states.${mode}.badge`) }}</span></div>
        <h3>{{ t(`grocery.status.states.${mode}.title`) }}</h3>
        <p>{{ t(`grocery.status.states.${mode}.body`) }}</p>
      </div>
    </div>
    <section v-if="!loading && !refreshRequired && storesToCheck.length" class="grocery-status__stores">
      <h4>{{ t('grocery.status.manualCheck.title') }}</h4>
      <p>{{ t('grocery.status.manualCheck.body') }}</p>
      <ul>
        <li v-for="store in storesToCheck" :key="store.storeId">
          <span class="grocery-status__store-name"><v-icon size="16" aria-hidden="true">mdi-storefront-outline</v-icon>{{ store.storeName }}</span>
          <span class="grocery-status__store-result">{{ t(`grocery.status.manualCheck.${store.status}`) }}</span>
        </li>
      </ul>
    </section>
    <div v-if="!loading" class="grocery-status__footer">
      <v-btn v-if="needsRefresh" size="small" variant="tonal" color="primary" rounded="lg" @click="emit('refresh')">{{ t('weekPlanning.refreshIdeas') }}</v-btn>
      <NuxtLink v-else-if="mode === 'NoStores'" to="/families" class="grocery-status__link">{{ t('grocery.status.settings') }}<v-icon size="15" aria-hidden="true">mdi-arrow-right</v-icon></NuxtLink>
      <span v-else class="grocery-status__reassurance"><v-icon size="15" aria-hidden="true">mdi-check</v-icon>{{ t('grocery.status.keepPlanning') }}</span>
      <details v-if="showDetails" class="grocery-status__details">
        <summary>{{ t('grocery.status.details') }}<v-icon size="16" aria-hidden="true">mdi-chevron-down</v-icon></summary>
        <div class="grocery-status__explanation">
          <ul><li v-for="detail in details" :key="detail">{{ detail }}</li></ul>
          <p v-if="coverage?.limitations.includes('source-unavailable')">{{ t('grocery.status.nextCheck') }}</p>
        </div>
      </details>
    </div>
    <div v-if="loading" class="grocery-status__loading" aria-hidden="true" />
  </div>
</template>

<style scoped>
.grocery-status { position: relative; overflow: hidden; margin: var(--space-4) 0 var(--space-6); border: 1px solid rgba(var(--color-primary-rgb), .22); border-radius: var(--radius-lg); background: var(--color-surface); box-shadow: var(--shadow-xs); }
.grocery-status__main { display: flex; gap: var(--space-4); padding: var(--space-4) var(--space-6); }
.grocery-status__icon { display: grid; place-items: center; width: 42px; height: 42px; flex-shrink: 0; border-radius: var(--radius-md); background: rgba(var(--color-primary-rgb), .12); color: var(--color-primary-dark); }
.grocery-status__copy { min-width: 0; }
.grocery-status__eyebrow { display: flex; flex-wrap: wrap; align-items: center; gap: var(--space-2); margin-bottom: var(--space-2); font-size: var(--text-xs); font-weight: 600; color: var(--color-text-secondary); }
.grocery-status__badge { padding: 2px 8px; border-radius: var(--radius-full); background: var(--color-surface-variant); font-weight: 500; }
.grocery-status h3 { margin: 0 0 var(--space-1); font-family: var(--font-body); font-size: var(--text-base); line-height: 1.4; font-weight: 700; }
.grocery-status__copy p { margin: 0; font-size: var(--text-sm); line-height: 1.65; color: var(--color-text-secondary); max-width: 64ch; }
.grocery-status__stores { margin: 0 var(--space-6) var(--space-4); padding-top: var(--space-4); border-top: 1px solid var(--color-border); }
.grocery-status__stores h4 { font-family: var(--font-body); font-size: var(--text-sm); font-weight: 600; margin: 0 0 var(--space-1); }
.grocery-status__stores p { font-size: var(--text-xs); color: var(--color-text-secondary); line-height: 1.6; margin: 0 0 var(--space-3); max-width: 70ch; }
.grocery-status__stores ul { list-style: none; padding: 0; margin: 0; display: grid; gap: var(--space-2); }
.grocery-status__stores li { display: flex; align-items: baseline; flex-wrap: wrap; gap: var(--space-1) var(--space-3); font-size: var(--text-xs); }
.grocery-status__store-name { display: inline-flex; align-items: center; gap: var(--space-2); font-weight: 600; overflow-wrap: anywhere; }
.grocery-status__store-name .v-icon { color: var(--color-text-secondary); flex-shrink: 0; }
.grocery-status__store-result { color: var(--color-text-secondary); }
.grocery-status__footer { display: flex; align-items: baseline; flex-wrap: wrap; gap: var(--space-2) var(--space-4); padding: var(--space-3) var(--space-6); border-top: 1px solid rgba(var(--color-primary-rgb), .12); background: rgba(var(--color-primary-rgb), .04); font-size: var(--text-xs); }
.grocery-status__reassurance, .grocery-status__link { display: inline-flex; align-items: center; gap: var(--space-1); color: var(--color-primary-dark); font-weight: 600; }
.grocery-status__link { text-decoration: none; }
.grocery-status__details { flex: 1; min-width: min(100%, 160px); }
.grocery-status__details summary { display: flex; align-items: center; justify-content: flex-end; gap: var(--space-1); cursor: pointer; color: var(--color-text-secondary); list-style: none; min-height: 28px; }
.grocery-status__details summary::-webkit-details-marker { display: none; }
.grocery-status__details[open] summary .v-icon { transform: rotate(180deg); }
.grocery-status__details[open] { flex-basis: 100%; }
.grocery-status__explanation { padding-top: var(--space-2); color: var(--color-text-secondary); font-size: var(--text-sm); line-height: 1.6; }
.grocery-status__explanation ul { padding-left: var(--space-4); }
.grocery-status__explanation li + li { margin-top: var(--space-2); }
.grocery-status__explanation p { margin-top: var(--space-3); }
.grocery-status__link:focus-visible, summary:focus-visible { outline: 2px solid var(--color-primary-dark); outline-offset: 4px; border-radius: var(--radius-sm); }
.grocery-status--action { border-color: rgba(var(--color-accent-rgb), .45); }
.grocery-status__loading { height: 3px; background: rgba(var(--color-primary-rgb), .15); }
.grocery-status__loading::after { content: ''; display: block; width: 35%; height: 100%; background: var(--color-primary); animation: checking 1.8s ease-in-out infinite alternate; }
@keyframes checking { from { transform: translateX(-100%); } to { transform: translateX(300%); } }
@media (prefers-reduced-motion: reduce) { .grocery-status__loading::after { animation: none; width: 100%; } }
@media (max-width: 599px) { .grocery-status__main { display: block; padding: var(--space-4); } .grocery-status__stores { margin-inline: var(--space-4); } .grocery-status__footer { padding: var(--space-3) var(--space-4); } .grocery-status__icon { position: absolute; top: var(--space-4); left: var(--space-4); width: 30px; height: 30px; } .grocery-status__eyebrow { padding-left: 40px; min-height: 30px; } }
</style>

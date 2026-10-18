<script setup lang="ts">
import { computed, shallowRef } from 'vue'
import type { DishRole } from '~/types'
import type { PlanningDish } from '~/types/week-planning'
import type { RecommendationState } from '~/types/dish-recommendations'
import type { GroceryCoverage } from '~/types/grocery-offers'
import { useGroceryOfferDeadlines } from '~/composables/useGroceryOfferDeadlines'
import PlanningDishCard from './PlanningDishCard.vue'
import DishCardSkeleton from '~/components/Dish/DishCardSkeleton.vue'
import CatalogSearch from '~/components/Catalog/Search.vue'
import GroceryStatus from './GroceryStatus.vue'
const props = defineProps<{ state: RecommendationState; entries: PlanningDish[]; constraints: string[]; role: DishRole | null; nameFilter?: string; scopeDecisionRequired: boolean; stale: boolean; scopedDay: string | null; planned: Record<string, string[]>; target: string | null; busy: boolean; loading?: boolean; coverage?: GroceryCoverage; refreshRequired?: boolean; staleLocale?: boolean; selectionChanged?: boolean }>()
const emit = defineEmits<{ ask: [text: string]; more: []; refresh: []; retry: []; reset: []; constraints: [constraints: string[]]; role: [role: DishRole | null]; nameFilter: [text: string]; scope: [choice: 'keep' | 'new']; assign: [id: string]; otherDate: [id: string]; dismiss: [id: string] }>()
const { t } = useI18n()
const offers = computed(() => props.entries.flatMap(entry => entry.recommendation?.groceryOffers ?? []))
const { now } = useGroceryOfferDeadlines(offers)
const coverageStatus = computed(() => offers.value.some(offer => now.value >= Date.parse(offer.usableUntil)) ? 'Stale' : props.coverage?.status ?? 'Unknown')
const currentOfferCount = computed(() => offers.value.filter(offer => now.value < Date.parse(offer.usableUntil)).length)
const text = shallowRef('')
const roleOptions = computed(() => ([null, 'Main', 'Side', 'Dessert', 'Other']).map(value => ({ value, title: t(`weekPlanning.roles.${value ?? 'All'}`) })))
function ask() { if (text.value.trim()) { emit('ask', text.value); text.value = '' } }
function resetRequest() { text.value = ''; emit('reset') }
function editConstraint(index: number, value: string) { emit('constraints', props.constraints.map((constraint, position) => position === index ? value : constraint).filter(constraint => constraint.trim())) }
</script>

<template>
  <section class="recommendation-explorer" :aria-label="t('weekPlanning.ideas')">
    <div class="recommendation-explorer__heading"><div><span class="recommendation-explorer__eyebrow">{{ t('grocery.status.forYourFamily') }}</span><h2 class="text-section-title">{{ t('weekPlanning.ideas') }}</h2></div><v-btn class="recommendation-explorer__refresh" variant="text" size="small" :aria-label="t('weekPlanning.refreshIdeas')" :disabled="state.status === 'loading'" @click="emit('refresh')"><v-icon size="18" aria-hidden="true">mdi-refresh</v-icon><span>{{ t('weekPlanning.refreshIdeas') }}</span></v-btn></div>
    <p v-if="stale" role="status">{{ t('weekPlanning.previousContext') }}</p>
    <GroceryStatus v-if="state.result || refreshRequired || state.status === 'loading'" :coverage="coverage" :status="coverageStatus" :offer-count="currentOfferCount" :loading="state.status === 'loading'" :refresh-required="refreshRequired" :stale-locale="staleLocale" @refresh="emit('refresh')" />
    <p v-if="state.result?.contextSummary">{{ state.result.contextSummary }}</p>
    <p v-if="scopedDay">{{ t('weekPlanning.requestScope', { date: scopedDay }) }}</p>
    <v-alert v-if="scopeDecisionRequired" type="info" variant="tonal">
      {{ t('weekPlanning.scopeChanged') }}
      <div class="request-actions"><v-btn @click="emit('scope', 'keep')">{{ t('weekPlanning.keepPreferences') }}</v-btn><v-btn @click="emit('scope', 'new')">{{ t('weekPlanning.startNew') }}</v-btn></div>
    </v-alert>
    <div v-if="constraints.length" class="request-constraints">
      <p>{{ t('weekPlanning.activeConstraints') }}</p>
      <div v-for="(constraint, index) in constraints" :key="index" class="request-constraint">
        <v-text-field variant="outlined" rounded="lg" density="compact" :model-value="constraint" :aria-label="t('weekPlanning.editConstraint')" hide-details @change="editConstraint(index, $event.target.value)" />
        <v-btn icon="mdi-close" variant="text" :aria-label="t('weekPlanning.removeConstraint')" @click="emit('constraints', constraints.filter((_, position) => position !== index))" />
      </div>
    </div>
    <form class="request-form" @submit.prevent="ask">
      <div class="request-form__intro"><v-icon size="18" aria-hidden="true">mdi-tune-variant</v-icon><span>{{ t('grocery.status.refine') }}</span></div>
      <div class="request-form__filters">
      <v-select variant="outlined" rounded="lg" density="compact" :model-value="role" :items="roleOptions" :label="t('weekPlanning.requestRole')" hide-details @update:model-value="emit('role', $event)" />
      <CatalogSearch :model-value="nameFilter ?? ''" :placeholder="t('weekPlanning.recommendationNameFilter')" :maxlength="100" @update:model-value="emit('nameFilter', $event)" />
      </div>
      <v-textarea v-model="text" variant="outlined" rounded="lg" density="compact" :label="t('weekPlanning.askLabel')" :placeholder="t('weekPlanning.askPlaceholder')" rows="2" auto-grow maxlength="2000" hide-details />
      <div class="request-actions"><v-btn type="submit" color="primary" variant="tonal" rounded="lg" :loading="state.status === 'loading'" :disabled="scopeDecisionRequired || !text.trim()">{{ t('weekPlanning.ask') }}</v-btn><v-btn variant="text" @click="resetRequest">{{ t('weekPlanning.startNew') }}</v-btn></div>
    </form>
    <div v-if="state.status === 'failed'" class="recommendation-error" role="alert"><v-icon size="22" aria-hidden="true">mdi-cloud-alert-outline</v-icon><div><h3>{{ t('grocery.status.requestFailedTitle') }}</h3><p>{{ t(state.result ? 'grocery.status.requestFailedRetained' : 'grocery.status.requestFailedEmpty') }}</p><v-btn variant="text" size="small" prepend-icon="mdi-refresh" @click="emit('retry')">{{ t('weekPlanning.retry') }}</v-btn></div></div>
    <p v-if="state.result && state.result.outcome !== 'Matches'" role="status">{{ state.result.message || t(`weekPlanning.outcomes.${state.result.outcome}`) }}</p>
    <div v-if="!entries.length && (loading || state.status === 'loading')" class="recommendation-grid" role="status" :aria-label="t('weekPlanning.loading')" aria-busy="true">
      <DishCardSkeleton v-for="index in 6" :key="index" />
    </div>
    <div v-else class="recommendation-grid">
      <PlanningDishCard v-for="entry in entries" :key="entry.dish.id" :entry="entry" :offer-now="now" :assigned-dates="planned[entry.dish.id] ?? []" :target="target" :busy="busy" @assign="emit('assign', $event)" @other-date="emit('otherDate', $event)" @dismiss="emit('dismiss', $event)" />
    </div>
    <v-btn v-if="state.result" variant="tonal" :disabled="refreshRequired || scopeDecisionRequired || state.status === 'loading'" @click="emit('more')">{{ t('weekPlanning.moreIdeas') }}</v-btn>
  </section>
</template>

<style scoped lang="scss">
@use "~/assets/dish-grid.scss" as dish-grid;
.recommendation-explorer { padding-bottom: var(--space-6); border-bottom: 1px solid var(--color-border-medium); }
.recommendation-explorer__eyebrow { display: block; font-size: var(--text-xs); font-weight: 600; color: var(--color-primary-dark); margin-bottom: var(--space-1); }
.recommendation-explorer__refresh span { margin-left: var(--space-2); }
.recommendation-explorer__heading { display: flex; align-items: center; justify-content: space-between; gap: var(--space-2); flex-wrap: wrap; }
.request-form { display: grid; gap: var(--space-4); margin: var(--space-4) 0 var(--space-6); padding: var(--space-4); background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-lg); }
.request-form__intro { display: flex; align-items: center; gap: var(--space-2); color: var(--color-text-secondary); font-size: var(--text-sm); font-weight: 600; }
.request-form__filters { display: grid; grid-template-columns: minmax(140px, .7fr) minmax(0, 1.3fr); gap: var(--space-3); align-items: center; }
.recommendation-error { display: flex; gap: var(--space-3); padding: var(--space-4); border: 1px solid rgba(var(--color-error-rgb), .2); border-radius: var(--radius-lg); background: var(--color-surface); }
.recommendation-error > .v-icon { color: var(--color-error); }
.recommendation-error h3 { font-size: var(--text-base); margin-bottom: var(--space-1); }
.recommendation-error p { font-size: var(--text-sm); color: var(--color-text-secondary); line-height: 1.6; }
.recommendation-error .v-btn { margin: var(--space-2) 0 0 calc(-1 * var(--space-3)); }
@media (max-width: 599px) { .request-form__filters { grid-template-columns: minmax(0, 1fr); } .recommendation-explorer__refresh { min-width: 36px; width: 36px; height: 36px; padding: 0; } .recommendation-explorer__refresh span { display: none; } }
.request-actions, .request-constraint { display: flex; gap: var(--space-2); align-items: center; }
.request-actions { flex-wrap: wrap; }
.request-constraints { margin-top: var(--space-3); }
.request-constraint { margin-bottom: var(--space-2); }
.recommendation-grid { @include dish-grid.layout; margin: var(--space-4) 0; }

</style>

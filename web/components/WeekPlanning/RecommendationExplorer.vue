<script setup lang="ts">
import { computed, shallowRef } from 'vue'
import type { DishRole } from '~/types'
import type { PlanningDish } from '~/types/week-planning'
import type { RecommendationState } from '~/types/dish-recommendations'
import PlanningDishCard from './PlanningDishCard.vue'
const props = defineProps<{ state: RecommendationState; entries: PlanningDish[]; constraints: string[]; role: DishRole | null; nameFilter?: string; scopeDecisionRequired: boolean; stale: boolean; scopedDay: string | null; planned: Record<string, string[]>; target: string | null; busy: boolean }>()
const emit = defineEmits<{ ask: [text: string]; more: []; refresh: []; retry: []; reset: []; constraints: [constraints: string[]]; role: [role: DishRole | null]; nameFilter: [text: string]; scope: [choice: 'keep' | 'new']; assign: [id: string]; otherDate: [id: string]; dismiss: [id: string] }>()
const { t } = useI18n()
const text = shallowRef('')
const roleOptions = computed(() => ([null, 'Main', 'Side', 'Dessert', 'Other']).map(value => ({ value, title: t(`weekPlanning.roles.${value ?? 'All'}`) })))
function ask() { if (text.value.trim()) { emit('ask', text.value); text.value = '' } }
function resetRequest() { text.value = ''; emit('reset') }
function editConstraint(index: number, value: string) { emit('constraints', props.constraints.map((constraint, position) => position === index ? value : constraint).filter(constraint => constraint.trim())) }
</script>

<template>
  <section class="recommendation-explorer" :aria-label="t('weekPlanning.ideas')">
    <div class="recommendation-explorer__heading"><h2 class="text-section-title">{{ t('weekPlanning.ideas') }}</h2><v-btn variant="text" @click="emit('refresh')">{{ t('weekPlanning.refreshIdeas') }}</v-btn></div>
    <p v-if="stale" role="status">{{ t('weekPlanning.previousContext') }}</p>
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
      <v-select variant="outlined" rounded="lg" density="compact" :model-value="role" :items="roleOptions" :label="t('weekPlanning.requestRole')" hide-details @update:model-value="emit('role', $event)" />
      <v-text-field variant="outlined" rounded="lg" density="compact" prepend-inner-icon="mdi-magnify" clearable :model-value="nameFilter ?? ''" :placeholder="t('weekPlanning.recommendationNameFilter')" :aria-label="t('weekPlanning.recommendationNameFilter')" maxlength="100" hide-details @update:model-value="emit('nameFilter', $event ?? '')" />
      <v-textarea v-model="text" variant="outlined" rounded="lg" density="compact" :label="t('weekPlanning.askLabel')" :placeholder="t('weekPlanning.askPlaceholder')" rows="2" auto-grow maxlength="2000" hide-details />
      <div class="request-actions"><v-btn type="submit" color="primary" variant="tonal" rounded="lg" :loading="state.status === 'loading'" :disabled="scopeDecisionRequired || !text.trim()">{{ t('weekPlanning.ask') }}</v-btn><v-btn variant="text" @click="resetRequest">{{ t('weekPlanning.startNew') }}</v-btn></div>
    </form>
    <v-alert v-if="state.status === 'failed'" type="error" variant="tonal" role="alert">{{ t('weekPlanning.recommendationFailed') }} <v-btn variant="text" @click="emit('retry')">{{ t('weekPlanning.retry') }}</v-btn></v-alert>
    <p v-if="state.result && state.result.outcome !== 'Matches'" role="status">{{ state.result.message || t(`weekPlanning.outcomes.${state.result.outcome}`) }}</p>
    <div class="recommendation-grid">
      <PlanningDishCard v-for="entry in entries" :key="entry.dish.id" :entry="entry" :assigned-dates="planned[entry.dish.id] ?? []" :target="target" :busy="busy" @assign="emit('assign', $event)" @other-date="emit('otherDate', $event)" @dismiss="emit('dismiss', $event)" />
    </div>
    <v-btn v-if="state.result" variant="tonal" :disabled="scopeDecisionRequired || state.status === 'loading'" @click="emit('more')">{{ t('weekPlanning.moreIdeas') }}</v-btn>
  </section>
</template>

<style scoped>
.recommendation-explorer { padding-bottom: var(--space-6); border-bottom: 1px solid var(--color-border-medium); }
.recommendation-explorer__heading { display: flex; align-items: center; justify-content: space-between; gap: var(--space-2); flex-wrap: wrap; }
.request-form { display: grid; gap: var(--space-4); margin: var(--space-4) 0; }
.request-actions, .request-constraint { display: flex; gap: var(--space-2); align-items: center; }
.request-actions { flex-wrap: wrap; }
.request-constraints { margin-top: var(--space-3); }
.request-constraint { margin-bottom: var(--space-2); }
.recommendation-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: var(--space-4); margin: var(--space-4) 0; }
@media (max-width: 600px) { .recommendation-grid { grid-template-columns: 1fr; } }
</style>

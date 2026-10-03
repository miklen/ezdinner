<script setup lang="ts">
import { computed, shallowRef, useId } from 'vue'
import PlanningDishCard from './PlanningDishCard.vue'
import type { PlanningDish, ExplorationPreferences } from '~/types/week-planning'
const props = defineProps<{ entries: PlanningDish[]; preferences: ExplorationPreferences; planned: Record<string, string[]>; target: string | null; busy: boolean; cuisines: string[] }>()
const emit = defineEmits<{ update: [preferences: ExplorationPreferences]; assign: [id: string]; otherDate: [id: string] }>()
const { t } = useI18n()
const filtersExpanded = shallowRef(false)
const filterId = useId()
const activeFilterCount = computed(() => Number(props.preferences.role !== 'All') + Number(props.preferences.effort !== 'All') + Number(props.preferences.season !== 'All') + Number(!!props.preferences.cuisine) + Number(props.preferences.wishesOnly))
const roleOptions = computed(() => (['All', 'Main', 'Side', 'Dessert', 'Other'] as const).map(value => ({ value, title: t(`weekPlanning.roles.${value}`) })))
const effortOptions = computed(() => (['All', 'Quick', 'Medium', 'Elaborate'] as const).map(value => ({ value, title: t(`weekPlanning.efforts.${value}`) })))
const seasonOptions = computed(() => (['All', 'Spring', 'Summer', 'Autumn', 'Winter', 'AllYear'] as const).map(value => ({ value, title: t(`weekPlanning.seasons.${value}`) })))
const sortOptions = computed(() => (['name', 'rating', 'usage', 'lastUsed'] as const).map(value => ({ value, title: t(`weekPlanning.sorts.${value}`) })))
function update<Key extends keyof ExplorationPreferences>(key: Key, value: ExplorationPreferences[Key]) { emit('update', { ...props.preferences, [key]: value }) }
</script>

<template>
  <section :aria-label="t('weekPlanning.catalog')">
    <h2 class="text-section-title catalog-heading">{{ t('weekPlanning.catalog') }}</h2>
    <v-text-field variant="outlined" rounded="lg" density="compact" prepend-inner-icon="mdi-magnify" clearable :placeholder="t('weekPlanning.search')" :aria-label="t('weekPlanning.search')" :model-value="preferences.search" hide-details class="catalog-search" @update:model-value="update('search', $event ?? '')" />
    <div class="catalog__sort" role="group" :aria-label="t('weekPlanning.sort')">
      <v-chip v-for="option in sortOptions" :key="option.value" :color="preferences.sort === option.value ? 'primary' : undefined" :variant="preferences.sort === option.value ? 'tonal' : 'outlined'" size="small" @click="update('sort', option.value)">{{ option.title }}</v-chip>
    </div>
    <div class="catalog__filter-section">
      <div class="catalog__filter-header">
        <button class="catalog__filter-toggle" :aria-expanded="filtersExpanded" :aria-controls="filterId" @click="filtersExpanded = !filtersExpanded">
          <span class="catalog__filter-label">{{ t('dishes.filter') }}</span>
          <span v-if="activeFilterCount" class="filter-count">{{ activeFilterCount }}</span>
          <span class="filter-chevron" :class="{ 'filter-chevron--open': filtersExpanded }" aria-hidden="true">›</span>
        </button>
        <button v-if="activeFilterCount" class="filter-clear" @click="emit('update', { ...preferences, role: 'All', effort: 'All', season: 'All', cuisine: '', wishesOnly: false })">{{ t('dishes.clearFilter') }}</button>
      </div>
      <div :id="filterId" class="catalog__filter-body" :class="{ 'catalog__filter-body--open': filtersExpanded }" :inert="!filtersExpanded">
        <div class="catalog__filter-inner">
          <button v-for="option in roleOptions" :key="option.value" class="filter-tag filter-tag--role" :class="{ 'filter-tag--active': preferences.role === option.value }" :aria-pressed="preferences.role === option.value" @click="update('role', option.value)"><span class="filter-tag__pip" aria-hidden="true" />{{ option.title }}</button>
          <button v-for="option in effortOptions" :key="option.value" class="filter-tag filter-tag--effort" :class="{ 'filter-tag--active': preferences.effort === option.value }" :aria-pressed="preferences.effort === option.value" @click="update('effort', preferences.effort === option.value ? 'All' : option.value)"><span class="filter-tag__pip" aria-hidden="true" />{{ option.title }}</button>
          <button v-for="option in seasonOptions" :key="option.value" class="filter-tag filter-tag--season" :class="{ 'filter-tag--active': preferences.season === option.value }" :aria-pressed="preferences.season === option.value" @click="update('season', preferences.season === option.value ? 'All' : option.value)"><span class="filter-tag__pip" aria-hidden="true" />{{ option.title }}</button>
          <button v-for="cuisine in cuisines" :key="cuisine" class="filter-tag filter-tag--cuisine" :class="{ 'filter-tag--active': preferences.cuisine === cuisine }" :aria-pressed="preferences.cuisine === cuisine" @click="update('cuisine', preferences.cuisine === cuisine ? '' : cuisine)"><span class="filter-tag__pip" aria-hidden="true" />{{ cuisine }}</button>
          <button class="filter-tag filter-tag--archive" :class="{ 'filter-tag--active': preferences.wishesOnly }" :aria-pressed="preferences.wishesOnly" @click="update('wishesOnly', !preferences.wishesOnly)"><span class="filter-tag__pip" aria-hidden="true" />{{ t('weekPlanning.wishesOnly') }}</button>
        </div>
      </div>
    </div>
    <p v-if="!entries.length" role="status">{{ t('weekPlanning.emptyCatalog') }}</p>
    <div class="dish-grid">
      <PlanningDishCard v-for="entry in entries" :key="entry.dish.id" :entry="entry" :assigned-dates="planned[entry.dish.id] ?? []" :target="target" :busy="busy" @assign="emit('assign', $event)" @other-date="emit('otherDate', $event)" />
    </div>
  </section>
</template>

<style scoped lang="scss">
@use "~/assets/catalog-controls.scss";
.catalog-search { margin-bottom: var(--space-3); }
.catalog-heading { margin: var(--space-6) 0 var(--space-4); }
.dish-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: var(--space-3); margin-top: var(--space-4); }
@media (max-width: 600px) { .dish-grid { grid-template-columns: 1fr; } }
</style>

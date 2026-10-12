<script setup lang="ts">
import { computed } from 'vue'
import PlanningDishCard from './PlanningDishCard.vue'
import DishCardSkeleton from '~/components/Dish/DishCardSkeleton.vue'
import EmptyState from '~/components/EmptyState.vue'
import CatalogControls from '~/components/Catalog/Controls.vue'
import type { CatalogFilterOption, CatalogSortOption } from '~/types/catalog-controls'
import { nextDishSortDirection } from '~/utils/dish-sorting'
import type { DishSortKey } from '~/utils/dish-sorting'
import type { DishRole, EffortLevel, SeasonAffinity } from '~/types'
import type { PlanningDish, ExplorationPreferences } from '~/types/week-planning'
const props = defineProps<{ entries: PlanningDish[]; preferences: ExplorationPreferences; planned: Record<string, string[]>; target: string | null; busy: boolean; loading?: boolean; cuisines: string[] }>()
const emit = defineEmits<{ update: [preferences: ExplorationPreferences]; assign: [id: string]; otherDate: [id: string] }>()
const { t } = useI18n()
const activeFilterCount = computed(() => Number(props.preferences.role !== 'All') + Number(props.preferences.effort !== 'All') + Number(props.preferences.season !== 'All') + Number(!!props.preferences.cuisine) + Number(props.preferences.wishesOnly))
const roles: (DishRole | 'All')[] = ['All', 'Main', 'Side', 'Dessert', 'Other']
const efforts: (EffortLevel | 'All')[] = ['All', 'Quick', 'Medium', 'Elaborate']
const seasons: (SeasonAffinity | 'All')[] = ['All', 'Spring', 'Summer', 'Autumn', 'Winter', 'AllYear']
const filters = computed<CatalogFilterOption[]>(() => [
  ...roles.map((value): CatalogFilterOption => ({ dimension: 'roles', value, label: t('weekPlanning.roles.' + value), selected: props.preferences.role === value })),
  ...efforts.map((value): CatalogFilterOption => ({ dimension: 'effort', value, label: t('weekPlanning.efforts.' + value), selected: props.preferences.effort === value })),
  ...seasons.map((value): CatalogFilterOption => ({ dimension: 'season', value, label: t('weekPlanning.seasons.' + value), selected: props.preferences.season === value })),
  ...props.cuisines.map((value): CatalogFilterOption => ({ dimension: 'cuisine', value, label: value, selected: props.preferences.cuisine === value })),
  { dimension: 'wishes', value: 'wishes', label: t('weekPlanning.wishesOnly'), selected: props.preferences.wishesOnly },
])
const sortKey = computed<DishSortKey>(() => props.preferences.sort === 'usage' ? 'timesUsed' : props.preferences.sort)
const sortOptions = computed<CatalogSortOption[]>(() => [
  { key: 'name', label: t('weekPlanning.sorts.name') },
  { key: 'rating', label: t('weekPlanning.sorts.rating') },
  { key: 'timesUsed', label: t('weekPlanning.sorts.usage') },
  { key: 'lastUsed', label: t('weekPlanning.sorts.lastUsed') },
])
function update<Key extends keyof ExplorationPreferences>(key: Key, value: ExplorationPreferences[Key]) { emit('update', { ...props.preferences, [key]: value }) }
function selectSort(key: DishSortKey) {
  emit('update', { ...props.preferences, sort: key === 'timesUsed' ? 'usage' : key, sortDir: nextDishSortDirection(sortKey.value, props.preferences.sortDir, key) })
}
function selectFilter(option: CatalogFilterOption) {
  if (option.dimension === 'roles') { update('role', option.value); return }
  if (option.dimension === 'effort') { update('effort', option.selected ? 'All' : option.value); return }
  if (option.dimension === 'season') { update('season', option.selected ? 'All' : option.value); return }
  if (option.dimension === 'cuisine') { update('cuisine', option.selected ? '' : option.value); return }
  if (option.dimension === 'wishes') update('wishesOnly', !props.preferences.wishesOnly)
}
function clearFilters() { emit('update', { ...props.preferences, role: 'All', effort: 'All', season: 'All', cuisine: '', wishesOnly: false }) }
</script>

<template>
  <section :aria-label="t('weekPlanning.catalog')">
    <h2 class="text-section-title catalog-heading">{{ t('weekPlanning.catalog') }}</h2>
    <CatalogControls :search="preferences.search" :search-placeholder="t('weekPlanning.search')" :sort-key="sortKey" :sort-direction="preferences.sortDir" :sort-options="sortOptions" :filters="filters" :active-filter-count="activeFilterCount" @update:search="update('search', $event)" @sort="selectSort" @filter="selectFilter" @clear="clearFilters" />
    <div v-if="loading && !entries.length" class="dish-grid" role="status" :aria-label="t('weekPlanning.loading')" aria-busy="true">
      <DishCardSkeleton v-for="index in 6" :key="index" />
    </div>
    <EmptyState v-else-if="!entries.length" icon="mdi-silverware-fork-knife" :message="t('weekPlanning.emptyCatalog')" role="status" />
    <div v-else class="dish-grid">
      <PlanningDishCard v-for="entry in entries" :key="entry.dish.id" :entry="entry" :assigned-dates="planned[entry.dish.id] ?? []" :target="target" :busy="busy" @assign="emit('assign', $event)" @other-date="emit('otherDate', $event)" />
    </div>
  </section>
</template>

<style scoped lang="scss">
@use "~/assets/dish-grid.scss" as dish-grid;
.catalog-heading { margin: var(--space-6) 0 var(--space-4); }
.dish-grid { @include dish-grid.layout; margin-top: var(--space-4); }
</style>

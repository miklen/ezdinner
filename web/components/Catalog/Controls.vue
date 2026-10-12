<script setup lang="ts">
import { shallowRef, useId } from 'vue'
import Search from './Search.vue'
import type { CatalogFilterOption, CatalogSortOption } from '~/types/catalog-controls'
import type { DishSortDirection, DishSortKey } from '~/utils/dish-sorting'

defineProps<{
  search: string
  searchPlaceholder: string
  sortKey: DishSortKey
  sortDirection: DishSortDirection
  sortOptions: CatalogSortOption[]
  filters: CatalogFilterOption[]
  activeFilterCount: number
}>()
const emit = defineEmits<{
  'update:search': [value: string]
  sort: [key: DishSortKey]
  filter: [option: CatalogFilterOption]
  clear: []
}>()
const { t } = useI18n()
const expanded = shallowRef(false)
const filterId = useId()
function filterClass(option: CatalogFilterOption) {
  if (option.dimension === 'roles') return 'filter-tag--role'
  if (option.dimension === 'wishes') return 'filter-tag--archive'
  return `filter-tag--${option.dimension}`
}
</script>

<template>
  <div class="catalog-controls">
    <Search :model-value="search" :placeholder="searchPlaceholder" class="catalog__search" @update:model-value="emit('update:search', $event)" />
    <div class="catalog__sort" role="group" :aria-label="t('dishes.sortAriaLabel')">
      <v-chip v-for="option in sortOptions" :key="option.key" :color="sortKey === option.key ? 'primary' : undefined" :variant="sortKey === option.key ? 'tonal' : 'outlined'" size="small" class="sort-chip" @click="emit('sort', option.key)">
        {{ option.label }}
        <span v-if="sortKey === option.key" class="sort-chip__dir" aria-hidden="true">{{ sortDirection === 'asc' ? '↑' : '↓' }}</span>
      </v-chip>
    </div>
    <div class="catalog__filter-section">
      <div class="catalog__filter-header">
        <button class="catalog__filter-toggle" :aria-expanded="expanded" :aria-controls="filterId" @click="expanded = !expanded">
          <span class="catalog__filter-label">{{ t('dishes.filter') }}</span>
          <span v-if="activeFilterCount" class="filter-count">{{ activeFilterCount }}</span>
          <span class="filter-chevron" :class="{ 'filter-chevron--open': expanded }" aria-hidden="true">›</span>
        </button>
        <button v-if="activeFilterCount" class="filter-clear" @click="emit('clear')">{{ t('dishes.clearFilter') }}</button>
      </div>
      <div :id="filterId" class="catalog__filter-body" :class="{ 'catalog__filter-body--open': expanded }" :inert="!expanded" role="group" :aria-label="t('dishes.filterAriaLabel')">
        <div class="catalog__filter-inner">
          <button v-for="option in filters" :key="`${option.dimension}:${option.value}`" class="filter-tag" :class="[filterClass(option), { 'filter-tag--active': option.selected }]" :aria-pressed="option.selected" :disabled="option.disabled" @click="emit('filter', option)"><span class="filter-tag__pip" aria-hidden="true" />{{ option.label }}</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@use "~/assets/catalog-controls.scss";
.catalog__search { margin-bottom: var(--space-3); }
</style>

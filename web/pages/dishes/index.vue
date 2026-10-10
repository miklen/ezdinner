<script setup lang="ts">
import type { DateTime } from 'luxon'
import type { Dish, DishStats } from '~/types'
import { useDishCatalogPreferences } from '~/composables/useDishCatalogPreferences'
import type { CatalogSortKey as SortKey } from '~/utils/dish-catalog-preferences'

useHead({ title: 'Dishes' })

const appStore = useAppStore()
const dishesStore = useDishesStore()
const dinnersStore = useDinnersStore()
const wishlistStore = useWishlistStore()
const { dishes: dishRepo } = useRepositories()
const { show: showSnackbar } = useSnackbar()
const { t } = useI18n()

// ── Assistant panel (sidebar + mobile sheet) ──────────────────────────────────

const { weekStart: panelWeekStart } = useWeekNav()
const mobileSheetOpen = ref(false)
const mobilePanelDataLoaded = ref(false)

const { mdAndUp } = useDisplay()

const panelWeekEnd = computed(() => panelWeekStart.value.endOf('week'))

async function loadPanelData() {
  await Promise.all([
    dinnersStore.populateDinners(panelWeekStart.value, panelWeekEnd.value),
    wishlistStore.fetchWishes(),
  ])
}

function onPanelWeekChanged(week: DateTime) {
  panelWeekStart.value = week
}

function onDishAssigned(_date: string, _dishId: string) {
  dinnersStore.populateDinners(panelWeekStart.value, panelWeekEnd.value)
  wishlistStore.fetchWishes()
}

function onMobileDishAssigned(date: string, dishId: string) {
  mobileSheetOpen.value = false
  onDishAssigned(date, dishId)
}

async function openMobilePanel() {
  mobileSheetOpen.value = true
  if (!mobilePanelDataLoaded.value) {
    mobilePanelDataLoaded.value = true
    await loadPanelData()
  }
}

// Desktop: load panel data eagerly since sidebar is always visible.
// Mobile: load on first FAB tap via openMobilePanel().
onMounted(() => {
  if (mdAndUp.value) {
    loadPanelData()
  }
})

// Re-load when the panel navigates to a different week
watch(panelWeekStart, loadPanelData)

const { $msal } = useNuxtApp()
const account = computed(() => $msal.isAuthenticated.value ? ($msal.getObjectId() ?? '') : '')
const catalogPreferences = useDishCatalogPreferences(account, computed(() => appStore.activeFamilyId))
const preferences = catalogPreferences.preferences
const searchDish = computed({
  get: () => preferences.value.search,
  set: (value: string | null) => catalogPreferences.update({ search: value ?? '' }),
})
const stats = ref<Record<string, DishStats>>({})
const newDishName = shallowRef('')
const newDishDialog = shallowRef(false)
const loading = shallowRef(true)
const showArchived = computed(() => preferences.value.showArchived)
// Local list for catalog display — allows toggling archived visibility independently.
// Does NOT sync with dishesStore.dishes, which always holds active dishes only.
// Other pages (plan, dish picker) rely on the store being a clean active-dish list.
const allDishes = ref<Dish[]>([])

// ── Filter state ──────────────────────────────────────────────────────────────

const filterRoles = computed(() => new Set(preferences.value.roles))
const filterEffort = computed(() => new Set(preferences.value.effort))
const filterSeason = computed(() => new Set(preferences.value.season))
const filterCuisine = computed(() => new Set(preferences.value.cuisine))

const filtersExpanded = shallowRef(false)

const activeFilterCount = computed(() =>
  filterRoles.value.size + filterEffort.value.size + filterSeason.value.size + filterCuisine.value.size + (showArchived.value ? 1 : 0),
)

const hasActiveFilters = computed(() => activeFilterCount.value > 0)

const { toggleFilter, clearFilters, selectSort } = catalogPreferences
const sortKey = computed(() => preferences.value.sortKey)
const sortDir = computed(() => preferences.value.sortDir)

const sortChips = computed(() => [
  { key: 'name' as SortKey, label: t('dishes.sortName') },
  { key: 'rating' as SortKey, label: t('dishes.sortRating') },
  { key: 'timesUsed' as SortKey, label: t('dishes.sortTimesUsed') },
  { key: 'lastUsed' as SortKey, label: t('dishes.sortLastUsed') },
])

// ── Derived filter options (from loaded dish list) ────────────────────────────

const availableRoles = computed(() => {
  const roles = new Set<string>()
  for (const d of allDishes.value) for (const r of d.roles ?? []) roles.add(r)
  return [...roles].sort()
})

const availableEffort = computed(() => {
  const order = ['Quick', 'Medium', 'Elaborate']
  const values = new Set<string>()
  for (const d of allDishes.value) if (d.effortLevel) values.add(d.effortLevel)
  return order.filter((e) => values.has(e))
})

const availableSeason = computed(() => {
  const order = ['Summer', 'Winter', 'Spring', 'Autumn', 'AllYear']
  const values = new Set<string>()
  for (const d of allDishes.value) if (d.seasonAffinity) values.add(d.seasonAffinity)
  return order.filter((s) => values.has(s))
})

const availableCuisine = computed(() => {
  const seen = new Map<string, string>()
  for (const d of allDishes.value) {
    if (d.cuisine) {
      const key = d.cuisine.toLowerCase()
      if (!seen.has(key)) seen.set(key, d.cuisine)
    }
  }
  return [...seen.entries()].sort((a, b) => a[1].localeCompare(b[1]))
})

// ── Derived dish list ─────────────────────────────────────────────────────────

const dishes = computed<Dish[]>(() => {
  const query = searchDish.value.toLowerCase()
  const filtered = allDishes.value.filter((d) => {
    if (showArchived.value !== d.isArchived) return false
    if (!d.name?.toLowerCase().includes(query)) return false
    if (filterRoles.value.size > 0 && !d.roles?.some((r) => filterRoles.value.has(r))) return false
    if (filterEffort.value.size > 0 && (!d.effortLevel || !filterEffort.value.has(d.effortLevel))) return false
    if (filterSeason.value.size > 0 && (!d.seasonAffinity || !filterSeason.value.has(d.seasonAffinity))) return false
    if (filterCuisine.value.size > 0 && (!d.cuisine || !filterCuisine.value.has(d.cuisine.toLowerCase()))) return false
    return true
  })

  const dir = sortDir.value === 'asc' ? 1 : -1

  return filtered.sort((a, b) => {
    switch (sortKey.value) {
      case 'name':
        return dir * a.name.localeCompare(b.name)
      case 'rating':
        return dir * (a.rating - b.rating)
      case 'timesUsed':
        return dir * ((stats.value[a.id]?.timesUsed ?? 0) - (stats.value[b.id]?.timesUsed ?? 0))
      case 'lastUsed': {
        const aMs = stats.value[a.id]?.lastUsed?.toMillis() ?? (dir === 1 ? Number.MAX_SAFE_INTEGER : Number.MIN_SAFE_INTEGER)
        const bMs = stats.value[b.id]?.lastUsed?.toMillis() ?? (dir === 1 ? Number.MAX_SAFE_INTEGER : Number.MIN_SAFE_INTEGER)
        return dir * (aMs - bMs)
      }
    }
  })
})

const activeDishes = computed(() => allDishes.value.filter((d) => !d.isArchived))
const archivedDishes = computed(() => allDishes.value.filter((d) => d.isArchived))

const hasNoDishesAtAll = computed(() => !loading.value && activeDishes.value.length === 0 && !showArchived.value)
const hasNoArchivedDishes = computed(() => !loading.value && archivedDishes.value.length === 0 && showArchived.value)
const hasNoSearchResults = computed(() => !loading.value && dishes.value.length === 0 && !hasNoDishesAtAll.value && !hasNoArchivedDishes.value)

// ── Data loading ──────────────────────────────────────────────────────────────

let loadGeneration = 0
onScopeDispose(() => { loadGeneration++ })

async function loadStats() {
  const generation = loadGeneration
  const usage = await dishRepo.allUsageStats(appStore.activeFamilyId)
  if (generation === loadGeneration) stats.value = usage
}

async function init() {
  const family = appStore.activeFamilyId
  const scope = catalogPreferences.scope.value
  const archived = showArchived.value
  const generation = ++loadGeneration
  allDishes.value = []
  stats.value = {}
  if (!family || !scope) { loading.value = false; return }
  loading.value = true
  try {
    const [catalog, usage] = await Promise.all([
      dishRepo.all(family, archived),
      dishRepo.allUsageStats(family),
      dishesStore.populateDishes(),
    ])
    if (generation !== loadGeneration || scope !== catalogPreferences.scope.value || archived !== showArchived.value) return
    allDishes.value = catalog
    stats.value = usage
    // Reconcile only after fresh data arrives, never against the initial empty list.
    catalogPreferences.reconcile({
      roles: availableRoles.value,
      effort: availableEffort.value,
      season: availableSeason.value,
      cuisine: availableCuisine.value.map(([key]) => key),
    })
    await dishesStore.populateStats()
  }
  finally { if (generation === loadGeneration) loading.value = false }
}

function toggleArchived() {
  if (!loading.value) catalogPreferences.update({ showArchived: !showArchived.value })
}

async function createDish() {
  const trimmed = newDishName.value.trim()
  if (!trimmed) return
  const dishId = await dishRepo.create(appStore.activeFamilyId, trimmed)
  newDishName.value = ''
  newDishDialog.value = false
  dishRepo.enrich(appStore.activeFamilyId, dishId).catch(() => {
    showSnackbar(t('dishes.aiAnalysisFailed'), { type: 'error' })
  })
  await init()
}

watch([catalogPreferences.scope, showArchived], init, { immediate: true })
</script>

<template>
  <div>
  <Content split desktop-only-support>
    <div class="catalog">
    <!-- ── Header row ──────────────────────────────────────────────────────── -->
    <div class="catalog__header">
      <h1 class="text-page-title catalog__heading">{{ $t('dishes.dishes') }}</h1>
      <div class="catalog__header-actions">
        <v-btn
          color="primary"
          variant="tonal"
          prepend-icon="mdi-plus"
          rounded="lg"
          @click="newDishDialog = true"
        >
          {{ $t('dishes.addDish') }}
        </v-btn>
      </div>
    </div>

    <!-- ── Search bar ──────────────────────────────────────────────────────── -->
    <v-text-field
      v-model="searchDish"
      :placeholder="$t('dishes.findADish')"
      prepend-inner-icon="mdi-magnify"
      variant="outlined"
      rounded="lg"
      density="compact"
      hide-details
      clearable
      class="catalog__search"
    />

    <!-- ── Sort row ────────────────────────────────────────────────────────── -->
    <div class="catalog__sort" role="group" aria-label="Sort by">
      <v-chip
        v-for="chip in sortChips"
        :key="chip.key"
        :color="sortKey === chip.key ? 'primary' : undefined"
        :variant="sortKey === chip.key ? 'tonal' : 'outlined'"
        size="small"
        class="sort-chip"
        @click="selectSort(chip.key)"
      >
        {{ chip.label }}
        <span v-if="sortKey === chip.key" class="sort-chip__dir" aria-hidden="true">{{ sortDir === 'asc' ? '↑' : '↓' }}</span>
      </v-chip>
    </div>

    <!-- ── Filter panel ─────────────────────────────────────────────────────── -->
    <div class="catalog__filter-section">
      <!-- Header row — always visible -->
      <div class="catalog__filter-header">
        <button
          class="catalog__filter-toggle"
          :aria-expanded="filtersExpanded"
          aria-controls="filter-panel"
          @click="filtersExpanded = !filtersExpanded"
        >
          <span class="catalog__filter-label">{{ $t('dishes.filter') }}</span>
          <span v-if="hasActiveFilters" class="filter-count">{{ activeFilterCount }}</span>
          <span class="filter-chevron" :class="{ 'filter-chevron--open': filtersExpanded }" aria-hidden="true">›</span>
        </button>
        <button v-if="hasActiveFilters" class="filter-clear" @click="clearFilters">
          {{ $t('dishes.clearFilter') }}
        </button>
      </div>

      <!-- Collapsible body -->
      <div
        id="filter-panel"
        class="catalog__filter-body"
        :class="{ 'catalog__filter-body--open': filtersExpanded }"
        role="group"
        aria-label="Filter by"
      >
        <div class="catalog__filter-inner">
          <template v-if="availableRoles.length">
            <button
              v-for="role in availableRoles"
              :key="role"
              :class="['filter-tag', 'filter-tag--role', { 'filter-tag--active': filterRoles.has(role) }]"
              :aria-pressed="filterRoles.has(role)"
              @click="toggleFilter('roles', role)"
            ><span class="filter-tag__pip" aria-hidden="true" />{{ $t(`dishes.roles.${role}`) }}</button>
          </template>

          <template v-if="availableEffort.length">
            <button
              v-for="effort in availableEffort"
              :key="effort"
              :class="['filter-tag', 'filter-tag--effort', { 'filter-tag--active': filterEffort.has(effort) }]"
              :aria-pressed="filterEffort.has(effort)"
              @click="toggleFilter('effort', effort)"
            ><span class="filter-tag__pip" aria-hidden="true" />{{ $t(`dishes.effortLevels.${effort}`) }}</button>
          </template>

          <template v-if="availableSeason.length">
            <button
              v-for="season in availableSeason"
              :key="season"
              :class="['filter-tag', 'filter-tag--season', { 'filter-tag--active': filterSeason.has(season) }]"
              :aria-pressed="filterSeason.has(season)"
              @click="toggleFilter('season', season)"
            ><span class="filter-tag__pip" aria-hidden="true" />{{ $t(`dishes.seasons.${season}`) }}</button>
          </template>

          <template v-if="availableCuisine.length">
            <button
              v-for="[key, label] in availableCuisine"
              :key="key"
              :class="['filter-tag', 'filter-tag--cuisine', { 'filter-tag--active': filterCuisine.has(key) }]"
              :aria-pressed="filterCuisine.has(key)"
              @click="toggleFilter('cuisine', key)"
            ><span class="filter-tag__pip" aria-hidden="true" />{{ label }}</button>
          </template>

          <button
            :class="['filter-tag', 'filter-tag--archive', { 'filter-tag--active': showArchived }]"
            :aria-pressed="showArchived"
            @click="toggleArchived"
          ><span class="filter-tag__pip" aria-hidden="true" />{{ $t('dishes.archived') }}</button>
        </div>
      </div>
    </div>

    <!-- ── Archived view banner ───────────────────────────────────────────── -->
    <div v-if="showArchived && !loading" class="catalog__archived-banner">
      <v-icon size="14" icon="mdi-archive-outline" class="catalog__archived-banner-icon" />
      {{ $t('dishes.archivedBannerText') }}
    </div>

    <!-- ── Skeleton loaders ────────────────────────────────────────────────── -->
    <div v-if="loading" class="dish-grid">
      <v-skeleton-loader
        v-for="i in 6"
        :key="i"
        type="card"
        :elevation="0"
      />
    </div>

    <!-- ── Empty: no dishes in family ─────────────────────────────────────── -->
    <EmptyState
      v-else-if="hasNoDishesAtAll"
      icon="mdi-silverware-fork-knife"
      :message="$t('dishes.noDishesYet')"
      :action-label="$t('dishes.addADish')"
    />

    <!-- ── Empty: no archived dishes ──────────────────────────────────────── -->
    <EmptyState
      v-else-if="hasNoArchivedDishes"
      icon="mdi-archive-outline"
      :message="$t('dishes.noArchivedDishes')"
    />

    <!-- ── Empty: search returned nothing ─────────────────────────────────── -->
    <EmptyState
      v-else-if="hasNoSearchResults"
      icon="mdi-magnify"
      :message="$t('dishes.noSearchResults', { query: searchDish })"
    />

    <!-- ── Dish grid ───────────────────────────────────────────────────────── -->
    <div v-else class="dish-grid">
      <DishCard
        v-for="dish in dishes"
        :key="dish.id"
        :dish="dish"
        :dish-stats="stats[dish.id]"
        :class="{ 'dish-card--archived': dish.isArchived }"
        @menuitem:moved="loadStats"
        @archived="init"
      />
    </div>

    <!-- ── Create dish dialog ──────────────────────────────────────────────── -->
    <v-dialog v-model="newDishDialog" width="440">
      <v-card>
        <v-card-title class="text-section-title pt-6 px-6">{{ $t('dishes.newDish') }}</v-card-title>
        <v-card-text class="px-6">
          <v-text-field
            v-model="newDishName"
            :label="$t('dishes.dishName')"
            variant="outlined"
            autofocus
            @keyup.enter="createDish"
          />
        </v-card-text>
        <v-card-actions class="px-6 pb-6">
          <v-spacer />
          <v-btn variant="text" @click="newDishDialog = false">{{ $t('common.cancel') }}</v-btn>
          <v-btn
            color="primary"
            variant="tonal"
            :disabled="!newDishName.trim()"
            @click="createDish"
          >{{ $t('common.create') }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>

    <template #support>
      <PlanAssistantPanel
        :dinners="dinnersStore.dinners"
        @dish:assigned="onDishAssigned"
        @week:changed="onPanelWeekChanged"
      />
    </template>
  </Content>

  <!-- Mobile: FAB to open panel as bottom sheet -->
  <v-btn
    class="d-flex d-md-none dishes-fab"
    icon="mdi-auto-fix"
    color="primary"
    size="large"
    elevation="4"
    :aria-label="$t('assistant.openPanelAriaLabel')"
    @click="openMobilePanel"
  />

  <v-bottom-sheet
    v-model="mobileSheetOpen"
    class="d-md-none"
    :max-height="'85dvh'"
    scrollable
  >
    <v-sheet class="mobile-sheet">
      <div class="mobile-sheet__handle" />
      <PlanAssistantPanel
        :dinners="dinnersStore.dinners"
        @dish:assigned="onMobileDishAssigned"
        @week:changed="onPanelWeekChanged"
      />
    </v-sheet>
  </v-bottom-sheet>
  </div>
</template>

<style scoped lang="scss">
@use "~/assets/catalog-controls.scss";
.catalog {
  padding: var(--space-6) var(--space-4);
  max-width: 1200px;
}

.catalog__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: var(--space-2);
  margin-bottom: var(--space-4);
}

.catalog__header-actions {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: var(--space-2);
}

.catalog__heading {
  margin: 0;
}

.catalog__search {
  margin-bottom: var(--space-3);
}

/* ── Sort row ──────────────────────────────────────────────────────────── */

/* CSS grid gives equal horizontal and vertical gaps */
.dish-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: var(--space-4);
}

@media (min-width: 960px) {
  .dish-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (min-width: 1280px) {
  .dish-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

.catalog__filters {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
  align-items: center;
  margin-bottom: var(--space-3);
  padding: var(--space-2) var(--space-3);
  background-color: var(--color-surface-variant);
  border-radius: var(--radius-sm);
}


.catalog__archived-banner {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-2) var(--space-4);
  margin-bottom: var(--space-4);
  background-color: var(--color-surface-variant);
  border-left: 3px solid var(--color-accent);
  border-radius: var(--radius-sm);
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
}

.catalog__archived-banner-icon {
  color: var(--color-accent);
  flex-shrink: 0;
}

.dish-card--archived {
  filter: grayscale(25%) opacity(0.65);
  transition: filter var(--duration-fast) var(--ease-out);
}

.dish-card--archived:hover {
  filter: grayscale(10%) opacity(0.85);
}

.dishes-fab {
  position: fixed;
  bottom: calc(64px + var(--space-4));
  right: var(--space-4);
  z-index: 100;
}

.mobile-sheet {
  padding: var(--space-4);
  height: 85dvh;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.mobile-sheet__handle {
  width: 36px;
  height: 4px;
  border-radius: var(--radius-full);
  background: rgba(0, 0, 0, 0.15);
  margin: 0 auto var(--space-4);
  flex-shrink: 0;
}
</style>

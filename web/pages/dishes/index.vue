<script setup lang="ts">
import type { Dish, DishStats } from '~/types'

useHead({ title: 'Dishes' })

const appStore = useAppStore()
const dishesStore = useDishesStore()
const { dishes: dishRepo } = useRepositories()
const { show: showSnackbar } = useSnackbar()

const searchDish = shallowRef('')
const stats = ref<Record<string, DishStats>>({})
const newDishName = shallowRef('')
const newDishDialog = shallowRef(false)
const loading = shallowRef(true)
const showArchived = shallowRef(false)
// Local list for catalog display — allows toggling archived visibility independently.
// Does NOT sync with dishesStore.dishes, which always holds active dishes only.
// Other pages (plan, dish picker) rely on the store being a clean active-dish list.
const allDishes = ref<Dish[]>([])

// ── Filter state ──────────────────────────────────────────────────────────────

const filterRoles = ref<Set<string>>(new Set())
const filterEffort = ref<Set<string>>(new Set())
const filterSeason = ref<Set<string>>(new Set())
const filterCuisine = ref<Set<string>>(new Set())

type FilterDimension = 'roles' | 'effort' | 'season' | 'cuisine'

const filtersExpanded = shallowRef(false)

const activeFilterCount = computed(() =>
  filterRoles.value.size + filterEffort.value.size + filterSeason.value.size + filterCuisine.value.size + (showArchived.value ? 1 : 0),
)

const hasActiveFilters = computed(() => activeFilterCount.value > 0)

// Refs accessed inside script context — never auto-unwrapped by template proxy
function toggleFilter(dimension: FilterDimension, value: string) {
  const refMap = { roles: filterRoles, effort: filterEffort, season: filterSeason, cuisine: filterCuisine }
  const set = refMap[dimension]
  const next = new Set(set.value)
  if (next.has(value)) next.delete(value)
  else next.add(value)
  set.value = next
}

async function clearFilters() {
  filterRoles.value = new Set()
  filterEffort.value = new Set()
  filterSeason.value = new Set()
  filterCuisine.value = new Set()
  if (showArchived.value) await toggleArchived()
}

// ── Sort state ────────────────────────────────────────────────────────────────

type SortKey = 'name' | 'rating' | 'timesUsed' | 'lastUsed'
type SortDir = 'asc' | 'desc'

const sortKey = shallowRef<SortKey>('name')
const sortDir = shallowRef<SortDir>('asc')

// Default direction when a chip is first selected
const defaultDir: Record<SortKey, SortDir> = {
  name: 'asc',
  rating: 'desc',
  timesUsed: 'desc',
  lastUsed: 'desc',
}

function selectSort(key: SortKey) {
  if (sortKey.value === key) {
    sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
  } else {
    sortKey.value = key
    sortDir.value = defaultDir[key]
  }
}

const sortChips: { key: SortKey; label: string }[] = [
  { key: 'name', label: 'Name' },
  { key: 'rating', label: 'Rating' },
  { key: 'timesUsed', label: 'Times used' },
  { key: 'lastUsed', label: 'Last used' },
]

// ── Derived filter options (from loaded dish list) ────────────────────────────

const SEASON_LABELS: Record<string, string> = {
  Summer: 'Summer', Winter: 'Winter', Spring: 'Spring', Autumn: 'Autumn', AllYear: 'All year',
}

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

async function loadStats() {
  stats.value = await dishRepo.allUsageStats(appStore.activeFamilyId)
}

async function loadDishes() {
  allDishes.value = await dishRepo.all(appStore.activeFamilyId, showArchived.value)
}

async function init() {
  loading.value = true
  await Promise.all([loadDishes(), dishesStore.populateDishes(), loadStats()])
  loading.value = false
}

async function toggleArchived() {
  if (loading.value) return
  showArchived.value = !showArchived.value
  loading.value = true
  await loadDishes()
  loading.value = false
}

async function createDish() {
  const trimmed = newDishName.value.trim()
  if (!trimmed) return
  const dishId = await dishRepo.create(appStore.activeFamilyId, trimmed)
  newDishName.value = ''
  newDishDialog.value = false
  dishRepo.enrich(appStore.activeFamilyId, dishId).catch(() => {
    showSnackbar('AI analysis failed — you can edit metadata manually from the dish page', { type: 'error' })
  })
  await init()
}

watch(
  () => appStore.activeFamilyId,
  (val) => {
    if (val) {
      stats.value = {}
      init()
    }
  },
  { immediate: true },
)
</script>

<template>
  <div class="catalog">
    <!-- ── Header row ──────────────────────────────────────────────────────── -->
    <div class="catalog__header">
      <h1 class="text-page-title catalog__heading">Dishes</h1>
      <div class="catalog__header-actions">
        <v-btn
          color="primary"
          variant="tonal"
          prepend-icon="mdi-plus"
          rounded="lg"
          @click="newDishDialog = true"
        >
          Add dish
        </v-btn>
      </div>
    </div>

    <!-- ── Search bar ──────────────────────────────────────────────────────── -->
    <v-text-field
      v-model="searchDish"
      placeholder="Find a dish…"
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
          <span class="catalog__filter-label">Filter</span>
          <span v-if="hasActiveFilters" class="filter-count">{{ activeFilterCount }}</span>
          <span class="filter-chevron" :class="{ 'filter-chevron--open': filtersExpanded }" aria-hidden="true">›</span>
        </button>
        <button v-if="hasActiveFilters" class="filter-clear" @click="clearFilters">
          × clear
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
            ><span class="filter-tag__pip" aria-hidden="true" />{{ role }}</button>
          </template>

          <template v-if="availableEffort.length">
            <button
              v-for="effort in availableEffort"
              :key="effort"
              :class="['filter-tag', 'filter-tag--effort', { 'filter-tag--active': filterEffort.has(effort) }]"
              :aria-pressed="filterEffort.has(effort)"
              @click="toggleFilter('effort', effort)"
            ><span class="filter-tag__pip" aria-hidden="true" />{{ effort }}</button>
          </template>

          <template v-if="availableSeason.length">
            <button
              v-for="season in availableSeason"
              :key="season"
              :class="['filter-tag', 'filter-tag--season', { 'filter-tag--active': filterSeason.has(season) }]"
              :aria-pressed="filterSeason.has(season)"
              @click="toggleFilter('season', season)"
            ><span class="filter-tag__pip" aria-hidden="true" />{{ SEASON_LABELS[season] ?? season }}</button>
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
          ><span class="filter-tag__pip" aria-hidden="true" />Archived</button>
        </div>
      </div>
    </div>

    <!-- ── Archived view banner ───────────────────────────────────────────── -->
    <div v-if="showArchived && !loading" class="catalog__archived-banner">
      <v-icon size="14" icon="mdi-archive-outline" class="catalog__archived-banner-icon" />
      Archived dishes are hidden from suggestions and the active catalog.
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
      message="No dishes yet. Add your first dish to get started."
      action-label="Add a dish"
    />

    <!-- ── Empty: no archived dishes ──────────────────────────────────────── -->
    <EmptyState
      v-else-if="hasNoArchivedDishes"
      icon="mdi-archive-outline"
      message="No archived dishes."
    />

    <!-- ── Empty: search returned nothing ─────────────────────────────────── -->
    <EmptyState
      v-else-if="hasNoSearchResults"
      icon="mdi-magnify"
      :message="`No dishes match &quot;${searchDish}&quot;`"
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
        <v-card-title class="text-section-title pt-6 px-6">New dish</v-card-title>
        <v-card-text class="px-6">
          <v-text-field
            v-model="newDishName"
            label="Dish name"
            variant="outlined"
            autofocus
            @keyup.enter="createDish"
          />
        </v-card-text>
        <v-card-actions class="px-6 pb-6">
          <v-spacer />
          <v-btn variant="text" @click="newDishDialog = false">Cancel</v-btn>
          <v-btn
            color="primary"
            variant="tonal"
            :disabled="!newDishName.trim()"
            @click="createDish"
          >Create</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<style scoped>
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

.catalog__sort {
  display: flex;
  flex-wrap: nowrap;
  overflow-x: auto;
  scrollbar-width: none;
  -ms-overflow-style: none;
  align-items: center;
  gap: var(--space-2);
  margin-bottom: var(--space-3);
}

.catalog__sort::-webkit-scrollbar {
  display: none;
}

.sort-chip__dir {
  margin-left: 2px;
  font-size: 11px;
  line-height: 1;
}

/* ── Filter panel ──────────────────────────────────────────────────────── */

.catalog__filter-section {
  margin-bottom: var(--space-4);
}

.catalog__filter-header {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  margin-bottom: var(--space-2);
}

.catalog__filter-toggle {
  display: inline-flex;
  align-items: center;
  gap: var(--space-2);
  background: none;
  border: none;
  padding: 0;
  cursor: pointer;
  font-family: var(--font-body);
}

.catalog__filter-label {
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.10em;
  text-transform: uppercase;
  color: var(--color-text-muted);
}

.filter-count {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 16px;
  height: 16px;
  padding: 0 4px;
  border-radius: var(--radius-full);
  background: var(--color-primary);
  color: #fff;
  font-size: 10px;
  font-weight: 700;
  line-height: 1;
}

.filter-chevron {
  font-size: 16px;
  color: var(--color-text-muted);
  line-height: 1;
  transition: transform var(--duration-fast) var(--ease-out);
  display: inline-block;
  transform: rotate(0deg);
}

.filter-chevron--open {
  transform: rotate(90deg);
}

/* Smooth height reveal using grid trick (overflow: hidden on wrapper, not content) */
.catalog__filter-body {
  display: grid;
  grid-template-rows: 0fr;
  overflow: hidden;
  transition: grid-template-rows var(--duration-normal) var(--ease-out);
}

.catalog__filter-body--open {
  grid-template-rows: 1fr;
}

.catalog__filter-inner {
  min-height: 0; /* required: lets the grid track actually collapse to 0fr */
  overflow: hidden; /* belt-and-suspenders: clip any child paint that exceeds 0 */
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-2);
}

.catalog__filter-body--open .catalog__filter-inner {
  padding-bottom: var(--space-1);
}

.filter-tag {
  --dim-color: rgba(0, 0, 0, 0.35);
  --dim-color-bg: rgba(0, 0, 0, 0.04);
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 4px 10px;
  border-radius: var(--radius-sm);
  border: 1.5px solid rgba(0, 0, 0, 0.10);
  background: transparent;
  font-family: var(--font-body);
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  color: var(--color-text-muted);
  cursor: pointer;
  transition:
    background-color var(--duration-fast) var(--ease-out),
    border-color var(--duration-fast) var(--ease-out),
    color var(--duration-fast) var(--ease-out),
    transform var(--duration-instant) var(--ease-out);
  user-select: none;
}

.filter-tag:hover {
  background: rgba(0, 0, 0, 0.04);
  border-color: rgba(0, 0, 0, 0.18);
  color: var(--color-text-secondary);
}

.filter-tag:active {
  transform: scale(0.95);
}

.filter-tag--active {
  background: var(--dim-color-bg);
  border-color: var(--dim-color);
  color: var(--dim-color);
}

.filter-tag--active:hover {
  background: var(--dim-color-bg);
  color: var(--dim-color);
}

/* Per-dimension color tokens */
.filter-tag--role    { --dim-color: #4A6B3E; --dim-color-bg: rgba(74, 107, 62, 0.09); }
.filter-tag--effort  { --dim-color: #8a6818; --dim-color-bg: rgba(229, 168, 59, 0.11); }
.filter-tag--season  { --dim-color: #B8511D; --dim-color-bg: rgba(212, 101, 42, 0.09); }
.filter-tag--cuisine { --dim-color: #3a6434; --dim-color-bg: rgba(74, 124, 63, 0.09); }
.filter-tag--archive { --dim-color: #5C4A3A; --dim-color-bg: rgba(92, 74, 58, 0.09); }

.filter-tag__pip {
  display: inline-block;
  width: 5px;
  height: 5px;
  border-radius: 1px;
  background: var(--dim-color);
  flex-shrink: 0;
  opacity: 0.7;
  transition: opacity var(--duration-fast) var(--ease-out);
}

.filter-tag--active .filter-tag__pip {
  opacity: 1;
}

.filter-clear {
  display: inline-flex;
  align-items: center;
  padding: 4px 8px;
  border: none;
  background: transparent;
  font-family: var(--font-body);
  font-size: 11px;
  font-weight: 500;
  letter-spacing: 0.03em;
  color: var(--color-text-muted);
  cursor: pointer;
  opacity: 0.65;
  transition: opacity var(--duration-fast) var(--ease-out);
  margin-left: var(--space-1);
}

.filter-clear:hover {
  opacity: 1;
  text-decoration: underline;
}

/* Touch targets on coarse-pointer devices */
@media (pointer: coarse) {
  .sort-btn,
  .filter-tag {
    padding-top: 10px;
    padding-bottom: 10px;
  }
}

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
</style>

<script setup lang="ts">
import type { Dish, DishStats } from '~/types'

useHead({ title: 'Dishes' })

const appStore = useAppStore()
const dishesStore = useDishesStore()
const { dishes: dishRepo } = useRepositories()

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

// ── Derived dish list ─────────────────────────────────────────────────────────

const dishes = computed<Dish[]>(() => {
  const query = searchDish.value.toLowerCase()
  const filtered = allDishes.value.filter((d) => {
    if (showArchived.value !== d.isArchived) return false
    return d.name?.toLowerCase().includes(query)
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
  await dishRepo.create(appStore.activeFamilyId, trimmed)
  newDishName.value = ''
  newDishDialog.value = false
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
          :color="showArchived ? 'secondary' : undefined"
          :variant="showArchived ? 'tonal' : 'outlined'"
          :aria-label="showArchived ? 'Hide archived dishes' : 'Show archived dishes'"
          prepend-icon="mdi-archive-outline"
          rounded="lg"
          size="small"
          @click="toggleArchived"
        >
          {{ showArchived ? 'Hide archived' : 'Show archived' }}
        </v-btn>
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

    <!-- ── Sort chips ──────────────────────────────────────────────────────── -->
    <div class="catalog__chips" role="group" aria-label="Sort by">
      <v-chip
        v-for="chip in sortChips"
        :key="chip.key"
        :color="sortKey === chip.key ? 'primary' : undefined"
        :variant="sortKey === chip.key ? 'tonal' : 'outlined'"
        size="small"
        class="catalog__chip"
        @click="selectSort(chip.key)"
      >
        {{ chip.label }}
        <v-icon
          v-if="sortKey === chip.key"
          :icon="sortDir === 'asc' ? 'mdi-arrow-up' : 'mdi-arrow-down'"
          size="14"
          class="ml-1"
        />
      </v-chip>
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
  margin-bottom: var(--space-4);
}

.catalog__header-actions {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.catalog__heading {
  margin: 0;
}

.catalog__search {
  margin-bottom: var(--space-3);
}

.catalog__chips {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
  margin-bottom: var(--space-4);
}

.catalog__chip {
  cursor: pointer;
  user-select: none;
  transition:
    background-color var(--duration-fast) var(--ease-out),
    color var(--duration-fast) var(--ease-out);
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

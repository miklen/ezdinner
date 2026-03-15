<script setup lang="ts">
// DishDetailHeader
import { DateTime } from 'luxon'
import type { Dish, DishStats } from '~/types'

const props = defineProps<{
  dish?: Dish
  dishStats?: DishStats
  loading?: boolean
}>()

const emit = defineEmits<{
  move: []
  delete: []
}>()

const dishesStore = useDishesStore()
const { dishes: dishRepo } = useRepositories()
const { show: showSnackbar } = useSnackbar()

const renameDialog = shallowRef(false)
const newName = shallowRef('')
const localName = shallowRef(props.dish.name)

watch(() => props.dish.name, (val) => { localName.value = val })

const effectiveStats = computed<DishStats>(() =>
  props.dishStats ?? { dishId: props.dish.id, lastUsed: undefined, timesUsed: 0 },
)

const daysAgo = computed(() => {
  if (!effectiveStats.value.lastUsed) return null
  const d = Math.floor(DateTime.now().diff(effectiveStats.value.lastUsed, 'days').days)
  return Math.max(0, d)
})

const statLine = computed(() => {
  const n = effectiveStats.value.timesUsed
  if (n === 0) return 'Never planned for dinner'
  const times = `${n} time${n === 1 ? '' : 's'}`
  if (daysAgo.value === null) return `Had ${times}`
  return `Had ${times} · last ${daysAgo.value} day${daysAgo.value === 1 ? '' : 's'} ago`
})

function openRename() {
  newName.value = localName.value
  renameDialog.value = true
}

async function doRename() {
  const trimmed = newName.value.trim()
  if (!trimmed) return
  try {
    await dishRepo.updateName(props.dish.id, trimmed)
    localName.value = trimmed
    await dishesStore.updateDish(props.dish.id)
    showSnackbar('Dish renamed', { type: 'success' })
  } catch {
    showSnackbar('Failed to rename dish', { type: 'error' })
  }
  renameDialog.value = false
}
</script>

<template>
  <!-- Skeleton -->
  <div v-if="loading" class="dish-header dish-header--skeleton">
    <v-skeleton-loader type="text" width="140" class="dish-header__skeleton-breadcrumb" />
    <v-skeleton-loader type="heading" width="360" class="dish-header__skeleton-title" />
    <v-skeleton-loader type="text" width="160" class="dish-header__skeleton-rating" />
    <v-skeleton-loader type="text" width="220" />
  </div>

  <!-- Loaded -->
  <div v-else class="dish-header">
    <!-- Breadcrumb -->
    <nav class="dish-header__breadcrumb text-caption-label" aria-label="Breadcrumb">
      <NuxtLink to="/dishes" class="dish-header__breadcrumb-link">
        <v-icon size="13" class="dish-header__breadcrumb-icon">mdi-silverware-fork-knife</v-icon>
        Dishes
      </NuxtLink>
      <span class="dish-header__breadcrumb-sep" aria-hidden="true">/</span>
      <span class="dish-header__breadcrumb-current">{{ localName }}</span>
    </nav>

    <!-- Title + overflow menu inline — button stays right after the dish name -->
    <div class="dish-header__title-row">
      <h1 class="dish-header__name text-page-title">{{ localName }}</h1>
      <div class="dish-header__menu">
        <DishOverflowMenu
          @edit-name="openRename"
          @move="emit('move')"
          @delete="emit('delete')"
          @converted="navigateTo('/dishes')"
        />
      </div>
    </div>

    <!-- Rating -->
    <div class="dish-header__rating-row">
      <DishRating :model-value="dish.rating" :size="22" />
      <span v-if="dish.rating > 0" class="dish-header__rating-label">
        {{ dish.rating.toFixed(1) }}
      </span>
    </div>

    <!-- Stat line -->
    <p class="dish-header__stat">{{ statLine }}</p>
  </div>

  <!-- Rename dialog -->
  <v-dialog v-model="renameDialog" width="400">
    <v-card>
      <v-card-title>Rename dish</v-card-title>
      <v-card-text>
        <v-text-field
          v-model="newName"
          label="Dish name"
          variant="outlined"
          density="compact"
          autofocus
          hide-details
          @keyup.enter="doRename"
          @keyup.esc="renameDialog = false"
        />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="renameDialog = false">Cancel</v-btn>
        <v-btn variant="text" color="primary" :disabled="!newName.trim()" @click="doRename">Save</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

</template>

<style scoped>
.dish-header {
  padding: 0 0 var(--space-3);
}

.dish-header--skeleton {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.dish-header__skeleton-breadcrumb {
  margin-bottom: var(--space-1);
}

.dish-header__skeleton-title {
  margin-bottom: var(--space-2);
}

.dish-header__skeleton-rating {
  margin-bottom: var(--space-1);
}

/* Breadcrumb */
.dish-header__breadcrumb {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  margin-bottom: var(--space-1);
  color: var(--color-text-muted);
}

.dish-header__breadcrumb-link {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  color: var(--color-text-muted);
  text-decoration: none;
  transition: color var(--duration-fast) var(--ease-out);
}

.dish-header__breadcrumb-link:hover {
  color: var(--color-primary-dark);
}

.dish-header__breadcrumb-icon {
  flex-shrink: 0;
}

.dish-header__breadcrumb-sep {
  color: var(--color-border-medium);
}

.dish-header__breadcrumb-current {
  color: var(--color-text-secondary);
  font-weight: 600;
  max-width: 300px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* Title row: title flows naturally, button is anchored top-right */
.dish-header__title-row {
  position: relative;
  padding-right: 36px;
  margin-bottom: var(--space-2);
}

.dish-header__name {
  margin: 0;
  word-break: break-word;
}

.dish-header__menu {
  position: absolute;
  top: 4px;
  right: 0;
}

.dish-header__menu :deep(.overflow-btn) {
  opacity: 1;
}

/* Rating row */
.dish-header__rating-row {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  margin-bottom: var(--space-1);
}

.dish-header__rating-label {
  font-size: var(--text-sm);
  font-weight: 600;
  color: var(--color-text-secondary);
}

/* Stat line */
.dish-header__stat {
  margin: 0;
  font-size: var(--text-sm);
  color: var(--color-text-muted);
}

</style>

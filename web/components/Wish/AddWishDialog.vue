<script setup lang="ts">
import type { Dish } from '~/types'

const props = defineProps<{
  modelValue: boolean
  preselectedDish?: Dish | null
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'wish:added': [dishId: string, dishName: string]
}>()

const appStore = useAppStore()
const dishesStore = useDishesStore()
const wishlistStore = useWishlistStore()
const { dishes: dishRepo } = useRepositories()
const { show: showSnackbar } = useSnackbar()
const { t } = useI18n()

const search = ref('')
const loading = ref(false)
const creating = ref(false)

const internalOpen = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val),
})

// Filtered dishes from catalog based on search query
const filteredDishes = computed(() => {
  const q = search.value.toLowerCase().trim()
  if (!q) return dishesStore.dishes.filter((d) => !d.isArchived).slice(0, 20)
  return dishesStore.dishes
    .filter((d) => !d.isArchived && d.name.toLowerCase().includes(q))
    .slice(0, 30)
})

// Check if a dish already has an active wish
function getExistingWish(dishId: string) {
  return wishlistStore.wishes.find((w) => w.dishId === dishId)
}

const canCreateNew = computed(() => {
  const q = search.value.trim()
  if (!q) return false
  return !dishesStore.dishes.some((d) => d.name.toLowerCase() === q.toLowerCase())
})

async function selectDish(dish: Dish) {
  const existing = getExistingWish(dish.id)
  if (existing) {
    // Already wished — upvote it
    await handleUpvote(existing.wishId, dish.name)
    return
  }
  await handleAddWish(dish.id, dish.name)
}

async function handleUpvote(wishId: string, dishName: string) {
  loading.value = true
  try {
    const { alreadyVoted } = await wishlistStore.upvoteWish(wishId)
    if (alreadyVoted) {
      showSnackbar(t('wishlist.alreadyVoted'), { type: 'info' })
    } else {
      showSnackbar(t('wishlist.upvoteAdded', { name: dishName }), { type: 'success' })
      internalOpen.value = false
    }
  } catch {
    showSnackbar(t('wishlist.errorUpvoting'), { type: 'error' })
  } finally {
    loading.value = false
  }
}

async function handleAddWish(dishId: string, dishName: string) {
  loading.value = true
  try {
    const result = await wishlistStore.addWish(dishId, dishName)
    if (result.alreadyExists) {
      // Race condition: added since we loaded — upvote the existing one
      showSnackbar(t('wishlist.wishAlreadyExists', { name: dishName }), { type: 'info' })
    } else {
      showSnackbar(t('wishlist.wishAdded', { name: dishName }), { type: 'success' })
      emit('wish:added', dishId, dishName)
      internalOpen.value = false
    }
  } catch {
    showSnackbar(t('wishlist.errorAdding'), { type: 'error' })
  } finally {
    loading.value = false
  }
}

async function createAndWish() {
  const dishName = search.value.trim()
  if (!dishName) return
  creating.value = true
  try {
    const dishId = (await dishRepo.create(appStore.activeFamilyId, dishName)) as string
    dishRepo.enrich(appStore.activeFamilyId, dishId).catch(() => {})
    await dishesStore.updateDish(dishId)
    await handleAddWish(dishId, dishName)
  } catch {
    showSnackbar(t('wishlist.errorAdding'), { type: 'error' })
  } finally {
    creating.value = false
  }
}

function close() {
  internalOpen.value = false
  search.value = ''
}

// Clear search on close; pre-fill from prop when opening
watch(
  () => props.modelValue,
  (open) => {
    if (!open) search.value = ''
    else if (props.preselectedDish) search.value = props.preselectedDish.name
  },
)
</script>

<template>
  <v-dialog v-model="internalOpen" max-width="480" :persistent="loading || creating">
    <v-card class="wish-dialog">
      <v-card-title class="wish-dialog__title">
        <v-icon size="18" icon="mdi-star-outline" class="wish-dialog__title-icon" />
        {{ $t('wishlist.addToWishList') }}
      </v-card-title>

      <v-card-text class="wish-dialog__body">
        <v-text-field
          v-model="search"
          :placeholder="$t('wishlist.searchDishesPlaceholder')"
          variant="outlined"
          density="compact"
          rounded="lg"
          prepend-inner-icon="mdi-magnify"
          clearable
          autofocus
          hide-details
          class="wish-dialog__search mb-4"
        />

        <!-- Dish list -->
        <div v-if="filteredDishes.length > 0" class="wish-dialog__list">
          <button
            v-for="dish in filteredDishes"
            :key="dish.id"
            class="wish-dialog__item"
            :disabled="loading"
            @click="selectDish(dish)"
          >
            <span class="wish-dialog__item-name">{{ dish.name }}</span>

            <template v-if="getExistingWish(dish.id)">
              <span class="wish-dialog__item-existing">
                <v-icon size="14" icon="mdi-star" class="wish-dialog__item-star-filled" />
                {{ $t('wishlist.alreadyWishedBy', { name: getExistingWish(dish.id)!.addedByName || $t('wishlist.someone') }) }}
                — {{ $t('wishlist.tapToUpvote') }}
              </span>
            </template>
            <template v-else>
              <span class="wish-dialog__item-action">
                <v-icon size="14" icon="mdi-star-plus-outline" />
                {{ $t('wishlist.addToWishList') }}
              </span>
            </template>
          </button>
        </div>

        <div v-else-if="search && !canCreateNew" class="wish-dialog__empty">
          {{ $t('wishlist.noMatchingDishes') }}
        </div>

        <!-- Create and wish new dish -->
        <button
          v-if="canCreateNew"
          class="wish-dialog__create"
          :disabled="creating"
          @click="createAndWish"
        >
          <v-icon size="16" icon="mdi-plus" />
          <span>{{ $t('wishlist.createAndWish', { name: search.trim() }) }}</span>
          <v-progress-circular v-if="creating" size="14" width="2" indeterminate />
        </button>
      </v-card-text>

      <v-card-actions class="wish-dialog__actions">
        <v-spacer />
        <v-btn variant="text" @click="close">{{ $t('common.cancel') }}</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.wish-dialog {
  border-radius: var(--radius-lg) !important;
}

.wish-dialog__title {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-4) var(--space-4) var(--space-2);
  font-family: var(--font-display);
  font-size: var(--text-lg);
  color: var(--color-text-primary);
}

.wish-dialog__title-icon {
  color: var(--color-primary);
}

.wish-dialog__body {
  padding: var(--space-2) var(--space-4) var(--space-2);
}

.wish-dialog__list {
  display: flex;
  flex-direction: column;
  gap: 2px;
  max-height: 320px;
  overflow-y: auto;
}

.wish-dialog__item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
  padding: var(--space-3) var(--space-3);
  background: none;
  border: none;
  border-radius: var(--radius-sm);
  cursor: pointer;
  text-align: left;
  width: 100%;
  transition: background-color var(--duration-fast) var(--ease-out);
}

.wish-dialog__item:hover:not(:disabled) {
  background-color: var(--color-surface-variant);
}

.wish-dialog__item:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.wish-dialog__item-name {
  font-family: var(--font-body);
  font-size: var(--text-sm);
  font-weight: 500;
  color: var(--color-text-primary);
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.wish-dialog__item-existing {
  font-size: var(--text-xs);
  color: var(--color-primary);
  white-space: nowrap;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  gap: var(--space-1);
}

.wish-dialog__item-star-filled {
  color: var(--color-primary);
}

.wish-dialog__item-action {
  font-size: var(--text-xs);
  color: var(--color-text-muted);
  white-space: nowrap;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  gap: var(--space-1);
  transition: color var(--duration-fast) var(--ease-out);
}

.wish-dialog__item:hover .wish-dialog__item-action {
  color: var(--color-primary);
}

.wish-dialog__create {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  width: 100%;
  margin-top: var(--space-2);
  padding: var(--space-3);
  background: none;
  border: 1px dashed var(--color-border-medium);
  border-radius: var(--radius-sm);
  font-family: var(--font-body);
  font-size: var(--text-sm);
  color: var(--color-text-muted);
  cursor: pointer;
  transition:
    border-color var(--duration-fast) var(--ease-out),
    color var(--duration-fast) var(--ease-out);
}

.wish-dialog__create:hover:not(:disabled) {
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.wish-dialog__create:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.wish-dialog__empty {
  font-size: var(--text-sm);
  color: var(--color-text-muted);
  text-align: center;
  padding: var(--space-4) 0;
}

.wish-dialog__actions {
  padding: var(--space-2) var(--space-4) var(--space-4);
}
</style>

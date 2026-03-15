<script setup lang="ts">
import { DateTime } from 'luxon'
import { useDisplay } from 'vuetify'
import type { Dinner, Dish } from '~/types'

const props = defineProps<{ dinner: Dinner }>()

const emit = defineEmits<{
  'dinner:close': []
  'dinner:menuupdated': [event: { date: DateTime; dishId: string; dishName: string }]
}>()

const appStore = useAppStore()
const dishesStore = useDishesStore()
const { dishes: dishRepo, dinners: dinnerRepo } = useRepositories()
const { smAndDown } = useDisplay()

const dishSearch = ref('')
const selectedDish = ref<Dish | null>(null)
const dishSelector = ref<HTMLElement | null>(null)
const mobileSheetOpen = ref(false)
const mobileSearchRef = ref<InstanceType<typeof import('vuetify/components').VTextField> | null>(null)

function getDaysSince(dish: Dish): number | null {
  const lastUsed = dish.dishStats?.lastUsed
  if (!lastUsed) return null
  const days = Math.round(
    DateTime.now()
      .diff(DateTime.fromISO(lastUsed as unknown as string), 'days')
      .days,
  )
  return days >= 0 ? days : null
}

function formatRating(rating: number): string {
  return rating > 0 ? rating.toFixed(1) : '—'
}

const filteredDishes = computed(() => {
  const q = dishSearch.value.toLowerCase().trim()
  if (!q) return dishesStore.dishes
  return dishesStore.dishes.filter((d) => d.name.toLowerCase().includes(q))
})

async function onDishSelected(dish: Dish | null) {
  if (!dish) return
  await addDishToMenu(dish.id)
  selectedDish.value = null
}

async function createDish() {
  const dishName = dishSearch.value.trim()
  if (!dishName) return
  dishSearch.value = ''
  dishSelector.value?.blur()
  const dishId = (await dishRepo.create(appStore.activeFamilyId, dishName)) as string
  await dishesStore.updateDish(dishId)
  await addDishToMenu(dishId)
}

async function addDishToMenu(dishId: string) {
  await dinnerRepo.addDishToMenu(appStore.activeFamilyId, props.dinner.date, dishId)
  const dish = dishesStore.dishes.find((d) => d.id === dishId)
  emit('dinner:menuupdated', { date: props.dinner.date, dishId, dishName: dish?.name ?? '' })
}

async function removeDishFromMenu(dishId: string) {
  await dinnerRepo.removeDishFromMenu(appStore.activeFamilyId, props.dinner.date, dishId)
  emit('dinner:menuupdated', { date: props.dinner.date, dishId, dishName: '' })
}

async function selectDishFromSheet(dish: Dish) {
  mobileSheetOpen.value = false
  dishSearch.value = ''
  await addDishToMenu(dish.id)
}

async function createDishFromSheet() {
  mobileSheetOpen.value = false
  await createDish()
}

watch(mobileSheetOpen, (open) => {
  if (!open) dishSearch.value = ''
})

function focusMobileSearch() {
  nextTick(() => {
    const input = mobileSearchRef.value?.$el?.querySelector('input')
    input?.focus()
  })
}
</script>

<template>
  <div class="dinner-details">
    <div class="dinner-details__body">
      <!-- Current menu as removable DishPills -->
      <div v-if="dinner.menu.length > 0" class="dinner-details__menu">
        <DishPill
          v-for="item in dinner.menu"
          :key="item.dishId"
          :name="item.dishName"
          :to="`/dishes/${item.dishId}`"
          size="md"
          removable
          @remove="removeDishFromMenu(item.dishId)"
        />
      </div>

      <!-- Mobile: button trigger -->
      <template v-if="smAndDown">
        <v-btn
          variant="outlined"
          color="primary"
          block
          class="dinner-details__mobile-trigger"
          prepend-icon="mdi-plus"
          @click="mobileSheetOpen = true"
        >
          Add dish to menu
        </v-btn>

        <v-bottom-sheet
          v-model="mobileSheetOpen"
          :max-height="'80vh'"
          @after-enter="focusMobileSearch"
        >
          <v-card class="sheet-card">
            <div class="sheet-handle" />
            <div class="sheet-header">
              <span class="sheet-title">Add dish</span>
            </div>

            <div class="sheet-search">
              <v-text-field
                ref="mobileSearchRef"
                v-model="dishSearch"
                variant="outlined"
                density="compact"
                placeholder="Search dishes..."
                prepend-inner-icon="mdi-magnify"
                clearable
                hide-details
              />
            </div>

            <div class="sheet-list">
              <v-list density="compact">
                <template v-if="filteredDishes.length > 0">
                  <v-list-item
                    v-for="dish in filteredDishes"
                    :key="dish.id"
                    :title="undefined"
                    class="dish-row"
                    min-height="48"
                    @click="selectDishFromSheet(dish)"
                  >
                    <div class="dish-row__inner">
                      <span class="dish-row__name">{{ dish.name }}</span>
                      <span class="dish-row__rating">
                        <v-icon size="12" color="primary">mdi-heart</v-icon>
                        {{ formatRating(dish.rating) }}
                      </span>
                      <span v-if="getDaysSince(dish) !== null" class="dish-row__days">
                        {{ getDaysSince(dish) }}d ago
                      </span>
                    </div>
                  </v-list-item>
                </template>

                <v-list-item v-if="filteredDishes.length === 0 && !dishSearch">
                  <span class="text-body-2 text-medium-emphasis">No dishes yet</span>
                </v-list-item>

                <template v-if="dishSearch">
                  <v-divider v-if="filteredDishes.length > 0" class="my-1" />
                  <v-list-item class="create-dish-item" min-height="48" @click="createDishFromSheet">
                    <template #prepend>
                      <v-icon size="16" color="primary">mdi-plus-circle-outline</v-icon>
                    </template>
                    <span class="text-body-2">
                      Create <strong>"{{ dishSearch }}"</strong>
                    </span>
                  </v-list-item>
                </template>
              </v-list>
            </div>
          </v-card>
        </v-bottom-sheet>
      </template>

      <!-- Desktop: compact autocomplete -->
      <v-autocomplete
        v-else
        ref="dishSelector"
        v-model="selectedDish"
        v-model:search="dishSearch"
        :items="dishesStore.dishes"
        item-title="name"
        item-value="id"
        return-object
        variant="outlined"
        density="compact"
        label="Add dish to menu"
        placeholder="Search dishes..."
        class="dinner-details__autocomplete"
        @update:model-value="onDishSelected"
        @keyup.enter="createDish"
      >
        <template #item="{ item, props: itemProps }">
          <v-list-item v-bind="itemProps" :title="undefined" min-height="36" class="dish-row">
            <div class="dish-row__inner">
              <span class="dish-row__name">{{ item.raw.name }}</span>
              <span class="dish-row__rating">
                <v-icon size="12" color="primary">mdi-heart</v-icon>
                {{ formatRating(item.raw.rating) }}
              </span>
              <span v-if="getDaysSince(item.raw) !== null" class="dish-row__days">
                {{ getDaysSince(item.raw) }}d ago
              </span>
            </div>
          </v-list-item>
        </template>

        <template #no-data>
          <v-list-item v-if="!dishSearch">
            <span class="text-body-2 text-medium-emphasis">Start typing to search</span>
          </v-list-item>
          <v-list-item v-else class="create-dish-item" @click="createDish">
            <template #prepend>
              <v-icon size="16" color="primary">mdi-plus-circle-outline</v-icon>
            </template>
            <span class="text-body-2">
              Create <strong>"{{ dishSearch }}"</strong>
            </span>
          </v-list-item>
        </template>

        <template #append-item>
          <template v-if="dishSearch">
            <v-divider class="my-1" />
            <v-list-item class="create-dish-item" @click="createDish">
              <template #prepend>
                <v-icon size="16" color="primary">mdi-plus-circle-outline</v-icon>
              </template>
              <span class="text-body-2">
                Create <strong>"{{ dishSearch }}"</strong>
              </span>
            </v-list-item>
          </template>
        </template>
      </v-autocomplete>
    </div>
  </div>
</template>

<style scoped>
.dinner-details {
  border-top: 1px solid rgba(0, 0, 0, 0.07);
}

/* Body */
.dinner-details__body {
  padding: var(--space-4);
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

/* Menu pills */
.dinner-details__menu {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}

/* Autocomplete */
.dinner-details__autocomplete {
  margin-bottom: 0;
}

/* Mobile trigger */
.dinner-details__mobile-trigger {
  text-transform: none;
  letter-spacing: 0;
}

/* Compact dish row (shared desktop + mobile) */
.dish-row__inner {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  width: 100%;
  min-height: 0;
  padding: 0;
}

.dish-row__name {
  flex: 1;
  font-size: var(--text-sm);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.dish-row__rating {
  display: flex;
  align-items: center;
  gap: 3px;
  font-size: var(--text-xs);
  color: var(--color-text-muted);
  white-space: nowrap;
  flex-shrink: 0;
}

.dish-row__days {
  font-size: var(--text-xs);
  color: var(--color-text-muted);
  white-space: nowrap;
  flex-shrink: 0;
}

/* Create new dish row */
.create-dish-item {
  cursor: pointer;
}

.create-dish-item:hover {
  background: var(--color-surface-variant);
}

/* Bottom sheet */
.sheet-card {
  border-radius: var(--radius-xl) var(--radius-xl) 0 0 !important;
  max-height: 80vh;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  padding-bottom: env(safe-area-inset-bottom, 0);
}

.sheet-handle {
  width: 36px;
  height: 4px;
  border-radius: var(--radius-full);
  background: rgba(0, 0, 0, 0.15);
  margin: 12px auto 0;
  flex-shrink: 0;
}

.sheet-header {
  padding: 12px var(--space-4) 8px;
  flex-shrink: 0;
}

.sheet-title {
  font-size: var(--text-base);
  font-weight: 600;
}

.sheet-search {
  padding: 0 var(--space-4) var(--space-3);
  flex-shrink: 0;
}

.sheet-list {
  overflow-y: auto;
  overscroll-behavior: contain;
  flex: 1;
}
</style>

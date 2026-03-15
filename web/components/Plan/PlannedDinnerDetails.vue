<script setup lang="ts">
import { DateTime } from 'luxon'
import type { Dinner, Dish } from '~/types'

const props = defineProps<{ dinner: Dinner }>()

const emit = defineEmits<{
  'dinner:close': []
  'dinner:menuupdated': [event: { date: DateTime; dishId: string; dishName: string }]
}>()

const appStore = useAppStore()
const dishesStore = useDishesStore()
const { dishes: dishRepo, dinners: dinnerRepo } = useRepositories()

const dishSearch = ref('')
const selectedDish = ref<Dish | null>(null)
const dishSelector = ref<HTMLElement | null>(null)

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

      <!-- Dish autocomplete -->
      <v-autocomplete
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
          <v-list-item v-bind="itemProps" :title="item.raw.name">
            <template #subtitle>
              <span class="dish-option-meta">
                <DishRating :model-value="item.raw.rating" size="x-small" />
                <span v-if="getDaysSince(item.raw) !== null" class="dish-option-days">
                  {{ getDaysSince(item.raw) }}d ago
                </span>
              </span>
            </template>
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

/* Dish option metadata */
.dish-option-meta {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  margin-top: 2px;
}

.dish-option-days {
  font-size: var(--text-xs);
  color: var(--color-text-muted);
}

/* Create new dish row */
.create-dish-item {
  cursor: pointer;
}

.create-dish-item:hover {
  background: var(--color-surface-variant);
}
</style>

<template>
  <v-card>
    <v-card-subtitle style="cursor: pointer" @click="emit('dinner:close')">
      <v-row>
        <v-col style="display: flex">
          {{ formatDate(dinner.date) }}
          <v-spacer />
          <v-icon>mdi-close-circle-outline</v-icon>
        </v-col>
      </v-row>
    </v-card-subtitle>

    <v-card-text>
      <v-row>
        <v-col>
          <v-autocomplete
            ref="dishSelector"
            v-model="selectedDish"
            v-model:search="dishSearch"
            :items="dishesStore.dishes"
            item-title="name"
            item-value="id"
            return-object
            variant="outlined"
            label="Add dish to menu"
            placeholder="Start typing to search"
            @update:model-value="onDishSelected"
            @keyup.enter="createDish"
          >
            <template #item="{ item, props: itemProps }">
              <v-list-item v-bind="itemProps" :title="item.raw.name">
                <template #append>
                  <DishRating :model-value="item.raw.rating" />
                </template>
              </v-list-item>
            </template>

            <template #no-data>
              <v-list-item v-if="!dishSearch">
                <span class="text-subtitle-1">Enter name of dish</span>
              </v-list-item>
              <v-list-item v-else @click="createDish">
                <span class="text-subtitle-1">Create "{{ dishSearch }}"</span>
              </v-list-item>
            </template>
          </v-autocomplete>

          <v-list>
            <v-list-item v-for="menuItem in dinner.menu" :key="menuItem.dishId">
              <v-list-item-title>{{ menuItem.dishName }}</v-list-item-title>
              <template #append>
                <v-btn icon variant="text" @click="navigateTo('/dishes/' + menuItem.dishId)">
                  <v-icon>mdi-information-outline</v-icon>
                </v-btn>
                <v-btn icon variant="text" @click="removeDishFromMenu(menuItem.dishId)">
                  <v-icon>mdi-close-circle-outline</v-icon>
                </v-btn>
              </template>
            </v-list-item>
          </v-list>
        </v-col>
      </v-row>
    </v-card-text>
  </v-card>
</template>

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

function formatDate(date: DateTime) {
  return date.toLocaleString(DateTime.DATE_HUGE)
}

async function onDishSelected(dish: Dish | null) {
  if (!dish) return
  await addDishToMenu(dish.id)
  selectedDish.value = null
}

async function createDish() {
  const dishName = dishSearch.value
  if (!dishName) return
  dishSearch.value = ''
  ;(dishSelector.value as any)?.blur()
  const dishId = await dishRepo.create(appStore.activeFamilyId, dishName) as string
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

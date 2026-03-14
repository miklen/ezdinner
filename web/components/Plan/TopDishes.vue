<template>
  <span>
    <v-row>
      <v-col class="text-center">
        <v-card-title>Top {{ top }}</v-card-title>
      </v-col>
      <v-col cols="3" xl="2">
        <v-select v-model="top" :items="choices" />
      </v-col>
    </v-row>

    <v-row>
      <v-col>
        <v-table>
          <thead>
            <tr>
              <th class="text-left">Dish</th>
              <th class="text-center">Times</th>
              <th class="text-center">Rating</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in topDishes" :key="item.name">
              <td>{{ item.name }}</td>
              <td class="text-center">{{ item.times }}</td>
              <td class="text-center">
                <DishRating :model-value="item.rating" />
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-col>
    </v-row>
  </span>
</template>

<script setup lang="ts">
import type { DishStats } from '~/types'

const appStore = useAppStore()
const dishesStore = useDishesStore()
const { dishes: dishRepo } = useRepositories()

const stats = ref<Record<string, DishStats>>({})
const top = ref(10)
const choices = [10, 25, 50]

const topDishes = computed(() =>
  dishesStore.dishes
    .map((d) => ({ name: d.name, times: stats.value[d.id]?.timesUsed ?? 0, rating: d.rating }))
    .sort((a, b) => b.times - a.times)
    .slice(0, Math.min(top.value, dishesStore.dishes.length)),
)

onMounted(async () => {
  stats.value = await dishRepo.allUsageStats(appStore.activeFamilyId)
  if (!dishesStore.dishes.length) {
    dishesStore.populateDishes()
  }
})
</script>

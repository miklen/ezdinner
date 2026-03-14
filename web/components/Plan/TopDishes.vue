<script setup lang="ts">
import type { DishStats } from '~/types'

const appStore = useAppStore()
const dishesStore = useDishesStore()
const { dishes: dishRepo } = useRepositories()

const stats = ref<Record<string, DishStats>>({})
const top = shallowRef(10)
const choices = [10, 25, 50]

const topDishes = computed(() =>
  dishesStore.dishes
    .map((d) => ({ name: d.name, times: stats.value[d.id]?.timesUsed ?? 0, rating: d.rating }))
    .sort((a, b) => b.times - a.times)
    .slice(0, Math.min(top.value, dishesStore.dishes.length)),
)

const maxTimes = computed(() => Math.max(1, ...topDishes.value.map((d) => d.times)))

function barWidth(times: number): number {
  return Math.round((times / maxTimes.value) * 100)
}

onMounted(async () => {
  stats.value = await dishRepo.allUsageStats(appStore.activeFamilyId)
  if (!dishesStore.dishes.length) {
    dishesStore.populateDishes()
  }
})
</script>

<template>
  <div class="top-dishes">
    <div class="top-dishes__header">
      <span class="text-section-title top-dishes__title">Top Dishes</span>
      <v-select
        v-model="top"
        :items="choices"
        density="compact"
        variant="outlined"
        rounded="lg"
        hide-details
        style="max-width: 90px"
      />
    </div>

    <div class="top-dishes__list">
      <div v-for="item in topDishes" :key="item.name" class="dish-row">
        <div class="dish-row__header">
          <span class="dish-row__name">{{ item.name }}</span>
          <DishRating :model-value="item.rating" />
        </div>
        <div class="dish-row__bar-track">
          <div class="dish-row__bar" :style="{ width: barWidth(item.times) + '%' }" />
          <span class="dish-row__count">{{ item.times }}×</span>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.top-dishes {
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.top-dishes__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
}

.top-dishes__title {
  color: var(--color-text-primary);
}

.top-dishes__list {
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.dish-row {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.dish-row__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-2);
}

.dish-row__name {
  font-family: var(--font-body);
  font-size: var(--text-sm);
  font-weight: 500;
  color: var(--color-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  flex: 1;
}

.dish-row__bar-track {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.dish-row__bar {
  height: 6px;
  background-color: var(--color-primary);
  border-radius: var(--radius-full);
  transition: width var(--duration-normal) var(--ease-out);
  opacity: 0.65;
  min-width: 4px;
}

.dish-row__count {
  font-family: var(--font-body);
  font-size: var(--text-xs);
  color: var(--color-text-muted);
  white-space: nowrap;
  flex-shrink: 0;
}
</style>

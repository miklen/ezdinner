<script setup lang="ts">
import { DateTime } from 'luxon'
import type { Dinner, DishStats } from '~/types'

useHead({ title: 'Home' })

const { $msal } = useNuxtApp()
const appStore = useAppStore()
const dishesStore = useDishesStore()
const { dinners: dinnerRepo, dishes: dishRepo } = useRepositories()

const loading = shallowRef(true)
const dinners = ref<Dinner[]>([])
const stats = ref<Record<string, DishStats>>({})

const firstName = computed(() => $msal.getFirstName() ?? '')

const tonight = computed(() => dinners.value[0] ?? null)
const tomorrow = computed(() => dinners.value[1] ?? null)

const weekDishCount = computed(() => {
  const dishIds = new Set<string>()
  dinners.value.forEach((d) => d.menu.forEach((m) => dishIds.add(m.dishId)))
  return dishIds.size
})

const topDishName = computed(() => {
  return dishesStore.dishes
    .map((d) => ({ name: d.name, times: stats.value[d.id]?.timesUsed ?? 0 }))
    .filter((d) => d.times > 0)
    .sort((a, b) => b.times - a.times)[0]?.name ?? null
})

const overdueFavorite = computed(() => {
  const candidate = dishesStore.dishes
    .filter((d) => d.rating > 0 && stats.value[d.id]?.lastUsed)
    .sort((a, b) => {
      const daysA = DateTime.now().diff(DateTime.fromISO(stats.value[a.id].lastUsed as unknown as string), 'days').days
      const daysB = DateTime.now().diff(DateTime.fromISO(stats.value[b.id].lastUsed as unknown as string), 'days').days
      return b.rating - a.rating || daysB - daysA
    })[0]
  if (!candidate) return null
  const lastUsed = DateTime.fromISO(stats.value[candidate.id].lastUsed as unknown as string)
  const days = Math.floor(DateTime.now().diff(lastUsed, 'days').days)
  return { name: candidate.name, days }
})

async function init() {
  if (!appStore.activeFamilyId) return
  loading.value = true
  try {
    const today = DateTime.now()
    type RawDinner = Omit<Dinner, 'date'> & { date: string }
    const [rawDinners, rawStats] = await Promise.all([
      dinnerRepo.getRange(appStore.activeFamilyId, today, today.plus({ days: 6 })),
      dishRepo.allUsageStats(appStore.activeFamilyId),
      dishesStore.populateDishes(),
    ])
    stats.value = rawStats as Record<string, DishStats>
    dinners.value = (rawDinners as unknown as RawDinner[]).map((dinner) => ({
      ...dinner,
      date: DateTime.fromISO(dinner.date),
      menu: dinner.menu.map((item) => ({
        ...item,
        dishName: dishesStore.dishMap[item.dishId] ?? 'Dish not available',
      })),
    }))
  } finally {
    loading.value = false
  }
}

watch(() => appStore.activeFamilyId, init, { immediate: true })
</script>

<template>
  <Content :split="!!appStore.activeFamilyId">
    <template v-if="!appStore.activeFamilyId">
      <div class="no-family">
        <EmptyState
          icon="mdi-account-group"
          message="Get started by creating a family and begin tracking and planning your meals."
          action-label="Go to Families"
          action-to="/families"
        />
      </div>
    </template>

    <template v-else>
      <div class="home-main">
        <HomeDinnerHeroCard :dinner="tonight" :loading="loading" :first-name="firstName" />

        <!-- Tomorrow preview -->
        <v-skeleton-loader v-if="loading" type="text" height="56" rounded="lg" />
        <div v-else class="tomorrow-card">
          <span class="tomorrow-card__label">TOMORROW</span>
          <template v-if="tomorrow && tomorrow.menu.length">
            <div class="tomorrow-card__menu">
              <DishPill
                v-for="item in tomorrow.menu"
                :key="item.dishId"
                :name="item.dishName"
                :to="`/dishes/${item.dishId}`"
                size="sm"
              />
            </div>
          </template>
          <span v-else class="tomorrow-card__empty">
            Nothing planned —
            <NuxtLink to="/plan" class="tomorrow-card__link">plan tomorrow</NuxtLink>
          </span>
        </div>

        <HomeQuickStats
          :week-dish-count="weekDishCount"
          :overdue-favorite-name="overdueFavorite?.name ?? null"
          :overdue-favorite-days="overdueFavorite?.days ?? null"
          :loading="loading"
        />
      </div>
    </template>

    <template #support>
      <PlanTopDishes v-if="appStore.activeFamilyId" />
    </template>
  </Content>
</template>

<style scoped>
.no-family {
  display: flex;
  justify-content: center;
  padding: var(--space-12) 0;
}

.home-main {
  display: flex;
  flex-direction: column;
  gap: var(--space-6);
}

.tomorrow-card {
  background: var(--color-surface-variant);
  border: 1px solid rgba(0, 0, 0, 0.05);
  border-radius: var(--radius-lg);
  padding: var(--space-4) var(--space-6);
  display: flex;
  align-items: center;
  gap: var(--space-4);
  flex-wrap: wrap;
}

.tomorrow-card__label {
  font-family: var(--font-body);
  font-size: var(--text-xs);
  font-weight: 600;
  letter-spacing: 0.08em;
  color: var(--color-text-muted);
  flex-shrink: 0;
}

.tomorrow-card__menu {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}

.tomorrow-card__empty {
  font-family: var(--font-body);
  font-size: var(--text-sm);
  color: var(--color-text-muted);
}

.tomorrow-card__link {
  color: var(--color-primary-dark);
  text-decoration: none;
}

.tomorrow-card__link:hover {
  text-decoration: underline;
}
</style>

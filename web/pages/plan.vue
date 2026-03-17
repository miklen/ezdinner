<template>
  <Content split>
    <div class="plan-page">
      <PlanWeekNav v-model="weekStart" class="mb-4" />

      <!-- Skeleton loading — 9 placeholders (7 days + 2 prev weekend) -->
      <template v-if="loading">
        <div v-for="i in 9" :key="i" class="skeleton-card mb-3">
          <v-skeleton-loader type="list-item-two-line" />
        </div>
      </template>

      <template v-else>
        <!-- Suggestion bar — show below week header, above dinner list -->
        <PlanSuggestionBar
          :week-start="weekStart"
          :planned-dinners="dinnersStore.dinners"
          class="mb-4"
          @dish:used="onSuggestionUsed"
        />

        <!-- Previous weekend — visually separated -->
        <template v-if="prevWeekendDinners.length > 0">
          <div class="week-section-label">{{ prevWeekendLabel }}</div>
          <PlanPlannedDinner
            v-for="dinner in prevWeekendDinners"
            :key="dinner.date.toFormat('yyyy-MM-dd')"
            :dinner="dinner"
            :selected="isDinnerSelected(dinner)"
            class="mb-3 dinner-prev-weekend"
            @dinner:clicked="selectedDate = dinner.date"
            @dinner:close="selectedDate = null"
            @dinner:menuupdated="menuUpdated"
            @dinner:optoutupdated="menuUpdated"
          />
          <div class="week-divider" />
        </template>

        <!-- Current week -->
        <PlanPlannedDinner
          v-for="dinner in currentWeekDinners"
          :key="dinner.date.toFormat('yyyy-MM-dd')"
          :dinner="dinner"
          :selected="isDinnerSelected(dinner)"
          class="mb-3"
          @dinner:clicked="selectedDate = dinner.date"
          @dinner:close="selectedDate = null"
          @dinner:menuupdated="menuUpdated"
          @dinner:optoutupdated="menuUpdated"
        />
      </template>
    </div>

    <template #support>
      <PlanTopDishes />
    </template>
  </Content>
</template>

<script setup lang="ts">
import { DateTime } from 'luxon'
import type { Dinner } from '~/types'

useHead({ title: 'Plan' })

const appStore = useAppStore()
const dishesStore = useDishesStore()
const dinnersStore = useDinnersStore()

// Default to next week when today is Saturday (6) or Sunday (7) —
// the user is likely planning ahead rather than the week that's ending.
const todayWeekday = DateTime.now().weekday
const defaultWeekStart = todayWeekday >= 6
  ? DateTime.now().plus({ weeks: 1 }).startOf('week')
  : DateTime.now().startOf('week')

const weekStart = ref(defaultWeekStart)
const selectedDate = ref<DateTime | null>(null)
const loading = ref(false)

// Load from the Saturday before the week's Monday so we always include
// the prev weekend at the top of the list.
const loadFrom = computed(() => weekStart.value.minus({ days: 2 }))
const weekEnd = computed(() => weekStart.value.endOf('week'))

const prevWeekendDinners = computed(() =>
  dinnersStore.dinners.filter((d) => d.date < weekStart.value),
)

// Label for the prev-weekend section — always uses week number so it
// remains accurate whether browsing current, past, or future weeks.
const prevWeekendLabel = computed(() => {
  const wk = weekStart.value.minus({ days: 2 }).weekNumber
  return `Week ${wk} weekend`
})

const currentWeekDinners = computed(() =>
  dinnersStore.dinners.filter((d) => d.date >= weekStart.value),
)

async function loadWeek() {
  loading.value = true
  await Promise.all([
    dishesStore.populateDishes(),
    dinnersStore.populateDinners(loadFrom.value, weekEnd.value),
    dinnersStore.fetchOptOutReasons(),
  ])
  loading.value = false
}

function isDinnerSelected(dinner: Dinner) {
  return !!selectedDate.value && dinner.date.equals(selectedDate.value)
}

function menuUpdated() {
  dinnersStore.populateDinners(loadFrom.value, weekEnd.value)
}

async function onSuggestionUsed(date: string, dishId: string, _dishName: string) {
  const { dinners: dinnerRepo } = useRepositories()
  const dt = DateTime.fromISO(date)
  await dinnerRepo.addDishToMenu(appStore.activeFamilyId, dt, dishId)
  await dinnersStore.populateDinners(loadFrom.value, weekEnd.value)
}

onMounted(loadWeek)
watch(weekStart, () => {
  selectedDate.value = null
  loadWeek()
})
watch(
  () => appStore.activeFamilyId,
  (val) => { if (val) loadWeek() },
)
</script>

<style scoped>
.plan-page {
  padding-bottom: var(--space-8);
}

.skeleton-card {
  border-radius: var(--radius-lg);
  overflow: hidden;
  box-shadow: var(--shadow-sm);
  border: 1px solid rgba(0, 0, 0, 0.06);
}

.week-section-label {
  font-size: var(--text-xs);
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--color-text-muted);
  padding: 0 var(--space-1);
  margin-bottom: var(--space-2);
}

.week-divider {
  height: 1px;
  background: rgba(0, 0, 0, 0.08);
  margin: var(--space-2) 0 var(--space-4);
}

/* Slightly reduced opacity to distinguish prev-week cards from this week */
:deep(.dinner-prev-weekend) {
  opacity: 0.8;
}
</style>

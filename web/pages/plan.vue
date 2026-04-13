<template>
  <div>
  <Content split desktop-only-support>
    <div class="plan-page">
      <PlanWeekNav v-model="weekStart" class="mb-4" />

      <!-- Skeleton loading — 9 placeholders (7 days + 2 prev weekend) -->
      <template v-if="loading">
        <div v-for="i in 9" :key="i" class="skeleton-card mb-3">
          <v-skeleton-loader type="list-item-two-line" />
        </div>
      </template>

      <template v-else>
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
      <PlanAssistantPanel
        :week-start="weekStart"
        :dinners="dinnersStore.dinners"
        @dish:assigned="onDishAssigned"
      />
    </template>
  </Content>

  <!-- Mobile: FAB to open panel as bottom sheet -->
  <v-btn
    class="d-flex d-md-none plan-fab"
    icon="mdi-auto-fix"
    color="primary"
    size="large"
    elevation="4"
    @click="mobileSheetOpen = true"
  />

  <v-bottom-sheet
    v-model="mobileSheetOpen"
    class="d-md-none"
    :max-height="'85dvh'"
    scrollable
  >
    <v-sheet class="mobile-sheet">
      <div class="mobile-sheet__handle" />
      <PlanAssistantPanel
        :week-start="weekStart"
        :dinners="dinnersStore.dinners"
        @dish:assigned="onMobileSheetDishAssigned"
      />
    </v-sheet>
  </v-bottom-sheet>
  </div>
</template>

<script setup lang="ts">
import type { DateTime } from 'luxon'
import type { Dinner } from '~/types'

useHead({ title: 'Plan' })

const appStore = useAppStore()
const dishesStore = useDishesStore()
const dinnersStore = useDinnersStore()
const wishlistStore = useWishlistStore()

const { weekStart } = useWeekNav()
const selectedDate = ref<DateTime | null>(null)
const loading = ref(false)
const mobileSheetOpen = ref(false)

// Load from the Saturday before the week's Monday so we always include
// the prev weekend at the top of the list.
const loadFrom = computed(() => weekStart.value.minus({ days: 2 }))
const weekEnd = computed(() => weekStart.value.endOf('week'))

const prevWeekendDinners = computed(() =>
  dinnersStore.dinners.filter((d) => d.date < weekStart.value),
)

// Label for the prev-weekend section — always uses week number so it
// remains accurate whether browsing current, past, or future weeks.
const { t } = useI18n()

const prevWeekendLabel = computed(() => {
  const wk = weekStart.value.minus({ days: 2 }).weekNumber
  return t('plan.weekWeekend', { week: wk })
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
    wishlistStore.fetchWishes(),
    dishesStore.populateStats(),
  ])
  loading.value = false
}

function isDinnerSelected(dinner: Dinner) {
  return !!selectedDate.value && dinner.date.equals(selectedDate.value)
}

function menuUpdated() {
  dinnersStore.populateDinners(loadFrom.value, weekEnd.value)
  wishlistStore.fetchWishes()
}

function onDishAssigned(_date: string, _dishId: string) {
  dinnersStore.populateDinners(loadFrom.value, weekEnd.value)
  wishlistStore.fetchWishes()
}

function onMobileSheetDishAssigned(date: string, dishId: string) {
  mobileSheetOpen.value = false
  onDishAssigned(date, dishId)
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

.plan-fab {
  position: fixed;
  bottom: calc(64px + var(--space-4));
  right: var(--space-4);
  z-index: 100;
}

.mobile-sheet {
  padding: var(--space-4);
  height: 85dvh;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.mobile-sheet__handle {
  width: 36px;
  height: 4px;
  border-radius: var(--radius-full);
  background: rgba(0, 0, 0, 0.15);
  margin: 0 auto var(--space-4);
  flex-shrink: 0;
}
</style>

<template>
  <div>
  <Content split desktop-only-support>
    <div class="plan-page">
      <WeekPlan
        :monday="weekStart"
        :dinners="dinnersStore.dinners"
        :selected-date="selectedDate"
        :loading="loading"
        @navigate="weekStart = $event"
        @select="selectedDate = $event"
        @dinner:menuupdated="menuUpdated"
        @dinner:optoutupdated="menuUpdated"
      />
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
import WeekPlan from '~/components/Plan/WeekPlan.vue'

useHead({ title: 'Plan' })

const appStore = useAppStore()
const dishesStore = useDishesStore()
const dinnersStore = useDinnersStore()
const wishlistStore = useWishlistStore()

const { weekStart } = useWeekNav()
const selectedDate = ref<string | null>(null)
const loading = ref(false)
const mobileSheetOpen = ref(false)

// Load from the Saturday before the week's Monday so we always include
// the prev weekend at the top of the list.
const loadFrom = computed(() => weekStart.value.minus({ days: 2 }))
const weekEnd = computed(() => weekStart.value.endOf('week'))

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

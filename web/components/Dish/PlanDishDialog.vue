<script setup lang="ts">
import { DateTime } from 'luxon'
import { useDisplay } from 'vuetify'
import type { Dinner } from '~/types'

const props = defineProps<{
  modelValue: boolean
  dishId: string
  dishName: string
  familyId: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  planned: [date: DateTime]
}>()

const { smAndDown, height: displayHeight } = useDisplay()
const { dinners: dinnerRepo } = useRepositories()
const { show: showSnackbar } = useSnackbar()
const { t, locale } = useI18n()
const dishesStore = useDishesStore()

// ── Week navigation — mirrors plan page ───────────────────────────────────────

const { weekStart } = useWeekNav()

// Load from Saturday before the week's Monday (same as plan.vue: weekStart - 2 days)
const loadFrom = computed(() => weekStart.value.minus({ days: 2 }))
const weekEnd = computed(() => weekStart.value.endOf('week'))

// 9-day window: Sat, Sun, Mon, Tue, Wed, Thu, Fri, Sat, Sun
const planningWindow = computed<DateTime[]>(() => {
  const days: DateTime[] = []
  for (let i = 0; i <= 8; i++) {
    days.push(loadFrom.value.plus({ days: i }))
  }
  return days
})

// ── Dinner data ────────────────────────────────────────────────────────────────

const dinners = ref<Dinner[]>([])
const loading = shallowRef(false)
const addingDate = shallowRef<string | null>(null)

async function fetchDinners() {
  loading.value = true
  try {
    await dishesStore.populateDishes()
    type RawDinner = Omit<import('~/types').Dinner, 'date'> & { date: string }
    const raw = await dinnerRepo.getRange(props.familyId, loadFrom.value, weekEnd.value) as unknown as RawDinner[]
    dinners.value = raw.map(dinner => ({
      ...dinner,
      date: DateTime.fromISO(dinner.date),
      menu: dinner.menu.map(item => ({
        ...item,
        dishName: dishesStore.dishMap[item.dishId] ?? item.dishName ?? '',
      })),
    }))
  } finally {
    loading.value = false
  }
}

function dinnerForDate(date: DateTime): Dinner | undefined {
  return dinners.value.find(d => {
    const dDate = d.date instanceof DateTime ? d.date : DateTime.fromISO(d.date as unknown as string)
    return dDate.toISODate() === date.toISODate()
  })
}

function menuForDate(date: DateTime) {
  return dinnerForDate(date)?.menu ?? []
}

function isDishOnDay(date: DateTime): boolean {
  return menuForDate(date).some(item => item.dishId === props.dishId)
}

// ── Add / remove ───────────────────────────────────────────────────────────────

async function toggleDay(date: DateTime) {
  if (isDishOnDay(date)) {
    await removeFromDay(date)
  } else {
    await addToDay(date)
  }
}

async function addToDay(date: DateTime) {
  const dateKey = date.toFormat('yyyy-MM-dd')
  addingDate.value = dateKey
  try {
    await dinnerRepo.addDishToMenu(props.familyId, date, props.dishId)
    const existing = dinnerForDate(date)
    if (existing) {
      existing.menu = [...existing.menu, { dishId: props.dishId, dishName: props.dishName }]
    } else {
      dinners.value = [
        ...dinners.value,
        {
          date,
          menu: [{ dishId: props.dishId, dishName: props.dishName }],
          description: '',
          isPlanned: true,
          isOptedOut: false,
          optOutReason: null,
          isResolved: false,
        },
      ]
    }
    const dayLabel = date.setLocale(locale.value).toFormat('EEEE d MMM')
    showSnackbar(t('dishes.planDish.plannedSnackbar', { dish: props.dishName, day: dayLabel }), { type: 'success' })
    emit('planned', date)
  } finally {
    addingDate.value = null
  }
}

async function removeFromDay(date: DateTime) {
  const dateKey = date.toFormat('yyyy-MM-dd')
  addingDate.value = dateKey
  try {
    await dinnerRepo.removeDishFromMenu(props.familyId, date, props.dishId)
    const existing = dinnerForDate(date)
    if (existing) {
      existing.menu = existing.menu.filter(item => item.dishId !== props.dishId)
    }
  } finally {
    addingDate.value = null
  }
}

// ── Lifecycle ──────────────────────────────────────────────────────────────────

watch(() => props.modelValue, (open) => {
  if (open) fetchDinners()
})

watch(weekStart, () => {
  if (props.modelValue) fetchDinners()
})

function close() {
  emit('update:modelValue', false)
}

// ── Helpers ────────────────────────────────────────────────────────────────────

function isWeekend(date: DateTime) {
  return date.weekday === 6 || date.weekday === 7
}

function dateKey(date: DateTime) {
  return date.toFormat('yyyy-MM-dd')
}

const sheetMaxHeight = computed(() =>
  displayHeight.value < 600 ? '90dvh' : '80dvh'
)
</script>

<template>
  <!-- Desktop: centered dialog -->
  <v-dialog
    v-if="!smAndDown"
    :model-value="modelValue"
    width="440"
    @update:model-value="close"
  >
    <v-card class="plan-dialog">
      <div class="plan-dialog__header">
        <span class="plan-dialog__title">{{ $t('dishes.planDish.dialogTitle', { dish: dishName }) }}</span>
        <button class="plan-dialog__close" :aria-label="$t('dishes.planDish.close')" @click="close">
          <v-icon size="18">mdi-close</v-icon>
        </button>
      </div>

      <!-- Week nav — same component as plan page -->
      <div class="plan-dialog__weeknav">
        <PlanWeekNav v-model="weekStart" />
      </div>

      <div class="plan-dialog__body">
        <template v-if="loading">
          <DishPlanDayRow v-for="i in 9" :key="i" loading />
        </template>

        <template v-else>
          <!-- Prev weekend (Sat, Sun) -->
          <DishPlanDayRow
            v-for="date in planningWindow.slice(0, 2)"
            :key="dateKey(date)"
            :date="date"
            :menu="menuForDate(date)"
            :is-planned="isDishOnDay(date)"
            :is-weekend="true"
            :is-adding="addingDate === dateKey(date)"
            :dish-name="dishName"
            @toggle="toggleDay(date)"
          />

          <div class="plan-dialog__divider" />

          <!-- Mon–Sun -->
          <DishPlanDayRow
            v-for="date in planningWindow.slice(2)"
            :key="dateKey(date)"
            :date="date"
            :menu="menuForDate(date)"
            :is-planned="isDishOnDay(date)"
            :is-weekend="isWeekend(date)"
            :is-adding="addingDate === dateKey(date)"
            :dish-name="dishName"
            @toggle="toggleDay(date)"
          />
        </template>
      </div>
    </v-card>
  </v-dialog>

  <!-- Mobile: bottom sheet -->
  <v-bottom-sheet
    v-else
    :model-value="modelValue"
    :max-height="sheetMaxHeight"
    @update:model-value="close"
  >
    <v-card class="plan-sheet">
      <div class="plan-sheet__handle" />
      <div class="plan-sheet__header">
        <span class="plan-sheet__title">{{ $t('dishes.planDish.dialogTitle', { dish: dishName }) }}</span>
      </div>

      <div class="plan-sheet__weeknav">
        <PlanWeekNav v-model="weekStart" />
      </div>

      <div class="plan-sheet__body">
        <template v-if="loading">
          <DishPlanDayRow v-for="i in 9" :key="i" loading />
        </template>

        <template v-else>
          <!-- Prev weekend -->
          <DishPlanDayRow
            v-for="date in planningWindow.slice(0, 2)"
            :key="dateKey(date)"
            :date="date"
            :menu="menuForDate(date)"
            :is-planned="isDishOnDay(date)"
            :is-weekend="true"
            :is-adding="addingDate === dateKey(date)"
            :dish-name="dishName"
            @toggle="toggleDay(date)"
          />

          <div class="plan-dialog__divider" />

          <!-- Mon–Sun -->
          <DishPlanDayRow
            v-for="date in planningWindow.slice(2)"
            :key="dateKey(date)"
            :date="date"
            :menu="menuForDate(date)"
            :is-planned="isDishOnDay(date)"
            :is-weekend="isWeekend(date)"
            :is-adding="addingDate === dateKey(date)"
            :dish-name="dishName"
            @toggle="toggleDay(date)"
          />
        </template>
      </div>
    </v-card>
  </v-bottom-sheet>
</template>

<style scoped>
/* ── Dialog ──────────────────────────────────────────────────────────────────── */
.plan-dialog {
  border-radius: var(--radius-lg) !important;
  overflow: hidden;
}

.plan-dialog__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--space-4) var(--space-4) var(--space-3);
}

.plan-dialog__title {
  font-size: var(--text-base);
  font-weight: 600;
  color: var(--color-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.plan-dialog__close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border: none;
  background: none;
  border-radius: var(--radius-full);
  cursor: pointer;
  color: var(--color-text-muted);
  flex-shrink: 0;
  transition: background-color var(--duration-fast) var(--ease-out);
}

.plan-dialog__close:hover {
  background-color: var(--color-surface-variant);
  color: var(--color-text-secondary);
}

.plan-dialog__weeknav {
  padding: 0 var(--space-4) var(--space-3);
}

.plan-dialog__body {
  padding-bottom: var(--space-2);
  max-height: 440px;
  overflow-y: auto;
}

.plan-dialog__divider {
  height: 1px;
  background: var(--color-border-medium);
  margin: var(--space-1) var(--space-4);
}

/* ── Sheet ───────────────────────────────────────────────────────────────────── */
.plan-sheet {
  border-radius: var(--radius-xl) var(--radius-xl) 0 0 !important;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  padding-bottom: env(safe-area-inset-bottom, 0);
}

.plan-sheet__handle {
  width: 36px;
  height: 4px;
  border-radius: var(--radius-full);
  background: rgba(0, 0, 0, 0.15);
  margin: 12px auto 0;
  flex-shrink: 0;
}

.plan-sheet__header {
  padding: 12px var(--space-4) var(--space-2);
  flex-shrink: 0;
}

.plan-sheet__title {
  font-size: var(--text-base);
  font-weight: 600;
  color: var(--color-text-primary);
}

.plan-sheet__weeknav {
  padding: 0 var(--space-4) var(--space-3);
  flex-shrink: 0;
}

.plan-sheet__body {
  overflow-y: auto;
  overscroll-behavior: contain;
  flex: 1;
  padding-bottom: var(--space-2);
}
</style>

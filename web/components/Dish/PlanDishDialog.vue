<script setup lang="ts">
import { DateTime } from 'luxon'
import PickerShell from '~/components/Plan/PickerShell.vue'
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

// ── Helpers ────────────────────────────────────────────────────────────────────

function isWeekend(date: DateTime) {
  return date.weekday === 6 || date.weekday === 7
}

function dateKey(date: DateTime) {
  return date.toFormat('yyyy-MM-dd')
}

</script>

<template>
  <PickerShell :open="modelValue" :title="$t('dishes.planDish.dialogTitle', { dish: dishName })" :close-label="$t('dishes.planDish.close')" @update:open="emit('update:modelValue', $event)">
    <template #navigation><PlanWeekNav v-model="weekStart" /></template>
    <template v-if="loading"><DishPlanDayRow v-for="i in 9" :key="i" loading /></template>
    <template v-else>
      <template v-for="(date, index) in planningWindow" :key="dateKey(date)">
        <div v-if="index === 2" class="plan-picker__divider" />
        <DishPlanDayRow :date="date" :menu="menuForDate(date)" :is-planned="isDishOnDay(date)" :is-weekend="isWeekend(date)" :is-adding="addingDate === dateKey(date)" :dish-name="dishName" @toggle="toggleDay(date)" />
      </template>
    </template>
  </PickerShell>
</template>

<template>
  <Content split>
    <v-row>
      <v-col>
        <v-menu v-model="showDatePicker" :close-on-content-click="false">
          <template #activator="{ props: menuProps }">
            <v-text-field
              v-bind="menuProps"
              :model-value="dateRangeText"
              label="Select date range"
              prepend-icon="mdi-calendar"
              readonly
              style="width: 220px"
            />
          </template>
          <v-date-picker v-model="dateRange" multiple="range" show-week />
        </v-menu>
      </v-col>
    </v-row>
    <v-row>
      <v-col cols="12" class="text-center">
        <v-card-title>Dinner plan</v-card-title>
      </v-col>
    </v-row>
    <v-row>
      <v-col>
        <div class="dinner-timeline">
          <template v-for="(dinner, index) in dinners" :key="index">
            <div v-if="dinner.date.weekday === 1" class="week-header">
              <span>Week {{ dinner.date.weekNumber }}</span>
              <span class="text-caption">{{ formatWeekDatesString(dinner.date) }}</span>
            </div>
            <PlanPlannedDinner
              :dinner="dinner"
              :selected="isDinnerDateSelected(dinner, selectedDinnerDate)"
              @dinner:clicked="selectedDinnerDate = dinner.date"
              @dinner:menuupdated="menuUpdated"
              @dinner:close="selectedDinnerDate = null"
            />
          </template>
        </div>
      </v-col>
    </v-row>
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

const showDatePicker = ref(false)
const dateRange = ref<Date[]>([])
const selectedDinnerDate = ref<DateTime | null>(null)

const dateRangeText = computed(() =>
  dateRange.value
    .filter((_, i) => i === 0 || i === dateRange.value.length - 1)
    .map((d) => DateTime.fromJSDate(d).toLocaleString(DateTime.DATE_SHORT))
    .join(' ~ '),
)

const dinners = computed(() => [...dinnersStore.dinners])

async function init() {
  await dishesStore.populateDishes()
  const to = DateTime.now().plus({ week: 1 })
  const from = to.minus({ month: 1 })
  dateRange.value = [from.toJSDate(), to.toJSDate()]
}

async function populateDinners() {
  if (dateRange.value.length < 2) return
  const sorted = [...dateRange.value].sort((a, b) => a.getTime() - b.getTime())
  const from = DateTime.fromJSDate(sorted[0])
  const to = DateTime.fromJSDate(sorted[sorted.length - 1])
  await dinnersStore.populateDinners(from, to)
}

function isDinnerDateSelected(dinner: Dinner, selectedDate: DateTime | null) {
  if (!dinner?.date || !selectedDate) return false
  return dinner.date.equals(selectedDate)
}

function formatWeekDatesString(startOfWeekDay: DateTime) {
  return `${startOfWeekDay.toLocaleString(DateTime.DATE_SHORT)} - ${startOfWeekDay.plus({ days: 7 }).toLocaleString(DateTime.DATE_SHORT)}`
}

function menuUpdated() {
  populateDinners()
}

onMounted(init)
watch(() => appStore.activeFamilyId, (val) => { if (val) init() })
watch(dateRange, (val) => { if (val.length >= 2) populateDinners() })
</script>

<style scoped>
.dinner-timeline {
  position: relative;
  width: 100%;
}
.dinner-timeline::before {
  content: '';
  position: absolute;
  left: 10px;
  top: 0;
  bottom: 0;
  width: 2px;
  background: rgba(0, 0, 0, 0.12);
}
.week-header {
  display: flex;
  justify-content: space-between;
  padding: 12px 0 4px 38px;
  color: rgba(0, 0, 0, 0.6);
}
</style>

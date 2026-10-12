<script setup lang="ts">
import { computed } from 'vue'
import type { DateTime } from 'luxon'
import type { Dinner } from '~/types'
import WeekNav from './WeekNav.vue'
import PlannedDinner from './PlannedDinner.vue'

const props = defineProps<{
  monday: DateTime
  dinners: Dinner[]
  selectedDate: string | null
  loading?: boolean
  navigationDisabled?: boolean
  clearTarget?: boolean
}>()
const emit = defineEmits<{
  navigate: [date: DateTime]
  select: [date: string | null]
  'dinner:menuupdated': [event: { date: DateTime; dishId: string; dishName: string }]
  'dinner:optoutupdated': []
}>()
const { t } = useI18n()
const previousWeekend = computed(() => props.dinners.filter(dinner => dinner.date < props.monday))
const currentWeek = computed(() => props.dinners.filter(dinner => dinner.date >= props.monday))
const groups = computed(() => [previousWeekend.value, currentWeek.value])
const previousWeekendLabel = computed(() => t('plan.weekWeekend', { week: props.monday.minus({ days: 2 }).weekNumber }))
</script>

<template>
  <div class="week-plan">
    <WeekNav :model-value="monday" :disabled="navigationDisabled" class="mb-4" @update:model-value="emit('navigate', $event)" />
    <v-btn v-if="clearTarget && selectedDate" variant="text" size="small" class="week-plan__clear" @click="emit('select', null)">{{ t('weekPlanning.clearTarget') }}</v-btn>
    <template v-if="loading">
      <div v-for="index in 9" :key="index" class="skeleton-card mb-3">
        <v-skeleton-loader type="list-item-two-line" />
      </div>
    </template>
    <div v-show="!loading" :inert="loading">
      <template v-for="(group, index) in groups" :key="index">
        <div v-if="index === 0 && group.length" class="week-section-label">{{ previousWeekendLabel }}</div>
        <PlannedDinner
          v-for="dinner in group"
          :key="dinner.date.toFormat('yyyy-MM-dd')"
          :dinner="dinner"
          :selected="selectedDate === dinner.date.toFormat('yyyy-MM-dd')"
          class="mb-3 week-day"
          :class="{ 'dinner-prev-weekend': index === 0 }"
          @dinner:clicked="emit('select', dinner.date.toFormat('yyyy-MM-dd'))"
          @dinner:close="emit('select', null)"
          @dinner:menuupdated="emit('dinner:menuupdated', $event)"
          @dinner:optoutupdated="emit('dinner:optoutupdated')"
        >
          <template v-if="$slots.details" #details>
            <slot v-if="selectedDate === dinner.date.toFormat('yyyy-MM-dd')" name="details" :dinner="dinner" />
          </template>
        </PlannedDinner>
        <div v-if="index === 0 && group.length" class="week-divider" />
      </template>
    </div>
  </div>
</template>

<style scoped>
.week-plan { min-width: 0; container: week-plan / inline-size; }
.week-plan__clear { margin-bottom: var(--space-2); }
.skeleton-card { border-radius: var(--radius-lg); overflow: hidden; box-shadow: var(--shadow-sm); border: 1px solid var(--color-border); }
.week-section-label { font-size: var(--text-xs); font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; color: var(--color-text-muted); padding: 0 var(--space-1); margin-bottom: var(--space-2); }
.week-divider { height: 1px; background: var(--color-border-medium); margin: var(--space-2) 0 var(--space-4); }
.dinner-prev-weekend { opacity: 0.8; }
@container week-plan (max-width: 599px) {
  :deep(.dinner-card__header) { flex-wrap: wrap; }
  :deep(.dinner-card__day-info) { min-width: 0; flex: 1; flex-wrap: wrap; }
  :deep(.dinner-card__summary) { order: 3; flex-basis: 100%; }
}
</style>

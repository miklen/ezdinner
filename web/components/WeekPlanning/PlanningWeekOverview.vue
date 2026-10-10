<script setup lang="ts">
import { computed } from 'vue'
import { DateTime } from 'luxon'
import type { Dinner } from '~/types'
import type { PlanningDay } from '~/types/week-planning'
import WeekPlan from '~/components/Plan/WeekPlan.vue'
import DinnerDayDetails from '~/components/Plan/DinnerDayDetails.vue'
const props = defineProps<{ monday: string; days: PlanningDay[]; selectedDate: string | null; busy: boolean; navigationDisabled?: boolean; reasons: string[] }>()
const emit = defineEmits<{ navigate: [offset: number]; select: [date: string | null]; remove: [dishId: string, date: string]; optOut: [date: string, reason: string | null] }>()
const { t } = useI18n()
const weekStart = computed(() => DateTime.fromISO(props.monday))
const dinners = computed<Dinner[]>(() => props.days.map(day => ({
  date: DateTime.fromISO(day.date), description: '', menu: day.menu,
  isPlanned: day.menu.length > 0, isOptedOut: day.optOutReason !== null,
  isResolved: day.menu.length > 0 || day.optOutReason !== null, optOutReason: day.optOutReason,
})))
function navigate(date: DateTime) {
  emit('navigate', Math.round(date.startOf('week').diff(weekStart.value, 'weeks').weeks))
}
</script>

<template>
  <section class="week-overview" :aria-label="t('weekPlanning.overview')">
    <WeekPlan :monday="weekStart" :dinners="dinners" :selected-date="selectedDate" :navigation-disabled="navigationDisabled" clear-target @navigate="navigate" @select="emit('select', $event)">
      <template #details="{ dinner }">
        <DinnerDayDetails :dinner="dinner" :reasons="reasons" :busy="busy" @remove-dish="emit('remove', $event, dinner.date.toFormat('yyyy-MM-dd'))" @set-opt-out="emit('optOut', dinner.date.toFormat('yyyy-MM-dd'), $event)" @remove-opt-out="emit('optOut', dinner.date.toFormat('yyyy-MM-dd'), null)">
          <template #assignment>
            <p class="week-day__note">{{ t('weekPlanning.selectDayHint') }}</p>
          </template>
        </DinnerDayDetails>
      </template>
    </WeekPlan>
  </section>
</template>

<style scoped>
.week-overview { min-width: 0; }
.week-day__note { font-size: var(--text-sm); color: var(--color-text-muted); }
</style>

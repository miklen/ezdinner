<script setup lang="ts">
import { DateTime } from 'luxon'
import { computed } from 'vue'
import PlanDayRow from '~/components/Dish/PlanDayRow.vue'
import PickerShell from '~/components/Plan/PickerShell.vue'
import WeekNav from '~/components/Plan/WeekNav.vue'
import type { PlanningDay } from '~/types/week-planning'
const props = defineProps<{ open: boolean; monday: string; dishId: string; dishName: string; days: PlanningDay[]; busy: boolean; loading: boolean; canMutate: boolean; error?: string | null }>()
const emit = defineEmits<{ close: []; assign: [date: string]; navigate: [offset: number]; retry: [] }>()
const { t } = useI18n()
const weekStart = computed(() => DateTime.fromISO(props.monday))
function navigate(date: DateTime) {
  if (!props.busy) emit('navigate', Math.round(date.startOf('week').diff(weekStart.value, 'weeks').weeks))
}
function assign(date: string) {
  if (props.canMutate && !props.busy) emit('assign', date)
}
</script>

<template>
  <PickerShell :open="open" :title="t('weekPlanning.assignTitle', { name: dishName })" :close-label="t('weekPlanning.close')" @update:open="!$event && emit('close')">
    <template #navigation><WeekNav :model-value="weekStart" :disabled="busy" @update:model-value="navigate" /></template>
    <div v-if="loading" role="status" :aria-label="t('weekPlanning.loading')" aria-live="polite" aria-busy="true">
      <PlanDayRow v-for="index in 9" :key="index" loading />
    </div>
    <template v-else>
      <v-alert v-if="error" class="assignment-picker__error" type="error" variant="tonal" role="alert">
        {{ t(`weekPlanning.${error}`) }}
        <v-btn v-if="error === 'readFailed'" variant="text" data-testid="picker-retry" @click="emit('retry')">{{ t('weekPlanning.retry') }}</v-btn>
      </v-alert>
      <template v-if="error !== 'readFailed'">
        <template v-for="(day, index) in days" :key="day.date">
          <div v-if="index === 2" class="plan-picker__divider" />
          <div class="assignment-day">
            <PlanDayRow :date="DateTime.fromISO(day.date)" :menu="day.menu" :is-weekend="DateTime.fromISO(day.date).weekday >= 6" :is-planned="day.menu.some(item => item.dishId === dishId)" :already-assigned="day.menu.some(item => item.dishId === dishId)" :disabled="!canMutate || busy" :dish-name="dishName" :note="day.optOutReason || t('weekPlanning.emptyDay')" @toggle="assign(day.date)" />
          </div>
        </template>
      </template>
    </template>
  </PickerShell>
</template>

<style scoped>
.assignment-picker__error { margin: var(--space-2) var(--space-4); }
</style>

<script setup lang="ts">
import { DateTime } from 'luxon'
import { computed } from 'vue'
import DayList from '~/components/Plan/DayList.vue'
import PickerShell from '~/components/Plan/PickerShell.vue'
import WeekNav from '~/components/Plan/WeekNav.vue'
import type { PlanningDay } from '~/types/week-planning'
const props = defineProps<{ open: boolean; monday: string; dishId: string; dishName: string; days: PlanningDay[]; busy: boolean; loading: boolean; canMutate: boolean; error?: string | null }>()
const emit = defineEmits<{ close: []; assign: [date: string]; navigate: [offset: number]; retry: [] }>()
const { t } = useI18n()
const weekStart = computed(() => DateTime.fromISO(props.monday))
const pickerDays = computed(() => props.days.map(day => ({ date: DateTime.fromISO(day.date), menu: day.menu, note: day.optOutReason || t('weekPlanning.emptyDay') })))
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
    <v-alert v-if="!loading && error" class="assignment-picker__error" type="error" variant="tonal" role="alert">
      {{ t('weekPlanning.' + error) }}
      <v-btn v-if="error === 'readFailed'" variant="text" data-testid="picker-retry" @click="emit('retry')">{{ t('weekPlanning.retry') }}</v-btn>
    </v-alert>
    <DayList v-if="loading || error !== 'readFailed'" :days="pickerDays" :dish-id="dishId" :dish-name="dishName" :loading="loading" :disabled="!canMutate || busy" assignment-mode="add" @select="assign($event.toFormat('yyyy-MM-dd'))" />
  </PickerShell>
</template>

<style scoped>
.assignment-picker__error { margin: var(--space-2) var(--space-4); }
</style>

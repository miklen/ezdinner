<script setup lang="ts">
import { DateTime } from 'luxon'
import PlanDayRow from '~/components/Dish/PlanDayRow.vue'
import type { PlanningDay } from '~/types/week-planning'
defineProps<{ open: boolean; dishId: string; dishName: string; days: PlanningDay[]; busy: boolean; error?: string | null }>()
const emit = defineEmits<{ close: []; assign: [date: string] }>()
const { t } = useI18n()
</script>

<template>
  <v-dialog :model-value="open" max-width="540" scrollable @update:model-value="!$event && emit('close')">
    <v-card class="assignment-picker">
      <v-card-title class="text-card-title pt-4 px-4">{{ t('weekPlanning.assignTitle', { name: dishName }) }}</v-card-title>
      <v-card-text class="assignment-picker__body">
        <v-alert v-if="error" type="error" variant="tonal" role="alert">{{ t(`weekPlanning.${error}`) }}</v-alert>
        <div v-for="(day, index) in days" :key="day.date" class="assignment-day" :class="{ 'assignment-day--monday': index === 2 }">
          <PlanDayRow :date="DateTime.fromISO(day.date)" :menu="day.menu" :is-weekend="DateTime.fromISO(day.date).weekday >= 6" :is-planned="day.menu.some(item => item.dishId === dishId)" :disabled="busy || day.menu.some(item => item.dishId === dishId)" :dish-name="dishName" :note="day.optOutReason || t('weekPlanning.emptyDay')" :aria-label="day.menu.some(item => item.dishId === dishId) ? t('weekPlanning.alreadyAssigned') : undefined" @toggle="emit('assign', day.date)" />
        </div>
      </v-card-text>
      <v-card-actions class="px-6 pb-4"><v-btn @click="emit('close')">{{ t('weekPlanning.close') }}</v-btn></v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.assignment-picker :deep(.v-card-title) { white-space: normal; overflow-wrap: anywhere; }
.assignment-picker__body { padding: 0 !important; }
.assignment-day--monday { border-top: 1px solid var(--color-border-medium); margin-top: var(--space-2); padding-top: var(--space-2); }
</style>

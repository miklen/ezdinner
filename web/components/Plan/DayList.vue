<script setup lang="ts">
import { computed } from 'vue'
import type { DateTime } from 'luxon'
import type { MenuItem } from '~/types'
import PlanDayRow from '~/components/Dish/PlanDayRow.vue'

const props = withDefaults(defineProps<{
  days: { date: DateTime; menu: MenuItem[]; note?: string | null }[]
  dishId: string
  dishName: string
  loading?: boolean
  disabled?: boolean
  addingDate?: string | null
  assignmentMode?: 'add' | 'toggle'
}>(), { loading: false, disabled: false, addingDate: null, assignmentMode: 'toggle' })
const emit = defineEmits<{ select: [date: DateTime] }>()
const { t } = useI18n()
const rows = computed(() => props.days.map(day => ({
  ...day,
  key: day.date.toFormat('yyyy-MM-dd'),
  planned: day.menu.some(item => item.dishId === props.dishId),
  weekend: day.date.weekday >= 6,
})))
</script>

<template>
  <div class="plan-day-list">
    <div v-if="loading" role="status" :aria-label="t('weekPlanning.loading')" aria-live="polite" aria-busy="true">
      <PlanDayRow v-for="index in 9" :key="index" loading />
    </div>
    <template v-else>
      <template v-for="(day, index) in rows" :key="day.key">
        <div v-if="index === 2" class="plan-picker__divider" />
        <div class="plan-picker__day">
          <PlanDayRow :date="day.date" :menu="day.menu" :is-weekend="day.weekend" :is-planned="day.planned" :already-assigned="assignmentMode === 'add' && day.planned" :disabled="disabled" :is-adding="addingDate === day.key" :dish-name="dishName" :note="day.note" @toggle="emit('select', day.date)" />
        </div>
      </template>
    </template>
  </div>
</template>

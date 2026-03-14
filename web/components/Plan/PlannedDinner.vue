<template>
  <div class="dinner-row" :class="{ 'font-weight-bold': isToday }">
    <div class="dinner-dot" :style="{ backgroundColor: dinner.isPlanned ? 'rgb(var(--v-theme-success))' : '#bdbdbd' }" />
    <div class="dinner-content">
      <div
        v-show="!selected"
        class="dinner-content-row"
        style="cursor: pointer"
        @click="emit('dinner:clicked')"
      >
        <span>
          {{ title }}
          <v-chip
            v-for="tag in dinner.tags"
            :key="tag.value"
            variant="outlined"
            size="x-small"
            :color="tag.color || 'primary'"
          >
            {{ tag.value }}
          </v-chip>
        </span>
        <span class="text-right">
          {{ formatDay(dinner.date) }}
          <span class="text-caption">{{ formatDate(dinner.date) }}</span>
        </span>
      </div>

      <PlanPlannedDinnerDetails
        v-show="selected"
        :dinner="dinner"
        @dinner:close="emit('dinner:close')"
        @dinner:menuupdated="emit('dinner:menuupdated', $event)"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { DateTime } from 'luxon'
import type { Dinner } from '~/types'

const props = defineProps<{
  dinner: Dinner
  selected: boolean
}>()

const emit = defineEmits<{
  'dinner:clicked': []
  'dinner:close': []
  'dinner:menuupdated': [event: { date: DateTime; dishId: string; dishName: string }]
}>()

const isToday = computed(() => props.dinner.date.hasSame(DateTime.local(), 'day'))

const title = computed(() => {
  if (!props.dinner.isPlanned) {
    return props.dinner.date < DateTime.now() ? 'Track dinner' : 'Plan dinner'
  }
  return props.dinner.menu.map((item) => item.dishName).join(', ')
})

function formatDate(date: DateTime) {
  return date.toFormat('D')
}

function formatDay(date: DateTime) {
  return date.toFormat('EEEE')
}
</script>

<style scoped>
.dinner-row {
  display: flex;
  align-items: flex-start;
  padding: 6px 0;
  position: relative;
}
.dinner-dot {
  width: 20px;
  height: 20px;
  border-radius: 50%;
  flex-shrink: 0;
  margin-right: 16px;
  margin-top: 1px;
  position: relative;
  z-index: 1;
}
.dinner-content {
  flex: 1;
  min-width: 0;
}
.dinner-content-row {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  width: 100%;
}
</style>

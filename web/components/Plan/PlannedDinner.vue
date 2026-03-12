<template>
  <div>
    <v-timeline-item class="mb-4" :dot-color="dinner.isPlanned ? 'green' : 'grey'" size="small">
      <v-row
        v-show="!selected"
        style="cursor: pointer"
        justify="space-between"
        :class="isToday ? 'font-weight-bold' : ''"
        @click="emit('dinner:clicked')"
      >
        <v-col cols="6">
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
        </v-col>
        <v-col class="text-right" cols="5">
          {{ formatDay(dinner.date) }}
          <span class="text-caption">{{ formatDate(dinner.date) }}</span>
        </v-col>
      </v-row>

      <PlannedDinnerDetails
        v-show="selected"
        :dinner="dinner"
        @dinner:close="emit('dinner:close')"
        @dinner:menuupdated="emit('dinner:menuupdated', $event)"
      />
    </v-timeline-item>
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
  'dinner:menuupdated': [event: any]
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
.v-timeline-item {
  padding-bottom: 0;
}
</style>

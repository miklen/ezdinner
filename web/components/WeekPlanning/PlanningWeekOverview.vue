<script setup lang="ts">
import { computed } from 'vue'
import { DateTime } from 'luxon'
import type { Dinner } from '~/types'
import type { PlanningDay } from '~/types/week-planning'
import WeekNav from '~/components/Plan/WeekNav.vue'
import PlannedDinner from '~/components/Plan/PlannedDinner.vue'
import DishPill from '~/components/Dish/DishPill.vue'
const props = defineProps<{ monday: string; days: PlanningDay[]; selectedDate: string | null; busy: boolean }>()
const emit = defineEmits<{ navigate: [offset: number]; select: [date: string | null]; remove: [dishId: string, date: string] }>()
const { t } = useI18n()
const weekStart = computed(() => DateTime.fromISO(props.monday))
const previousWeekendLabel = computed(() => t('plan.weekWeekend', { week: weekStart.value.minus({ days: 2 }).weekNumber }))
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
    <WeekNav :model-value="weekStart" @update:model-value="navigate" />
    <v-btn v-if="selectedDate" variant="text" size="small" class="week-overview__clear" @click="emit('select', null)">{{ t('weekPlanning.clearTarget') }}</v-btn>
    <div class="text-caption-label week-overview__section">{{ previousWeekendLabel }}</div>
    <template v-for="(dinner, index) in dinners" :key="days[index].date">
      <div v-if="index === 2" class="week-overview__divider" />
      <PlannedDinner
        :dinner="dinner"
        :selected="selectedDate === days[index].date"
        class="week-day"
        :class="{ 'week-day--previous': index < 2 }"
        @dinner:clicked="emit('select', days[index].date)"
        @dinner:close="emit('select', null)"
      >
        <template #summary>
          <div v-if="dinner.menu.length" class="week-day__pills" @click.stop @keydown.stop>
            <DishPill v-for="item in dinner.menu" :key="item.dishId" :name="item.dishName" :to="`/dishes/${item.dishId}`" size="sm" />
          </div>
          <span v-else class="week-day__note">{{ dinner.optOutReason || t('weekPlanning.emptyDay') }}</span>
        </template>
        <template #details>
          <div class="week-day__details">
            <p class="week-day__note">{{ t('weekPlanning.selectDayHint') }}</p>
            <div v-for="item in dinner.menu" :key="item.dishId" class="week-day__dish">
              <DishPill :name="item.dishName" :to="`/dishes/${item.dishId}`" />
              <v-btn icon="mdi-close" variant="text" size="small" :disabled="busy" :aria-label="t('weekPlanning.removeDish', { name: item.dishName })" @click="emit('remove', item.dishId, days[index].date)" />
            </div>
          </div>
        </template>
      </PlannedDinner>
    </template>
  </section>
</template>

<style scoped>
.week-overview { min-width: 0; }
.week-overview__section { margin: var(--space-4) var(--space-1) var(--space-2); }
.week-overview__divider { height: 1px; background: var(--color-border-medium); margin: var(--space-4) 0; }
.week-overview__clear { margin-top: var(--space-2); }
.week-day { margin-bottom: var(--space-3); }
.week-day--previous { opacity: 0.8; }
.week-day__pills { display: flex; flex-wrap: wrap; gap: var(--space-1); min-width: 0; }
.week-day__note { font-size: var(--text-sm); color: var(--color-text-muted); }
.week-day__details { padding: 0 var(--space-4) var(--space-3); }
.week-day__dish { display: flex; align-items: center; justify-content: space-between; gap: var(--space-2); margin-top: var(--space-2); }
.week-overview :deep(.dinner-card__header) { flex-wrap: wrap; }
.week-overview :deep(.dinner-card__day-info) { min-width: 0; flex: 1; flex-wrap: wrap; }
.week-overview :deep(.dinner-card__summary) { order: 3; flex-basis: 100%; }
</style>

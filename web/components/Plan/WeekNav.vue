<script setup lang="ts">
import { DateTime } from 'luxon'
import { useSwipe } from '@vueuse/core'

const props = defineProps<{
  modelValue: DateTime
}>()

const emit = defineEmits<{
  'update:modelValue': [value: DateTime]
}>()

const navEl = ref<HTMLElement | null>(null)

const weekStart = computed(() => props.modelValue.startOf('week'))
const weekEnd = computed(() => weekStart.value.endOf('week'))
const weekNumber = computed(() => weekStart.value.weekNumber)

const weekLabel = computed(() => {
  const todayWeek = DateTime.now().startOf('week')
  const diffWeeks = Math.round(weekStart.value.diff(todayWeek, 'weeks').weeks)
  if (diffWeeks === 0) return 'This week'
  if (diffWeeks === -1) return 'Last week'
  if (diffWeeks === 1) return 'Next week'
  return weekStart.value.toFormat('MMM d') + ' – ' + weekEnd.value.toFormat('MMM d')
})

const weekRangeDetail = computed(() => {
  const s = weekStart.value
  const e = weekEnd.value
  const currentYear = DateTime.now().year
  let dates: string
  if (s.month === e.month) {
    dates = s.year !== currentYear
      ? `${s.toFormat('MMM d')} – ${e.toFormat('d, yyyy')}`
      : `${s.toFormat('MMM d')} – ${e.toFormat('d')}`
  } else {
    dates = s.year !== currentYear
      ? `${s.toFormat('MMM d')} – ${e.toFormat('MMM d, yyyy')}`
      : `${s.toFormat('MMM d')} – ${e.toFormat('MMM d')}`
  }
  return `${dates} · Wk ${weekNumber.value}`
})

const isCurrentWeek = computed(() =>
  weekStart.value.hasSame(DateTime.now().startOf('week'), 'day'),
)

function prev() {
  emit('update:modelValue', props.modelValue.minus({ weeks: 1 }))
}

function next() {
  emit('update:modelValue', props.modelValue.plus({ weeks: 1 }))
}

function goToThisWeek() {
  if (!isCurrentWeek.value) {
    emit('update:modelValue', DateTime.now().startOf('week'))
  }
}

useSwipe(navEl, {
  onSwipeEnd(_e, dir) {
    if (dir === 'left') next()
    else if (dir === 'right') prev()
  },
})
</script>

<template>
  <div ref="navEl" class="week-nav">
    <button class="week-nav__arrow" aria-label="Previous week" @click="prev">
      <v-icon size="20">mdi-chevron-left</v-icon>
    </button>

    <button class="week-nav__center" @click="goToThisWeek">
      <span class="week-nav__label">{{ weekLabel }}</span>
      <span class="week-nav__range">{{ weekRangeDetail }}</span>
      <span v-if="!isCurrentWeek" class="week-nav__today-hint">Tap to return to this week</span>
    </button>

    <button class="week-nav__arrow" aria-label="Next week" @click="next">
      <v-icon size="20">mdi-chevron-right</v-icon>
    </button>
  </div>
</template>

<style scoped>
.week-nav {
  display: flex;
  align-items: center;
  gap: var(--space-1);
  background: var(--color-surface);
  border-radius: var(--radius-lg);
  border: 1px solid rgba(0, 0, 0, 0.06);
  box-shadow: var(--shadow-sm);
  padding: var(--space-2);
  user-select: none;
  touch-action: pan-y;
}

.week-nav__arrow {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 48px;
  min-height: 48px;
  border-radius: var(--radius-full);
  border: none;
  background: transparent;
  cursor: pointer;
  color: var(--color-text-secondary);
  transition:
    background var(--duration-fast) var(--ease-standard),
    color var(--duration-fast) var(--ease-standard);
}

.week-nav__arrow:hover {
  background: rgba(var(--v-theme-primary), 0.08);
  color: rgb(var(--v-theme-primary));
}

.week-nav__arrow:focus-visible {
  outline: 2px solid var(--color-primary);
  outline-offset: 2px;
}

.week-nav__center {
  flex: 1;
  text-align: center;
  cursor: pointer;
  padding: var(--space-2) var(--space-3);
  border-radius: var(--radius-md);
  border: none;
  background: transparent;
  transition: background var(--duration-fast) var(--ease-standard);
}

.week-nav__center:hover {
  background: var(--color-surface-variant);
}

.week-nav__center:focus-visible {
  outline: 2px solid var(--color-primary);
  outline-offset: 2px;
}

.week-nav__label {
  display: block;
  font-family: var(--font-heading);
  font-size: var(--text-xl);
  color: var(--color-text-primary);
  line-height: 1.2;
}

.week-nav__range {
  display: block;
  font-size: var(--text-sm);
  color: var(--color-text-muted);
  margin-top: 2px;
}

.week-nav__today-hint {
  display: block;
  font-size: var(--text-xs);
  color: var(--color-primary);
  margin-top: 3px;
}
</style>

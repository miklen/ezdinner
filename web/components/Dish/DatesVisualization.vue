<script setup lang="ts">
import { DateTime } from 'luxon'
import type { DinnerDate } from '~/types'

const props = defineProps<{
  dates: DinnerDate[]
  dishName: string
  loading?: boolean
}>()

const TIMELINE_MONTHS = 6
const SHOW_COUNT = 10

const showAll = shallowRef(false)

// ── Summary ────────────────────────────────────────────────────────────────────

const averageInterval = computed(() => {
  const intervals = props.dates
    .filter((d) => d.daysSinceLast > 0)
    .map((d) => d.daysSinceLast)
  if (!intervals.length) return null
  return Math.round(intervals.reduce((sum, d) => sum + d, 0) / intervals.length)
})

const summaryText = computed(() => {
  const n = props.dates.length
  if (n === 0) return `${props.dishName} hasn't been had yet.`
  if (n === 1) return 'Had once.'
  const avg = averageInterval.value
  return avg ? `Had ${n} times · roughly every ${avg} days` : `Had ${n} times`
})

// ── Dot Timeline ───────────────────────────────────────────────────────────────
// Shows the last N months as a horizontal axis with dots for each occurrence.

const timelineNow = DateTime.now()
const timelineStart = timelineNow.minus({ months: TIMELINE_MONTHS })
const timelineSpanDays = timelineNow.diff(timelineStart, 'days').days

// Month label positions
const monthLabels = computed(() => {
  const labels: { label: string; pct: number }[] = []
  let cur = timelineStart.startOf('month')
  while (cur <= timelineNow) {
    const offsetDays = cur.diff(timelineStart, 'days').days
    const pct = Math.min(100, Math.max(0, (offsetDays / timelineSpanDays) * 100))
    labels.push({ label: cur.toFormat('MMM'), pct })
    cur = cur.plus({ months: 1 })
  }
  return labels
})

// Dots for occurrences that fall within the timeline window
const timelineDots = computed(() =>
  props.dates
    .map((d) => {
      const dt = DateTime.fromISO(d.date)
      if (dt < timelineStart || dt > timelineNow) return null
      const offsetDays = dt.diff(timelineStart, 'days').days
      const pct = Math.min(98, Math.max(2, (offsetDays / timelineSpanDays) * 100))
      const daysAgo = Math.floor(timelineNow.diff(dt, 'days').days)
      return {
        key: d.date,
        pct,
        tooltip: `${dt.toLocaleString(DateTime.DATE_MED)} (${daysAgo}d ago)`,
      }
    })
    .filter(Boolean) as { key: string; pct: number; tooltip: string }[],
)

const hasTimelineDots = computed(() => timelineDots.value.length > 0)

// ── Date list ──────────────────────────────────────────────────────────────────

const visibleDates = computed(() =>
  showAll.value ? props.dates : props.dates.slice(0, SHOW_COUNT),
)

const hasMore = computed(() => props.dates.length > SHOW_COUNT && !showAll.value)

function formatDate(iso: string) {
  return DateTime.fromISO(iso).toLocaleString(DateTime.DATE_FULL)
}

function daysAgoLabel(iso: string) {
  const days = Math.floor(DateTime.now().diff(DateTime.fromISO(iso), 'days').days)
  if (days === 0) return 'Today'
  if (days === 1) return 'Yesterday'
  return `${days}d ago`
}
</script>

<template>
  <!-- Skeleton -->
  <v-card v-if="loading" class="dates-card">
    <v-card-text>
      <v-skeleton-loader type="text" width="260" class="mb-4" />
      <v-skeleton-loader type="image" height="52" class="mb-4" />
      <v-skeleton-loader v-for="i in 4" :key="i" type="list-item" />
    </v-card-text>
  </v-card>

  <!-- Loaded -->
  <v-card v-else class="dates-card">
    <div class="dates-card__header">
      <span class="text-card-title">Dinner history</span>
    </div>

    <div class="dates-card__body">
      <!-- Summary line -->
      <p class="dates-card__summary">{{ summaryText }}</p>

      <!-- Dot Timeline -->
      <div v-if="hasTimelineDots" class="dates-card__timeline">
        <!-- Dots row -->
        <div class="dates-card__dots-row" aria-hidden="true">
          <div
            v-for="dot in timelineDots"
            :key="dot.key"
            class="dates-card__dot"
            :style="{ left: dot.pct + '%' }"
            :title="dot.tooltip"
          />
        </div>

        <!-- Axis line + month labels -->
        <div class="dates-card__axis">
          <div
            v-for="m in monthLabels"
            :key="m.label"
            class="dates-card__month-label"
            :style="{ left: m.pct + '%' }"
          >
            {{ m.label }}
          </div>
        </div>
      </div>

      <p v-else-if="!props.dates.length" class="dates-card__empty">
        No dinner history yet.
      </p>

      <!-- Date list -->
      <div v-if="props.dates.length" class="dates-card__list">
        <div
          v-for="d in visibleDates"
          :key="d.date"
          class="dates-card__row"
        >
          <div class="dates-card__dot-sm" />
          <span class="dates-card__date">{{ formatDate(d.date) }}</span>
          <span class="dates-card__days-ago">{{ daysAgoLabel(d.date) }}</span>
          <span
            v-if="d.daysSinceLast > 0"
            class="dates-card__interval"
          >
            +{{ d.daysSinceLast }}d
          </span>
        </div>

        <button
          v-if="hasMore"
          class="dates-card__show-more"
          @click="showAll = true"
        >
          Show {{ props.dates.length - SHOW_COUNT }} more
        </button>
      </div>
    </div>
  </v-card>
</template>

<style scoped>
.dates-card__header {
  padding: var(--space-3) var(--space-3) var(--space-2);
}

.dates-card__body {
  padding: 0 var(--space-3) var(--space-3);
}

/* Summary */
.dates-card__summary {
  margin: 0 0 var(--space-4);
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
}

/* Timeline */
.dates-card__timeline {
  margin-bottom: var(--space-5);
  padding: 0 4px;
}

/* Dots row: line + dots above */
.dates-card__dots-row {
  position: relative;
  height: 24px;
  margin-bottom: 6px;
  border-bottom: 2px solid var(--color-border-medium);
}

.dates-card__dot {
  position: absolute;
  bottom: -5px;
  width: 10px;
  height: 10px;
  border-radius: var(--radius-full);
  background-color: var(--color-primary);
  transform: translateX(-50%);
  cursor: default;
  transition: transform var(--duration-fast) var(--ease-spring);
}

.dates-card__dot:hover {
  transform: translateX(-50%) scale(1.4);
}

/* Axis: month labels */
.dates-card__axis {
  position: relative;
  height: 18px;
}

.dates-card__month-label {
  position: absolute;
  transform: translateX(-50%);
  font-size: var(--text-xs);
  font-weight: 600;
  letter-spacing: 0.04em;
  color: var(--color-text-muted);
  white-space: nowrap;
}

/* Date list */
.dates-card__list {
  display: flex;
  flex-direction: column;
  gap: 1px;
}

.dates-card__row {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-2) var(--space-2);
  border-radius: var(--radius-sm);
  transition: background-color var(--duration-fast) var(--ease-out);
}

.dates-card__row:hover {
  background-color: var(--color-surface-variant);
}

.dates-card__dot-sm {
  width: 7px;
  height: 7px;
  border-radius: var(--radius-full);
  background-color: var(--color-primary-light);
  flex-shrink: 0;
}

.dates-card__date {
  flex: 1;
  font-size: var(--text-sm);
  color: var(--color-text-primary);
}

.dates-card__days-ago {
  font-size: var(--text-xs);
  color: var(--color-text-muted);
  font-weight: 500;
}

.dates-card__interval {
  font-size: var(--text-xs);
  color: var(--color-text-muted);
  background-color: var(--color-surface-variant);
  padding: 1px 6px;
  border-radius: var(--radius-full);
  font-weight: 500;
}

.dates-card__empty {
  font-size: var(--text-sm);
  color: var(--color-text-muted);
  font-style: italic;
  margin: 0;
}

.dates-card__show-more {
  margin-top: var(--space-3);
  font-size: var(--text-sm);
  color: var(--color-primary-dark);
  font-weight: 500;
  background: none;
  border: none;
  cursor: pointer;
  padding: var(--space-1) var(--space-2);
  border-radius: var(--radius-sm);
  transition: background-color var(--duration-fast) var(--ease-out);
}

.dates-card__show-more:hover {
  background-color: var(--color-surface-variant);
}

/* Spacing between sections */
.dates-card__list + .dates-card__show-more {
  align-self: flex-start;
}
</style>

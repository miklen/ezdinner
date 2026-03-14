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

const today = DateTime.now().startOf('day')

const isToday = computed(() => props.dinner.date.hasSame(today, 'day'))
const isPast = computed(() => props.dinner.date < today)
const isWeekend = computed(() => props.dinner.date.weekday >= 6)

const cardState = computed(() => {
  if (isToday.value) return 'today'
  if (isPast.value && props.dinner.isPlanned) return 'past-planned'
  if (isPast.value && !props.dinner.isPlanned) return 'past-unplanned'
  if (props.dinner.isPlanned) return 'future-planned'
  return 'future-unplanned'
})

const dayName = computed(() => props.dinner.date.toFormat('EEEE'))
const dateStr = computed(() => props.dinner.date.toFormat('MMM d'))

function handleHeaderClick() {
  if (props.selected) {
    emit('dinner:close')
  } else {
    emit('dinner:clicked')
  }
}

</script>

<template>
  <div
    class="dinner-card"
    :class="[
      `dinner-card--${cardState}`,
      { 'dinner-card--weekend': isWeekend, 'dinner-card--expanded': selected },
    ]"
  >
    <!-- Header — always visible -->
    <div class="dinner-card__header" @click="handleHeaderClick">
      <div class="dinner-card__day-info">
        <span class="dinner-card__day-name">{{ dayName }}</span>
        <span class="dinner-card__date">{{ dateStr }}</span>
        <span v-if="isToday" class="today-badge">TODAY</span>
      </div>

      <div class="dinner-card__summary">
        <template v-if="dinner.isPlanned && !selected">
          <div class="dinner-card__pills">
            <DishPill
              v-for="item in dinner.menu"
              :key="item.dishId"
              :name="item.dishName"
              size="sm"
            />
          </div>
        </template>
        <span v-else-if="isPast && !dinner.isPlanned" class="dinner-card__hint dinner-card__hint--muted">
          not tracked
        </span>
        <span v-else-if="!dinner.isPlanned && !selected" class="dinner-card__hint dinner-card__hint--cta">
          Tap to plan
        </span>
      </div>

      <div class="dinner-card__action">
        <v-icon v-if="dinner.isPlanned && isPast && !selected" color="success" size="18">
          mdi-check-circle
        </v-icon>
        <v-icon v-else-if="!dinner.isPlanned && !isPast && !selected" size="18" class="dinner-card__plus-icon">
          mdi-plus-circle-outline
        </v-icon>
        <v-icon v-else-if="selected" size="18" class="dinner-card__chevron">
          mdi-chevron-up
        </v-icon>
        <v-icon v-else size="18" class="dinner-card__chevron">
          mdi-chevron-down
        </v-icon>
      </div>
    </div>

    <!-- Expandable details — grid-row animation (no max-height) -->
    <div class="dinner-card__details-wrapper" :class="{ 'is-open': selected }">
      <div class="dinner-card__details-inner">
        <PlanPlannedDinnerDetails
          :dinner="dinner"
          @dinner:close="emit('dinner:close')"
          @dinner:menuupdated="emit('dinner:menuupdated', $event)"
        />
      </div>
    </div>
  </div>
</template>

<style scoped>
/* ── Base card ─────────────────────────────────────────────────── */
.dinner-card {
  background: var(--color-surface);
  border-radius: var(--radius-lg);
  border: 1px solid rgba(0, 0, 0, 0.06);
  box-shadow: var(--shadow-sm);
  overflow: hidden;
  transition:
    box-shadow var(--duration-fast) var(--ease-standard),
    border-color var(--duration-fast) var(--ease-standard);
}

/* ── State variants ────────────────────────────────────────────── */
.dinner-card--today {
  border-left: 4px solid var(--color-primary);
  background: oklch(from var(--color-primary) l c h / 0.04);
}

.dinner-card--past-unplanned {
  opacity: 0.55;
}

.dinner-card--future-unplanned {
  border: 1.5px dashed rgba(0, 0, 0, 0.2);
  box-shadow: none;
}

.dinner-card--future-unplanned.dinner-card--expanded {
  border-style: solid;
  border-color: rgba(0, 0, 0, 0.12);
}

/* ── Weekend tint ──────────────────────────────────────────────── */
.dinner-card--weekend:not(.dinner-card--today) {
  background: var(--color-surface-variant);
}

/* ── Header ────────────────────────────────────────────────────── */
.dinner-card__header {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-3) var(--space-4);
  min-height: 64px;
  cursor: pointer;
  transition: background var(--duration-fast) var(--ease-standard);
}

.dinner-card__header:hover {
  background: rgba(0, 0, 0, 0.025);
}

/* ── Day info ──────────────────────────────────────────────────── */
.dinner-card__day-info {
  display: flex;
  align-items: baseline;
  gap: var(--space-2);
  flex-shrink: 0;
  min-width: 136px;
}

.dinner-card__day-name {
  font-size: var(--text-base);
  font-weight: 500;
  color: var(--color-text-primary);
}

.dinner-card__date {
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
}

.today-badge {
  font-size: 10px;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--color-primary-dark);
  background: rgba(212, 101, 42, 0.12);
  border-radius: var(--radius-sm);
  padding: 2px 6px;
  line-height: 1.6;
}

/* ── Summary ───────────────────────────────────────────────────── */
.dinner-card__summary {
  flex: 1;
  min-width: 0;
  display: flex;
  align-items: center;
}

.dinner-card__pills {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-1);
  overflow: hidden;
}

.dinner-card__hint {
  font-size: var(--text-sm);
}

.dinner-card__hint--muted {
  color: var(--color-text-muted);
  font-style: italic;
}

.dinner-card__hint--cta {
  color: var(--color-primary);
  font-weight: 500;
}

/* ── Action icon ───────────────────────────────────────────────── */
.dinner-card__action {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  color: var(--color-text-muted);
}

.dinner-card__plus-icon {
  color: var(--color-primary) !important;
  opacity: 0.7;
}

/* ── Expandable details (grid-row animation) ───────────────────── */
.dinner-card__details-wrapper {
  display: grid;
  grid-template-rows: 0fr;
  transition: grid-template-rows var(--duration-normal) var(--ease-out);
}

.dinner-card__details-wrapper.is-open {
  grid-template-rows: 1fr;
}

.dinner-card__details-inner {
  overflow: hidden;
}

/* ── Reduced motion ────────────────────────────────────────────── */
@media (prefers-reduced-motion: reduce) {
  .dinner-card__details-wrapper {
    transition: none;
  }
}

/* ── Mobile ────────────────────────────────────────────────────── */
@media (max-width: 599px) {
  .dinner-card__day-info {
    min-width: 100px;
  }

  .dinner-card__day-name {
    font-size: var(--text-sm);
  }
}
</style>

<script setup lang="ts">
import type { DateTime } from 'luxon'

const props = withDefaults(defineProps<{
  date?: DateTime
  menu?: Array<{ dishId: string; dishName: string }>
  isPlanned?: boolean
  isWeekend?: boolean
  isAdding?: boolean
  dishName?: string
  loading?: boolean
}>(), {
  date: undefined,
  menu: () => [],
  isPlanned: false,
  isWeekend: false,
  isAdding: false,
  dishName: '',
  loading: false,
})

const emit = defineEmits<{
  toggle: []
}>()

const { t, locale } = useI18n()

const dayLabelText = computed(() =>
  props.date?.setLocale(locale.value).toFormat('EEE') ?? ''
)
const dateLabelText = computed(() =>
  props.date?.setLocale(locale.value).toFormat('d MMM') ?? ''
)
const fullDateLabel = computed(() =>
  props.date?.setLocale(locale.value).toFormat('EEEE d MMM') ?? ''
)

const ariaLabel = computed(() =>
  props.isPlanned
    ? t('dishes.planDish.removeDayAriaLabel', { dish: props.dishName, day: fullDateLabel.value })
    : t('dishes.planDish.addDayAriaLabel', { dish: props.dishName, day: fullDateLabel.value })
)
</script>

<template>
  <div v-if="loading" class="plan-day-row plan-day-row--skeleton">
    <div class="plan-day-row__skeleton-label" />
    <div class="plan-day-row__skeleton-pills" />
  </div>
  <button
    v-else
    class="plan-day-row"
    :class="{ 'plan-day-row--weekend': isWeekend, 'plan-day-row--planned': isPlanned }"
    :disabled="isAdding"
    :aria-label="ariaLabel"
    @click="emit('toggle')"
  >
    <div class="plan-day-row__info">
      <span class="plan-day-row__day">{{ dayLabelText }}</span>
      <span class="plan-day-row__date">{{ dateLabelText }}</span>
    </div>
    <div class="plan-day-row__menu">
      <template v-if="menu.length > 0">
        <DishPill v-for="item in menu" :key="item.dishId" :name="item.dishName" size="sm" />
      </template>
      <span v-else class="plan-day-row__free" />
    </div>
    <v-progress-circular v-if="isAdding" size="16" width="2" indeterminate color="primary" class="plan-day-row__spinner" />
    <v-icon v-else-if="isPlanned" size="16" class="plan-day-row__remove-icon">mdi-close-circle</v-icon>
    <v-icon v-else size="16" class="plan-day-row__add-icon">mdi-calendar-plus</v-icon>
  </button>
</template>

<style scoped>
.plan-day-row {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  width: 100%;
  min-height: 44px;
  padding: var(--space-3) var(--space-4);
  background: none;
  border: none;
  font-family: var(--font-body);
  cursor: pointer;
  text-align: left;
  transition: background-color var(--duration-fast) var(--ease-out);
  border-left: 3px solid transparent;
}

.plan-day-row:hover:not(:disabled) {
  background-color: rgba(var(--color-primary-rgb), 0.04);
}

.plan-day-row:disabled {
  cursor: default;
  opacity: 0.7;
}

.plan-day-row--weekend {
  border-left-color: rgba(var(--color-primary-rgb), 0.25);
  background-color: rgba(var(--color-primary-rgb), 0.02);
}

.plan-day-row--weekend:hover:not(:disabled) {
  background-color: rgba(var(--color-primary-rgb), 0.07);
}

.plan-day-row__info {
  display: flex;
  flex-direction: column;
  min-width: 56px;
  flex-shrink: 0;
}

.plan-day-row__day {
  font-size: var(--text-sm);
  font-weight: 600;
  color: var(--color-text-primary);
  text-transform: capitalize;
}

.plan-day-row__date {
  font-size: var(--text-xs);
  color: var(--color-text-muted);
}

.plan-day-row__menu {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-1);
  flex: 1;
  align-items: center;
  min-width: 0;
  min-height: 36px;
}

.plan-day-row__free {
  display: block;
  width: 48px;
  height: 1px;
  border-top: 1.5px dashed var(--color-border-medium);
  border-radius: 1px;
}

.plan-day-row--planned {
  background-color: rgba(var(--color-primary-rgb), 0.05);
}

.plan-day-row--planned:hover:not(:disabled) {
  background-color: rgba(var(--color-error-rgb, 211, 47, 47), 0.06);
}

.plan-day-row__add-icon {
  color: var(--color-text-muted);
  flex-shrink: 0;
  opacity: 0;
  transition: opacity var(--duration-fast) var(--ease-out);
}

.plan-day-row:hover:not(:disabled) .plan-day-row__add-icon {
  opacity: 1;
  color: var(--color-primary);
}

.plan-day-row__remove-icon {
  color: var(--color-primary);
  flex-shrink: 0;
  opacity: 0.5;
  transition: opacity var(--duration-fast) var(--ease-out), color var(--duration-fast) var(--ease-out);
}

.plan-day-row:hover:not(:disabled) .plan-day-row__remove-icon {
  opacity: 1;
  color: var(--color-error);
}

.plan-day-row__spinner {
  flex-shrink: 0;
}

/* ── Skeleton ────────────────────────────────────────────────────────────────── */
.plan-day-row--skeleton {
  cursor: default;
  pointer-events: none;
}

.plan-day-row__skeleton-label {
  width: 56px;
  height: 32px;
  border-radius: var(--radius-sm);
  background: linear-gradient(90deg, var(--color-surface-variant) 25%, var(--color-border) 50%, var(--color-surface-variant) 75%);
  background-size: 200% 100%;
  animation: skeleton-shimmer 1.4s ease-in-out infinite;
}

.plan-day-row__skeleton-pills {
  flex: 1;
  height: 20px;
  border-radius: var(--radius-sm);
  background: linear-gradient(90deg, var(--color-surface-variant) 25%, var(--color-border) 50%, var(--color-surface-variant) 75%);
  background-size: 200% 100%;
  animation: skeleton-shimmer 1.4s ease-in-out infinite;
  max-width: 160px;
}

@keyframes skeleton-shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}
</style>

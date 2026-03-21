<script setup lang="ts">
import { DateTime } from 'luxon'
import type { Dinner, EffortLevel } from '~/types'

const props = defineProps<{
  weekStart: DateTime
  plannedDinners: Dinner[]
}>()

const emit = defineEmits<{
  'dish:used': [date: string, dishId: string, dishName: string]
}>()

const { suggestions, loading, exhausted, suggestWeek, rerollWeek, rerollDay, clearSuggestionForDate, reset } =
  useDinnerSuggestions()

const weekStartIso = computed(() => props.weekStart.toFormat('yyyy-MM-dd'))
const hasLoaded = ref(false)
const expanded = ref(false)

// Per-day effort preferences — ephemeral to this planning session (task 7.3)
const effortPreferences = ref<Record<string, EffortLevel | null>>({})

const activeEffortPreferences = computed(() => {
  const result: Record<string, EffortLevel> = {}
  for (const [date, level] of Object.entries(effortPreferences.value)) {
    if (level) result[date] = level
  }
  return Object.keys(result).length > 0 ? result : undefined
})

const plannedDates = computed(() =>
  new Set(
    props.plannedDinners
      .filter((d) => d.isPlanned && d.date >= props.weekStart)
      .map((d) => d.date.toFormat('yyyy-MM-dd')),
  ),
)

const unplannedWeekDays = computed(() =>
  Array.from({ length: 7 }, (_, i) => props.weekStart.plus({ days: i }).toFormat('yyyy-MM-dd')).filter(
    (date) => !plannedDates.value.has(date),
  ),
)

const visibleSuggestions = computed(() =>
  suggestions.value.filter((s) => !plannedDates.value.has(s.date) && s.suggestion !== null),
)

const hasSuggestions = computed(() => visibleSuggestions.value.length > 0)
const showEmptyMessage = computed(() =>
  !loading.value && (exhausted.value || (hasLoaded.value && !hasSuggestions.value)),
)

function onReset() {
  reset()
  effortPreferences.value = {}
  hasLoaded.value = false
}

async function onSuggest() {
  if (!hasLoaded.value) {
    await suggestWeek(weekStartIso.value, activeEffortPreferences.value)
    hasLoaded.value = true
  } else {
    await rerollWeek(weekStartIso.value, activeEffortPreferences.value)
  }
  expanded.value = true
}

function onUse(date: string, dishId: string, dishName: string) {
  clearSuggestionForDate(date)
  emit('dish:used', date, dishId, dishName)
}

function onReroll(date: string) {
  rerollDay(date, effortPreferences.value[date] ?? null)
}
</script>

<template>
  <div class="suggestion-bar">
    <div class="bar-header">
      <button
        class="bar-label-toggle clickable"
        @click="expanded = !expanded"
      >
        <v-icon size="14" class="bar-icon">mdi-lightbulb-outline</v-icon>
        <span class="bar-label">Suggestions</span>
        <v-icon size="14" class="bar-chevron">
          {{ expanded ? 'mdi-chevron-up' : 'mdi-chevron-down' }}
        </v-icon>
      </button>
      <div class="bar-actions">
        <button v-if="hasLoaded" class="reset-btn" title="Clear suggestions" @click="onReset">
          <v-icon size="14">mdi-close</v-icon>
        </button>
        <button class="suggest-btn" :class="{ loading }" :disabled="loading" @click="onSuggest">
          <v-icon size="14">{{ hasLoaded ? 'mdi-dice-multiple' : 'mdi-lightbulb-outline' }}</v-icon>
          {{ hasLoaded ? 'Suggest again' : 'Suggest week' }}
        </button>
      </div>
    </div>

    <template v-if="expanded">
      <template v-if="!hasLoaded">
        <div class="suggestion-list">
          <div
            v-for="date in unplannedWeekDays"
            :key="date"
            class="suggestion-row"
          >
            <div class="suggestion-row__meta">
              <span class="suggestion-day">{{ DateTime.fromISO(date).toFormat('EEE d') }}</span>
              <PlanEffortSelector v-model="effortPreferences[date]" />
            </div>
          </div>
        </div>
      </template>

      <template v-else>
        <p v-if="showEmptyMessage" class="empty-message">
          No more options — all days are covered or candidates exhausted.
        </p>

        <div v-if="hasSuggestions" class="suggestion-list">
          <div
            v-for="(item, i) in visibleSuggestions"
            :key="item.date"
            class="suggestion-row"
            :style="{ animationDelay: `${i * 40}ms` }"
          >
            <div class="suggestion-row__meta">
              <span class="suggestion-day">{{ DateTime.fromISO(item.date).toFormat('EEE d') }}</span>
              <PlanEffortSelector v-model="effortPreferences[item.date]" />
            </div>
            <PlanSuggestedDish
              :date="item.date"
              :suggestion="item.suggestion!"
              @use="onUse"
              @reroll="onReroll"
            />
          </div>
        </div>
      </template>
    </template>
  </div>
</template>

<style scoped>
.suggestion-bar {
  position: relative;
  padding: var(--space-3) var(--space-3) var(--space-3) var(--space-4);
  background: linear-gradient(135deg, rgba(var(--color-primary-rgb), 0.04) 0%, rgba(var(--color-primary-rgb), 0.01) 100%);
  border-radius: var(--radius-lg);
  border: 1px solid rgba(var(--color-primary-rgb), 0.14);
  margin-bottom: var(--space-4);
  overflow: hidden;
}

/* Left accent strip */
.suggestion-bar::before {
  content: '';
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  width: 3px;
  background: linear-gradient(to bottom, rgba(var(--color-primary-rgb), 0.9), rgba(var(--color-primary-rgb), 0.3));
  border-radius: var(--radius-lg) 0 0 var(--radius-lg);
}

.bar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-2);
}

.bar-label-toggle {
  display: flex;
  align-items: center;
  gap: 5px;
  background: none;
  border: none;
  padding: 0;
  cursor: default;
}

.bar-label-toggle.clickable {
  cursor: pointer;
}

.bar-icon {
  color: rgba(var(--color-primary-rgb), 0.7);
}

.bar-label {
  font-size: var(--text-xs);
  font-weight: 700;
  letter-spacing: 0.09em;
  text-transform: uppercase;
  color: rgba(var(--color-primary-rgb), 0.8);
}

.bar-chevron {
  color: rgba(var(--color-primary-rgb), 0.5);
  transition: transform 0.2s ease;
}

.bar-actions {
  display: flex;
  align-items: center;
  gap: var(--space-1);
}

.suggest-btn {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 5px 12px;
  border-radius: 20px;
  border: 1px solid rgba(var(--color-primary-rgb), 0.35);
  background: rgba(var(--color-primary-rgb), 0.08);
  color: rgba(var(--color-primary-rgb), 0.9);
  font-size: var(--text-xs);
  font-weight: 600;
  letter-spacing: 0.02em;
  cursor: pointer;
  transition: background 0.15s ease, border-color 0.15s ease;
}

.suggest-btn:hover:not(:disabled) {
  background: rgba(var(--color-primary-rgb), 0.14);
  border-color: rgba(var(--color-primary-rgb), 0.5);
}

.suggest-btn:disabled {
  opacity: 0.6;
  cursor: default;
}

.reset-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  border: none;
  background: none;
  color: var(--color-text-muted);
  cursor: pointer;
  opacity: 0.6;
  transition: opacity 0.15s ease, background 0.15s ease;
}

.reset-btn:hover {
  opacity: 1;
  background: rgba(0, 0, 0, 0.06);
}

.suggestion-list {
  margin-top: var(--space-3);
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.suggestion-row {
  display: flex;
  flex-direction: column;
  gap: 5px;
  animation: row-in 0.22s ease both;
}

.suggestion-row__meta {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

@media (min-width: 600px) {
  .suggestion-row {
    flex-direction: row;
    align-items: center;
    gap: var(--space-2);
  }

  .suggestion-row__meta {
    flex-shrink: 0;
  }
}

@keyframes row-in {
  from {
    opacity: 0;
    transform: translateY(6px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.suggestion-day {
  width: 48px;
  font-size: 11px;
  font-weight: 600;
  color: rgba(var(--color-primary-rgb), 0.7);
  letter-spacing: 0.03em;
  flex-shrink: 0;
  text-transform: uppercase;
}

.empty-message {
  margin-top: var(--space-3);
  font-size: var(--text-sm);
  color: var(--color-text-muted);
  font-style: italic;
}
</style>

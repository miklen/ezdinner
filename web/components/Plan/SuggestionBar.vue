<script setup lang="ts">
import { DateTime } from 'luxon'
import type { Dinner } from '~/types'

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
const expanded = ref(true)

const plannedDates = computed(() =>
  new Set(
    props.plannedDinners
      .filter((d) => d.isPlanned && d.date >= props.weekStart)
      .map((d) => d.date.toFormat('yyyy-MM-dd')),
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
  hasLoaded.value = false
  expanded.value = true
}

async function onSuggest() {
  if (!hasLoaded.value) {
    await suggestWeek(weekStartIso.value)
    hasLoaded.value = true
    expanded.value = true
  } else {
    await rerollWeek(weekStartIso.value)
    expanded.value = true
  }
}

function onRerollDay(date: string) {
  rerollDay(date)
}

function onUse(date: string, dishId: string, dishName: string) {
  clearSuggestionForDate(date)
  emit('dish:used', date, dishId, dishName)
}
</script>

<template>
  <div class="suggestion-bar">
    <div class="bar-header">
      <button
        class="bar-label-toggle"
        :class="{ clickable: hasLoaded }"
        :disabled="!hasLoaded"
        @click="hasLoaded && (expanded = !expanded)"
      >
        <v-icon size="14" class="bar-icon">mdi-lightbulb-outline</v-icon>
        <span class="bar-label">Suggestions</span>
        <v-icon v-if="hasLoaded" size="14" class="bar-chevron">
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

    <template v-if="expanded && hasLoaded">
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
          <span class="suggestion-day">{{ DateTime.fromISO(item.date).toFormat('EEE d') }}</span>
          <PlanSuggestedDish
            :date="item.date"
            :suggestion="item.suggestion!"
            @use="onUse"
            @reroll="onRerollDay"
          />
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.suggestion-bar {
  position: relative;
  padding: var(--space-3) var(--space-3) var(--space-3) var(--space-4);
  background: linear-gradient(135deg, rgba(212, 101, 42, 0.04) 0%, rgba(212, 101, 42, 0.01) 100%);
  border-radius: var(--radius-lg);
  border: 1px solid rgba(212, 101, 42, 0.14);
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
  background: linear-gradient(to bottom, rgba(212, 101, 42, 0.9), rgba(212, 101, 42, 0.3));
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
  color: rgba(212, 101, 42, 0.7);
}

.bar-label {
  font-size: var(--text-xs);
  font-weight: 700;
  letter-spacing: 0.09em;
  text-transform: uppercase;
  color: rgba(212, 101, 42, 0.8);
}

.bar-chevron {
  color: rgba(212, 101, 42, 0.5);
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
  border: 1px solid rgba(212, 101, 42, 0.35);
  background: rgba(212, 101, 42, 0.08);
  color: rgba(212, 101, 42, 0.9);
  font-size: var(--text-xs);
  font-weight: 600;
  letter-spacing: 0.02em;
  cursor: pointer;
  transition: background 0.15s ease, border-color 0.15s ease;
}

.suggest-btn:hover:not(:disabled) {
  background: rgba(212, 101, 42, 0.14);
  border-color: rgba(212, 101, 42, 0.5);
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
  align-items: center;
  gap: var(--space-3);
  animation: row-in 0.22s ease both;
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
  color: rgba(212, 101, 42, 0.7);
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

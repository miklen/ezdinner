<script setup lang="ts">
import { ref } from 'vue'
import type { DishSuggestion } from '~/types'

defineProps<{
  date: string
  suggestion: DishSuggestion
}>()

const emit = defineEmits<{
  'use': [date: string, dishId: string, dishName: string]
  'reroll': [date: string]
}>()

const showReasons = ref(false)

function formatRating(rating: number): string {
  return rating > 0 ? rating.toFixed(1) : '—'
}

function formatDaysAgo(days: number): string {
  if (days <= 0) return 'never'
  if (days === 1) return '1d ago'
  return `${days}d ago`
}
</script>

<template>
  <div class="suggested-dish">
    <div class="dish-row">
      <div class="dish-main">
        <span class="dish-name">{{ suggestion.dishName }}</span>
        <div class="dish-meta">
          <span class="dish-rating">
            <v-icon size="11" color="primary">mdi-heart</v-icon>
            {{ formatRating(suggestion.rating) }}
          </span>
          <span class="dish-days">{{ formatDaysAgo(suggestion.daysSinceLast) }}</span>
        </div>
      </div>
      <div class="actions">
        <button
          v-if="suggestion.reasons.length > 0"
          class="info-btn"
          :class="{ active: showReasons }"
          :aria-label="showReasons ? 'Hide reasons for this suggestion' : 'Show reasons for this suggestion'"
          @click="showReasons = !showReasons"
        >
          <v-icon size="13">mdi-information-outline</v-icon>
        </button>
        <button class="use-btn" @click="emit('use', date, suggestion.dishId, suggestion.dishName)">
          Use
        </button>
        <button class="reroll-btn" title="Reroll this day" @click="emit('reroll', date)">
          <v-icon size="13">mdi-dice-multiple</v-icon>
        </button>
      </div>
    </div>
    <div class="reason-tags-wrapper" :class="{ open: showReasons && suggestion.reasons.length > 0 }">
      <div class="reason-tags">
        <span v-for="(reason, i) in suggestion.reasons" :key="`${reason}-${i}`" class="reason-tag">{{ reason }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.suggested-dish {
  flex: 1;
  display: flex;
  flex-direction: column;
  padding: 6px var(--space-2);
  background: rgba(255, 255, 255, 0.7);
  border-radius: 10px;
  border: 1px solid rgba(var(--color-primary-rgb), 0.12);
  min-width: 0;
}

.dish-row {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.dish-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.dish-name {
  font-size: var(--text-sm);
  font-weight: 500;
  color: var(--color-text);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.dish-meta {
  display: flex;
  align-items: center;
  gap: 6px;
}

.dish-rating {
  display: inline-flex;
  align-items: center;
  gap: 2px;
  font-size: 11px;
  font-weight: 500;
  color: var(--color-text-muted);
}

.dish-days {
  font-size: 11px;
  color: var(--color-text-muted);
  opacity: 0.7;
}

.actions {
  display: flex;
  align-items: center;
  gap: 4px;
  flex-shrink: 0;
}

.use-btn {
  padding: 3px 10px;
  border-radius: 12px;
  border: 1px solid rgba(var(--color-primary-rgb), 0.4);
  background: rgba(var(--color-primary-rgb), 0.09);
  color: rgba(var(--color-primary-rgb), 0.9);
  font-size: 11px;
  font-weight: 600;
  cursor: pointer;
  letter-spacing: 0.02em;
  transition: background 0.15s ease;
  white-space: nowrap;
}

.use-btn:hover {
  background: rgba(var(--color-primary-rgb), 0.18);
}

.reroll-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 44px;
  height: 44px;
  border-radius: 50%;
  border: none;
  background: none;
  color: var(--color-text-muted);
  cursor: pointer;
  opacity: 0.5;
  transition: opacity 0.15s ease;
}

.reroll-btn:hover {
  opacity: 1;
}

.info-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 44px;
  height: 44px;
  border-radius: 50%;
  border: none;
  background: none;
  color: var(--color-text-muted);
  cursor: pointer;
  opacity: 0.65;
  transition: opacity 0.15s ease, color 0.15s ease;
}

.info-btn:hover,
.info-btn.active {
  opacity: 1;
  color: rgba(var(--color-primary-rgb), 0.9);
}

.reason-tags-wrapper {
  display: grid;
  grid-template-rows: 0fr;
  transition: grid-template-rows 0.2s ease;
}

.reason-tags-wrapper.open {
  grid-template-rows: 1fr;
}

.reason-tags {
  overflow: hidden;
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  padding-top: 4px;
}

.reason-tag {
  font-size: 10px;
  font-weight: 500;
  color: rgba(var(--color-primary-rgb), 0.85);
  background: rgba(var(--color-primary-rgb), 0.08);
  border: 1px solid rgba(var(--color-primary-rgb), 0.2);
  border-radius: 8px;
  padding: 1px 7px;
  white-space: nowrap;
}
</style>

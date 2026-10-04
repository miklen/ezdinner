<script setup lang="ts">
import { computed } from 'vue'
import { DateTime } from 'luxon'
import type { RatingReminder, ReminderAction } from '~/types/rating-reminders'

const props = defineProps<{ reminder: RatingReminder; today: string; pendingAction: ReminderAction; selection: number; disabled?: boolean; saved?: boolean }>()
const emit = defineEmits<{ rate: [value: number]; dismiss: [] }>()
const { t, locale } = useI18n()
const menuContext = computed(() => props.reminder.dinnerDate === DateTime.fromISO(props.today).minus({ days: 1 }).toFormat('yyyy-MM-dd')
  ? t('ratingReminders.yesterday')
  : t('ratingReminders.menuDate', { date: DateTime.fromISO(props.reminder.dinnerDate).setLocale(locale.value).toFormat('d MMMM') }))
</script>

<template>
  <v-card class="rating-reminder" :aria-busy="pendingAction !== 'idle'">
    <h2 class="text-card-title">{{ t('ratingReminders.heading') }}</h2>
    <div class="rating-reminder__context">
      <DishPill :name="reminder.dishName" :to="`/dishes/${reminder.dishId}`" />
      <span class="rating-reminder__date">{{ menuContext }}</span>
    </div>
    <div class="rating-reminder__actions">
      <div class="rating-reminder__hearts">
        <span class="rating-reminder__label">{{ t('ratingReminders.yourRating') }}</span>
        <DishRating :model-value="selection" :size="24" editable :disabled="disabled || pendingAction !== 'idle'" :label="t('ratingReminders.yourRating')" @update:model-value="emit('rate', $event)" />
      </div>
      <v-btn variant="text" :disabled="disabled || pendingAction !== 'idle'" :loading="pendingAction === 'dismissing'" @click="emit('dismiss')">
        {{ t('ratingReminders.notNow') }}
      </v-btn>
    </div>
    <p v-if="pendingAction !== 'idle'" role="status" class="rating-reminder__date">{{ t('ratingReminders.pending') }}</p>
    <p v-else-if="saved" class="rating-reminder__confirmation"><v-icon icon="mdi-check" size="18" aria-hidden="true" /> {{ t('ratingReminders.rated') }}</p>
  </v-card>
</template>

<style scoped>
.rating-reminder { padding: var(--space-4) var(--space-6); }
.rating-reminder__context { display: flex; flex-wrap: wrap; align-items: center; gap: var(--space-3); margin: var(--space-3) 0; min-width: 0; }
.rating-reminder__date { font-size: var(--text-sm); color: var(--color-text-muted); }
.rating-reminder__actions { display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: var(--space-2); }
.rating-reminder__hearts { display: flex; flex-wrap: wrap; align-items: center; gap: var(--space-3); }
.rating-reminder__label { font-size: var(--text-sm); color: var(--color-text-secondary); }
.rating-reminder__confirmation { display: flex; align-items: center; gap: var(--space-2); font-size: var(--text-sm); color: var(--color-primary); }
@media (max-width: 600px) { .rating-reminder { padding: var(--space-4); } }
</style>

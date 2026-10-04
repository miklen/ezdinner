<script setup lang="ts">
import { useRatingReminderPreference } from '~/composables/useRatingReminderPreference'
const props = defineProps<{ subscribed: boolean }>()
const { $msal } = useNuxtApp()
const { ratingReminders } = useRepositories()
const { t } = useI18n()
const { enabled, state, active, setEnabled, refresh } = useRatingReminderPreference({
  account: () => $msal.getObjectId() ?? '', subscribed: () => props.subscribed, repository: ratingReminders,
})
function toggle(value: boolean | null) { if (value !== null) void setEnabled(value) }
</script>

<template>
  <v-list-item class="rating-preference" prepend-icon="mdi-heart-outline" :title="t('ratingReminders.preference')">
    <template #append>
      <v-switch :model-value="enabled" :disabled="!active" :loading="state === 'saving' || state === 'loading'" :aria-label="t('ratingReminders.preference')" color="primary" density="compact" hide-details @update:model-value="toggle" />
    </template>
  </v-list-item>
  <p class="rating-preference-hint">{{ t(subscribed ? 'ratingReminders.preferenceHint' : 'ratingReminders.subscriptionRequired') }}</p>
  <p v-if="state === 'saveFailed' || state === 'loadFailed'" role="alert" class="rating-preference-hint">
    {{ t(state === 'saveFailed' ? 'ratingReminders.preferenceFailed' : 'ratingReminders.preferenceLoadFailed') }}
    <v-btn v-if="state === 'loadFailed'" variant="text" @click="refresh">{{ t('ratingReminders.retry') }}</v-btn>
  </p>
</template>

<style scoped>
.rating-preference :deep(.v-list-item-title) { white-space: normal; line-height: 1.4; }
.rating-preference-hint { padding: 0 var(--space-4) var(--space-2); font-size: var(--text-xs); line-height: 1.4; color: var(--color-text-muted); }
</style>

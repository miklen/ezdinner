<template>
  <div class="empty-state">
    <div class="empty-state__icon-wrap">
      <v-icon :icon="icon" size="40" class="empty-state__icon" />
    </div>
    <p class="empty-state__message">{{ message }}</p>
    <v-btn
      v-if="actionLabel && actionTo"
      :to="actionTo"
      color="primary"
      variant="tonal"
      rounded="lg"
      class="mt-2"
    >
      {{ actionLabel }}
    </v-btn>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  icon: string
  message: string
  actionLabel?: string
  actionTo?: string
}>()
</script>

<style scoped>
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--space-12) var(--space-6);
  text-align: center;
  gap: var(--space-4);
}

.empty-state__icon-wrap {
  width: 72px;
  height: 72px;
  border-radius: var(--radius-full);
  background-color: var(--color-surface-variant);
  display: flex;
  align-items: center;
  justify-content: center;
}

.empty-state__icon {
  color: var(--color-text-muted);
}

.empty-state__message {
  font-family: var(--font-body);
  font-size: var(--text-base);
  color: var(--color-text-secondary);
  max-width: 280px;
  line-height: 1.6;
  margin: 0;
}
</style>

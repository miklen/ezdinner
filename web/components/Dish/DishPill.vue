<template>
  <span class="dish-pill" :class="`dish-pill--${size}`">
    <span class="dish-pill__name">{{ name }}</span>
    <button
      v-if="removable"
      class="dish-pill__remove"
      :aria-label="`Remove ${name}`"
      @click.stop="emit('remove')"
    >
      <v-icon size="14">mdi-close</v-icon>
    </button>
  </span>
</template>

<script setup lang="ts">
defineProps<{
  name: string
  size?: 'sm' | 'md'
  removable?: boolean
}>()

const emit = defineEmits<{
  remove: []
}>()
</script>

<style scoped>
.dish-pill {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
  border-radius: var(--radius-sm);
  border: 1px solid var(--color-border-medium);
  background-color: var(--color-surface);
  font-family: var(--font-body);
  font-weight: 400;
  color: var(--color-text-primary);
  white-space: nowrap;
  max-width: 200px;
}

.dish-pill--sm {
  padding: 2px var(--space-2);
  font-size: var(--text-xs);
}

.dish-pill--md {
  padding: var(--space-1) var(--space-3);
  font-size: var(--text-sm);
}

.dish-pill__name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.dish-pill__remove {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;
  border-radius: var(--radius-full);
  border: none;
  background: transparent;
  cursor: pointer;
  color: var(--color-text-muted);
  padding: 0;
  flex-shrink: 0;
  transition: background-color var(--duration-instant) var(--ease-out), color var(--duration-instant) var(--ease-out);
  /* Ensure 48px tap target via parent — the pill itself sits inside a larger row */
  min-height: 18px;
}

.dish-pill__remove:hover {
  background-color: var(--color-surface-variant);
  color: var(--color-error);
}
</style>

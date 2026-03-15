<script setup lang="ts">
const props = withDefaults(defineProps<{
  name: string
  size?: 'sm' | 'md'
  removable?: boolean
  to?: string
}>(), {
  size: 'md',
  to: undefined,
})

const emit = defineEmits<{
  remove: []
}>()
</script>

<template>
  <span
    class="dish-pill"
    :class="[`dish-pill--${props.size}`, { 'dish-pill--linked': !!props.to }]"
  >
    <NuxtLink v-if="to" :to="to" class="dish-pill__name dish-pill__link">{{ name }}</NuxtLink>
    <span v-else class="dish-pill__name">{{ name }}</span>
    <button
      v-if="removable"
      class="dish-pill__remove"
      :aria-label="`Remove ${name}`"
      @click="emit('remove')"
    >
      <v-icon size="14">mdi-close</v-icon>
    </button>
  </span>
</template>

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
  text-decoration: none;
}

.dish-pill--sm {
  padding: 2px var(--space-2);
  font-size: var(--text-xs);
}

.dish-pill--md {
  padding: var(--space-1) var(--space-3);
  font-size: var(--text-sm);
}

.dish-pill--linked {
  cursor: pointer;
  transition:
    background-color var(--duration-fast) var(--ease-out),
    border-color var(--duration-fast) var(--ease-out);
}

.dish-pill--linked:hover {
  background-color: var(--color-primary-light);
  border-color: var(--color-primary);
}

.dish-pill__name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.dish-pill__link {
  color: inherit;
  text-decoration: none;
}

.dish-pill--linked:hover .dish-pill__link {
  text-decoration: underline;
  text-decoration-color: var(--color-primary-dark);
  text-underline-offset: 2px;
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
  /* Expand tap target to 48px without affecting visual size */
  position: relative;
}

.dish-pill__remove::after {
  content: '';
  position: absolute;
  inset: -15px;
}

.dish-pill__remove:hover {
  background-color: var(--color-surface-variant);
  color: var(--color-error);
}
</style>

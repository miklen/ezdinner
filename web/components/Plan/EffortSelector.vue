<script setup lang="ts">
import type { EffortLevel } from '~/types'

const props = defineProps<{
  modelValue: EffortLevel | null
}>()

const emit = defineEmits<{
  'update:modelValue': [value: EffortLevel | null]
}>()

const options: { label: string; value: EffortLevel }[] = [
  { label: 'Quick', value: 'Quick' },
  { label: 'Med', value: 'Medium' },
  { label: 'Elab', value: 'Elaborate' },
]

function toggle(value: EffortLevel) {
  emit('update:modelValue', props.modelValue === value ? null : value)
}
</script>

<template>
  <div class="effort-selector" role="group" aria-label="Effort preference">
    <button
      v-for="option in options"
      :key="option.value"
      class="effort-btn"
      :class="{ 'effort-btn--active': modelValue === option.value }"
      :aria-pressed="modelValue === option.value"
      @click="toggle(option.value)"
    >
      {{ option.label }}
    </button>
  </div>
</template>

<style scoped>
.effort-selector {
  display: flex;
  gap: 2px;
  flex-shrink: 0;
}

.effort-btn {
  padding: 5px 10px;
  border-radius: 10px;
  border: 1px solid rgba(var(--color-primary-rgb), 0.2);
  background: transparent;
  color: rgba(var(--color-primary-rgb), 0.55);
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.02em;
  cursor: pointer;
  transition:
    background 0.12s ease,
    border-color 0.12s ease,
    color 0.12s ease;
  line-height: 1.4;
  min-height: 30px;
}

.effort-btn:hover {
  background: rgba(var(--color-primary-rgb), 0.06);
  color: rgba(var(--color-primary-rgb), 0.8);
  border-color: rgba(var(--color-primary-rgb), 0.35);
}

.effort-btn--active {
  background: rgba(var(--color-primary-rgb), 0.14);
  border-color: rgba(var(--color-primary-rgb), 0.5);
  color: rgba(var(--color-primary-rgb), 0.9);
}
</style>

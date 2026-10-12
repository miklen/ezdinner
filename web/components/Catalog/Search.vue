<script setup lang="ts">
defineProps<{ modelValue: string; placeholder: string; maxlength?: number }>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()
</script>

<template>
  <v-text-field :model-value="modelValue" :placeholder="placeholder" :aria-label="placeholder" :maxlength="maxlength" prepend-inner-icon="mdi-magnify" variant="outlined" rounded="lg" density="compact" hide-details clearable @update:model-value="emit('update:modelValue', $event ?? '')" />
</template>

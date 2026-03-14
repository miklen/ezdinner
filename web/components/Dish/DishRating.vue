<template>
  <v-rating
    color="primary"
    half-increments
    empty-icon="mdi-heart-outline"
    full-icon="mdi-heart"
    half-icon="mdi-heart-half-full"
    length="5"
    :size="size ?? 18"
    :model-value="modelValue"
    readonly
    style="gap: 4px;"
  />
</template>

<script setup lang="ts">
defineProps<{
  modelValue: number
  size?: number | string
}>()
</script>


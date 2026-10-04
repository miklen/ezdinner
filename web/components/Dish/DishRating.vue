<script setup lang="ts">
const props = withDefaults(defineProps<{
  size?: number | string
  editable?: boolean
  disabled?: boolean
  label?: string
  gap?: number
}>(), { size: 18, editable: false, disabled: false, label: undefined, gap: 4 })
const model = defineModel<number>({ required: true })
const { t } = useI18n()
function update(value: number | string) {
  if (!props.editable || props.disabled) return
  const rating = Number(value)
  if (Number.isFinite(rating) && rating >= 0 && rating <= 5) model.value = rating
}
</script>

<template>
  <v-rating
    class="dish-rating"
    color="var(--color-heart)"
    half-increments
    empty-icon="mdi-heart-outline"
    full-icon="mdi-heart"
    half-icon="mdi-heart-half-full"
    length="5"
    :size="size"
    :model-value="model"
    :readonly="!editable"
    :disabled="disabled"
    :aria-label="label ?? t('ratingReminders.yourRating')"
    item-aria-label="ratingReminders.heartLabel"
    :style="{ gap: `${gap}px` }"
    @update:model-value="update"
  />
</template>

<style scoped>
.dish-rating :deep(.v-btn) {
  min-width: unset !important;
  width: auto !important;
  padding: 0 !important;
}
</style>


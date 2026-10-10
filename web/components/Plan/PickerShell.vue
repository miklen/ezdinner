<script setup lang="ts">
import { computed, nextTick, shallowRef, watch } from 'vue'
import { useDisplay } from 'vuetify'
import { VBottomSheet, VDialog } from 'vuetify/components'

const props = defineProps<{ open: boolean; title: string; closeLabel: string }>()
const emit = defineEmits<{ 'update:open': [open: boolean] }>()
defineSlots<{ default(): unknown; navigation?(): unknown; footer?(): unknown }>()
const { smAndDown, height } = useDisplay()
const sheetMaxHeight = computed(() => height.value < 600 ? '90dvh' : '80dvh')
const initiatingAction = shallowRef<HTMLElement | null>(null)
watch(() => props.open, open => {
  if (open && document.activeElement instanceof HTMLElement) initiatingAction.value = document.activeElement
}, { flush: 'sync' })
async function restoreFocus() {
  await nextTick()
  if (!props.open && initiatingAction.value?.isConnected) initiatingAction.value.focus({ preventScroll: true })
}
</script>

<template>
  <component :is="smAndDown ? VBottomSheet : VDialog" :model-value="open" :width="smAndDown ? undefined : 440" :max-height="smAndDown ? sheetMaxHeight : undefined" @update:model-value="emit('update:open', $event)" @after-leave="restoreFocus">
    <v-card class="plan-picker" :class="smAndDown ? 'plan-picker--sheet' : 'plan-picker--dialog'" :style="smAndDown ? { maxHeight: sheetMaxHeight } : undefined">
      <div v-if="smAndDown" class="plan-picker__handle" aria-hidden="true" />
      <div class="plan-picker__header">
        <span class="plan-picker__title">{{ title }}</span>
        <button type="button" class="plan-picker__close" :aria-label="closeLabel" @click="emit('update:open', false)"><v-icon size="18">mdi-close</v-icon></button>
      </div>
      <div v-if="$slots.navigation" class="plan-picker__weeknav"><slot name="navigation" /></div>
      <div class="plan-picker__body"><slot /></div>
      <div v-if="$slots.footer" class="plan-picker__footer"><slot name="footer" /></div>
    </v-card>
  </component>
</template>

<style scoped>
.plan-picker { display: flex; flex-direction: column; overflow: hidden; max-height: calc(100dvh - var(--space-12)); }
.plan-picker--dialog { border-radius: var(--radius-lg) !important; }
.plan-picker--sheet { border-radius: var(--radius-xl) var(--radius-xl) 0 0 !important; padding-bottom: env(safe-area-inset-bottom, 0); }
.plan-picker__handle { width: 36px; height: 4px; border-radius: var(--radius-full); background: var(--color-border-medium); margin: var(--space-3) auto 0; flex-shrink: 0; }
.plan-picker__header { display: flex; align-items: center; justify-content: space-between; gap: var(--space-2); padding: var(--space-4) var(--space-4) var(--space-3); flex-shrink: 0; }
.plan-picker--sheet .plan-picker__header { padding-top: var(--space-3); padding-bottom: var(--space-2); }
.plan-picker__title { font-size: var(--text-base); font-weight: 600; color: var(--color-text-primary); min-width: 0; overflow-wrap: anywhere; }
.plan-picker__close { display: flex; align-items: center; justify-content: center; width: 28px; height: 28px; border: none; background: none; border-radius: var(--radius-full); cursor: pointer; color: var(--color-text-muted); flex-shrink: 0; transition: background-color var(--duration-fast) var(--ease-out); }
.plan-picker--sheet .plan-picker__close { width: 44px; height: 44px; }
.plan-picker__close:hover { background-color: var(--color-surface-variant); color: var(--color-text-secondary); }
.plan-picker__close:focus-visible { outline: 2px solid var(--color-primary-dark); outline-offset: 2px; }
.plan-picker__weeknav { padding: 0 var(--space-4) var(--space-3); flex-shrink: 0; }
.plan-picker__body { padding-bottom: var(--space-2); max-height: 440px; min-height: 0; overflow-y: auto; overscroll-behavior: contain; }
.plan-picker--sheet .plan-picker__body { flex: 1; max-height: none; }
.plan-picker__body :deep(.plan-picker__divider) { height: 1px; background: var(--color-border-medium); margin: var(--space-1) var(--space-4); }
.plan-picker__footer { padding: var(--space-2) var(--space-4) var(--space-4); flex-shrink: 0; }
</style>

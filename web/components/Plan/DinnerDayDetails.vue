<script setup lang="ts">
import { computed, shallowRef, watch } from 'vue'
import type { Dinner } from '~/types'
import DishPill from '~/components/Dish/DishPill.vue'
const props = defineProps<{ dinner: Dinner; reasons: string[]; busy: boolean }>()
const emit = defineEmits<{ removeDish: [dishId: string]; setOptOut: [reason: string]; removeOptOut: [] }>()
const { t } = useI18n()
const showOptOut = shallowRef(false)
const customOptOutReason = shallowRef('')
const defaultOptOutPicks = computed(() => ['vacation', 'eatingOut', 'restaurant', 'guests', 'leftovers'].map(reason => t('plan.optOut.' + reason)))
const optOutQuickPicks = computed(() => props.reasons.length ? props.reasons : defaultOptOutPicks.value)
function setOptOut(reason: string) {
  if (props.busy || !reason.trim()) return
  emit('setOptOut', reason.trim())
}
watch(() => props.dinner.isOptedOut, optedOut => {
  if (!optedOut) return
  customOptOutReason.value = ''
  showOptOut.value = false
})
</script>

<template>
  <div class="dinner-details">
    <div class="dinner-details__body">
      <template v-if="dinner.isOptedOut">
        <div class="dinner-details__optout-status">
          <v-icon size="16" class="dinner-details__optout-icon">mdi-calendar-remove-outline</v-icon>
          <span class="dinner-details__optout-label">{{ dinner.optOutReason }}</span>
          <v-btn
            variant="text"
            size="small"
            color="primary"
            :loading="busy"
            :disabled="busy"
            class="dinner-details__optout-remove"
            @click="emit('removeOptOut')"
          >
            {{ $t('common.remove') }}
          </v-btn>
        </div>
      </template>

      <template v-else>
        <div v-if="dinner.menu.length" class="dinner-details__menu">
          <DishPill v-for="item in dinner.menu" :key="item.dishId" :name="item.dishName" :to="'/dishes/' + item.dishId" size="md" removable :disabled="busy" @remove="emit('removeDish', item.dishId)" />
        </div>
        <slot name="assignment" />

        <div class="dinner-details__optout-toggle-row">
          <button class="dinner-details__optout-toggle" :disabled="busy" :aria-expanded="showOptOut" @click="showOptOut = !showOptOut">
            <v-icon size="13">{{ showOptOut ? 'mdi-chevron-up' : 'mdi-calendar-remove-outline' }}</v-icon>
            {{ showOptOut ? $t('plan.cancelSkip') : $t('plan.skipDay') }}
          </button>
        </div>

        <div v-if="showOptOut" class="dinner-details__optout-section">
          <div class="dinner-details__optout-picks">
            <button
              v-for="pick in optOutQuickPicks"
              :key="pick"
              class="dinner-details__optout-chip"
              :disabled="busy"
              @click="setOptOut(pick)"
            >
              {{ pick }}
            </button>
          </div>
          <div class="dinner-details__optout-custom">
            <input
              v-model="customOptOutReason"
              class="dinner-details__optout-input"
              :placeholder="$t('plan.otherReasonPlaceholder')"
              :aria-label="$t('plan.otherReasonPlaceholder')"
              :disabled="busy"
              maxlength="50"
              @keyup.enter="setOptOut(customOptOutReason)"
            >
            <button
              v-if="customOptOutReason.trim()"
              class="dinner-details__optout-submit" :aria-label="$t('common.save')"
              :disabled="busy"
              @click="setOptOut(customOptOutReason)"
            >
              <v-icon size="16">mdi-check</v-icon>
            </button>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
.dinner-details {
  border-top: 1px solid rgba(0, 0, 0, 0.07);
}

.dinner-details__body {
  padding: var(--space-4);
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.dinner-details__menu {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}

.dinner-details__optout-status {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-2) var(--space-3);
  background: rgba(0, 0, 0, 0.035);
  border-radius: var(--radius-md);
}

.dinner-details__optout-icon {
  color: var(--color-text-muted) !important;
  flex-shrink: 0;
}

.dinner-details__optout-label {
  flex: 1;
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
  font-style: italic;
  overflow-wrap: anywhere;
}

.dinner-details__optout-remove {
  text-transform: none !important;
  letter-spacing: 0 !important;
  padding: 0 var(--space-2) !important;
  height: auto !important;
  min-width: 0 !important;
}

.dinner-details__optout-toggle-row {
  display: flex;
}

.dinner-details__optout-toggle {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: var(--text-xs);
  font-family: inherit;
  color: var(--color-text-muted);
  background: none;
  border: none;
  padding: 0;
  cursor: pointer;
  transition: color var(--duration-fast) var(--ease-standard);
}

.dinner-details__optout-toggle:hover {
  color: var(--color-text-secondary);
}

.dinner-details__optout-section {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.dinner-details__optout-picks {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-1);
}

.dinner-details__optout-chip {
  font-size: var(--text-xs);
  font-family: inherit;
  color: var(--color-text-secondary);
  background: rgba(0, 0, 0, 0.055);
  border: 1px solid rgba(0, 0, 0, 0.1);
  border-radius: var(--radius-full);
  padding: 3px 10px;
  cursor: pointer;
  transition:
    background var(--duration-fast) var(--ease-standard),
    color var(--duration-fast) var(--ease-standard);
  white-space: nowrap;
}

.dinner-details__optout-chip:hover:not(:disabled) {
  background: rgba(0, 0, 0, 0.1);
  color: var(--color-text-primary);
}

.dinner-details__optout-chip:disabled {
  opacity: 0.5;
  cursor: default;
}

.dinner-details__optout-custom {
  display: flex;
  align-items: center;
  gap: var(--space-1);
}

.dinner-details__optout-input {
  flex: 1;
  min-width: 0;
  font-size: var(--text-sm);
  font-family: inherit;
  color: var(--color-text-primary);
  background: transparent;
  border: none;
  border-bottom: 1px solid rgba(0, 0, 0, 0.15);
  padding: 2px 2px 3px;
  outline: none;
  transition: border-color var(--duration-fast) var(--ease-standard);
}

.dinner-details__optout-input:focus {
  border-bottom-color: var(--color-primary);
}

.dinner-details__optout-input::placeholder {
  color: var(--color-text-muted);
}

.dinner-details__optout-submit {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border: none;
  border-radius: var(--radius-full);
  background: var(--color-primary);
  color: white;
  cursor: pointer;
  flex-shrink: 0;
  transition: opacity var(--duration-fast) var(--ease-standard);
}

.dinner-details__optout-submit:disabled {
  opacity: 0.5;
  cursor: default;
}
.dinner-details__optout-toggle:disabled { opacity: 0.5; cursor: default; }
</style>

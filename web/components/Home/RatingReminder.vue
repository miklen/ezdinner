<script setup lang="ts">
import { computed, nextTick, onScopeDispose, shallowRef, useTemplateRef, watch } from 'vue'
import { useRatingReminders } from '~/composables/useRatingReminders'
import type { RatingReminder } from '~/types/rating-reminders'

const { $msal } = useNuxtApp()
const appStore = useAppStore()
const { ratingReminders, dishes } = useRepositories()
const { t } = useI18n()
const host = useTemplateRef<HTMLElement>('host')
const reminders = useRatingReminders({ family: () => appStore.activeFamilyId,
  account: () => $msal.getObjectId() ?? '', repository: ratingReminders, dishes })
const { current, request, action, today, pending } = reminders
const selection = shallowRef(0)
const announcement = shallowRef('')
const confirmation = shallowRef<{ reminder: RatingReminder; status: 'saving' | 'saved' }>()
const displayed = computed(() => confirmation.value?.reminder ?? current.value)
const identity = computed(() => JSON.stringify([appStore.activeFamilyId, $msal.getObjectId()]))
const leaving = shallowRef(false)
const locked = computed(() => !!confirmation.value || leaving.value || pending.value)
let confirmationTimer: ReturnType<typeof setTimeout> | undefined
let presentationGeneration = 0
let restoreFocus = false
const pendingAction = computed(() => confirmation.value?.status === 'saving' ? 'rating'
  : action.value.status === 'rating' || action.value.status === 'dismissing' ? action.value.status : 'idle')
const actionError = computed(() => action.value.status === 'failed'
  ? t(action.value.action === 'rating' ? 'ratingReminders.rateFailed' : 'ratingReminders.dismissFailed') : '')
watch(() => displayed.value?.dishId, () => { selection.value = 0 })
watch(identity, resetConfirmation, { flush: 'sync' })
onScopeDispose(resetConfirmation)

function resetConfirmation() {
  presentationGeneration++
  clearTimeout(confirmationTimer)
  confirmation.value = undefined
  selection.value = 0
  announcement.value = ''
  leaving.value = false
  restoreFocus = false
}
function focusAfterProgress() {
  if (restoreFocus && (document.activeElement === document.body || host.value?.contains(document.activeElement))) host.value?.focus()
  restoreFocus = false
}
async function finishConfirmation() {
  confirmation.value = undefined
  if (current.value) announcement.value += ' ' + t('ratingReminders.nextDish', { name: current.value.dishName })
  await nextTick()
  if (!leaving.value) focusAfterProgress()
}
function finishLeaving() {
  leaving.value = false
  focusAfterProgress()
}

async function act(kind: 'rate' | 'dismiss', value = selection.value) {
  if (locked.value || !current.value) return
  const startedGeneration = presentationGeneration
  const focused = !!host.value?.contains(document.activeElement)
  if (kind === 'rate') {
    selection.value = value
    confirmation.value = { reminder: current.value, status: 'saving' }
  }
  const result = kind === 'rate' ? await reminders.rate(value) : await reminders.dismiss()
  if (startedGeneration !== presentationGeneration) return
  if (result !== 'saved') { confirmation.value = undefined; return }
  announcement.value = t(kind === 'rate' ? 'ratingReminders.rated' : 'ratingReminders.dismissed')
  restoreFocus = focused
  if (confirmation.value) {
    confirmation.value = { reminder: confirmation.value.reminder, status: 'saved' }
    confirmationTimer = setTimeout(() => { void finishConfirmation() }, 1200)
    return
  }
  await finishConfirmation()
}
function retryAction() { if (action.value.status === 'failed') void act(action.value.action === 'rating' ? 'rate' : 'dismiss') }
</script>

<template>
  <section ref="host" tabindex="-1" :aria-label="t('ratingReminders.heading')">
    <p role="status" aria-live="polite" class="rating-reminder-live">{{ announcement }}</p>
    <Transition :key="identity" name="reminder-fade" mode="out-in" @before-leave="leaving = true" @after-leave="finishLeaving">
      <HomeRatingReminderCard v-if="displayed" :key="displayed.dishId" :reminder="displayed" :today="today" :pending-action="pendingAction" :selection="selection" :disabled="locked" :saved="confirmation?.status === 'saved'" @rate="act('rate', $event)" @dismiss="act('dismiss')" />
      <v-skeleton-loader v-else-if="request.status === 'loading'" key="loading" type="text, actions" height="140" :loading-text="t('ratingReminders.loading')" />
    </Transition>
    <div v-if="actionError" role="alert" class="rating-reminder-error">
      {{ actionError }} <v-btn variant="text" :disabled="locked" @click="retryAction">{{ t('ratingReminders.retry') }}</v-btn>
    </div>
    <div v-if="request.status === 'failed'" role="alert" class="rating-reminder-error">
      {{ t('ratingReminders.loadFailed') }} <v-btn variant="text" :disabled="locked" @click="reminders.refresh">{{ t('ratingReminders.retry') }}</v-btn>
    </div>
  </section>
</template>

<style scoped>
.rating-reminder-live { position: absolute; width: 1px; height: 1px; padding: 0; overflow: hidden; clip-path: inset(50%); }
.rating-reminder-error { font-size: var(--text-sm); color: var(--color-text-secondary); }
.reminder-fade-enter-active, .reminder-fade-leave-active { transition: opacity var(--duration-normal) var(--ease-out); }
.reminder-fade-enter-from, .reminder-fade-leave-to { opacity: 0; }
section:has(> .rating-reminder-live:only-child) { display: none; }
</style>

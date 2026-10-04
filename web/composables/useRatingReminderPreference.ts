import { computed, onScopeDispose, readonly, shallowRef, toValue, watch } from 'vue'
import type { MaybeRefOrGetter } from 'vue'
import type { RatingRemindersRepository } from '~/repository/rating-reminders-repository'

export function useRatingReminderPreference(options: { account: MaybeRefOrGetter<string>; subscribed: MaybeRefOrGetter<boolean>;
  repository: Pick<RatingRemindersRepository, 'preferences' | 'setPreference'> }) {
  const enabled = shallowRef(false)
  const state = shallowRef<'idle' | 'loading' | 'ready' | 'saving' | 'loadFailed' | 'saveFailed'>('idle')
  const active = computed(() => !!toValue(options.account) && toValue(options.subscribed) && (state.value === 'ready' || state.value === 'saveFailed'))
  let generation = 0
  let controller: AbortController | null = null

  async function refresh() {
    controller?.abort()
    const identity = ++generation
    if (!toValue(options.account)) { enabled.value = false; state.value = 'idle'; return }
    controller = new AbortController()
    state.value = 'loading'
    try {
      const result = await options.repository.preferences(controller.signal)
      if (identity !== generation) return
      enabled.value = result.pushEnabled
      state.value = 'ready'
    } catch { if (identity === generation) state.value = 'loadFailed' }
  }

  async function setEnabled(value: boolean) {
    if (!active.value) return
    const identity = generation
    state.value = 'saving'
    try {
      const result = await options.repository.setPreference(value)
      if (identity !== generation) return
      enabled.value = result.pushEnabled
      state.value = 'ready'
    } catch { if (identity === generation) state.value = 'saveFailed' }
  }

  watch(() => toValue(options.account), () => { enabled.value = false; void refresh() }, { immediate: true, flush: 'sync' })
  onScopeDispose(() => { generation++; controller?.abort() })
  return { enabled: readonly(enabled), state: readonly(state), active, refresh, setEnabled }
}

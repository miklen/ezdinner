import { getCurrentInstance, onActivated, onDeactivated, onScopeDispose, shallowRef, watch } from 'vue'
import type { Ref } from 'vue'
import { DateTime } from 'luxon'
import type { GroceryOffer } from '~/types/grocery-offers'

export function useGroceryOfferDeadlines(offers: Ref<GroceryOffer[]>) {
  const now = shallowRef(Date.now())
  let timeout: ReturnType<typeof setTimeout> | undefined
  let active = true
  function stop() { if (timeout !== undefined) clearTimeout(timeout); timeout = undefined }
  function recheck() {
    stop()
    now.value = Date.now()
    if (!active || !offers.value.length) return
    const midnight = DateTime.fromMillis(now.value, { zone: 'Europe/Copenhagen' }).plus({ days: 1 }).startOf('day').toMillis()
    const deadlines = offers.value.map(offer => Date.parse(offer.usableUntil)).filter(deadline => deadline > now.value)
    timeout = setTimeout(recheck, Math.max(1, Math.min(midnight, ...deadlines) - now.value))
  }
  function resume() { active = true; recheck() }
  function pause() { active = false; stop() }
  function visibility() { if (document.visibilityState === 'visible') recheck() }
  watch(offers, recheck, { immediate: true })
  if (typeof document !== 'undefined') document.addEventListener('visibilitychange', visibility)
  if (getCurrentInstance()) { onActivated(resume); onDeactivated(pause) }
  onScopeDispose(() => { stop(); if (typeof document !== 'undefined') document.removeEventListener('visibilitychange', visibility) })
  return { now }
}

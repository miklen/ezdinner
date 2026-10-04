import { computed, onScopeDispose, shallowRef, toValue, watch } from 'vue'
import type { MaybeRefOrGetter } from 'vue'
import type { Dish, FamilyMember } from '~/types'
import type { DishesRepository } from '~/repository/dishes-repository'
import type { FamilyRepository } from '~/repository/family-repository'
import { parseRatingReminderDestination } from '~/utils/rating-reminder-navigation'

export function useReminderDishEntry(options: {
  route: MaybeRefOrGetter<{ id: string; fullPath: string }>; account: MaybeRefOrGetter<string>;
  family: MaybeRefOrGetter<string>; selectFamily: (id: string) => void;
  dishes: Pick<DishesRepository, 'getFull'>; families: Pick<FamilyRepository, 'familySelectors' | 'get'>;
  catalog: () => void;
}) {
  const dish = shallowRef<Dish | null>(null)
  const members = shallowRef<FamilyMember[]>([])
  const loading = shallowRef(true)
  const unavailable = shallowRef(false)
  const resolving = shallowRef(false)
  const intent = computed(() => parseRatingReminderDestination(toValue(options.route).fullPath))
  const focusKey = computed(() => unavailable.value ? undefined : intent.value?.to)
  let generation = 0
  let familyAtLoad = ''

  async function load() {
    const token = ++generation
    const route = toValue(options.route)
    const entry = parseRatingReminderDestination(route.fullPath)
    const initialFamily = toValue(options.family)
    familyAtLoad = initialFamily
    const account = toValue(options.account)
    dish.value = null; members.value = []; loading.value = true; unavailable.value = false
    resolving.value = !!entry
    try {
      if (!account) return
      if (!entry && /[?&](familyId|ratingReminderDate)=|#my-rating/.test(route.fullPath)) throw new Error('INVALID_REMINDER_DESTINATION')
      if (entry) {
        const accessible = await options.families.familySelectors()
        if (token !== generation) return
        if (!accessible.some(family => family.id === entry.familyId)) throw new Error('FAMILY_UNAVAILABLE')
        if (initialFamily && toValue(options.family) !== initialFamily && toValue(options.family) !== entry.familyId) return
        options.selectFamily(entry.familyId)
      }
      const familyId = toValue(options.family)
      if (!familyId) throw new Error('FAMILY_UNAVAILABLE')
      const family = await options.families.get(familyId)
      if (token !== generation || familyId !== toValue(options.family)) return
      if (!family.familyMembers.some(member => member.id === account && member.hasAutonomy)) throw new Error('MEMBER_UNAVAILABLE')
      const result = await options.dishes.getFull(route.id, familyId)
      if (token !== generation || familyId !== toValue(options.family)) return
      dish.value = result
      members.value = family.familyMembers
    } catch { if (token === generation) unavailable.value = true }
    finally { if (token === generation) { loading.value = false; resolving.value = false } }
  }

  watch([() => toValue(options.route).fullPath, () => toValue(options.account)], () => { void load() }, { immediate: true, flush: 'sync' })
  watch(() => toValue(options.family), () => {
    if (resolving.value && intent.value && (!familyAtLoad || intent.value.familyId === toValue(options.family))) return
    generation++
    dish.value = null
    members.value = []
    loading.value = false
    options.catalog()
  }, { flush: 'sync' })
  onScopeDispose(() => { generation++ })
  return { dish, familyMembers: computed(() => members.value), loading, unavailable, focusKey, load }
}

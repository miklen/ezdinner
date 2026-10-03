import { computed, shallowRef } from 'vue'
import type { Ref } from 'vue'
import type { ExplorationPreferences, PlanningDish } from '~/types/week-planning'
import { defaultExplorationPreferences } from '~/utils/dish-exploration-preferences'

export function useDishExploration(dishes: Ref<PlanningDish[]>, locale: Ref<string>, preferences: Ref<ExplorationPreferences> = shallowRef(defaultExplorationPreferences())) {
  const visible = computed(() => {
    const choice = preferences.value
    const matching = dishes.value.filter(({ dish, wishVotes }) => !dish.isArchived &&
      dish.name.toLocaleLowerCase(locale.value).includes(choice.search.toLocaleLowerCase(locale.value)) &&
      (choice.role === 'All' || dish.roles?.includes(choice.role) || (choice.role === 'Main' && !dish.roles?.length)) &&
      (choice.effort === 'All' || dish.effortLevel === choice.effort) &&
      (choice.season === 'All' || dish.seasonAffinity === choice.season) &&
      (!choice.cuisine || dish.cuisine === choice.cuisine) &&
      (!choice.wishesOnly || wishVotes !== undefined))
    return matching.sort((first, second) => {
      if (choice.sort === 'rating') return second.dish.rating - first.dish.rating || first.dish.name.localeCompare(second.dish.name, locale.value)
      if (choice.sort === 'usage') return (second.stats?.timesUsed ?? 0) - (first.stats?.timesUsed ?? 0) || first.dish.name.localeCompare(second.dish.name, locale.value)
      if (choice.sort === 'lastUsed') return (first.stats?.lastUsed?.toMillis() ?? -Infinity) - (second.stats?.lastUsed?.toMillis() ?? -Infinity) || first.dish.name.localeCompare(second.dish.name, locale.value)
      return first.dish.name.localeCompare(second.dish.name, locale.value)
    })
  })
  function update(next: ExplorationPreferences) { preferences.value = next }
  function reset() { preferences.value = defaultExplorationPreferences() }
  let position = 0
  let trigger: HTMLElement | null = null
  function rememberPosition(element?: HTMLElement) {
    position = window.scrollY
    trigger = element ?? (document.activeElement instanceof HTMLElement ? document.activeElement : null)
  }
  function restorePosition(restoreScroll = true) {
    if (trigger?.isConnected) trigger.focus({ preventScroll: true })
    if (restoreScroll) window.scrollTo({ top: position, behavior: 'instant' })
  }
  return { preferences, visible, update, reset, rememberPosition, restorePosition }
}

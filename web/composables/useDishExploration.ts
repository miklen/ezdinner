import { computed, shallowRef } from 'vue'
import type { Ref } from 'vue'
import type { ExplorationPreferences, PlanningDish } from '~/types/week-planning'
import { defaultExplorationPreferences } from '~/utils/dish-exploration-preferences'
import { sortDishes } from '~/utils/dish-sorting'

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
    return sortDishes(matching, choice.sort === 'usage' ? 'timesUsed' : choice.sort, choice.sortDir, locale.value)
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

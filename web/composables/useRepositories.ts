import { DishesRepository } from '~/repository/dishes-repository'
import { DinnerRepository } from '~/repository/dinner-repository'
import { FamilyRepository } from '~/repository/family-repository'

/**
 * Provides typed repository instances pre-wired with auth.
 * Replaces the old repository-plugin.ts + $repositories injection.
 *
 * Usage: const { dishes, dinners, families } = useRepositories()
 */
export function useRepositories() {
  const { apiFetch, apiFetchRaw } = useApiFetch()

  return {
    dishes: new DishesRepository(apiFetch),
    dinners: new DinnerRepository(apiFetch),
    families: new FamilyRepository(apiFetch, apiFetchRaw),
  }
}

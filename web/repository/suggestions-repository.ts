import type { DaySuggestion, DishSuggestion } from '~/types'

type ApiFetch = <T>(path: string, options?: Parameters<typeof $fetch>[1]) => Promise<T>

export class SuggestionsRepository {
  constructor(private apiFetch: ApiFetch) {}

  suggestDay(familyId: string, date: string, excludedDishIds?: string[]) {
    const exclude = excludedDishIds?.join(',')
    const query = exclude ? `?exclude=${exclude}` : ''
    return this.apiFetch<DishSuggestion | null>(
      `/api/suggest/family/${familyId}/day/${date}${query}`,
    )
  }

  suggestWeek(familyId: string, weekStart: string, excludedDishIds?: string[]) {
    const exclude = excludedDishIds?.join(',')
    const query = exclude ? `?exclude=${exclude}` : ''
    return this.apiFetch<DaySuggestion[]>(
      `/api/suggest/family/${familyId}/week/${weekStart}${query}`,
    )
  }
}

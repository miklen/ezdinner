import type { DaySuggestion, DishSuggestion, EffortLevel } from '~/types'

type ApiFetch = <T>(path: string, options?: Parameters<typeof $fetch>[1]) => Promise<T>

export class SuggestionsRepository {
  constructor(private apiFetch: ApiFetch) {}

  suggestDay(familyId: string, date: string, excludedDishIds?: string[], effortPreference?: EffortLevel | null) {
    const params: string[] = []
    const exclude = excludedDishIds?.join(',')
    if (exclude) params.push(`exclude=${exclude}`)
    if (effortPreference) params.push(`effortPreference=${effortPreference}`)
    const query = params.length > 0 ? `?${params.join('&')}` : ''
    return this.apiFetch<DishSuggestion | null>(
      `/api/suggest/family/${familyId}/day/${date}${query}`,
    )
  }

  suggestWeek(familyId: string, weekStart: string, excludedDishIds?: string[], effortPreferences?: Record<string, EffortLevel>) {
    const params: string[] = []
    const exclude = excludedDishIds?.join(',')
    if (exclude) params.push(`exclude=${exclude}`)
    if (effortPreferences) {
      for (const [date, level] of Object.entries(effortPreferences)) {
        params.push(`effortPref=${date}:${level}`)
      }
    }
    const query = params.length > 0 ? `?${params.join('&')}` : ''
    return this.apiFetch<DaySuggestion[]>(
      `/api/suggest/family/${familyId}/week/${weekStart}${query}`,
    )
  }
}

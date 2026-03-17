import type { DaySuggestion } from '~/types'

export function useDinnerSuggestions() {
  const appStore = useAppStore()
  const suggestions = ref<DaySuggestion[]>([])
  const loading = ref(false)
  const exhausted = ref(false)

  // Per-day exclusion lists — grows with each reroll so we cycle through dishes
  const excludedByDate = ref<Record<string, string[]>>({})

  // Global week-level exclusion list (for rerollWeek)
  const weekExcluded = ref<string[]>([])

  async function suggestWeek(weekStart: string) {
    loading.value = true
    exhausted.value = false
    excludedByDate.value = {}
    weekExcluded.value = []
    try {
      const { suggestions: repo } = useRepositories()
      suggestions.value = await repo.suggestWeek(appStore.activeFamilyId, weekStart)
      weekExcluded.value = suggestions.value
        .map((s) => s.suggestion?.dishId)
        .filter((id): id is string => !!id)
    } finally {
      loading.value = false
    }
  }

  async function suggestDay(date: string) {
    loading.value = true
    try {
      const { suggestions: repo } = useRepositories()
      const result = await repo.suggestDay(appStore.activeFamilyId, date, excludedByDate.value[date])
      const idx = suggestions.value.findIndex((s) => s.date === date)
      const entry: DaySuggestion = { date, suggestion: result }
      if (idx >= 0) {
        suggestions.value = suggestions.value.map((s) => (s.date === date ? entry : s))
      } else {
        suggestions.value = [...suggestions.value, entry]
      }
    } finally {
      loading.value = false
    }
  }

  async function rerollWeek(weekStart: string) {
    loading.value = true
    try {
      const { suggestions: repo } = useRepositories()
      const newSuggestions = await repo.suggestWeek(
        appStore.activeFamilyId,
        weekStart,
        weekExcluded.value,
      )
      // Accumulate new suggestions into exclusion list for next reroll
      const newIds = newSuggestions
        .map((s) => s.suggestion?.dishId)
        .filter((id): id is string => !!id)
      // Exhausted when every returned dish was already in the exclusion list (backend fell back)
      const prevExcluded = new Set(weekExcluded.value)
      exhausted.value = newIds.length > 0 && newIds.every((id) => prevExcluded.has(id))
      weekExcluded.value = [...weekExcluded.value, ...newIds]
      suggestions.value = newSuggestions
    } finally {
      loading.value = false
    }
  }

  async function rerollDay(date: string) {
    const currentSuggestion = suggestions.value.find((s) => s.date === date)?.suggestion
    if (currentSuggestion) {
      excludedByDate.value = {
        ...excludedByDate.value,
        [date]: [...(excludedByDate.value[date] ?? []), currentSuggestion.dishId],
      }
    }
    await suggestDay(date)
  }

  function clearSuggestionForDate(date: string) {
    suggestions.value = suggestions.value.filter((s) => s.date !== date)
  }

  function reset() {
    suggestions.value = []
    exhausted.value = false
    excludedByDate.value = {}
    weekExcluded.value = []
  }

  return {
    suggestions,
    loading,
    exhausted,
    suggestWeek,
    suggestDay,
    rerollWeek,
    rerollDay,
    clearSuggestionForDate,
    reset,
  }
}

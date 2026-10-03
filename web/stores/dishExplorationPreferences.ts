import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { ExplorationPreferences } from '~/types/week-planning'
import { defaultExplorationPreferences } from '~/utils/dish-exploration-preferences'

// Keep weekly-explorer controls across route remounts, separately for each identity.
export const useDishExplorationPreferencesStore = defineStore('dishExplorationPreferences', () => {
  const entries = ref<Record<string, ExplorationPreferences>>({})
  function restore(scope: string) {
    if (!entries.value[scope]) entries.value[scope] = defaultExplorationPreferences()
  }
  function update(scope: string, preferences: ExplorationPreferences) {
    entries.value[scope] = preferences
  }
  return { entries, restore, update }
})

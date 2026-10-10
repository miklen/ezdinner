import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { DishCatalogPreferences } from '~/utils/dish-catalog-preferences'

// Preferences survive route unmounts; dish data remains owned by the catalog page.
export const useDishCatalogPreferencesStore = defineStore('dishCatalogPreferences', () => {
  const entries = ref<Record<string, DishCatalogPreferences>>({})

  function replace(key: string, preferences: DishCatalogPreferences) {
    entries.value[key] = preferences
  }

  return { entries, replace }
})

import { computed, shallowRef, watch } from 'vue'
import type { Ref } from 'vue'
import { useDishCatalogPreferencesStore } from '~/stores/dishCatalogPreferences'
import { catalogPreferencesKey, decodeCatalogPreferences, defaultCatalogPreferences } from '~/utils/dish-catalog-preferences'
import type { CatalogFilterDimension, CatalogSortKey, DishCatalogPreferences } from '~/utils/dish-catalog-preferences'
import { nextDishSortDirection } from '~/utils/dish-sorting'

type PreferenceStorage = Pick<Storage, 'getItem' | 'setItem'>

function browserStorage(): PreferenceStorage | null {
  try { return typeof window === 'undefined' ? null : window.localStorage }
  catch { return null }
}

export function useDishCatalogPreferences(account: Ref<string>, family: Ref<string>, storage = browserStorage()) {
  const store = useDishCatalogPreferencesStore()
  const scope = computed(() => catalogPreferencesKey(account.value, family.value))
  const anonymous = shallowRef(defaultCatalogPreferences())

  // Browser-local v1 records store only controls, never dishes. Restore synchronously
  // before consumers fetch or write; in-memory entries also work if storage is blocked.
  watch(scope, key => {
    anonymous.value = defaultCatalogPreferences()
    if (!key || store.entries[key]) return
    let saved: string | null = null
    try { saved = storage?.getItem(key) ?? null }
    catch { /* Storage may be unavailable; controls still work in memory. */ }
    store.replace(key, decodeCatalogPreferences(saved))
  }, { immediate: true, flush: 'sync' })

  const preferences = computed(() => scope.value ? store.entries[scope.value] ?? defaultCatalogPreferences() : anonymous.value)
  watch(() => [scope.value, preferences.value] as const, ([key, value]) => {
    if (!key) return
    try { storage?.setItem(key, JSON.stringify({ version: 1, ...value })) }
    catch { /* Keep the in-memory entry usable when saving fails. */ }
  }, { flush: 'sync' })

  function update(changes: Partial<DishCatalogPreferences>) {
    const value = { ...preferences.value, ...changes }
    if (scope.value) store.replace(scope.value, value)
    else anonymous.value = value
  }

  function toggleFilter(dimension: CatalogFilterDimension, value: string) {
    const key = dimension === 'cuisine' ? value.toLowerCase() : value
    const selected = preferences.value[dimension]
    update({ [dimension]: selected.includes(key) ? selected.filter(item => item !== key) : [...selected, key] })
  }

  function selectSort(key: CatalogSortKey) {
    const current = preferences.value
    update({ sortKey: key, sortDir: nextDishSortDirection(current.sortKey, current.sortDir, key) })
  }

  function clearFilters() {
    update({ roles: [], effort: [], season: [], cuisine: [], showArchived: false })
  }

  function reconcile(options: Record<CatalogFilterDimension, string[]>) {
    const changes: Partial<DishCatalogPreferences> = {}
    for (const dimension of ['roles', 'effort', 'season', 'cuisine'] as const) {
      const selected = preferences.value[dimension]
      const valid = selected.filter(value => options[dimension].includes(value))
      if (valid.length !== selected.length) changes[dimension] = valid
    }
    if (Object.keys(changes).length) update(changes)
  }

  return { scope, preferences, update, toggleFilter, selectSort, clearFilters, reconcile }
}

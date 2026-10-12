import type { DishSortDirection, DishSortKey } from './dish-sorting'

export type CatalogSortKey = DishSortKey
export type CatalogSortDirection = DishSortDirection
export type CatalogFilterDimension = 'roles' | 'effort' | 'season' | 'cuisine'

export interface DishCatalogPreferences {
  search: string
  roles: string[]
  effort: string[]
  season: string[]
  cuisine: string[]
  showArchived: boolean
  sortKey: CatalogSortKey
  sortDir: CatalogSortDirection
}

export function defaultCatalogPreferences(): DishCatalogPreferences {
  return { search: '', roles: [], effort: [], season: [], cuisine: [], showArchived: false, sortKey: 'name', sortDir: 'asc' }
}

function selections(value: unknown, lowercase = false): string[] {
  if (!Array.isArray(value) || !value.every(item => typeof item === 'string')) return []
  return [...new Set(value.map(item => lowercase ? item.toLowerCase() : item))]
}

export function decodeCatalogPreferences(raw: string | null): DishCatalogPreferences {
  const defaults = defaultCatalogPreferences()
  try {
    const value: unknown = JSON.parse(raw ?? 'null')
    if (!value || typeof value !== 'object' || !('version' in value) || value.version !== 1) return defaults
    const record = value as Record<string, unknown>
    return {
      search: typeof record.search === 'string' ? record.search : '',
      roles: selections(record.roles),
      effort: selections(record.effort),
      season: selections(record.season),
      cuisine: selections(record.cuisine, true),
      showArchived: typeof record.showArchived === 'boolean' ? record.showArchived : false,
      sortKey: record.sortKey === 'rating' || record.sortKey === 'timesUsed' || record.sortKey === 'lastUsed' ? record.sortKey : 'name',
      sortDir: record.sortDir === 'desc' ? 'desc' : 'asc',
    }
  }
  catch { return defaults }
}

export function catalogPreferencesKey(account: string, family: string): string | null {
  return account && family ? `ezdinner:dishes-catalog:v1:${encodeURIComponent(account)}:${encodeURIComponent(family)}` : null
}

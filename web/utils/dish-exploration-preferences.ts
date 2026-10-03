import type { ExplorationPreferences } from '~/types/week-planning'

export function defaultExplorationPreferences(): ExplorationPreferences {
  return { search: '', role: 'Main', effort: 'All', season: 'All', cuisine: '', wishesOnly: false, sort: 'name' }
}

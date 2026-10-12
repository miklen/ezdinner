import type { Dish, DishStats } from '~/types'

export type DishSortKey = 'name' | 'rating' | 'timesUsed' | 'lastUsed'
export type DishSortDirection = 'asc' | 'desc'

interface DishSortEntry {
  dish: Pick<Dish, 'name' | 'rating'>
  stats?: Pick<DishStats, 'timesUsed' | 'lastUsed'>
}

export function nextDishSortDirection(currentKey: string, currentDirection: DishSortDirection, selectedKey: string): DishSortDirection {
  if (currentKey === selectedKey) return currentDirection === 'asc' ? 'desc' : 'asc'
  return selectedKey === 'name' ? 'asc' : 'desc'
}

export function sortDishes<Entry extends DishSortEntry>(entries: readonly Entry[], key: DishSortKey, direction: DishSortDirection, locale: string): Entry[] {
  const multiplier = direction === 'asc' ? 1 : -1
  return [...entries].sort((first, second) => {
    const alphabetical = first.dish.name.localeCompare(second.dish.name, locale)
    if (key === 'name') return multiplier * alphabetical
    if (key === 'rating') return multiplier * (first.dish.rating - second.dish.rating) || alphabetical
    if (key === 'timesUsed') return multiplier * ((first.stats?.timesUsed ?? 0) - (second.stats?.timesUsed ?? 0)) || alphabetical
    const firstUsed = first.stats?.lastUsed?.toMillis()
    const secondUsed = second.stats?.lastUsed?.toMillis()
    if (firstUsed === undefined) return secondUsed === undefined ? alphabetical : 1
    if (secondUsed === undefined) return -1
    return multiplier * (firstUsed - secondUsed) || alphabetical
  })
}

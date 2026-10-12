import type { DishRole, EffortLevel, SeasonAffinity } from '~/types'
import type { DishSortKey } from '~/utils/dish-sorting'

export interface CatalogSortOption { key: DishSortKey; label: string }

interface FilterPresentation { label: string; selected: boolean; disabled?: boolean }

export type CatalogFilterOption = FilterPresentation & (
  | { dimension: 'roles'; value: DishRole | 'All' }
  | { dimension: 'effort'; value: EffortLevel | 'All' }
  | { dimension: 'season'; value: SeasonAffinity | 'All' }
  | { dimension: 'cuisine'; value: string }
  | { dimension: 'archive' | 'wishes'; value: string }
)

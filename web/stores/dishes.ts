import type { Dish, DishStats } from '~/types'

export const useDishesStore = defineStore('dishes', () => {
  const appStore = useAppStore()
  const dishes = ref<Dish[]>([])
  let updating: Promise<void> | null = null

  const dishMap = computed<Record<string, string>>(() =>
    dishes.value.reduce((acc, dish) => {
      acc[dish.id] = dish.name
      return acc
    }, {} as Record<string, string>),
  )

  async function populateDishes() {
    if (updating) return updating
    const { dishes: dishRepo } = useRepositories()
    updating = dishRepo.all(appStore.activeFamilyId).then((result) => {
      // Preserve any client-side dishStats that were previously applied via populateStats(),
      // since those come from a separate API call and are not included in the dishes list response.
      const statsMap = new Map(dishes.value.filter(d => d.dishStats).map(d => [d.id, d.dishStats]))
      dishes.value = result.map(d => statsMap.has(d.id) ? { ...d, dishStats: statsMap.get(d.id)! } : d)
      updating = null
    })
    return updating
  }

  async function populateStats() {
    const { dishes: dishRepo } = useRepositories()
    const stats = await dishRepo.allUsageStats(appStore.activeFamilyId)
    applyStats(stats)
  }

  function applyStats(stats: Record<string, DishStats>) {
    dishes.value = dishes.value.map((dish) =>
      stats[dish.id] ? { ...dish, dishStats: stats[dish.id] } : dish,
    )
  }

  async function updateDish(dishId: string) {
    const { dishes: dishRepo } = useRepositories()
    const dish = await dishRepo.get(dishId)
    const index = dishes.value.findIndex((d) => d.id === dish.id)
    if (index === -1) dishes.value.push(dish)
    else dishes.value[index] = dish
  }

  return { dishes, dishMap, populateDishes, populateStats, applyStats, updateDish }
})

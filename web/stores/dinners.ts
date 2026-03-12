import { DateTime } from 'luxon'
import type { Dinner } from '~/types'

export const useDinnersStore = defineStore('dinners', () => {
  const appStore = useAppStore()
  const dishesStore = useDishesStore()
  const dinners = ref<Dinner[]>([])

  async function populateDinners(from: DateTime, to: DateTime) {
    const { dinners: dinnerRepo } = useRepositories()
    const result = await dinnerRepo.getRange(appStore.activeFamilyId, from, to)

    // Client-side join: attach dish names from dishMap
    dinners.value = (result as any[]).map((dinner) => {
      dinner.date = DateTime.fromISO(dinner.date)
      dinner.menu = dinner.menu.map((item: any) => {
        item.dishName = dishesStore.dishMap[item.dishId] ?? 'Dish not available'
        return item
      })
      return dinner as Dinner
    })
  }

  return { dinners, populateDinners }
})

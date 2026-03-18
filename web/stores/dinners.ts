import { DateTime } from 'luxon'
import type { Dinner } from '~/types'

export const useDinnersStore = defineStore('dinners', () => {
  const appStore = useAppStore()
  const dishesStore = useDishesStore()
  const dinners = ref<Dinner[]>([])
  const previousOptOutReasons = ref<string[]>([])

  async function populateDinners(from: DateTime, to: DateTime) {
    const { dinners: dinnerRepo } = useRepositories()
    const [result] = await Promise.all([
      dinnerRepo.getRange(appStore.activeFamilyId, from, to),
      dishesStore.populateDishes(),
    ])

    type RawDinner = Omit<Dinner, 'date'> & { date: string }
    // Client-side join: attach dish names from dishMap
    dinners.value = (result as unknown as RawDinner[]).map((dinner) => ({
      ...dinner,
      date: DateTime.fromISO(dinner.date),
      menu: dinner.menu.map((item) => ({
        ...item,
        dishName: dishesStore.dishMap[item.dishId] ?? 'Dish not available',
      })),
    }))
  }

  async function fetchOptOutReasons() {
    const { dinners: dinnerRepo } = useRepositories()
    previousOptOutReasons.value = await dinnerRepo.getOptOutReasons(appStore.activeFamilyId)
  }

  return { dinners, previousOptOutReasons, populateDinners, fetchOptOutReasons }
})

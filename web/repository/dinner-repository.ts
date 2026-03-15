import type { DateTime } from 'luxon'
import type { Dinner } from '~/types'

type ApiFetch = <T>(path: string, options?: Parameters<typeof $fetch>[1]) => Promise<T>

export class DinnerRepository {
  constructor(private apiFetch: ApiFetch) {}

  getRange(familyId: string, from: DateTime, to: DateTime) {
    return this.apiFetch<Dinner[]>(
      `/api/dinners/family/${familyId}/dates/${from.toISODate()}/${to.toISODate()}`,
    )
  }

  get(familyId: string, exactDate: DateTime) {
    return this.apiFetch<Dinner>(`/api/dinners/family/${familyId}/date/${exactDate.toISODate()}`)
  }

  addDishToMenu(familyId: string, date: DateTime, dishId: string) {
    return this.apiFetch('/api/dinners/menuitem', {
      method: 'PUT',
      body: { date: date.toISODate(), dishId, familyId },
    })
  }

  removeDishFromMenu(familyId: string, date: DateTime, dishId: string) {
    return this.apiFetch('/api/dinners/menuitem/remove', {
      method: 'PUT',
      body: { date: date.toISODate(), dishId, familyId },
    })
  }

  moveDinnerDishes(familyId: string, dishId: string, newDishId: string) {
    return this.apiFetch('/api/dinners/menuitem/replace', {
      method: 'PUT',
      body: { familyId, dishId, newDishId },
    })
  }

  setOptOut(familyId: string, date: DateTime, reason: string) {
    return this.apiFetch('/api/dinners/optout', {
      method: 'PUT',
      body: { familyId, date: date.toFormat('yyyy-MM-dd'), reason },
    })
  }

  removeOptOut(familyId: string, date: DateTime) {
    return this.apiFetch('/api/dinners/optout/remove', {
      method: 'PUT',
      body: { familyId, date: date.toFormat('yyyy-MM-dd') },
    })
  }
}

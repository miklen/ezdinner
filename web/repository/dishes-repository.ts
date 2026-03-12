import { DateTime } from 'luxon'
import type { Dish, DishStats } from '~/types'

type ApiFetch = <T>(path: string, options?: Parameters<typeof $fetch>[1]) => Promise<T>

export class DishesRepository {
  constructor(private apiFetch: ApiFetch) {}

  all(familyId: string) {
    return this.apiFetch<Dish[]>(`/api/dishes/family/${familyId}`)
  }

  get(dishId: string) {
    return this.apiFetch<Dish>(`/api/dishes/${dishId}`)
  }

  async getFull(dishId: string, familyId: string) {
    const result = await this.apiFetch<Dish>(`/api/dishes/${dishId}/full/family/${familyId}`)
    if (result.dishStats?.lastUsed) {
      result.dishStats.lastUsed = DateTime.fromISO(result.dishStats.lastUsed as unknown as string)
    }
    return result
  }

  create(familyId: string, dishName: string) {
    return this.apiFetch('/api/dishes', { method: 'POST', body: { name: dishName, familyId } })
  }

  delete(familyId: string, dishId: string) {
    return this.apiFetch(`/api/dishes/family/${familyId}/id/${dishId}`, { method: 'DELETE' })
  }

  async allUsageStats(familyId: string): Promise<Record<string, DishStats>> {
    const result = await this.apiFetch<Record<string, DishStats>>(`/api/dishes/stats/family/${familyId}`)
    for (const key of Object.keys(result)) {
      if (result[key].lastUsed) {
        result[key].lastUsed = DateTime.fromISO(result[key].lastUsed as unknown as string)
      }
    }
    return result
  }

  updateName(dishId: string, newName: string) {
    return this.apiFetch(`/api/dishes/${dishId}/name/`, { method: 'PUT', body: { name: newName } })
  }

  updateRating(dishId: string, newRating: number, familyMemberId: string) {
    return this.apiFetch(`/api/dishes/${dishId}/rating`, { method: 'PUT', body: { rating: newRating, familyMemberId } })
  }

  updateNotes(dishId: string, notes: string, url: string) {
    return this.apiFetch(`/api/dishes/${dishId}/notes`, { method: 'PUT', body: { notes, url } })
  }
}

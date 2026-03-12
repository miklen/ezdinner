import { describe, it, expect, vi } from 'vitest'
import { DateTime } from 'luxon'
import { DishesRepository } from '~/repository/dishes-repository'

function makeFetch(returnValue: unknown) {
  return vi.fn().mockResolvedValue(returnValue)
}

describe('DishesRepository', () => {
  describe('getFull', () => {
    it('parses lastUsed string into a Luxon DateTime', async () => {
      const isoDate = '2025-06-15T00:00:00.000Z'
      const apiFetch = makeFetch({
        id: 'dish-1',
        name: 'Pasta',
        url: '',
        notes: '',
        rating: 4,
        tags: [],
        dates: [],
        ratings: [],
        dishStats: { dishId: 'dish-1', lastUsed: isoDate, timesUsed: 3 },
      })
      const repo = new DishesRepository(apiFetch)
      const dish = await repo.getFull('dish-1', 'family-1')

      expect(dish.dishStats.lastUsed).toBeInstanceOf(DateTime)
      expect((dish.dishStats.lastUsed as DateTime).toISODate()).toBe('2025-06-15')
    })

    it('leaves lastUsed undefined when not present', async () => {
      const apiFetch = makeFetch({
        id: 'dish-2',
        name: 'Pizza',
        url: '',
        notes: '',
        rating: 3,
        tags: [],
        dates: [],
        ratings: [],
        dishStats: { dishId: 'dish-2', lastUsed: undefined, timesUsed: 0 },
      })
      const repo = new DishesRepository(apiFetch)
      const dish = await repo.getFull('dish-2', 'family-1')

      expect(dish.dishStats.lastUsed).toBeUndefined()
    })

    it('calls the correct endpoint', async () => {
      const apiFetch = makeFetch({ dishStats: {} })
      const repo = new DishesRepository(apiFetch)
      await repo.getFull('dish-3', 'family-99')

      expect(apiFetch).toHaveBeenCalledWith('/api/dishes/dish-3/full/family/family-99')
    })
  })

  describe('allUsageStats', () => {
    it('converts lastUsed strings to DateTime instances for each entry', async () => {
      const apiFetch = makeFetch({
        'dish-a': { dishId: 'dish-a', lastUsed: '2024-12-01', timesUsed: 5 },
        'dish-b': { dishId: 'dish-b', lastUsed: '2025-01-20', timesUsed: 2 },
        'dish-c': { dishId: 'dish-c', lastUsed: undefined, timesUsed: 0 },
      })
      const repo = new DishesRepository(apiFetch)
      const stats = await repo.allUsageStats('family-1')

      expect(stats['dish-a'].lastUsed).toBeInstanceOf(DateTime)
      expect((stats['dish-a'].lastUsed as DateTime).toISODate()).toBe('2024-12-01')
      expect(stats['dish-b'].lastUsed).toBeInstanceOf(DateTime)
      expect(stats['dish-c'].lastUsed).toBeUndefined()
    })

    it('calls the correct endpoint', async () => {
      const apiFetch = makeFetch({})
      const repo = new DishesRepository(apiFetch)
      await repo.allUsageStats('family-42')

      expect(apiFetch).toHaveBeenCalledWith('/api/dishes/stats/family/family-42')
    })
  })

  describe('updateRating', () => {
    it('sends the correct body and method', async () => {
      const apiFetch = makeFetch(undefined)
      const repo = new DishesRepository(apiFetch)
      await repo.updateRating('dish-1', 4.5, 'member-1')

      expect(apiFetch).toHaveBeenCalledWith('/api/dishes/dish-1/rating', {
        method: 'PUT',
        body: { rating: 4.5, familyMemberId: 'member-1' },
      })
    })
  })

  describe('updateNotes', () => {
    it('sends notes and url in body', async () => {
      const apiFetch = makeFetch(undefined)
      const repo = new DishesRepository(apiFetch)
      await repo.updateNotes('dish-1', '## Recipe', 'https://example.com')

      expect(apiFetch).toHaveBeenCalledWith('/api/dishes/dish-1/notes', {
        method: 'PUT',
        body: { notes: '## Recipe', url: 'https://example.com' },
      })
    })
  })

  describe('create', () => {
    it('posts to the dishes endpoint with name and familyId', async () => {
      const apiFetch = makeFetch('new-dish-id')
      const repo = new DishesRepository(apiFetch)
      await repo.create('family-1', 'Tacos')

      expect(apiFetch).toHaveBeenCalledWith('/api/dishes', {
        method: 'POST',
        body: { name: 'Tacos', familyId: 'family-1' },
      })
    })
  })

  describe('delete', () => {
    it('sends a DELETE request to the correct endpoint', async () => {
      const apiFetch = makeFetch(undefined)
      const repo = new DishesRepository(apiFetch)
      await repo.delete('family-1', 'dish-9')

      expect(apiFetch).toHaveBeenCalledWith('/api/dishes/family/family-1/id/dish-9', {
        method: 'DELETE',
      })
    })
  })
})

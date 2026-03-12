import { describe, it, expect, vi } from 'vitest'
import { DateTime } from 'luxon'
import { DinnerRepository } from '~/repository/dinner-repository'

function makeFetch(returnValue: unknown) {
  return vi.fn().mockResolvedValue(returnValue)
}

const FROM = DateTime.fromISO('2025-03-01')
const TO = DateTime.fromISO('2025-04-01')

describe('DinnerRepository', () => {
  describe('getRange', () => {
    it('calls the correct endpoint with ISO date strings', async () => {
      const apiFetch = makeFetch([])
      const repo = new DinnerRepository(apiFetch)
      await repo.getRange('family-1', FROM, TO)

      expect(apiFetch).toHaveBeenCalledWith(
        '/api/dinners/family/family-1/dates/2025-03-01/2025-04-01',
      )
    })
  })

  describe('get', () => {
    it('calls the correct single-date endpoint', async () => {
      const apiFetch = makeFetch({})
      const repo = new DinnerRepository(apiFetch)
      const date = DateTime.fromISO('2025-03-15')
      await repo.get('family-1', date)

      expect(apiFetch).toHaveBeenCalledWith('/api/dinners/family/family-1/date/2025-03-15')
    })
  })

  describe('addDishToMenu', () => {
    it('sends PUT with correct body', async () => {
      const apiFetch = makeFetch(undefined)
      const repo = new DinnerRepository(apiFetch)
      const date = DateTime.fromISO('2025-03-10')
      await repo.addDishToMenu('family-1', date, 'dish-42')

      expect(apiFetch).toHaveBeenCalledWith('/api/dinners/menuitem', {
        method: 'PUT',
        body: { date: '2025-03-10', dishId: 'dish-42', familyId: 'family-1' },
      })
    })
  })

  describe('removeDishFromMenu', () => {
    it('sends PUT to remove endpoint with correct body', async () => {
      const apiFetch = makeFetch(undefined)
      const repo = new DinnerRepository(apiFetch)
      const date = DateTime.fromISO('2025-03-10')
      await repo.removeDishFromMenu('family-1', date, 'dish-42')

      expect(apiFetch).toHaveBeenCalledWith('/api/dinners/menuitem/remove', {
        method: 'PUT',
        body: { date: '2025-03-10', dishId: 'dish-42', familyId: 'family-1' },
      })
    })
  })

  describe('moveDinnerDishes', () => {
    it('sends PUT to replace endpoint with correct body', async () => {
      const apiFetch = makeFetch(undefined)
      const repo = new DinnerRepository(apiFetch)
      await repo.moveDinnerDishes('family-1', 'old-dish', 'new-dish')

      expect(apiFetch).toHaveBeenCalledWith('/api/dinners/menuitem/replace', {
        method: 'PUT',
        body: { familyId: 'family-1', dishId: 'old-dish', newDishId: 'new-dish' },
      })
    })
  })
})

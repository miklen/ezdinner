import { describe, expect, it, vi } from 'vitest'
import { RatingRemindersRepository } from '~/repository/rating-reminders-repository'
import { DishesRepository } from '~/repository/dishes-repository'

const dishId = '00000001-0000-0000-0000-000000000000'
describe('reminder repository contracts', () => {
  it('uses exact query, dismissal and preference routes', async () => {
    const api = vi.fn().mockResolvedValue({ today: '2026-10-12', reminders: [{ dishId, dishName: 'Lasagne', dinnerDate: '2026-10-11' }] })
    const repository = new RatingRemindersRepository(api)
    expect((await repository.get('family')).reminders[0].dishId).toBe(dishId)
    expect(api).toHaveBeenLastCalledWith('/api/families/family/rating-reminders', { signal: undefined })
    await repository.dismiss('family', dishId, '2026-10-11')
    expect(api).toHaveBeenLastCalledWith(`/api/families/family/rating-reminders/${dishId}/dismiss`, { method: 'PUT', body: { dinnerDate: '2026-10-11' } })
    api.mockResolvedValue({ pushEnabled: false })
    expect(await repository.preferences()).toEqual({ pushEnabled: false })
    api.mockResolvedValue({ pushEnabled: true })
    expect(await repository.setPreference(true)).toEqual({ pushEnabled: true })
    expect(api).toHaveBeenLastCalledWith('/api/rating-reminders/preferences', { method: 'PUT', body: { pushEnabled: true } })
  })

  it.each([{ today: '2026-02-30', reminders: [] }, { today: '2026-10-12', reminders: [{ dishId: 'bad', dishName: 'Dish', dinnerDate: '2026-10-11' }] }])('rejects malformed query responses', async (response) => {
    const repository = new RatingRemindersRepository(vi.fn().mockResolvedValue(response))
    await expect(repository.get('family')).rejects.toThrow()
  })

  it('rejects invalid preferences and dates', async () => {
    const repository = new RatingRemindersRepository(vi.fn().mockResolvedValue({ pushEnabled: 'yes' }))
    await expect(repository.preferences()).rejects.toThrow()
    expect(() => repository.dismiss('family', dishId, '2026-02-30')).toThrow()
  })

  it('preserves the existing personal rating contract', async () => {
    const api = vi.fn().mockResolvedValue(undefined)
    await new DishesRepository(api).updateRating(dishId, 3.5, 'account')
    expect(api).toHaveBeenCalledWith(`/api/dishes/${dishId}/rating`, { method: 'PUT', body: { rating: 3.5, familyMemberId: 'account' } })
  })
})

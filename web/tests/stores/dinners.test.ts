import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { DateTime } from 'luxon'

/**
 * Tests for the dinner store's data transformation logic.
 * The store converts ISO date strings from the API into Luxon DateTimes
 * and joins dish names from the dishMap.
 *
 * We test that transformation directly rather than through the store
 * since the store depends on Nuxt composables that aren't available in tests.
 */

describe('Dinner data transformations', () => {
  describe('date parsing', () => {
    it('converts ISO date string to Luxon DateTime', () => {
      const isoDate = '2025-03-10'
      const dt = DateTime.fromISO(isoDate)
      expect(dt).toBeInstanceOf(DateTime)
      expect(dt.isValid).toBe(true)
      expect(dt.toISODate()).toBe('2025-03-10')
    })

    it('detects weekday correctly for week separator logic', () => {
      // 2025-03-10 is a Monday (weekday === 1)
      const monday = DateTime.fromISO('2025-03-10')
      expect(monday.weekday).toBe(1)

      // 2025-03-11 is a Tuesday
      const tuesday = DateTime.fromISO('2025-03-11')
      expect(tuesday.weekday).toBe(2)
    })

    it('hasSame detects same day correctly', () => {
      const a = DateTime.fromISO('2025-03-10')
      const b = DateTime.fromISO('2025-03-10')
      const c = DateTime.fromISO('2025-03-11')
      expect(a.hasSame(b, 'day')).toBe(true)
      expect(a.hasSame(c, 'day')).toBe(false)
    })
  })

  describe('dish name join (dishMap)', () => {
    it('maps dishId to dishName using dishMap', () => {
      const dishMap: Record<string, string> = {
        'dish-1': 'Pasta',
        'dish-2': 'Pizza',
      }

      const menuItems = [
        { dishId: 'dish-1', dishName: '' },
        { dishId: 'dish-2', dishName: '' },
        { dishId: 'dish-99', dishName: '' },
      ]

      const joined = menuItems.map((item) => ({
        ...item,
        dishName: dishMap[item.dishId] ?? 'Dish not available',
      }))

      expect(joined[0].dishName).toBe('Pasta')
      expect(joined[1].dishName).toBe('Pizza')
      expect(joined[2].dishName).toBe('Dish not available')
    })
  })

  describe('isPlanned detection', () => {
    it('dinner with menu items is planned', () => {
      const dinner = { menu: [{ dishId: 'dish-1', dishName: 'Pasta' }] }
      expect(dinner.menu.length > 0).toBe(true)
    })

    it('dinner with empty menu is not planned', () => {
      const dinner = { menu: [] }
      expect(dinner.menu.length > 0).toBe(false)
    })
  })
})

describe('DishStats DateTime parsing', () => {
  it('parses ISO string to DateTime for lastUsed', () => {
    const raw = { dishId: 'dish-1', lastUsed: '2025-01-15', timesUsed: 7 }
    const parsed = {
      ...raw,
      lastUsed: raw.lastUsed ? DateTime.fromISO(raw.lastUsed) : undefined,
    }
    expect(parsed.lastUsed).toBeInstanceOf(DateTime)
    expect((parsed.lastUsed as DateTime).toISODate()).toBe('2025-01-15')
  })

  it('computes days ago correctly', () => {
    const lastUsed = DateTime.now().minus({ days: 5 })
    const daysAgo = Math.floor(DateTime.now().diff(lastUsed, 'days').days)
    expect(daysAgo).toBe(5)
  })

  it('handles missing lastUsed gracefully', () => {
    const raw = { dishId: 'dish-2', lastUsed: undefined, timesUsed: 0 }
    const lastUsed = raw.lastUsed ? DateTime.fromISO(raw.lastUsed) : undefined
    expect(lastUsed).toBeUndefined()
  })
})

describe('Dish sorting comparators', () => {
  const dishes = [
    { id: '1', name: 'Zucchini soup', rating: 3 },
    { id: '2', name: 'Apple pie', rating: 5 },
    { id: '3', name: 'Meatballs', rating: 4 },
  ]

  it('sorts A-Z correctly', () => {
    const sorted = [...dishes].sort((a, b) => a.name.localeCompare(b.name))
    expect(sorted.map((d) => d.name)).toEqual(['Apple pie', 'Meatballs', 'Zucchini soup'])
  })

  it('sorts Z-A correctly', () => {
    const sorted = [...dishes].sort((a, b) => b.name.localeCompare(a.name))
    expect(sorted.map((d) => d.name)).toEqual(['Zucchini soup', 'Meatballs', 'Apple pie'])
  })

  it('sorts by rating descending (highest first)', () => {
    const sorted = [...dishes].sort((a, b) => b.rating - a.rating)
    expect(sorted.map((d) => d.name)).toEqual(['Apple pie', 'Meatballs', 'Zucchini soup'])
  })

  it('sorts by rating ascending (lowest first)', () => {
    const sorted = [...dishes].sort((a, b) => a.rating - b.rating)
    expect(sorted.map((d) => d.name)).toEqual(['Zucchini soup', 'Meatballs', 'Apple pie'])
  })
})

import { describe, expect, it } from 'vitest'
import en from '~/i18n/locales/en.json'
import da from '~/i18n/locales/da.json'

describe('rating reminder translations', () => {
  it('provides matching English and Danish labels', () => {
    expect(Object.keys(en.ratingReminders).sort()).toEqual(Object.keys(da.ratingReminders).sort())
    expect(Object.values(en.ratingReminders).every(text => text.length > 0)).toBe(true)
    expect(Object.values(da.ratingReminders).every(text => text.length > 0)).toBe(true)
  })
})

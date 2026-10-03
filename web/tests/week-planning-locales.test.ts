import { describe, expect, it } from 'vitest'
import english from '~/i18n/locales/en.json'
import danish from '~/i18n/locales/da.json'

function keys(value: unknown, prefix = ''): string[] {
  if (typeof value !== 'object' || value === null) return [prefix]
  return Object.entries(value).flatMap(([key, child]) => keys(child, prefix ? `${prefix}.${key}` : key))
}

describe('planning localization', () => {
  it('provides matching English and Danish keys with proper Danish characters', () => {
    expect(keys(danish.weekPlanning).sort()).toEqual(keys(english.weekPlanning).sort())
    expect(danish.weekPlanning.title).toBe('Planlæg din uge')
    expect(danish.weekPlanning.emptyCatalog).toContain('Prøv')
    expect(danish.weekPlanning.clearOptOutWarning).toContain('Tilføjelse')
  })
})

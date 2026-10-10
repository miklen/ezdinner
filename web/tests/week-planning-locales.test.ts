import { describe, expect, it } from 'vitest'
import english from '~/i18n/locales/en.json'
import danish from '~/i18n/locales/da.json'

function keys(value: unknown, prefix = ''): string[] {
  if (typeof value !== 'object' || value === null) return [prefix]
  return Object.entries(value).flatMap(([key, child]) => keys(child, prefix ? `${prefix}.${key}` : key))
}

describe('planning localization', () => {
  it.each([english, danish])('localizes assigned-date labels and all shared navigation/read controls', messages => {
    for (const text of [messages.weekPlanning.alreadyAssignedDay, messages.weekPlanning.loading, messages.weekPlanning.readFailed, messages.weekPlanning.retry, messages.weekPlanning.close, messages.weekPlanning.assignTitle, messages.plan.previousWeekAriaLabel, messages.plan.nextWeekAriaLabel, messages.plan.tapToReturnToThisWeek]) expect(text.length).toBeGreaterThan(0)
    expect(messages.weekPlanning.alreadyAssignedDay).toContain('{dish}')
    expect(messages.weekPlanning.alreadyAssignedDay).toContain('{day}')
  })
  it('provides matching English and Danish keys with proper Danish characters', () => {
    expect(keys(danish.weekPlanning).sort()).toEqual(keys(english.weekPlanning).sort())
    expect(danish.weekPlanning.title).toBe('Planlæg din uge')
    expect(danish.weekPlanning.emptyCatalog).toContain('Prøv')
    expect(danish.weekPlanning.clearOptOutWarning).toContain('Tilføjelse')
  })
})

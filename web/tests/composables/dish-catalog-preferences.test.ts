import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { effectScope, shallowRef } from 'vue'
import type { EffectScope } from 'vue'
import { useDishCatalogPreferences } from '~/composables/useDishCatalogPreferences'
import { catalogPreferencesKey, decodeCatalogPreferences, defaultCatalogPreferences } from '~/utils/dish-catalog-preferences'

const scopes: EffectScope[] = []
beforeEach(() => setActivePinia(createPinia()))
afterEach(() => scopes.splice(0).forEach(scope => scope.stop()))

function storage() {
  const records = new Map<string, string>()
  return {
    records,
    getItem: vi.fn((key: string) => records.get(key) ?? null),
    setItem: vi.fn((key: string, value: string) => { records.set(key, value) }),
  }
}

function consumer(saved = storage(), account = shallowRef('account'), family = shallowRef('family')) {
  const lifecycle = effectScope()
  scopes.push(lifecycle)
  const state = lifecycle.run(() => useDishCatalogPreferences(account, family, saved))!
  return { state, lifecycle, saved, account, family }
}

describe('catalog browsing preferences', () => {
  it('keeps every control when the page unmounts and returns, then restores a new app session', () => {
    const first = consumer()
    first.state.update({ search: 'pasta', showArchived: true })
    first.state.toggleFilter('roles', 'Main')
    first.state.toggleFilter('roles', 'Side')
    first.state.toggleFilter('effort', 'Quick')
    first.state.toggleFilter('season', 'Summer')
    first.state.toggleFilter('cuisine', 'Italian')
    first.state.selectSort('rating')
    first.state.selectSort('rating')
    const expected = { search: 'pasta', roles: ['Main', 'Side'], effort: ['Quick'], season: ['Summer'], cuisine: ['italian'], showArchived: true, sortKey: 'rating', sortDir: 'asc' }
    first.lifecycle.stop()
    expect(consumer(first.saved).state.preferences.value).toEqual(expected)
    setActivePinia(createPinia())
    expect(consumer(first.saved).state.preferences.value).toEqual(expected)
  })

  it('restores a scope before any writes, and isolates account/family transitions', () => {
    const saved = storage()
    const otherKey = catalogPreferencesKey('account', 'other')!
    saved.records.set(otherKey, JSON.stringify({ version: 1, ...defaultCatalogPreferences(), search: 'other search', showArchived: true }))
    const { state, family, account } = consumer(saved)
    state.update({ search: 'original' })
    family.value = 'other'
    expect(state.preferences.value.search).toBe('other search')
    expect(state.preferences.value.showArchived).toBe(true)
    expect(saved.setItem.mock.calls.filter(([key]) => key === otherKey).every(([, value]) => JSON.parse(value).search === 'other search')).toBe(true)
    state.selectSort('lastUsed')
    family.value = 'family'
    expect(state.preferences.value.search).toBe('original')
    expect(state.preferences.value.sortKey).toBe('name')
    account.value = 'different account'
    expect(state.preferences.value).toEqual(defaultCatalogPreferences())
    state.update({ search: 'different' })
    account.value = 'account'
    expect(state.preferences.value.search).toBe('original')
    family.value = 'other'
    expect(state.preferences.value.sortKey).toBe('lastUsed')
  })

  it('does not persist unidentified scopes or transfer anonymous selections into a family', () => {
    const { state, family, saved } = consumer(storage(), shallowRef('account'), shallowRef(''))
    state.update({ search: 'anonymous' })
    expect(saved.getItem).not.toHaveBeenCalled()
    expect(saved.setItem).not.toHaveBeenCalled()
    family.value = 'family'
    expect(state.preferences.value.search).toBe('')
  })

  it('keeps navigation state usable when storage reads and writes fail', () => {
    const saved = storage()
    saved.getItem.mockImplementation(() => { throw new Error('blocked') })
    saved.setItem.mockImplementation(() => { throw new Error('quota') })
    const first = consumer(saved)
    first.state.update({ search: 'still works' })
    first.state.toggleFilter('roles', 'Main')
    first.lifecycle.stop()
    expect(consumer(saved).state.preferences.value).toMatchObject({ search: 'still works', roles: ['Main'] })
  })

  it('clears metadata/archive while preserving search and sorting, and persists search clearing', () => {
    const { state, saved } = consumer()
    state.update({ search: 'rice', showArchived: true })
    state.toggleFilter('roles', 'Side')
    state.selectSort('timesUsed')
    state.clearFilters()
    expect(state.preferences.value).toEqual({ ...defaultCatalogPreferences(), search: 'rice', sortKey: 'timesUsed', sortDir: 'desc' })
    state.update({ search: '' })
    setActivePinia(createPinia())
    expect(consumer(saved).state.preferences.value.search).toBe('')
  })

  it('toggles selections and sorting using the existing control behavior', () => {
    const { state } = consumer()
    state.toggleFilter('cuisine', 'Italian')
    state.toggleFilter('cuisine', 'ITALIAN')
    expect(state.preferences.value.cuisine).toEqual([])
    state.selectSort('name')
    expect(state.preferences.value.sortDir).toBe('desc')
    state.selectSort('rating')
    expect(state.preferences.value.sortDir).toBe('desc')
    state.selectSort('name')
    expect(state.preferences.value.sortDir).toBe('asc')
  })

  it('retains restored filters until a caller supplies successfully loaded options', () => {
    const { state, saved } = consumer()
    state.update({ roles: ['Main', 'Side'], season: ['Summer'], search: 'rice' })
    state.reconcile({ roles: ['Main'], effort: [], season: ['Summer'], cuisine: [] })
    expect(state.preferences.value).toMatchObject({ roles: ['Main'], season: ['Summer'], search: 'rice' })
    setActivePinia(createPinia())
    expect(consumer(saved).state.preferences.value.roles).toEqual(['Main'])
  })

  it.each([null, '{broken', '{"version":2}', '[]', 'null'])('falls back safely for an unusable saved record: %s', raw => {
    expect(decodeCatalogPreferences(raw)).toEqual(defaultCatalogPreferences())
  })

  it('validates fields independently, ignores unknown fields and deduplicates cuisine keys', () => {
    expect(decodeCatalogPreferences(JSON.stringify({ version: 1, search: null, roles: ['Main', 1], effort: ['Quick', 'Quick'], season: 'Summer', cuisine: ['ITALIAN', 'italian'], showArchived: 'yes', sortKey: 'unsupported', sortDir: 'up', extra: true })))
      .toEqual({ ...defaultCatalogPreferences(), effort: ['Quick'], cuisine: ['italian'] })
  })
})

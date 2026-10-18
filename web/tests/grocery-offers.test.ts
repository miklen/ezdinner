import { afterEach, describe, expect, it, vi } from 'vitest'
import { effectScope, shallowRef } from 'vue'
import { groceryOfferSchema, groceryCoverageSchema, combineGroceryCoverage } from '~/types/grocery-offers'
import type { GroceryCoverage } from '~/types/grocery-offers'
import { recommendationResultSchema } from '~/types/dish-recommendations'
import { useDishRecommendations } from '~/composables/useDishRecommendations'
import { useGroceryOfferDeadlines } from '~/composables/useGroceryOfferDeadlines'
import { DishRecommendationsRepository } from '~/repository/dish-recommendations-repository'
import { FamilyRepository } from '~/repository/family-repository'

export const offerFixture = { offerId: 'prej:1', ingredient: 'minced-beef', storeId: 'netto', storeName: 'Netto', productName: 'Minced beef', price: 27, referencePrice: 30,
  currency: 'DKK', discountFraction: 0.1, conditions: ['chain-price'], freshness: 'Fresh', validFrom: '2026-10-18', validThrough: '2026-10-20',
  observedAt: '2026-10-18T12:00:00Z', usableUntil: '2026-10-20T12:00:00Z', sourceUrl: 'https://example.com/offer', relationship: 'Inference' }
const dishId = '00000000-0000-4000-8000-000000000001'
function result(coverage: Partial<GroceryCoverage> = {}, id = dishId) {
  return { outcome: 'Matches', activeConstraints: [], contextSummary: 'Original language', message: null,
    groceryCoverage: { status: 'Fresh', evaluatedAt: '2026-10-18T12:00:00Z', storeIds: ['netto'], limitations: [], ...coverage },
    dishes: [{ dishId: id, name: 'Lasagne', rating: 8, roles: ['Main'], isUnclassified: false, isWished: false, wishVotes: 0,
      lastServed: null, servingCount: 0, neverUsed: true, typicalSpacingDays: null, assignedDates: [], historicalReasons: [], explanations: [], limitations: [], groceryOffers: [offerFixture] }] }
}
const scopes: ReturnType<typeof effectScope>[] = []
function scoped<T>(create: () => T): T {
  const scope = effectScope(); scopes.push(scope)
  const value = scope.run(create)
  if (!value) throw new Error('Missing scoped value')
  return value
}
afterEach(() => { scopes.splice(0).forEach(scope => scope.stop()); vi.useRealTimers() })
function workspace(fetch = vi.fn().mockResolvedValue(result())) {
  const storeIds = shallowRef<string[] | null>(['netto'])
  const locale = shallowRef('en')
  const recommendations = scoped(() => useDishRecommendations({ family: shallowRef('family'), account: shallowRef('account'), monday: shallowRef('2026-10-19'), target: shallowRef(null), locale, storeIds, repository: new DishRecommendationsRepository(fetch) }))
  return { recommendations, storeIds, locale, fetch }
}

describe('grocery contracts', () => {
  it('accepts older responses without store details and validates selected store results', () => {
    const coverage = result().groceryCoverage
    expect(groceryCoverageSchema.parse(coverage).stores).toBeUndefined()
    const store = { storeId: 'netto', storeName: 'Netto', status: 'Fresh', usableOfferCount: 0 }
    expect(groceryCoverageSchema.parse({ ...coverage, stores: [store] }).stores).toEqual([store])
    for (const stores of [[store, store], [{ ...store, storeId: 'other-family-store' }], [{ ...store, usableOfferCount: -1 }], [{ ...store, status: 'invented' }]]) {
      expect(groceryCoverageSchema.safeParse({ ...coverage, stores }).success).toBe(false)
    }
  })
  it('uses the latest store check when appending ideas and never carries removed stores', () => {
    const previous = groceryCoverageSchema.parse({ ...result().groceryCoverage,
      stores: [{ storeId: 'netto', storeName: 'Netto', status: 'Unavailable', usableOfferCount: 0 }] })
    const recovered = groceryCoverageSchema.parse({ ...result().groceryCoverage,
      stores: [{ storeId: 'netto', storeName: 'Netto', status: 'Fresh', usableOfferCount: 3 }] })
    expect(combineGroceryCoverage(previous, recovered).stores).toEqual(recovered.stores)
    expect(combineGroceryCoverage(previous, groceryCoverageSchema.parse(result().groceryCoverage)).stores).toEqual(previous.stores)
    expect(combineGroceryCoverage(previous, { ...recovered, storeIds: [], stores: [] }).stores).toEqual([])
  })
  it('keeps unknown promotional expiry explicit with a known evidence deadline', () => {
    expect(groceryOfferSchema.parse({ ...offerFixture, validThrough: null }).validThrough).toBeNull()
    expect(recommendationResultSchema.parse({ outcome: 'NoMatch', dishes: [], activeConstraints: [], contextSummary: '', message: null }).groceryCoverage.status).toBe('Unknown')
  })
  it.each([{ price: -1 }, { price: 30 }, { currency: 'EUR' }, { usableUntil: '2026-10-18T11:00:00Z' }, { sourceUrl: 'javascript:alert(1)' }, { sourceUrl: 'http://example.com' }, { validThrough: '2026-02-30' }, { discountFraction: 1.1 }])('rejects malformed offer evidence %o', replacement => {
    expect(groceryOfferSchema.safeParse({ ...offerFixture, ...replacement }).success).toBe(false)
  })
  it('rejects unsorted and duplicate coverage identities', () => {
    for (const storeIds of [['rema', 'netto'], ['netto', 'netto']]) expect(groceryCoverageSchema.safeParse({ ...result().groceryCoverage, storeIds }).success).toBe(false)
  })
  it('saves selected chains through the focused endpoint and returns the server selection', async () => {
    const fetch = vi.fn().mockResolvedValue({ groceryStores: ['netto', 'rema'] })
    const repository = new FamilyRepository(fetch, vi.fn())
    expect(await repository.saveGroceryStores('family', ['rema', 'netto'])).toEqual(['netto', 'rema'])
    expect(fetch).toHaveBeenCalledWith('/api/families/family/grocery-stores', { method: 'PUT', body: { storeIds: ['rema', 'netto'] } })
  })
})

describe('retained grocery recommendations', () => {
  it('does not report matching family settings arriving after recommendations as a store change', async () => {
    const { recommendations, storeIds, fetch } = workspace()
    storeIds.value = null
    await recommendations.submit('automatic')
    storeIds.value = ['netto']
    expect(recommendations.selectionChanged.value).toBe(false)
    expect(recommendations.refreshRequired.value).toBe(false)
    await recommendations.submit('more')
    expect(fetch).toHaveBeenCalledTimes(2)
  })
  it('keeps an initial recommendation request running while family settings load', async () => {
    let resolve: (value: unknown) => void = () => {}
    const fetch = vi.fn().mockImplementation(() => new Promise(done => { resolve = done }))
    const { recommendations, storeIds } = workspace(fetch)
    storeIds.value = null
    const pending = recommendations.submit('automatic')
    storeIds.value = ['netto']
    expect(fetch.mock.calls[0][1].signal.aborted).toBe(false)
    resolve(result())
    await pending
    expect(recommendations.visible.value).toHaveLength(1)
    expect(recommendations.refreshRequired.value).toBe(false)
  })
  it('requires refresh when loaded settings differ from the stores used for retained ideas', async () => {
    const { recommendations, storeIds } = workspace()
    storeIds.value = null
    await recommendations.submit('automatic')
    storeIds.value = ['rema']
    expect(recommendations.selectionChanged.value).toBe(true)
    expect(recommendations.visible.value).toHaveLength(1)
  })
  it('compares settings loaded during the initial request with the returned store selection', async () => {
    let resolve: (value: unknown) => void = () => {}
    const { recommendations, storeIds } = workspace(vi.fn().mockImplementation(() => new Promise(done => { resolve = done })))
    storeIds.value = null
    const pending = recommendations.submit('automatic')
    storeIds.value = ['rema']
    resolve(result())
    await pending
    expect(recommendations.visible.value).toHaveLength(1)
    expect(recommendations.selectionChanged.value).toBe(true)
  })
  const mixedCoverageCases: [GroceryCoverage['status'], string][] = [
    ['Unavailable', 'source-unavailable'], ['Stale', 'source-stale'], ['Unknown', 'coverage-unknown'], ['Disabled', 'source-disabled'], ['Partial', 'coverage-partial'],
  ]
  it.each(mixedCoverageCases)('reports mixed %s and fresh pages as partial without losing the earlier limitation', async (status, limitation) => {
    const fetch = vi.fn().mockResolvedValueOnce(result({ status, limitations: [] })).mockResolvedValue(result())
    const { recommendations } = workspace(fetch)
    await recommendations.submit('automatic')
    await recommendations.submit('more')
    expect(recommendations.coverage.value).toMatchObject({ status: 'Partial', limitations: [limitation] })
    await recommendations.submit('more')
    expect(recommendations.coverage.value).toMatchObject({ status: 'Partial', limitations: [limitation] })
  })
  it('preserves both mixed status limitations and the earliest known evaluation instant', () => {
    const previous: GroceryCoverage = { status: 'Stale', limitations: ['profiles-partial'], evaluatedAt: '2026-10-18T12:00:00Z', storeIds: ['netto'] }
    const next: GroceryCoverage = { status: 'Unavailable', limitations: [], evaluatedAt: null, storeIds: ['netto'] }
    expect(combineGroceryCoverage(previous, next)).toEqual({ ...previous, status: 'Partial', limitations: ['profiles-partial', 'source-stale', 'source-unavailable'] })
    expect(combineGroceryCoverage(next, previous).evaluatedAt).toBe(previous.evaluatedAt)
    expect(combineGroceryCoverage(previous, { ...next, evaluatedAt: '2026-10-18T12:00:00.001Z' }).evaluatedAt).toBe(previous.evaluatedAt)
  })
  it.each(['automatic', 'request'])('keeps dismissals and exclusions through a failed %s refresh until retry succeeds', async mode => {
    const otherId = '00000000-0000-4000-8000-000000000002'
    const initial = result()
    initial.dishes.push(...result({}, otherId).dishes)
    const fetch = vi.fn().mockResolvedValueOnce(initial).mockRejectedValueOnce(new Error('Unavailable')).mockResolvedValue(result())
    const { recommendations } = workspace(fetch)
    await recommendations.submit('request', 'No fish')
    recommendations.dismiss(dishId)
    const refresh = recommendations.submit(mode === 'automatic' ? 'automatic' : 'request')
    expect(recommendations.visible.value.map(dish => dish.dishId)).toEqual([otherId])
    expect(recommendations.exclusions.value).toEqual([dishId, otherId])
    await refresh
    expect(recommendations.state.value.status).toBe('failed')
    expect(recommendations.visible.value.map(dish => dish.dishId)).toEqual([otherId])
    expect(recommendations.exclusions.value).toEqual([dishId, otherId])
    expect(fetch.mock.calls[1][1].body.excludedDishIds).toEqual([])
    await recommendations.retry()
    expect(recommendations.visible.value.map(dish => dish.dishId)).toEqual([dishId])
    expect(recommendations.exclusions.value).toEqual([dishId])
    expect(recommendations.constraints.value).toEqual(['No fish'])
    expect(fetch.mock.calls[2][1].body.excludedDishIds).toEqual([])
  })
  it('retains cards, cancels local selection changes and blocks More until explicit refresh', async () => {
    const { recommendations, storeIds, fetch } = workspace()
    await recommendations.submit('request', 'No fish')
    recommendations.dismiss(dishId)
    storeIds.value = ['rema']
    await recommendations.submit('more')
    expect(fetch).toHaveBeenCalledTimes(1)
    expect(recommendations.refreshRequired.value).toBe(true)
    await recommendations.submit('request')
    expect(recommendations.constraints.value).toEqual(['No fish'])
    expect(recommendations.visible.value).toHaveLength(1)
    expect(fetch.mock.calls[1][1].body.excludedDishIds).toEqual([])
  })
  it('rejects a More page revealing a different remote selection', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(result()).mockResolvedValue(result({ storeIds: ['rema'] }, '00000000-0000-4000-8000-000000000002'))
    const { recommendations } = workspace(fetch)
    await recommendations.submit('automatic')
    await recommendations.submit('more')
    expect(recommendations.visible.value.map(dish => dish.dishId)).toEqual([dishId])
    expect(recommendations.refreshRequired.value).toBe(true)
    expect(recommendations.coverage.value.storeIds).toEqual(['netto'])
  })
  it('does not let a complete later page erase retained partial coverage', async () => {
    const fetch = vi.fn().mockResolvedValueOnce(result({ status: 'Partial', limitations: ['profiles-partial'] })).mockResolvedValue(result())
    const { recommendations } = workspace(fetch)
    await recommendations.submit('automatic'); await recommendations.submit('more')
    expect(recommendations.coverage.value).toMatchObject({ status: 'Partial', limitations: ['profiles-partial'] })
    await recommendations.submit('automatic')
    expect(recommendations.coverage.value).toMatchObject({ status: 'Fresh', limitations: [] })
  })
  it('retains prose and cards on language change without making requests', async () => {
    const { recommendations, locale, fetch } = workspace()
    await recommendations.submit('automatic')
    locale.value = 'da'
    await recommendations.submit('more')
    expect(fetch).toHaveBeenCalledTimes(1)
    expect(recommendations.visible.value).toHaveLength(1)
    expect(recommendations.state.value.result?.contextSummary).toBe('Original language')
    expect(recommendations.staleLocale.value).toBe(true)
  })
  it('ignores an obsolete response after a saved store change', async () => {
    let resolve: (value: unknown) => void = () => {}
    const { recommendations, storeIds } = workspace(vi.fn().mockImplementation(() => new Promise(done => { resolve = done })))
    const pending = recommendations.submit('automatic')
    storeIds.value = ['rema']; resolve(result()); await pending
    expect(recommendations.visible.value).toEqual([])
  })
})

describe('single offer deadline lifecycle', () => {
  it.each(['2026-10-20T12:00:00Z', '2026-10-20T22:00:00Z'])('rechecks the exclusive cutoff %s while continuously visible without a request', deadline => {
    vi.useFakeTimers(); vi.setSystemTime(Date.parse(deadline) - 1000)
    const offers = shallowRef([groceryOfferSchema.parse({ ...offerFixture, usableUntil: deadline })])
    const { now } = scoped(() => useGroceryOfferDeadlines(offers))
    expect(vi.getTimerCount()).toBe(1)
    vi.advanceTimersByTime(1000)
    expect(now.value).toBe(Date.parse(deadline))
    expect(vi.getTimerCount()).toBe(1)
    scopes[0].stop(); expect(vi.getTimerCount()).toBe(0)
  })
  it('reschedules to earlier evidence and rechecks a resumed tab', async () => {
    vi.useFakeTimers(); vi.setSystemTime('2026-10-20T11:00:00Z')
    const offers = shallowRef([groceryOfferSchema.parse(offerFixture)])
    const { now } = scoped(() => useGroceryOfferDeadlines(offers))
    offers.value = [groceryOfferSchema.parse({ ...offerFixture, usableUntil: '2026-10-20T11:30:00Z' })]
    await Promise.resolve(); expect(vi.getTimerCount()).toBe(1)
    vi.advanceTimersByTime(1800000); expect(now.value).toBe(Date.parse('2026-10-20T11:30:00Z'))
    offers.value = []; await Promise.resolve(); expect(vi.getTimerCount()).toBe(0)
  })
  it('rechecks at Copenhagen midnight before a later evidence cutoff', () => {
    vi.useFakeTimers(); vi.setSystemTime('2026-10-19T21:59:59Z')
    const { now } = scoped(() => useGroceryOfferDeadlines(shallowRef([groceryOfferSchema.parse(offerFixture)])))
    vi.advanceTimersByTime(1000)
    expect(now.value).toBe(Date.parse('2026-10-19T22:00:00Z'))
    expect(vi.getTimerCount()).toBe(1)
  })
})

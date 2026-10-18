// @vitest-environment jsdom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, nextTick, shallowRef } from 'vue'
import { mount, flushPromises, config } from '@vue/test-utils'
import GroceryStores from '~/components/Family/GroceryStores.vue'
import GroceryOfferReasons from '~/components/WeekPlanning/GroceryOfferReasons.vue'
import RecommendationExplorer from '~/components/WeekPlanning/RecommendationExplorer.vue'
import GroceryStatus from '~/components/WeekPlanning/GroceryStatus.vue'
import type { Family } from '~/types'
import { groceryOfferSchema } from '~/types/grocery-offers'
import en from '~/i18n/locales/en.json'
import da from '~/i18n/locales/da.json'
import { useGroceryOfferDeadlines } from '~/composables/useGroceryOfferDeadlines'

const locale = shallowRef('en')
function t(key: string, values: Record<string, unknown> = {}) {
  let translated: unknown = locale.value === 'en' ? en : da
  for (const segment of key.split('.')) {
    if (!translated || typeof translated !== 'object') return key
    translated = Reflect.get(translated, segment)
  }
  if (typeof translated !== 'string') return key
  return Object.entries(values).reduce((text, [name, value]) => text.replace(`{${name}}`, String(value)), translated)
}
const saveGroceryStores = vi.fn()
const updateGroceryStores = vi.fn()
const Picker = defineComponent({ props: { open: Boolean }, setup: (props, { slots }) => () => props.open ? h('div', { role: 'dialog' }, [slots.navigation?.(), slots.default?.(), slots.footer?.()]) : null })
const Button = defineComponent({ props: { disabled: Boolean }, setup: (props, { slots }) => () => h('button', { disabled: props.disabled }, slots.default?.()) })
const family: Family = { id: 'family', name: 'Family', familyMembers: [], groceryStores: ['netto'], supportedGroceryStores: ['netto', 'rema', 'other-chain'].map(id => ({ id, name: id, isAvailable: true, isBorderTrade: id === 'other-chain', isFlyerOnly: false })) }
const wrappers: ReturnType<typeof mount>[] = []
config.global.stubs.VIcon = true
config.global.stubs.NuxtLink = { template: '<a><slot /></a>' }
function stores(owner = true) {
  const wrapper = mount(GroceryStores, { props: { family, owner, account: 'owner' }, global: { stubs: { PickerShell: Picker, VBtn: Button, VIcon: true } } }); wrappers.push(wrapper); return wrapper
}
beforeEach(() => {
  locale.value = 'en'; saveGroceryStores.mockReset().mockResolvedValue(['netto', 'rema']); updateGroceryStores.mockReset()
  vi.stubGlobal('useI18n', () => ({ t, locale }))
  vi.stubGlobal('useRepositories', () => ({ families: { saveGroceryStores } }))
  vi.stubGlobal('useFamiliesStore', () => ({ updateGroceryStores }))
})
afterEach(() => { wrappers.splice(0).forEach(wrapper => wrapper.unmount()); vi.unstubAllGlobals(); vi.useRealTimers() })

describe('family grocery stores', () => {
  it.each(['en', 'da'])('shows a compact selection and opens every catalog chain in %s', async language => {
    locale.value = language
    const wrapper = stores()
    expect(wrapper.findAll('input')).toHaveLength(0)
    expect(wrapper.text()).toContain(t('grocery.storesTitle'))
    expect(wrapper.get('ul').text()).toContain('netto')
    await wrapper.get('[data-test="edit-stores"]').trigger('click')
    expect(wrapper.findAll('input')).toHaveLength(3)
    expect(wrapper.text()).toContain(t('grocery.borderStores'))
    expect(wrapper.get('[data-test="save-stores"]').attributes('disabled')).toBeDefined()
  })
  it('lets members read selection without editing', () => {
    const wrapper = stores(false)
    expect(wrapper.findAll('input')).toHaveLength(0)
    expect(wrapper.get('ul').text()).toBe('netto')
    expect(wrapper.find('button').exists()).toBe(false)
    expect(wrapper.text()).toContain(t('grocery.storesReadOnly'))
  })
  it('allows owners to select catalog stores whose live offers are unavailable', async () => {
    const wrapper = stores()
    await wrapper.setProps({ family: { ...family, supportedGroceryStores: [{ id: 'new-chain', name: 'New chain', isAvailable: false, isBorderTrade: false, isFlyerOnly: true }] } })
    await wrapper.get('[data-test="edit-stores"]').trigger('click')
    expect(wrapper.find('input').attributes('disabled')).toBeUndefined()
    await wrapper.find('input').setValue(true)
    await wrapper.get('[data-test="save-stores"]').trigger('click'); await flushPromises()
    expect(saveGroceryStores).toHaveBeenCalledWith('family', ['netto', 'new-chain'])
  })
  it('keeps failed drafts separate from persisted settings and retries', async () => {
    saveGroceryStores.mockRejectedValueOnce(new Error('Unavailable'))
    const wrapper = stores()
    await wrapper.get('[data-test="edit-stores"]').trigger('click')
    await wrapper.findAll('input')[1].setValue(true)
    expect(family.groceryStores).toEqual(['netto'])
    await wrapper.get('[data-test="save-stores"]').trigger('click'); await flushPromises()
    expect(wrapper.find('[role="alert"]').exists()).toBe(true)
    expect(updateGroceryStores).not.toHaveBeenCalled()
    await wrapper.get('[data-test="save-stores"]').trigger('click'); await flushPromises()
    expect(wrapper.emitted('saved')).toEqual([[['netto', 'rema']]])
    expect(updateGroceryStores).toHaveBeenCalledWith('family', ['netto', 'rema'])
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
  })
  it('ignores a save response after the account changes', async () => {
    let resolve: (value: string[]) => void = () => {}
    saveGroceryStores.mockImplementation(() => new Promise(done => { resolve = done }))
    const wrapper = stores(); await wrapper.get('[data-test="edit-stores"]').trigger('click')
    await wrapper.findAll('input')[1].setValue(true)
    await wrapper.get('[data-test="save-stores"]').trigger('click')
    await wrapper.setProps({ account: 'another-account' })
    resolve(['netto', 'rema']); await flushPromises()
    expect(updateGroceryStores).not.toHaveBeenCalled()
    expect(wrapper.emitted('saved')).toBeUndefined()
  })
  it('discards cancelled edits and restores the saved selection on reopening', async () => {
    const wrapper = stores()
    await wrapper.get('[data-test="edit-stores"]').trigger('click')
    await wrapper.findAll('input')[1].setValue(true)
    await wrapper.get('[data-test="cancel-stores"]').trigger('click')
    expect(saveGroceryStores).not.toHaveBeenCalled()
    await wrapper.get('[data-test="edit-stores"]').trigger('click')
    expect(wrapper.findAll('input')[1].element.checked).toBe(false)
    expect(wrapper.get('[data-test="save-stores"]').attributes('disabled')).toBeDefined()
  })
  it('explains unavailable offers once while keeping all shops selectable', async () => {
    const wrapper = stores()
    await wrapper.setProps({ family: { ...family, supportedGroceryStores: family.supportedGroceryStores?.map(store => ({ ...store, isAvailable: false })) } })
    await wrapper.get('[data-test="edit-stores"]').trigger('click')
    expect(wrapper.findAll('.grocery-stores__notice')).toHaveLength(1)
    expect(wrapper.text()).toContain(t('grocery.offersPending'))
    expect(wrapper.findAll('input').every(input => input.attributes('disabled') === undefined)).toBe(true)
  })
})

describe('structured offer explanations', () => {
  it.each(['en', 'da'])('names only checked stores without usable discounts, with distinct reasons in %s', async language => {
    locale.value = language
    const wrapper = mount(GroceryStatus, { props: { status: 'Partial', offerCount: 0, loading: false,
      coverage: { status: 'Partial', evaluatedAt: '2026-10-18T12:00:00Z', storeIds: ['lidl', 'nemlig', 'netto', 'rema1000'], limitations: ['source-unavailable'], stores: [
        { storeId: 'lidl', storeName: 'Lidl', status: 'Partial', usableOfferCount: 0 },
        { storeId: 'nemlig', storeName: 'Nemlig', status: 'Unavailable', usableOfferCount: 0 },
        { storeId: 'netto', storeName: 'Netto', status: 'Fresh', usableOfferCount: 0 },
        { storeId: 'rema1000', storeName: 'REMA 1000', status: 'Fresh', usableOfferCount: 4 },
      ] } }, global: { stubs: { VIcon: true, VBtn: Button } } })
    wrappers.push(wrapper)
    const list = wrapper.get('.grocery-status__stores')
    expect(list.findAll('li')).toHaveLength(3)
    expect(list.text()).toContain('Nemlig')
    expect(list.text()).toContain('Netto')
    expect(list.text()).not.toContain('REMA 1000')
    expect(list.text()).toContain(t('grocery.status.manualCheck.Unavailable'))
    expect(list.text()).toContain(t('grocery.status.manualCheck.Fresh'))
    expect(list.text()).toContain(t('grocery.status.manualCheck.Partial'))
    expect(list.text()).toContain(t('grocery.status.manualCheck.body'))
    await wrapper.setProps({ loading: true })
    expect(wrapper.find('.grocery-status__stores').exists()).toBe(false)
    await wrapper.setProps({ loading: false, refreshRequired: true })
    expect(wrapper.find('.grocery-status__stores').exists()).toBe(false)
    await wrapper.setProps({ refreshRequired: false, coverage: undefined })
    expect(wrapper.find('.grocery-status__stores').exists()).toBe(false)
  })
  it.each(['en', 'da'])('separates a store-selection change from refreshed unavailable coverage in %s', async language => {
    locale.value = language
    const wrapper = mount(RecommendationExplorer, {
      props: {
        state: { status: 'ready', result: { outcome: 'NoMatch', dishes: [], activeConstraints: [], contextSummary: '', message: null,
          groceryCoverage: { status: 'Unavailable', evaluatedAt: '2026-10-18T12:00:00Z', storeIds: ['netto'], limitations: ['source-unavailable'] } } },
        entries: [], constraints: [], role: null, scopeDecisionRequired: false, stale: false, scopedDay: null, planned: {}, target: null, busy: false,
        coverage: { status: 'Unavailable', evaluatedAt: '2026-10-18T12:00:00Z', storeIds: ['netto'], limitations: ['source-unavailable'] },
        selectionChanged: true, refreshRequired: true,
      },
      global: { stubs: { VBtn: Button, VIcon: true, VSelect: true, VTextarea: true, VAlert: true, VTextField: true, CatalogSearch: true } },
    })
    wrappers.push(wrapper)
    expect(wrapper.text()).toContain(t('grocery.status.states.Selection.title'))
    expect(wrapper.find('details').exists()).toBe(false)
    await wrapper.getComponent(GroceryStatus).get('button').trigger('click')
    expect(wrapper.emitted('refresh')).toHaveLength(1)
    await wrapper.setProps({ selectionChanged: false, refreshRequired: false })
    expect(wrapper.text()).not.toContain(t('grocery.status.states.Selection.title'))
    expect(wrapper.get('[role="status"]').text()).toContain(t('grocery.status.states.Unavailable.title'))
    expect(wrapper.get('details').attributes('open')).toBeUndefined()
    expect(wrapper.getComponent(GroceryStatus).find('button').exists()).toBe(false)
    expect(wrapper.text()).toContain(t('grocery.status.nextCheck'))
    await wrapper.setProps({ coverage: { status: 'Partial', evaluatedAt: '2026-10-18T12:00:00Z', storeIds: ['netto'], limitations: ['source-unavailable'] } })
    expect(wrapper.get('details').text()).toContain(t('grocery.limitations.source-unavailable'))
    expect(wrapper.get('[role="status"]').text()).not.toContain(t('grocery.limitations.source-unavailable'))
    await wrapper.setProps({ state: { status: 'failed', result: null, error: 'recommendationFailed' } })
    expect(wrapper.get('[role="alert"]').text()).toContain(t('grocery.status.requestFailedEmpty'))
    expect(wrapper.get('[role="alert"]').text()).not.toContain(t('grocery.status.requestFailedRetained'))
    await wrapper.get('[role="alert"] button').trigger('click')
    expect(wrapper.emitted('retry')).toHaveLength(1)
  })
  it.each(['en', 'da'])('distinguishes checked offers with no match from loading and optional setup in %s', async language => {
    locale.value = language
    const wrapper = mount(GroceryStatus, { props: { status: 'Fresh', offerCount: 0, loading: false }, global: { stubs: { VIcon: true, VBtn: Button, NuxtLink: { template: '<a><slot /></a>' } } } })
    wrappers.push(wrapper)
    expect(wrapper.text()).toContain(t('grocery.status.states.NoMatch.title'))
    expect(wrapper.find('button').exists()).toBe(false)
    await wrapper.setProps({ loading: true })
    expect(wrapper.text()).toContain(t('grocery.status.states.Loading.title'))
    expect(wrapper.attributes('aria-busy')).toBe('true')
    await wrapper.setProps({ loading: false, status: 'Disabled' })
    expect(wrapper.text()).toContain(t('grocery.status.states.NoStores.title'))
    expect(wrapper.get('a').text()).toContain(t('grocery.status.settings'))
    await wrapper.setProps({ status: 'Stale' })
    await wrapper.get('button').trigger('click')
    expect(wrapper.emitted('refresh')).toHaveLength(1)
  })
  it('uses complete matching English and Danish grocery keys', () => {
    expect(Object.keys(en.grocery)).toEqual(Object.keys(da.grocery))
    expect(Object.keys(en.grocery.ingredients)).toEqual(Object.keys(da.grocery.ingredients))
    expect(Object.keys(en.grocery.limitations)).toEqual(Object.keys(da.grocery.limitations))
    expect(JSON.stringify(da.grocery)).not.toContain('?')
  })
  it.each(['en', 'da'])('formats facts, inference and separate source links in %s', language => {
    locale.value = language
    const offer = groceryOfferSchema.parse({ offerId: '1', ingredient: 'minced-beef', storeId: 'netto', storeName: 'Netto', productName: 'Beef', price: 27, referencePrice: 30, currency: 'DKK', discountFraction: 0.1, conditions: ['chain-price'], freshness: 'Fresh', validFrom: null, validThrough: null, observedAt: '2026-10-18T12:00:00Z', usableUntil: '2026-10-18T22:00:00Z', sourceUrl: 'https://example.com/offer', relationship: 'Inference' })
    const wrapper = mount(GroceryOfferReasons, { props: { offers: [offer], now: Date.parse('2026-10-18T14:00:00Z') } }); wrappers.push(wrapper)
    expect(wrapper.text()).toContain(new Intl.NumberFormat(language, { style: 'currency', currency: 'DKK' }).format(27))
    expect(wrapper.text()).toContain(t('grocery.unknownExpiry'))
    expect(wrapper.text()).toContain(t('weekPlanning.evidence.Fact'))
    expect(wrapper.text()).toContain(t('weekPlanning.evidence.Inference'))
    expect(wrapper.text()).toContain(t('grocery.purchaseAhead'))
    expect(wrapper.find('a').attributes('href')).toBe('https://example.com/offer')
  })
  it.each(['en', 'da'])('keeps a stale observation usable until its exclusive deadline in %s', async language => {
    locale.value = language
    const offer = groceryOfferSchema.parse({ offerId: '1', ingredient: 'minced-beef', storeId: 'netto', storeName: 'Netto', productName: 'Beef', price: 27, referencePrice: 30, currency: 'DKK', discountFraction: 0.1, conditions: ['chain-price'], freshness: 'Stale', validFrom: null, validThrough: '2026-10-20', observedAt: '2026-10-18T12:00:00Z', usableUntil: '2026-10-20T12:00:00Z', sourceUrl: null, relationship: 'Inference' })
    const wrapper = mount(GroceryOfferReasons, { props: { offers: [offer], now: Date.parse(offer.usableUntil) - 1 } }); wrappers.push(wrapper)
    expect(wrapper.text()).toContain(t('grocery.staleObservation'))
    expect(wrapper.text()).toContain(t('grocery.buyBy', { date: language === 'en' ? '20 Oct 2026' : '20 okt. 2026' }))
    expect(wrapper.text()).toContain(t('grocery.purchaseAhead'))
    expect(wrapper.text()).not.toContain(t('grocery.expiredEvidence'))
    await wrapper.setProps({ now: Date.parse(offer.usableUntil) })
    expect(wrapper.text()).toContain(t('grocery.expiredEvidence'))
    expect(wrapper.text()).not.toContain(t('grocery.staleObservation'))
    expect(wrapper.text()).not.toContain(t('grocery.purchaseAhead'))
  })
  it('rechecks retained evidence when the page becomes visible and removes its listener on disposal', async () => {
    vi.useFakeTimers(); vi.setSystemTime('2026-10-18T12:00:00Z')
    const visibility = vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('visible')
    const remove = vi.spyOn(document, 'removeEventListener')
    const Host = defineComponent({ setup: () => {
      const { now } = useGroceryOfferDeadlines(shallowRef([]))
      return () => h('p', String(now.value))
    } })
    const wrapper = mount(Host)
    vi.setSystemTime('2026-10-18T13:00:00Z')
    document.dispatchEvent(new Event('visibilitychange'))
    await nextTick()
    expect(wrapper.text()).toBe(String(Date.parse('2026-10-18T13:00:00Z')))
    wrapper.unmount()
    expect(remove).toHaveBeenCalledWith('visibilitychange', expect.any(Function))
    expect(vi.getTimerCount()).toBe(0)
    visibility.mockRestore(); remove.mockRestore()
  })
})

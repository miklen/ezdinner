import { z } from 'zod'

const instant = z.iso.datetime({ precision: null })
const storeId = z.string().min(1).max(100)
const ingredient = z.enum(['minced-meat', 'minced-beef', 'minced-pork', 'poultry', 'chicken', 'turkey', 'beef', 'pork', 'fish', 'salmon', 'cod', 'lentils', 'beans', 'chickpeas', 'tofu', 'eggs', 'cheese', 'potatoes', 'rice', 'pasta', 'tomatoes', 'mushrooms', 'prepared-meatballs', 'sausages', 'salt'])
export const groceryStoreSchema = z.object({
  id: storeId, name: z.string().min(1), isAvailable: z.boolean(), isBorderTrade: z.boolean(), isFlyerOnly: z.boolean(),
})
export const grocerySelectionSchema = z.object({ groceryStores: z.array(storeId) })
const coverageStatus = z.enum(['Fresh', 'Stale', 'Unavailable', 'Partial', 'Disabled', 'Unknown'])
const groceryStoreCoverageSchema = z.object({
  storeId, storeName: z.string().min(1), status: coverageStatus, usableOfferCount: z.number().int().nonnegative(),
})
export const groceryCoverageSchema = z.object({
  status: coverageStatus,
  evaluatedAt: instant.nullable(),
  storeIds: z.array(storeId).refine(ids => ids.every((id, index) => index === 0 || ids[index - 1] < id), 'Store IDs must be unique and sorted'),
  limitations: z.array(z.string()),
  stores: z.array(groceryStoreCoverageSchema).optional(),
}).refine(coverage => !coverage.stores || (coverage.stores.every(store => coverage.storeIds.includes(store.storeId))
  && new Set(coverage.stores.map(store => store.storeId)).size === coverage.stores.length), 'Store coverage must be unique and belong to the evaluated selection')
export type GroceryCoverage = z.infer<typeof groceryCoverageSchema>
export const unknownGroceryCoverage = (): GroceryCoverage => ({ status: 'Unknown', evaluatedAt: null, storeIds: [], limitations: [] })
export const groceryOfferSchema = z.object({
  offerId: z.string().min(1), ingredient, storeId, storeName: z.string().min(1), productName: z.string().min(1),
  price: z.number().finite().positive(), referencePrice: z.number().finite().positive().nullable(), currency: z.literal('DKK'),
  discountFraction: z.number().finite().min(0).max(1), conditions: z.array(z.string()), freshness: z.enum(['Fresh', 'Stale']),
  validFrom: z.iso.date().nullable(), validThrough: z.iso.date().nullable(),
  observedAt: instant, usableUntil: instant,
  sourceUrl: z.url({ protocol: /^https$/ }).nullable(), relationship: z.enum(['Fact', 'Inference']),
}).refine(offer => (offer.referencePrice === null || offer.price < offer.referencePrice) && Date.parse(offer.observedAt) < Date.parse(offer.usableUntil)
  && (!offer.validFrom || !offer.validThrough || offer.validFrom <= offer.validThrough), 'Invalid grocery offer evidence')
export type GroceryOffer = z.infer<typeof groceryOfferSchema>
export type GroceryStore = z.infer<typeof groceryStoreSchema>

function retainedCoverageLimitations(coverage: GroceryCoverage): string[] {
  if (coverage.status === 'Fresh' || (coverage.status === 'Partial' && coverage.limitations.length)) return coverage.limitations
  const limitation: Record<Exclude<GroceryCoverage['status'], 'Fresh'>, string> = {
    Stale: 'source-stale', Unavailable: 'source-unavailable', Partial: 'coverage-partial', Disabled: 'source-disabled', Unknown: 'coverage-unknown',
  }
  return [...coverage.limitations, limitation[coverage.status]]
}

export function combineGroceryCoverage(previous: GroceryCoverage, next: GroceryCoverage): GroceryCoverage {
  const mixed = previous.status !== next.status
  const limitations = mixed ? [...retainedCoverageLimitations(previous), ...retainedCoverageLimitations(next)] : [...previous.limitations, ...next.limitations]
  const stores = new Map([...(previous.stores ?? []), ...(next.stores ?? [])].map(store => [store.storeId, store]))
  return { ...next, status: mixed ? 'Partial' : next.status,
    ...(previous.stores || next.stores ? { stores: [...stores.values()].filter(store => next.storeIds.includes(store.storeId)).sort((a, b) => a.storeId.localeCompare(b.storeId)) } : {}),
    evaluatedAt: previous.evaluatedAt !== null && (next.evaluatedAt === null || Date.parse(previous.evaluatedAt) < Date.parse(next.evaluatedAt)) ? previous.evaluatedAt : next.evaluatedAt,
    limitations: [...new Set(limitations)] }
}

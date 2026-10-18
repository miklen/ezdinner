<script setup lang="ts">
import { DateTime } from 'luxon'
import type { GroceryOffer } from '~/types/grocery-offers'
const props = defineProps<{ offers: GroceryOffer[]; now: number }>()
const { t, locale } = useI18n()
function money(amount: number) { return new Intl.NumberFormat(locale.value, { style: 'currency', currency: 'DKK' }).format(amount) }
function date(value: string) { return DateTime.fromISO(value, { zone: 'Europe/Copenhagen' }).setLocale(locale.value).toFormat('d MMM yyyy') }
function deadline(value: string) { return DateTime.fromISO(value).setZone('Europe/Copenhagen').setLocale(locale.value).toFormat('d MMM yyyy HH:mm') }
function conditionLabel(condition: string) { return t(`grocery.conditions.${['chain-price', 'border-trade'].includes(condition) ? condition : 'unknown'}`) }
function expired(offer: GroceryOffer) { return props.now >= Date.parse(offer.usableUntil) }
</script>

<template>
  <div v-for="offer in offers" :key="offer.offerId" class="grocery-reason" :class="{ 'grocery-reason--expired': expired(offer) }">
    <div class="grocery-reason__top"><span class="grocery-reason__store"><v-icon size="15" aria-hidden="true">mdi-storefront-outline</v-icon>{{ offer.storeName }}</span><span v-if="!expired(offer)" class="grocery-reason__saving">{{ t('grocery.offerSaving', { discount: new Intl.NumberFormat(locale, { style: 'percent', maximumFractionDigits: 0 }).format(offer.discountFraction) }) }}</span></div>
    <div class="grocery-reason__product"><span>{{ offer.productName }}</span><strong>{{ money(offer.price) }}</strong></div>
    <p v-if="expired(offer)" class="grocery-reason__muted">{{ t('grocery.expiredEvidence') }}</p>
    <template v-else>
      <p v-if="offer.freshness === 'Stale'" class="grocery-reason__muted">{{ t('grocery.staleObservation') }}</p>
      <p class="grocery-reason__deadline">{{ offer.validThrough ? t('grocery.buyBy', { date: date(offer.validThrough) }) : t('grocery.unknownExpiry') }}</p>
      <p class="grocery-reason__muted">{{ t('grocery.evidenceDeadline', { date: deadline(offer.usableUntil) }) }}</p>
    </template>
    <details class="grocery-reason__details">
      <summary>{{ t('grocery.offerDetails') }}<v-icon size="15" aria-hidden="true">mdi-chevron-down</v-icon></summary>
      <div class="grocery-reason__explanation">
        <p>{{ t(expired(offer) || offer.freshness === 'Stale' ? 'grocery.observedProduct' : 'grocery.product', { product: offer.productName, store: offer.storeName, price: money(offer.price), date: deadline(offer.observedAt) }) }} <small>{{ t('weekPlanning.evidence.Fact') }}</small></p>
        <p v-if="offer.referencePrice !== null">{{ t('grocery.referencePrice', { price: money(offer.referencePrice), discount: new Intl.NumberFormat(locale, { style: 'percent', maximumFractionDigits: 0 }).format(offer.discountFraction) }) }}</p>
        <p>{{ t('grocery.ingredientMatch', { ingredient: t(`grocery.ingredients.${offer.ingredient}`) }) }} <small>{{ t(`weekPlanning.evidence.${offer.relationship}`) }}</small></p>
        <p v-if="!expired(offer)" class="grocery-reason__muted">{{ t('grocery.purchaseAhead') }}</p>
        <p v-for="condition in offer.conditions" :key="condition" class="grocery-reason__muted">{{ conditionLabel(condition) }}</p>
      </div>
    </details>
    <a v-if="offer.sourceUrl" :href="offer.sourceUrl" target="_blank" rel="noopener noreferrer" class="grocery-reason__source">{{ t('grocery.viewSource', { store: offer.storeName }) }}</a>
  </div>
</template>

<style scoped>
.grocery-reason { margin-top: var(--space-3); padding: var(--space-3); border: 1px solid rgba(var(--color-primary-rgb), .2); border-radius: var(--radius-md); background: rgba(var(--color-primary-rgb), .05); overflow-wrap: anywhere; font-size: var(--text-xs); line-height: 1.6; }
.grocery-reason p { margin: 0; }
.grocery-reason__top, .grocery-reason__product { display: flex; align-items: baseline; justify-content: space-between; flex-wrap: wrap; gap: var(--space-1) var(--space-2); }
.grocery-reason__store { display: inline-flex; gap: var(--space-1); align-items: center; font-weight: 600; color: var(--color-primary-dark); }
.grocery-reason__saving { color: var(--color-primary-dark); font-weight: 700; }
.grocery-reason__product { margin: var(--space-2) 0; color: var(--color-text-primary); font-size: var(--text-sm); }
.grocery-reason__product > span { flex: 1; min-width: 100px; }
.grocery-reason__product strong { white-space: nowrap; font-size: var(--text-base); font-variant-numeric: tabular-nums; }
.grocery-reason__deadline { font-weight: 600; color: var(--color-text-secondary); }
.grocery-reason__details { margin-top: var(--space-2); }
.grocery-reason__details summary { display: inline-flex; gap: var(--space-1); align-items: center; color: var(--color-text-secondary); cursor: pointer; list-style: none; min-height: 32px; }
.grocery-reason__details summary::-webkit-details-marker { display: none; }
.grocery-reason__details[open] summary .v-icon { transform: rotate(180deg); }
.grocery-reason__explanation { display: grid; gap: var(--space-2); padding: var(--space-2) 0; }
.grocery-reason--expired { background: var(--color-surface-variant); border-color: var(--color-border-medium); }
.grocery-reason__muted { color: var(--color-text-muted); }
.grocery-reason__source { color: var(--color-primary-dark); display: inline-block; margin-top: var(--space-1); font-weight: 600; }
.grocery-reason__source:focus-visible, summary:focus-visible { outline: 2px solid var(--color-primary-dark); outline-offset: 3px; }
</style>

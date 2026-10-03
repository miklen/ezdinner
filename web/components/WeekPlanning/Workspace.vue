<script setup lang="ts">
import { computed, nextTick, onActivated, shallowRef, watch } from 'vue'
import { DateTime } from 'luxon'
import type { Dish, DishStats, WishlistItem } from '~/types'
import type { PlanningDish } from '~/types/week-planning'
import { useWeekPlanning } from '~/composables/useWeekPlanning'
import { useDishExploration } from '~/composables/useDishExploration'
import { useDishRecommendations } from '~/composables/useDishRecommendations'
import { useDishExplorationPreferencesStore } from '~/stores/dishExplorationPreferences'
import { defaultExplorationPreferences } from '~/utils/dish-exploration-preferences'
import DishExplorer from './DishExplorer.vue'
import RecommendationExplorer from './RecommendationExplorer.vue'
import PlanningWeekOverview from './PlanningWeekOverview.vue'
import PlanningAssignmentPicker from './PlanningAssignmentPicker.vue'

const app = useAppStore()
const { $msal } = useNuxtApp()
const repository = useRepositories()
const { t, locale } = useI18n()
const family = computed(() => app.activeFamilyId)
const account = computed(() => $msal.isAuthenticated.value ? ($msal.getObjectId() ?? '') : '')
const dishes = shallowRef<Dish[]>([])
const stats = shallowRef<Record<string, DishStats>>({})
const wishes = shallowRef<WishlistItem[]>([])
const catalogError = shallowRef(false)
const catalogLoading = shallowRef(false)
const mobileOverview = shallowRef(false)
const planning = useWeekPlanning({ family, account, dishes, unavailableDishLabel: computed(() => t('weekPlanning.dishUnavailable')), dinners: repository.dinners, recommendations: repository.dishRecommendations })
const recommendations = useDishRecommendations({ family, account, monday: planning.monday, target: planning.selectedDate, locale, repository: repository.dishRecommendations })
const entries = computed<PlanningDish[]>(() => dishes.value.filter(dish => !dish.isArchived).map(dish => ({ dish, stats: stats.value[dish.id], wishVotes: wishes.value.find(wish => wish.dishId === dish.id)?.voteCount })))
const explorationStore = useDishExplorationPreferencesStore()
const explorationScope = computed(() => account.value && family.value ? JSON.stringify([account.value, family.value]) : null)
const anonymousPreferences = shallowRef(defaultExplorationPreferences())
watch(explorationScope, scope => {
  anonymousPreferences.value = defaultExplorationPreferences()
  if (scope) explorationStore.restore(scope)
}, { immediate: true, flush: 'sync' })
const explorationPreferences = computed({
  get: () => explorationScope.value ? explorationStore.entries[explorationScope.value] : anonymousPreferences.value,
  set: value => {
    if (explorationScope.value) explorationStore.update(explorationScope.value, value)
    else anonymousPreferences.value = value
  },
})
const exploration = useDishExploration(entries, locale, explorationPreferences)
const cuisines = computed(() => [...new Set(dishes.value.map(dish => dish.cuisine).filter((cuisine): cuisine is string => !!cuisine))].sort())
const suggested = computed<PlanningDish[]>(() => recommendations.visible.value.flatMap(recommendation => {
  const entry = entries.value.find(entry => entry.dish.id === recommendation.dishId)
  return entry ? [{ ...entry, recommendation }] : []
}))
const planned = computed(() => {
  const assigned: Record<string, string[]> = {}
  for (const day of planning.days.value) for (const item of day.menu) assigned[item.dishId] = [...(assigned[item.dishId] ?? []), day.date]
  return assigned
})
const weekLabel = computed(() => DateTime.fromISO(planning.monday.value).setLocale(locale.value).toFormat('d MMM yyyy'))
const pickerDish = shallowRef<Dish | null>(null)
const optOutConfirmation = shallowRef<{ dishId: string; date: string; reason: string } | null>(null)
let catalogGeneration = 0
let activated = false
let recipeReturn = false

async function loadCatalog(refreshIdeas = true) {
  const currentFamily = family.value
  const token = ++catalogGeneration
  if (!currentFamily) return
  catalogLoading.value = true
  catalogError.value = false
  try {
    const [catalog, usage, activeWishes] = await Promise.all([repository.dishes.all(currentFamily, true), repository.dishes.allUsageStats(currentFamily, DateTime.utc()), repository.wishlist.getWishlist(currentFamily)])
    if (token !== catalogGeneration) return
    dishes.value = catalog
    stats.value = usage
    wishes.value = activeWishes
    if (refreshIdeas) await recommendations.submit('automatic')
  }
  catch { if (token === catalogGeneration) catalogError.value = true }
  finally { if (token === catalogGeneration) catalogLoading.value = false }
}

function openPicker(id: string) { exploration.rememberPosition(); pickerDish.value = dishes.value.find(dish => dish.id === id) ?? null }
function rememberRecipeLink(event: MouseEvent) {
  if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return
  const link = event.target instanceof Element ? event.target.closest('a') : null
  if (!(link instanceof HTMLAnchorElement) || !link.getAttribute('href')?.startsWith('/dishes/')) return
  exploration.rememberPosition(link)
  recipeReturn = true
}
async function assign(id: string, date: string | null = planning.selectedDate.value, confirmed = false) {
  if (!date) { openPicker(id); return }
  const reason = planning.days.value.find(day => day.date === date)?.optOutReason
  if (reason && !confirmed) { optOutConfirmation.value = { dishId: id, date, reason }; return }
  const outcome = await planning.mutate('added', id, date, confirmed)
  if (outcome === 'Changed') wishes.value = wishes.value.filter(wish => wish.dishId !== id)
}
async function confirmOptOut() { const confirmation = optOutConfirmation.value; if (!confirmation) return; optOutConfirmation.value = null; await assign(confirmation.dishId, confirmation.date, true) }
function resetRequest() { recommendations.reset(); void recommendations.submit('automatic') }

watch([family, account], () => {
  recipeReturn = false
  dishes.value = []
  stats.value = {}
  wishes.value = []
  pickerDish.value = null
  optOutConfirmation.value = null
  mobileOverview.value = false
  void loadCatalog()
}, { immediate: true })
watch(planning.monday, () => { pickerDish.value = null; optOutConfirmation.value = null })
onActivated(async () => {
  if (!activated) { activated = true; return }
  await Promise.all([planning.refresh(), loadCatalog(false)])
  await nextTick()
  if (recipeReturn) { exploration.restorePosition(false); recipeReturn = false }
})
onScopeDispose(() => { catalogGeneration++ })
</script>

<template>
  <div class="planning-workspace" @click.capture="rememberRecipeLink">
    <h1 class="text-page-title">{{ t('weekPlanning.title') }}</h1>
    <p class="workspace-intro">{{ t('weekPlanning.intro') }}</p>
    <v-btn class="mobile-week-control" block variant="tonal" color="primary" :aria-expanded="mobileOverview" @click="mobileOverview = !mobileOverview">{{ t('weekPlanning.weekOf', { date: weekLabel }) }} · {{ t('weekPlanning.overview') }}</v-btn>
    <div class="workspace-layout">
      <main class="workspace-exploration">
        <v-alert v-if="catalogError" type="error">{{ t('weekPlanning.catalogFailed') }} <v-btn @click="loadCatalog()">{{ t('weekPlanning.retry') }}</v-btn></v-alert>
        <v-progress-linear v-if="catalogLoading" indeterminate :aria-label="t('weekPlanning.loading')" />
        <RecommendationExplorer :key="explorationScope" :state="recommendations.state.value" :entries="suggested" :constraints="recommendations.constraints.value" :role="recommendations.role.value" :name-filter="recommendations.nameFilter.value" :scope-decision-required="recommendations.scopeDecisionRequired.value" :stale="recommendations.staleContext.value" :scoped-day="recommendations.scopedDay.value" :planned="planned" :target="planning.selectedDate.value" :busy="!planning.canMutate.value" @ask="recommendations.submit('request', $event)" @more="recommendations.submit('more')" @refresh="recommendations.submit(recommendations.constraints.value.length ? 'request' : 'automatic')" @retry="recommendations.retry" @reset="resetRequest" @constraints="recommendations.constraints.value = $event" @role="recommendations.role.value = $event" @name-filter="recommendations.nameFilter.value = $event" @scope="recommendations.decideScope" @assign="assign" @other-date="openPicker" @dismiss="recommendations.dismiss" />
        <DishExplorer :entries="exploration.visible.value" :preferences="exploration.preferences.value" :planned="planned" :target="planning.selectedDate.value" :busy="!planning.canMutate.value" :cuisines="cuisines" @update="exploration.update" @assign="assign" @other-date="openPicker" />
      </main>
      <aside class="workspace-week" :class="{ 'workspace-week--open': mobileOverview }">
        <v-progress-linear v-if="planning.loading.value" indeterminate :aria-label="t('weekPlanning.loading')" />
        <v-alert v-if="planning.error.value" type="error" role="alert">{{ t(`weekPlanning.${planning.error.value}`) }} <v-btn @click="planning.refresh">{{ t('weekPlanning.refreshWeek') }}</v-btn></v-alert>
        <PlanningWeekOverview :monday="planning.monday.value" :days="planning.days.value" :selected-date="planning.selectedDate.value" :busy="!planning.canMutate.value" @navigate="planning.navigate" @select="planning.selectedDate.value = $event" @remove="(id, date) => planning.mutate('removed', id, date)" />
      </aside>
    </div>
    <div v-if="planning.feedback.value || planning.notice.value" class="workspace-feedback" role="status">
      <template v-if="planning.feedback.value">{{ t(`weekPlanning.${planning.feedback.value.kind}`, { name: dishes.find(dish => dish.id === planning.feedback.value?.dishId)?.name, date: DateTime.fromISO(planning.feedback.value.date).setLocale(locale).toFormat('ccc d/M') }) }} <v-btn :disabled="planning.busy.value" variant="text" @click="planning.undo">{{ t('weekPlanning.undo') }}</v-btn></template>
      <template v-else-if="planning.notice.value">{{ t(`weekPlanning.${planning.notice.value}`) }}</template>
    </div>
    <PlanningAssignmentPicker :open="!!pickerDish" :dish-id="pickerDish?.id ?? ''" :dish-name="pickerDish?.name ?? ''" :days="planning.days.value" :busy="!planning.canMutate.value" :error="planning.error.value" @close="pickerDish = null" @assign="pickerDish && assign(pickerDish.id, $event)" />
    <v-dialog :model-value="!!optOutConfirmation" max-width="460" @update:model-value="!$event && (optOutConfirmation = null)">
      <v-card :title="t('weekPlanning.clearOptOutTitle')"><v-card-text>{{ t('weekPlanning.clearOptOutWarning', { reason: optOutConfirmation?.reason }) }}</v-card-text><v-card-actions><v-btn @click="optOutConfirmation = null">{{ t('weekPlanning.cancel') }}</v-btn><v-btn color="primary" @click="confirmOptOut">{{ t('weekPlanning.confirmAdd') }}</v-btn></v-card-actions></v-card>
    </v-dialog>
  </div>
</template>

<style scoped>
.planning-workspace { padding: var(--space-6) var(--space-4); }
.workspace-intro { margin-top: var(--space-2); color: var(--color-text-secondary); margin-bottom: var(--space-4); }
.workspace-layout { display: grid; grid-template-columns: minmax(0, 1fr) minmax(320px, 36%); gap: var(--space-6); align-items: start; }
.workspace-exploration { min-width: 0; }
.workspace-week { position: sticky; top: 90px; max-height: calc(100dvh - 110px); overflow-y: auto; }
.mobile-week-control { display: none; }
.workspace-feedback { position: fixed; bottom: 80px; left: 50%; transform: translateX(-50%); z-index: 2000; background: var(--color-surface); padding: var(--space-2) var(--space-4); border: 1px solid var(--color-primary); border-radius: var(--radius-lg); box-shadow: var(--shadow-lg); max-width: 90vw; }
@media (max-width: 959px) {
  .workspace-layout { display: flex; flex-direction: column; gap: var(--space-3); }
  .workspace-exploration { width: 100%; order: 2; }
  .mobile-week-control { display: flex; position: sticky; top: 56px; z-index: 5; background-color: var(--color-surface); margin-bottom: var(--space-3); white-space: normal; height: auto; min-height: 48px; }
  .workspace-week { display: none; position: static; max-height: none; width: 100%; order: 1; }
  .workspace-week--open { display: block; }
}
</style>

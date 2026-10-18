import { computed, onScopeDispose, shallowRef, watch } from 'vue'
import type { Ref } from 'vue'
import type { DishRole } from '~/types'
import type { DishRecommendationsRepository } from '~/repository/dish-recommendations-repository'
import type { RecommendedDish, RecommendationRequest, RecommendationState } from '~/types/dish-recommendations'
import { combineGroceryCoverage, unknownGroceryCoverage } from '~/types/grocery-offers'
import type { GroceryCoverage } from '~/types/grocery-offers'

export function useDishRecommendations(options: { family: Ref<string>; account: Ref<string>; monday: Ref<string>; target: Ref<string | null>; locale: Ref<string>; storeIds?: Ref<string[] | null>; repository: DishRecommendationsRepository }) {
  const state = shallowRef<RecommendationState>({ status: 'idle', result: null })
  const accumulatedDishes = shallowRef<RecommendedDish[]>([])
  const constraints = shallowRef<string[]>([])
  const exclusions = shallowRef<string[]>([])
  const role = shallowRef<DishRole | null>(null)
  const nameFilter = shallowRef('')
  const scopedDay = shallowRef<string | null>(null)
  const scoped = shallowRef(false)
  const resultMonday = shallowRef<string | null>(null)
  const resultLocale = shallowRef<string | null>(null)
  const selectionChanged = shallowRef(false)
  const coverage = shallowRef<GroceryCoverage>(unknownGroceryCoverage())
  const staleLocale = computed(() => resultLocale.value !== null && resultLocale.value !== options.locale.value)
  const refreshRequired = computed(() => staleLocale.value || selectionChanged.value)
  const scopeDecisionRequired = computed(() => scoped.value && scopedDay.value !== options.target.value)
  const staleContext = computed(() => resultMonday.value !== null && resultMonday.value !== options.monday.value)
  const visible = computed(() => accumulatedDishes.value.filter(dish => !dismissed.value.includes(dish.dishId)))
  const dismissed = shallowRef<string[]>([])
  let generation = 0
  let controller: AbortController | null = null
  let retryIntent: RecommendationRequest | null = null
  let resultQuery: string | null = null

  function cancel() { generation++; controller?.abort(); controller = null; if (state.value.status === 'loading') state.value = { status: 'idle', result: state.value.result } }
  function reset() {
    cancel()
    state.value = { status: 'idle', result: null }
    accumulatedDishes.value = []
    resultQuery = null
    constraints.value = []
    exclusions.value = []
    dismissed.value = []
    scopedDay.value = null
    scoped.value = false
    resultMonday.value = null
    resultLocale.value = null
    selectionChanged.value = false
    coverage.value = unknownGroceryCoverage()
    role.value = null
    nameFilter.value = ''
    retryIntent = null
  }
  function decideScope(choice: 'keep' | 'new') {
    if (choice === 'new') reset()
    scopedDay.value = options.target.value
    scoped.value = options.target.value !== null
  }
  function dismiss(id: string) { dismissed.value = [...dismissed.value, id]; exclusions.value = [...new Set([...exclusions.value, id])] }
  async function submit(mode: RecommendationRequest['mode'], text = '') {
    if (scopeDecisionRequired.value || !options.family.value) return
    if (mode === 'more' && refreshRequired.value) return
    if (mode === 'request' && text.trim()) constraints.value = [...constraints.value, text.trim()]
    const locale = options.locale.value
    if (locale !== 'en' && locale !== 'da') throw new Error('INVALID_LOCALE')
    if (!scoped.value && options.target.value !== null && mode === 'request') { scoped.value = true; scopedDay.value = options.target.value }
    const intent: RecommendationRequest = { selectedMonday: options.monday.value, targetDate: scoped.value ? scopedDay.value : null,
      mode, locale, turns: [], constraints: [...constraints.value], excludedDishIds: [...exclusions.value],
      role: mode === 'automatic' || (mode === 'more' && !constraints.value.length) ? (role.value ?? 'Main') : role.value,
      nameFilter: nameFilter.value }
    await perform(intent)
  }
  async function perform(intent: RecommendationRequest) {
    cancel()
    const { mode, excludedDishIds, ...query } = intent
    const queryKey = JSON.stringify({ ...query, selectedRole: role.value })
    const append = mode === 'more' && resultQuery === queryKey
    intent = { ...intent, excludedDishIds: append ? [...new Set([...excludedDishIds, ...accumulatedDishes.value.map(dish => dish.dishId)])] : [] }
    const token = generation
    const family = options.family.value
    const storesKnownAtRequest = options.storeIds?.value != null
    const previous = state.value.result
    controller = new AbortController()
    retryIntent = intent
    state.value = { status: 'loading', result: previous }
    try {
      const result = await options.repository.recommend(family, intent, controller.signal)
      if (token !== generation) return
      const returnedCoverage = result.groceryCoverage ?? unknownGroceryCoverage()
      if (append && JSON.stringify(coverage.value.storeIds) !== JSON.stringify(returnedCoverage.storeIds)) {
        selectionChanged.value = true
        state.value = { status: 'idle', result: previous }
        return
      }
      const retained = append ? accumulatedDishes.value : []
      const retainedIds = new Set(retained.map(dish => dish.dishId))
      if (!append) dismissed.value = []
      accumulatedDishes.value = [...retained, ...result.dishes.filter(dish => !retainedIds.has(dish.dishId))]
      resultQuery = queryKey
      state.value = { status: 'ready', result }
      coverage.value = append ? combineGroceryCoverage(coverage.value, returnedCoverage) : returnedCoverage
      resultLocale.value = intent.locale
      const loadedStores = options.storeIds?.value
      selectionChanged.value = !storesKnownAtRequest && loadedStores != null
        && JSON.stringify([...loadedStores].sort()) !== JSON.stringify(returnedCoverage.storeIds)
      resultMonday.value = intent.selectedMonday
      exclusions.value = [...new Set([...(append ? exclusions.value : []), ...result.dishes.map(dish => dish.dishId)])]
    }
    catch { if (token === generation) state.value = { status: 'failed', result: previous, error: 'recommendationFailed' } }
  }
  async function retry() {
    if (!retryIntent || scopeDecisionRequired.value || !options.family.value) return
    if (retryIntent.mode === 'more' && refreshRequired.value) return
    const locale = options.locale.value
    if (locale !== 'en' && locale !== 'da') throw new Error('INVALID_LOCALE')
    const mode = retryIntent.mode
    await perform({ ...retryIntent, selectedMonday: options.monday.value,
      targetDate: scoped.value ? scopedDay.value : null, locale, constraints: [...constraints.value],
      role: mode === 'automatic' || (mode === 'more' && !constraints.value.length) ? (role.value ?? 'Main') : role.value,
      nameFilter: nameFilter.value,
      excludedDishIds: [...new Set([...retryIntent.excludedDishIds, ...exclusions.value])] })
  }
  watch([options.family, options.account], reset)
  watch(options.monday, cancel)
  watch(options.locale, cancel, { flush: 'sync' })
  if (options.storeIds) watch(() => JSON.stringify(options.storeIds?.value?.slice().sort()), (selection, previous) => {
    if (selection === previous || selection === undefined) return
    if (previous === undefined && (resultLocale.value === null || selection === JSON.stringify(coverage.value.storeIds))) return
    cancel()
    if (resultLocale.value !== null) selectionChanged.value = true
  }, { flush: 'sync' })
  watch(options.target, cancel)
  onScopeDispose(cancel)
  return { state, constraints, exclusions, role, nameFilter, scopedDay, scopeDecisionRequired, staleContext, staleLocale, selectionChanged, refreshRequired, coverage, visible, submit, retry, reset, decideScope, dismiss }
}

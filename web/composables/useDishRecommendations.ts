import { computed, onScopeDispose, shallowRef, watch } from 'vue'
import type { Ref } from 'vue'
import type { DishRole } from '~/types'
import type { DishRecommendationsRepository } from '~/repository/dish-recommendations-repository'
import type { RecommendationRequest, RecommendationState } from '~/types/dish-recommendations'

export function useDishRecommendations(options: { family: Ref<string>; account: Ref<string>; monday: Ref<string>; target: Ref<string | null>; locale: Ref<string>; repository: DishRecommendationsRepository }) {
  const state = shallowRef<RecommendationState>({ status: 'idle', result: null })
  const constraints = shallowRef<string[]>([])
  const exclusions = shallowRef<string[]>([])
  const role = shallowRef<DishRole | null>(null)
  const nameFilter = shallowRef('')
  const scopedDay = shallowRef<string | null>(null)
  const scoped = shallowRef(false)
  const resultMonday = shallowRef<string | null>(null)
  const scopeDecisionRequired = computed(() => scoped.value && scopedDay.value !== options.target.value)
  const staleContext = computed(() => resultMonday.value !== null && resultMonday.value !== options.monday.value)
  const visible = computed(() => (state.value.result?.dishes ?? []).filter(dish => !dismissed.value.includes(dish.dishId)))
  const dismissed = shallowRef<string[]>([])
  let generation = 0
  let controller: AbortController | null = null
  let retryIntent: RecommendationRequest | null = null

  function cancel() { generation++; controller?.abort(); controller = null; if (state.value.status === 'loading') state.value = { status: 'idle', result: state.value.result } }
  function reset() {
    cancel()
    state.value = { status: 'idle', result: null }
    constraints.value = []
    exclusions.value = []
    dismissed.value = []
    scopedDay.value = null
    scoped.value = false
    resultMonday.value = null
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
    if (mode === 'request' && text.trim()) constraints.value = [...constraints.value, text.trim()]
    if (mode === 'request') { exclusions.value = []; dismissed.value = [] }
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
    const token = generation
    const family = options.family.value
    const previous = state.value.result
    controller = new AbortController()
    retryIntent = intent
    state.value = { status: 'loading', result: previous }
    try {
      const result = await options.repository.recommend(family, intent, controller.signal)
      if (token !== generation) return
      state.value = { status: 'ready', result }
      resultMonday.value = intent.selectedMonday
      exclusions.value = [...new Set([...exclusions.value, ...result.dishes.map(dish => dish.dishId)])]
    }
    catch { if (token === generation) state.value = { status: 'failed', result: previous, error: 'recommendationFailed' } }
  }
  async function retry() {
    if (!retryIntent || scopeDecisionRequired.value || !options.family.value) return
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
  watch(options.locale, () => {
    cancel()
    state.value = { status: 'idle', result: null }
    resultMonday.value = null
  })
  watch(options.target, cancel)
  onScopeDispose(cancel)
  return { state, constraints, exclusions, role, nameFilter, scopedDay, scopeDecisionRequired, staleContext, visible, submit, retry, reset, decideScope, dismiss }
}

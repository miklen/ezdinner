<script setup lang="ts">
import { DateTime } from 'luxon'
import type { Dinner, Dish, EffortLevel } from '~/types'

interface AiSuggestion {
  date: string        // original suggested date from the AI
  targetDate: string  // actual date to assign — starts as date, user can change it
  dishId: string
  dishName: string
  reason: string
  accepted?: boolean
  skipped?: boolean
}

const props = defineProps<{
  weekStart: DateTime
  dinners: Dinner[]
}>()

const emit = defineEmits<{
  'dish:assigned': [date: string, dishId: string]
}>()

const appStore = useAppStore()
const dishesStore = useDishesStore()
const wishlistStore = useWishlistStore()
const { dinners: dinnerRepo } = useRepositories()
const { show: showSnackbar } = useSnackbar()
const { t, locale } = useI18n()

// ─── Mode toggle ─────────────────────────────────────────────────────────────
type Mode = 'plan' | 'wishlist'
const mode = ref<Mode>('wishlist')

const wishCount = computed(() => wishlistStore.wishes.length)

// ─── Dish list ────────────────────────────────────────────────────────────────
const search = ref('')
const effortFilter = ref<EffortLevel | null>(null)

const effortOptions = computed<Array<{ label: string; value: EffortLevel }>>(() => [
  { label: t('plan.effort.quick'), value: 'Quick' },
  { label: t('plan.effort.medium'), value: 'Medium' },
  { label: t('plan.effort.elaborate'), value: 'Elaborate' },
])

const filteredDishes = computed<Dish[]>(() => {
  const q = search.value.toLowerCase().trim()
  let result = dishesStore.dishes.filter((d) => !d.isArchived)

  if (q) result = result.filter((d) => d.name.toLowerCase().includes(q))

  if (effortFilter.value) {
    result = result.filter((d) => d.effortLevel === effortFilter.value)
  }

  return [...result].sort((a, b) => {
    const aLastUsed = a.dishStats?.lastUsed
    const bLastUsed = b.dishStats?.lastUsed
    const aLast = aLastUsed instanceof DateTime ? aLastUsed.toMillis() : (aLastUsed ? DateTime.fromISO(aLastUsed as unknown as string).toMillis() : null)
    const bLast = bLastUsed instanceof DateTime ? bLastUsed.toMillis() : (bLastUsed ? DateTime.fromISO(bLastUsed as unknown as string).toMillis() : null)
    if (aLast === null && bLast === null) return 0
    if (aLast === null) return -1
    if (bLast === null) return 1
    return aLast - bLast
  })
})

function onDishAssigned(date: string, dishId: string) {
  emit('dish:assigned', date, dishId)
}

// ─── AI Week Planner ──────────────────────────────────────────────────────────
const aiContext = ref('')
const aiLoading = ref(false)
const aiDraft = ref<AiSuggestion[] | null>(null)
const aiError = ref(false)
const acceptingAll = ref(false)

// Dish IDs the user has skipped — persists across re-suggestions within the session
const skippedDishIds = ref<Set<string>>(new Set())

// Which suggestion row has its date-picker open (keyed by original date)
const activePickerDate = ref<string | null>(null)

// 7 day slots for the current week (used in date reassignment picker)
const weekDays = computed(() =>
  Array.from({ length: 7 }, (_, i) => props.weekStart.plus({ days: i })),
)

function weekDayLabel(day: DateTime): string {
  return day.setLocale(locale.value).toFormat('EEE d')
}

function targetDayLabel(isoDate: string): string {
  return DateTime.fromISO(isoDate).setLocale(locale.value).toFormat('EEE d')
}

async function planWithAi() {
  if (aiLoading.value) return
  aiLoading.value = true
  aiError.value = false
  aiDraft.value = null
  activePickerDate.value = null
  try {
    const excluded = skippedDishIds.value.size > 0 ? [...skippedDishIds.value] : undefined
    const result = await dinnerRepo.aiWeekPlan(
      appStore.activeFamilyId,
      props.weekStart.toFormat('yyyy-MM-dd'),
      aiContext.value.trim() || undefined,
      excluded,
    )
    aiDraft.value = (result as AiSuggestion[]).map((s) =>
      reactive({ ...s, targetDate: s.date, accepted: false, skipped: false }),
    )
  } catch {
    aiError.value = true
    showSnackbar(t('assistant.aiError'), { type: 'error' })
  } finally {
    aiLoading.value = false
  }
}

async function acceptSuggestion(suggestion: AiSuggestion) {
  const day = DateTime.fromISO(suggestion.targetDate)
  await dinnerRepo.addDishToMenu(appStore.activeFamilyId, day, suggestion.dishId)
  emit('dish:assigned', suggestion.targetDate, suggestion.dishId)
  suggestion.accepted = true
  if (activePickerDate.value === suggestion.date) activePickerDate.value = null
}

function skipSuggestion(suggestion: AiSuggestion) {
  suggestion.skipped = true
  skippedDishIds.value = new Set([...skippedDishIds.value, suggestion.dishId])
  if (activePickerDate.value === suggestion.date) activePickerDate.value = null
}

function toggleDatePicker(suggestion: AiSuggestion) {
  activePickerDate.value = activePickerDate.value === suggestion.date ? null : suggestion.date
}

function reassignDay(suggestion: AiSuggestion, isoDay: string) {
  suggestion.targetDate = isoDay
  activePickerDate.value = null
}

async function acceptAll() {
  if (acceptingAll.value || !aiDraft.value) return
  acceptingAll.value = true
  try {
    const pending = aiDraft.value.filter((s) => !s.skipped && !s.accepted)
    for (const s of pending) {
      await acceptSuggestion(s)
    }
  } finally {
    acceptingAll.value = false
  }
}

const hasPendingSuggestions = computed(() =>
  aiDraft.value?.some((s) => !s.skipped && !s.accepted) ?? false,
)

// ─── Effort badge helpers ─────────────────────────────────────────────────────
function effortLabel(level: EffortLevel | null | undefined): string | null {
  if (!level) return null
  const map: Record<EffortLevel, string> = {
    Quick: t('plan.effort.quick'),
    Medium: t('plan.effort.medium'),
    Elaborate: t('plan.effort.elaborate'),
  }
  return map[level] ?? null
}

function effortClass(level: EffortLevel | null | undefined): string | null {
  if (!level) return null
  const map: Record<EffortLevel, string> = {
    Quick: 'effort--quick',
    Medium: 'effort--medium',
    Elaborate: 'effort--elaborate',
  }
  return map[level] ?? null
}
</script>

<template>
  <div class="ap">
    <!-- ─── Mode toggle ─────────────────────────────────────────────────────── -->
    <div class="ap__tabs">
      <button
        class="ap__tab"
        :class="{ 'ap__tab--active': mode === 'wishlist' }"
        @click="mode = 'wishlist'"
      >
        {{ $t('assistant.wishlist') }}
        <span v-if="wishCount > 0" class="ap__tab-badge">{{ wishCount }}</span>
      </button>
      <button
        class="ap__tab"
        :class="{ 'ap__tab--active': mode === 'plan' }"
        @click="mode = 'plan'"
      >
        {{ $t('assistant.plan') }}
      </button>
    </div>

    <!-- ─── Plan mode ──────────────────────────────────────────────────────── -->
    <template v-if="mode === 'plan'">
      <!-- Search + effort filter -->
      <div class="ap__filters">
        <div class="ap__search">
          <v-icon size="14" class="ap__search-icon">mdi-magnify</v-icon>
          <input
            v-model="search"
            class="ap__search-input"
            :placeholder="$t('assistant.search')"
            type="search"
          >
        </div>
        <div class="ap__effort-btns">
          <button
            v-for="opt in effortOptions"
            :key="opt.value"
            class="ap__effort-btn"
            :class="{
              'ap__effort-btn--active': effortFilter === opt.value,
              [`ap__effort-btn--${opt.value.toLowerCase()}`]: effortFilter === opt.value,
            }"
            @click="effortFilter = effortFilter === opt.value ? null : opt.value"
          >
            {{ opt.label }}
          </button>
        </div>
      </div>

      <!-- ─── AI Week Planner ──────────────────────────────────────────── -->
      <div class="ap__ai">
        <!-- Context input + trigger — always visible unless loading -->
        <div v-if="!aiLoading" class="ap__ai-trigger">
          <input
            v-model="aiContext"
            class="ap__ai-context"
            :placeholder="$t('assistant.aiContextPlaceholder')"
            maxlength="200"
          >
          <div class="ap__ai-trigger-actions">
            <button
              v-if="!aiDraft"
              class="ap__ai-btn"
              @click="planWithAi"
            >
              <v-icon size="13">mdi-auto-fix</v-icon>
              {{ $t('assistant.planWithAi') }}
            </button>
            <template v-else>
              <button
                v-if="hasPendingSuggestions"
                class="ap__ai-btn ap__ai-btn--secondary"
                @click="planWithAi"
              >
                <v-icon size="13">mdi-refresh</v-icon>
                {{ $t('assistant.resuggestRemaining') }}
              </button>
            </template>
          </div>
        </div>

        <div v-if="aiLoading" class="ap__ai-loading">
          <v-progress-circular size="14" width="2" indeterminate color="primary" />
          <span>{{ $t('assistant.aiLoading') }}</span>
        </div>

        <template v-if="aiDraft">
          <div class="ap__ai-header">
            <span class="ap__ai-label">
              <v-icon size="13">mdi-auto-fix</v-icon>
              {{ $t('assistant.aiDraft') }}
            </span>
            <div class="ap__ai-header-actions">
              <button
                v-if="hasPendingSuggestions"
                class="ap__ai-accept-all"
                :disabled="acceptingAll"
                @click="acceptAll"
              >
                {{ $t('assistant.acceptAll') }}
              </button>
              <button class="ap__ai-close" @click="aiDraft = null; activePickerDate = null">
                <v-icon size="14">mdi-close</v-icon>
              </button>
            </div>
          </div>

          <!-- Draft rows — click outside a picker to close it -->
          <div class="ap__ai-draft" @click="activePickerDate = null">
            <div
              v-for="suggestion in aiDraft"
              :key="suggestion.date"
              class="ap__ai-row"
              :class="{
                'ap__ai-row--accepted': suggestion.accepted,
                'ap__ai-row--skipped': suggestion.skipped,
              }"
              @click.stop
            >
              <div class="ap__ai-row-info">
                <!-- Clickable day chip — opens reassignment picker -->
                <button
                  class="ap__ai-date"
                  :class="{
                    'ap__ai-date--changed': suggestion.targetDate !== suggestion.date,
                    'ap__ai-date--open': activePickerDate === suggestion.date,
                  }"
                  :disabled="suggestion.accepted || suggestion.skipped"
                  @click.stop="toggleDatePicker(suggestion)"
                >
                  {{ targetDayLabel(suggestion.targetDate) }}
                  <v-icon v-if="!suggestion.accepted && !suggestion.skipped" size="9">mdi-chevron-down</v-icon>
                </button>

                <span class="ap__ai-dish">{{ suggestion.dishName }}</span>
                <span
                  v-if="dishesStore.dishes.find(d => d.id === suggestion.dishId)?.effortLevel"
                  class="ap__ai-effort"
                  :class="effortClass(dishesStore.dishes.find(d => d.id === suggestion.dishId)?.effortLevel)"
                >
                  {{ effortLabel(dishesStore.dishes.find(d => d.id === suggestion.dishId)?.effortLevel) }}
                </span>
              </div>

              <!-- Inline day reassignment picker -->
              <div
                v-if="activePickerDate === suggestion.date"
                class="ap__ai-day-picker"
                @click.stop
              >
                <button
                  v-for="day in weekDays"
                  :key="day.toFormat('yyyy-MM-dd')"
                  class="ap__ai-day-slot"
                  :class="{
                    'ap__ai-day-slot--selected': suggestion.targetDate === day.toFormat('yyyy-MM-dd'),
                    'ap__ai-day-slot--original': suggestion.date === day.toFormat('yyyy-MM-dd'),
                  }"
                  @click.stop="reassignDay(suggestion, day.toFormat('yyyy-MM-dd'))"
                >
                  {{ weekDayLabel(day) }}
                </button>
              </div>

              <p v-if="suggestion.reason" class="ap__ai-reason">{{ suggestion.reason }}</p>

              <div v-if="!suggestion.accepted && !suggestion.skipped" class="ap__ai-actions">
                <button class="ap__ai-accept" @click="acceptSuggestion(suggestion)">
                  <v-icon size="13">mdi-check</v-icon>
                  {{ $t('assistant.accept') }}
                </button>
                <button class="ap__ai-skip" @click="skipSuggestion(suggestion)">
                  {{ $t('assistant.skip') }}
                </button>
              </div>
              <div v-else-if="suggestion.accepted" class="ap__ai-status ap__ai-status--accepted">
                <v-icon size="12">mdi-check-circle</v-icon>
                {{ targetDayLabel(suggestion.targetDate) }}
              </div>
            </div>
          </div>
        </template>
      </div>

      <!-- Dish list -->
      <div class="ap__dish-list">
        <div v-if="filteredDishes.length === 0" class="ap__empty">
          {{ $t('assistant.noDishes') }}
        </div>
        <PlanAssistantDishRow
          v-for="dish in filteredDishes"
          :key="dish.id"
          :dish="dish"
          :week-start="weekStart"
          :dinners="dinners"
          @dish:assigned="onDishAssigned"
        />
      </div>
    </template>

    <!-- ─── Wishlist mode ──────────────────────────────────────────────────── -->
    <template v-else>
      <PlanWishList />
    </template>
  </div>
</template>

<style scoped>
.ap {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
  height: 100%;
  width: 100%;
  min-width: 0;
}

/* Mode tabs */
.ap__tabs {
  display: flex;
  gap: 2px;
  background: rgba(0, 0, 0, 0.05);
  border-radius: var(--radius-lg);
  padding: 3px;
}

.ap__tab {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-1);
  padding: 5px var(--space-3);
  border-radius: var(--radius-md);
  border: none;
  background: none;
  font-family: var(--font-body);
  font-size: var(--text-xs);
  font-weight: 600;
  color: var(--color-text-muted);
  cursor: pointer;
  transition:
    background var(--duration-fast) var(--ease-standard),
    color var(--duration-fast) var(--ease-standard);
}

.ap__tab--active {
  background: var(--color-surface);
  color: var(--color-text-primary);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.ap__tab-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 16px;
  height: 16px;
  padding: 0 4px;
  border-radius: var(--radius-full);
  background: var(--color-primary);
  color: white;
  font-size: 10px;
  font-weight: 700;
  line-height: 1;
}

/* Filters */
.ap__filters {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.ap__search {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  background: rgba(0, 0, 0, 0.04);
  border: 1px solid rgba(0, 0, 0, 0.08);
  border-radius: var(--radius-md);
  padding: 5px var(--space-3);
}

.ap__search-icon {
  color: var(--color-text-muted) !important;
  flex-shrink: 0;
}

.ap__search-input {
  flex: 1;
  background: none;
  border: none;
  outline: none;
  font-family: var(--font-body);
  font-size: var(--text-sm);
  color: var(--color-text-primary);
}

.ap__search-input::placeholder {
  color: var(--color-text-muted);
}

.ap__search-input::-webkit-search-cancel-button {
  cursor: pointer;
}

.ap__effort-btns {
  display: flex;
  gap: var(--space-1);
}

.ap__effort-btn {
  flex: 1;
  padding: 3px var(--space-2);
  border-radius: var(--radius-full);
  border: 1px solid rgba(0, 0, 0, 0.12);
  background: rgba(0, 0, 0, 0.03);
  font-family: var(--font-body);
  font-size: var(--text-xs);
  font-weight: 600;
  color: var(--color-text-secondary);
  cursor: pointer;
  transition:
    background var(--duration-fast) var(--ease-standard),
    border-color var(--duration-fast) var(--ease-standard),
    color var(--duration-fast) var(--ease-standard);
}

.ap__effort-btn:hover {
  background: rgba(0, 0, 0, 0.07);
}

.ap__effort-btn--active { border-color: transparent; }

.ap__effort-btn--quick.ap__effort-btn--active {
  background: rgba(80, 160, 90, 0.14);
  color: rgb(50, 130, 60);
  border-color: rgba(80, 160, 90, 0.3);
}

.ap__effort-btn--medium.ap__effort-btn--active {
  background: rgba(200, 150, 40, 0.14);
  color: rgb(160, 110, 20);
  border-color: rgba(200, 150, 40, 0.3);
}

.ap__effort-btn--elaborate.ap__effort-btn--active {
  background: rgba(var(--color-primary-rgb), 0.14);
  color: var(--color-primary);
  border-color: rgba(var(--color-primary-rgb), 0.35);
}

/* AI planner container */
.ap__ai {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  border: 1px solid rgba(var(--color-primary-rgb), 0.15);
  border-radius: var(--radius-md);
  padding: var(--space-3);
  background: linear-gradient(135deg, rgba(var(--color-primary-rgb), 0.03) 0%, rgba(var(--color-primary-rgb), 0.01) 100%);
}

.ap__ai-trigger {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.ap__ai-trigger-actions {
  display: flex;
  gap: var(--space-1);
  flex-wrap: wrap;
}

.ap__ai-context {
  width: 100%;
  background: rgba(0, 0, 0, 0.03);
  border: 1px solid rgba(0, 0, 0, 0.1);
  border-radius: var(--radius-sm);
  padding: 5px var(--space-2);
  font-family: var(--font-body);
  font-size: var(--text-xs);
  color: var(--color-text-primary);
  outline: none;
  transition: border-color var(--duration-fast) var(--ease-standard);
  box-sizing: border-box;
}

.ap__ai-context:focus {
  border-color: rgba(var(--color-primary-rgb), 0.4);
}

.ap__ai-context::placeholder {
  color: var(--color-text-muted);
}

.ap__ai-btn {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 5px 12px;
  border-radius: var(--radius-full);
  border: 1px solid rgba(var(--color-primary-rgb), 0.35);
  background: rgba(var(--color-primary-rgb), 0.08);
  color: rgba(var(--color-primary-rgb), 0.9);
  font-family: var(--font-body);
  font-size: var(--text-xs);
  font-weight: 600;
  cursor: pointer;
  transition: background var(--duration-fast), border-color var(--duration-fast);
}

.ap__ai-btn:hover:not(:disabled) {
  background: rgba(var(--color-primary-rgb), 0.14);
  border-color: rgba(var(--color-primary-rgb), 0.5);
}

.ap__ai-btn--secondary {
  background: rgba(0, 0, 0, 0.04);
  border-color: rgba(0, 0, 0, 0.15);
  color: var(--color-text-secondary);
}

.ap__ai-btn--secondary:hover:not(:disabled) {
  background: rgba(0, 0, 0, 0.08);
  border-color: rgba(0, 0, 0, 0.25);
}

.ap__ai-loading {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  font-size: var(--text-xs);
  color: var(--color-text-muted);
  font-style: italic;
}

.ap__ai-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-2);
}

.ap__ai-label {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: var(--text-xs);
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: rgba(var(--color-primary-rgb), 0.8);
}

.ap__ai-header-actions {
  display: flex;
  align-items: center;
  gap: var(--space-1);
}

.ap__ai-accept-all {
  padding: 3px 10px;
  border-radius: var(--radius-full);
  border: 1px solid rgba(var(--color-primary-rgb), 0.3);
  background: rgba(var(--color-primary-rgb), 0.08);
  color: var(--color-primary);
  font-family: var(--font-body);
  font-size: 10px;
  font-weight: 700;
  cursor: pointer;
  transition: background var(--duration-fast);
}

.ap__ai-accept-all:hover:not(:disabled) {
  background: rgba(var(--color-primary-rgb), 0.15);
}

.ap__ai-close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  border: none;
  background: none;
  color: var(--color-text-muted);
  cursor: pointer;
  opacity: 0.7;
  transition: opacity var(--duration-fast);
}

.ap__ai-close:hover { opacity: 1; }

.ap__ai-draft {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.ap__ai-row {
  border-radius: var(--radius-sm);
  border: 1px solid rgba(0, 0, 0, 0.08);
  background: var(--color-surface);
  padding: 6px var(--space-2);
  transition: opacity var(--duration-fast);
}

.ap__ai-row--accepted { opacity: 0.5; }

.ap__ai-row--skipped {
  opacity: 0.3;
  text-decoration: line-through;
}

.ap__ai-row-info {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
  min-width: 0;
}

/* Day chip — clickable, opens reassignment picker */
.ap__ai-date {
  display: inline-flex;
  align-items: center;
  gap: 2px;
  padding: 2px 6px;
  border-radius: var(--radius-sm);
  border: 1px solid rgba(var(--color-primary-rgb), 0.2);
  background: rgba(var(--color-primary-rgb), 0.06);
  font-size: 10px;
  font-weight: 700;
  color: rgba(var(--color-primary-rgb), 0.75);
  letter-spacing: 0.05em;
  text-transform: uppercase;
  white-space: nowrap;
  cursor: pointer;
  transition: background var(--duration-fast), border-color var(--duration-fast);
  font-family: var(--font-body);
  flex-shrink: 0;
}

.ap__ai-date:hover:not(:disabled) {
  background: rgba(var(--color-primary-rgb), 0.12);
  border-color: rgba(var(--color-primary-rgb), 0.4);
}

.ap__ai-date:disabled {
  cursor: default;
  pointer-events: none;
}

/* Highlight when user has changed it from the original suggestion */
.ap__ai-date--changed {
  background: rgba(var(--color-primary-rgb), 0.12);
  border-color: rgba(var(--color-primary-rgb), 0.45);
  color: var(--color-primary);
}

.ap__ai-date--open {
  background: rgba(var(--color-primary-rgb), 0.14);
  border-color: rgba(var(--color-primary-rgb), 0.5);
}

/* Day reassignment picker */
.ap__ai-day-picker {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  padding: 6px 0 2px;
}

.ap__ai-day-slot {
  flex: 1;
  min-width: 36px;
  padding: 4px 4px;
  border-radius: var(--radius-sm);
  border: 1px solid rgba(0, 0, 0, 0.1);
  background: rgba(0, 0, 0, 0.02);
  font-family: var(--font-body);
  font-size: 10px;
  font-weight: 600;
  color: var(--color-text-secondary);
  letter-spacing: 0.02em;
  text-transform: uppercase;
  text-align: center;
  cursor: pointer;
  transition:
    background var(--duration-fast),
    border-color var(--duration-fast),
    color var(--duration-fast);
  white-space: nowrap;
}

.ap__ai-day-slot:hover {
  background: rgba(var(--color-primary-rgb), 0.08);
  border-color: rgba(var(--color-primary-rgb), 0.3);
  color: var(--color-primary);
}

/* Currently selected target day */
.ap__ai-day-slot--selected {
  background: rgba(var(--color-primary-rgb), 0.12);
  border-color: rgba(var(--color-primary-rgb), 0.45);
  color: var(--color-primary);
}

/* The AI's original suggestion day — subtle underline hint */
.ap__ai-day-slot--original:not(.ap__ai-day-slot--selected) {
  border-style: dashed;
}

.ap__ai-dish {
  flex: 1;
  font-size: var(--text-sm);
  font-weight: 500;
  color: var(--color-text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  min-width: 0;
}

.ap__ai-effort {
  font-size: 10px;
  font-weight: 700;
  border-radius: var(--radius-full);
  padding: 1px 5px;
  flex-shrink: 0;
}

.effort--quick {
  background: rgba(80, 160, 90, 0.12);
  color: rgb(50, 130, 60);
}

.effort--medium {
  background: rgba(200, 150, 40, 0.12);
  color: rgb(160, 110, 20);
}

.effort--elaborate {
  background: rgba(var(--color-primary-rgb), 0.12);
  color: var(--color-primary);
}

.ap__ai-reason {
  margin: 4px 0 0;
  font-size: 11px;
  color: var(--color-text-muted);
  font-style: italic;
  line-height: 1.4;
}

.ap__ai-actions {
  display: flex;
  align-items: center;
  gap: var(--space-1);
  margin-top: 5px;
}

.ap__ai-accept {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  padding: 2px 9px;
  border-radius: var(--radius-full);
  border: 1px solid rgba(var(--color-primary-rgb), 0.3);
  background: rgba(var(--color-primary-rgb), 0.08);
  color: var(--color-primary);
  font-family: var(--font-body);
  font-size: 10px;
  font-weight: 700;
  cursor: pointer;
  transition: background var(--duration-fast);
}

.ap__ai-accept:hover {
  background: rgba(var(--color-primary-rgb), 0.15);
}

.ap__ai-skip {
  padding: 2px 9px;
  border-radius: var(--radius-full);
  border: 1px solid rgba(0, 0, 0, 0.1);
  background: none;
  color: var(--color-text-muted);
  font-family: var(--font-body);
  font-size: 10px;
  font-weight: 600;
  cursor: pointer;
  transition: background var(--duration-fast);
}

.ap__ai-skip:hover {
  background: rgba(0, 0, 0, 0.06);
}

.ap__ai-status {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  margin-top: 4px;
  font-size: 10px;
  font-weight: 600;
}

.ap__ai-status--accepted {
  color: rgb(50, 130, 60);
}

/* Dish list */
.ap__dish-list {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  overscroll-behavior: contain;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.ap__empty {
  padding: var(--space-6) var(--space-4);
  text-align: center;
  font-size: var(--text-sm);
  color: var(--color-text-muted);
  font-style: italic;
}
</style>

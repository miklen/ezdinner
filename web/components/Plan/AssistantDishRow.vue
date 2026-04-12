<script setup lang="ts">
import { DateTime } from 'luxon'
import type { Dinner, Dish, EffortLevel } from '~/types'

const props = defineProps<{
  dish: Dish
  weekStart: DateTime
  dinners: Dinner[]
}>()

const emit = defineEmits<{
  'dish:assigned': [date: string, dishId: string]
}>()

const appStore = useAppStore()
const wishlistStore = useWishlistStore()
const { dinners: dinnerRepo } = useRepositories()
const { t, locale } = useI18n()

const pickerOpen = ref(false)
const assigning = ref<string | null>(null)
const rowEl = ref<HTMLElement | null>(null)

const wish = computed(() => wishlistStore.wishes.find((w) => w.dishId === props.dish.id) ?? null)

const daysSince = computed(() => {
  const lastUsed = props.dish.dishStats?.lastUsed
  if (!lastUsed) return null
  const ldt = lastUsed instanceof DateTime ? lastUsed : DateTime.fromISO(lastUsed as unknown as string)
  const days = Math.round(DateTime.now().diff(ldt, 'days').days)
  return days >= 0 ? days : null
})

const neverUsed = computed(() => !props.dish.dishStats?.lastUsed)

const effortLabel = computed<string | null>(() => {
  const level = props.dish.effortLevel as EffortLevel | null | undefined
  if (!level) return null
  const map: Record<EffortLevel, string> = {
    Quick: t('plan.effort.quick'),
    Medium: t('plan.effort.medium'),
    Elaborate: t('plan.effort.elaborate'),
  }
  return map[level] ?? null
})

const effortClass = computed<string | null>(() => {
  const level = props.dish.effortLevel as EffortLevel | null | undefined
  if (!level) return null
  const map: Record<EffortLevel, string> = {
    Quick: 'effort--quick',
    Medium: 'effort--medium',
    Elaborate: 'effort--elaborate',
  }
  return map[level] ?? null
})

// 7 day slots for the current week
const weekDays = computed(() =>
  Array.from({ length: 7 }, (_, i) => props.weekStart.plus({ days: i })),
)

function dayLabel(day: DateTime): string {
  return day.setLocale(locale.value).toFormat('EEE d')
}

function dishCountForDay(day: DateTime): number {
  const isoDay = day.toFormat('yyyy-MM-dd')
  const dinner = props.dinners.find((d) => d.date.toFormat('yyyy-MM-dd') === isoDay)
  return dinner?.menu.length ?? 0
}

function isDishOnDay(day: DateTime): boolean {
  const isoDay = day.toFormat('yyyy-MM-dd')
  const dinner = props.dinners.find((d) => d.date.toFormat('yyyy-MM-dd') === isoDay)
  return dinner?.menu.some((m) => m.dishId === props.dish.id) ?? false
}

async function assignToDay(day: DateTime) {
  const isoDay = day.toFormat('yyyy-MM-dd')
  if (assigning.value) return
  assigning.value = isoDay
  try {
    await dinnerRepo.addDishToMenu(appStore.activeFamilyId, day, props.dish.id)
    emit('dish:assigned', isoDay, props.dish.id)
    pickerOpen.value = false
  } finally {
    assigning.value = null
  }
}

function togglePicker() {
  pickerOpen.value = !pickerOpen.value
}

function handleKeyDown(e: KeyboardEvent) {
  if (e.key === 'Escape') pickerOpen.value = false
}

function handleClickOutside(e: MouseEvent) {
  if (rowEl.value && !rowEl.value.contains(e.target as Node)) {
    pickerOpen.value = false
  }
}

onMounted(() => {
  document.addEventListener('keydown', handleKeyDown)
  document.addEventListener('click', handleClickOutside, true)
})

onUnmounted(() => {
  document.removeEventListener('keydown', handleKeyDown)
  document.removeEventListener('click', handleClickOutside, true)
})
</script>

<template>
  <div ref="rowEl" class="adr" :class="{ 'adr--picker-open': pickerOpen }">
    <div class="adr__main">
      <!-- Dish name -->
      <NuxtLink
        :to="`/dishes/${dish.id}`"
        class="adr__name"
        :class="{ 'adr__name--never': neverUsed }"
      >{{ dish.name }}</NuxtLink>

      <!-- Badges row -->
      <div class="adr__meta">
        <span v-if="effortLabel" class="adr__effort" :class="effortClass">{{ effortLabel }}</span>

        <span v-if="neverUsed" class="adr__never">{{ $t('assistant.neverUsed') }}</span>
        <span v-else-if="daysSince !== null" class="adr__days">{{ $t('plan.dAgo', { days: daysSince }) }}</span>

        <span v-if="wish" class="adr__wish">
          <v-icon size="10" icon="mdi-star" />
          {{ wish.voteCount }}
        </span>
      </div>

      <!-- Assign button -->
      <button
        class="adr__assign-btn"
        :class="{ 'adr__assign-btn--active': pickerOpen }"
        :aria-label="$t('assistant.assignToDay')"
        @click.stop="togglePicker"
      >
        <v-icon size="14">{{ pickerOpen ? 'mdi-close' : 'mdi-plus' }}</v-icon>
      </button>
    </div>

    <!-- Inline day picker -->
    <div v-if="pickerOpen" class="adr__picker">
      <button
        v-for="day in weekDays"
        :key="day.toFormat('yyyy-MM-dd')"
        class="adr__day"
        :class="{
          'adr__day--assigned': isDishOnDay(day),
          'adr__day--loading': assigning === day.toFormat('yyyy-MM-dd'),
        }"
        :disabled="!!assigning"
        @click.stop="assignToDay(day)"
      >
        <span class="adr__day-label">{{ dayLabel(day) }}</span>
        <v-icon v-if="isDishOnDay(day)" size="10" class="adr__day-check">mdi-check</v-icon>
        <span v-else-if="dishCountForDay(day) > 0" class="adr__day-count">{{ dishCountForDay(day) }}</span>
      </button>
    </div>
  </div>
</template>

<style scoped>
.adr {
  border-radius: var(--radius-md);
  transition: background var(--duration-fast) var(--ease-standard);
}

.adr--picker-open {
  background: rgba(var(--color-primary-rgb), 0.04);
}

.adr__main {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  padding: 5px var(--space-2);
  min-height: 36px;
  min-width: 0;
}

.adr__name {
  flex: 1;
  font-size: var(--text-sm);
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  color: var(--color-text-primary);
  text-decoration: none;
  min-width: 0;
}

.adr__name:hover {
  text-decoration: underline;
  text-underline-offset: 2px;
}

.adr__name--never {
  color: var(--color-text-secondary);
  font-style: italic;
}

.adr__meta {
  display: flex;
  align-items: center;
  gap: 4px;
  flex-shrink: 0;
}

.adr__effort {
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.02em;
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

.adr__never {
  font-size: 10px;
  font-weight: 600;
  color: var(--color-text-muted);
  background: rgba(0, 0, 0, 0.06);
  border-radius: var(--radius-full);
  padding: 1px 5px;
  white-space: nowrap;
}

.adr__days {
  font-size: var(--text-xs);
  color: var(--color-text-muted);
  white-space: nowrap;
}

.adr__wish {
  display: flex;
  align-items: center;
  gap: 2px;
  font-size: 10px;
  font-weight: 700;
  color: var(--color-primary);
  background: rgba(var(--color-primary-rgb), 0.1);
  border-radius: var(--radius-full);
  padding: 1px 5px 1px 3px;
  white-space: nowrap;
}

.adr__assign-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border-radius: var(--radius-full);
  border: 1px solid rgba(var(--color-primary-rgb), 0.25);
  background: rgba(var(--color-primary-rgb), 0.06);
  color: var(--color-primary);
  cursor: pointer;
  flex-shrink: 0;
  transition:
    background var(--duration-fast) var(--ease-standard),
    border-color var(--duration-fast) var(--ease-standard);
}

.adr__assign-btn:hover {
  background: rgba(var(--color-primary-rgb), 0.14);
  border-color: rgba(var(--color-primary-rgb), 0.5);
}

.adr__assign-btn--active {
  background: rgba(var(--color-primary-rgb), 0.14);
  border-color: rgba(var(--color-primary-rgb), 0.5);
}

/* Day picker */
.adr__picker {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  padding: 4px var(--space-2) 8px;
}

.adr__day {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
  padding: 4px 6px;
  border-radius: var(--radius-sm);
  border: 1px solid rgba(0, 0, 0, 0.1);
  background: var(--color-surface);
  cursor: pointer;
  font-family: var(--font-body);
  transition:
    background var(--duration-fast) var(--ease-standard),
    border-color var(--duration-fast) var(--ease-standard);
  flex: 1;
  min-width: 36px;
}

.adr__day:hover:not(:disabled) {
  background: rgba(var(--color-primary-rgb), 0.08);
  border-color: rgba(var(--color-primary-rgb), 0.3);
}

.adr__day--assigned {
  border-color: rgba(var(--color-primary-rgb), 0.4);
  background: rgba(var(--color-primary-rgb), 0.08);
}

.adr__day--loading {
  opacity: 0.6;
}

.adr__day:disabled {
  cursor: default;
  opacity: 0.7;
}

.adr__day-label {
  font-size: 10px;
  font-weight: 600;
  letter-spacing: 0.02em;
  color: var(--color-text-secondary);
  text-transform: uppercase;
  white-space: nowrap;
}

.adr__day-check {
  color: var(--color-primary) !important;
}

.adr__day-count {
  font-size: 10px;
  font-weight: 700;
  color: var(--color-text-muted);
  background: rgba(0, 0, 0, 0.08);
  border-radius: var(--radius-full);
  width: 14px;
  height: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
}
</style>

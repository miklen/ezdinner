<script setup lang="ts">
import { DateTime } from 'luxon'
import { useDisplay } from 'vuetify'
import type { Dinner, Dish } from '~/types'

const props = defineProps<{ dinner: Dinner }>()

const emit = defineEmits<{
  'dinner:close': []
  'dinner:menuupdated': [event: { date: DateTime; dishId: string; dishName: string }]
  'dinner:optoutupdated': []
}>()

const appStore = useAppStore()
const dishesStore = useDishesStore()
const dinnersStore = useDinnersStore()
const { dishes: dishRepo, dinners: dinnerRepo } = useRepositories()
const { smAndDown } = useDisplay()

const DEFAULT_OPT_OUT_PICKS = ['Vacation', 'Eating out', 'Restaurant', 'Guests', 'Leftovers']

const optOutQuickPicks = computed(() =>
  dinnersStore.previousOptOutReasons.length > 0
    ? dinnersStore.previousOptOutReasons
    : DEFAULT_OPT_OUT_PICKS,
)
const customOptOutReason = ref('')
const optOutLoading = ref(false)
const showOptOut = ref(false)

async function setOptOut(reason: string) {
  if (!reason.trim()) return
  optOutLoading.value = true
  try {
    const trimmed = reason.trim()
    await dinnerRepo.setOptOut(appStore.activeFamilyId, props.dinner.date, trimmed)
    if (!dinnersStore.previousOptOutReasons.includes(trimmed)) {
      dinnersStore.previousOptOutReasons.push(trimmed)
    }
    customOptOutReason.value = ''
    showOptOut.value = false
    emit('dinner:optoutupdated')
  } finally {
    optOutLoading.value = false
  }
}

async function removeOptOut() {
  optOutLoading.value = true
  try {
    await dinnerRepo.removeOptOut(appStore.activeFamilyId, props.dinner.date)
    emit('dinner:optoutupdated')
  } finally {
    optOutLoading.value = false
  }
}

const dishSearch = ref('')
const selectedDish = ref<Dish | null>(null)
const dishSelector = ref<HTMLElement | null>(null)
const mobileSheetOpen = ref(false)
const mobileSearchRef = ref<InstanceType<typeof import('vuetify/components').VTextField> | null>(null)

function getDaysSince(dish: Dish): number | null {
  const lastUsed = dish.dishStats?.lastUsed
  if (!lastUsed) return null
  const days = Math.round(
    DateTime.now()
      .diff(DateTime.fromISO(lastUsed as unknown as string), 'days')
      .days,
  )
  return days >= 0 ? days : null
}

function formatRating(rating: number): string {
  return rating > 0 ? rating.toFixed(1) : '—'
}

const filteredDishes = computed(() => {
  const q = dishSearch.value.toLowerCase().trim()
  if (!q) return dishesStore.dishes
  return dishesStore.dishes.filter((d) => d.name.toLowerCase().includes(q))
})

async function onDishSelected(dish: Dish | null) {
  if (!dish) return
  await addDishToMenu(dish.id)
  selectedDish.value = null
}

async function createDish() {
  const dishName = dishSearch.value.trim()
  if (!dishName) return
  dishSearch.value = ''
  dishSelector.value?.blur()
  const dishId = (await dishRepo.create(appStore.activeFamilyId, dishName)) as string
  await dishesStore.updateDish(dishId)
  await addDishToMenu(dishId)
}

async function addDishToMenu(dishId: string) {
  await dinnerRepo.addDishToMenu(appStore.activeFamilyId, props.dinner.date, dishId)
  const dish = dishesStore.dishes.find((d) => d.id === dishId)
  emit('dinner:menuupdated', { date: props.dinner.date, dishId, dishName: dish?.name ?? '' })
}

async function removeDishFromMenu(dishId: string) {
  await dinnerRepo.removeDishFromMenu(appStore.activeFamilyId, props.dinner.date, dishId)
  emit('dinner:menuupdated', { date: props.dinner.date, dishId, dishName: '' })
}

async function selectDishFromSheet(dish: Dish) {
  mobileSheetOpen.value = false
  dishSearch.value = ''
  await addDishToMenu(dish.id)
}

async function createDishFromSheet() {
  mobileSheetOpen.value = false
  await createDish()
}

watch(mobileSheetOpen, (open) => {
  if (!open) dishSearch.value = ''
})

function focusMobileSearch() {
  nextTick(() => {
    const input = mobileSearchRef.value?.$el?.querySelector('input')
    input?.focus()
  })
}
</script>

<template>
  <div class="dinner-details">
    <div class="dinner-details__body">
      <!-- Opted-out state: show reason + remove option -->
      <template v-if="dinner.isOptedOut">
        <div class="dinner-details__optout-status">
          <v-icon size="16" class="dinner-details__optout-icon">mdi-calendar-remove-outline</v-icon>
          <span class="dinner-details__optout-label">{{ dinner.optOutReason }}</span>
          <v-btn
            variant="text"
            size="small"
            color="primary"
            :loading="optOutLoading"
            class="dinner-details__optout-remove"
            @click="removeOptOut"
          >
            Remove
          </v-btn>
        </div>
      </template>

      <!-- Normal planning: dish pills + selector + opt-out options -->
      <template v-else>
        <!-- Current menu as removable DishPills -->
        <div v-if="dinner.menu.length > 0" class="dinner-details__menu">
          <DishPill
            v-for="item in dinner.menu"
            :key="item.dishId"
            :name="item.dishName"
            :to="`/dishes/${item.dishId}`"
            size="md"
            removable
            @remove="removeDishFromMenu(item.dishId)"
          />
        </div>

      <!-- Mobile: button trigger -->
      <template v-if="smAndDown">
        <v-btn
          variant="outlined"
          color="primary"
          block
          class="dinner-details__mobile-trigger"
          prepend-icon="mdi-plus"
          @click="mobileSheetOpen = true"
        >
          Add dish to menu
        </v-btn>

        <v-bottom-sheet
          v-model="mobileSheetOpen"
          :max-height="'80vh'"
          @after-enter="focusMobileSearch"
        >
          <v-card class="sheet-card">
            <div class="sheet-handle" />
            <div class="sheet-header">
              <span class="sheet-title">Add dish</span>
            </div>

            <div class="sheet-search">
              <v-text-field
                ref="mobileSearchRef"
                v-model="dishSearch"
                variant="outlined"
                density="compact"
                placeholder="Search dishes..."
                prepend-inner-icon="mdi-magnify"
                clearable
                hide-details
              />
            </div>

            <div class="sheet-list">
              <v-list density="compact">
                <template v-if="filteredDishes.length > 0">
                  <v-list-item
                    v-for="dish in filteredDishes"
                    :key="dish.id"
                    :title="undefined"
                    class="dish-row"
                    min-height="48"
                    @click="selectDishFromSheet(dish)"
                  >
                    <div class="dish-row__inner">
                      <span class="dish-row__name">{{ dish.name }}</span>
                      <span class="dish-row__rating">
                        <v-icon size="12" color="primary">mdi-heart</v-icon>
                        {{ formatRating(dish.rating) }}
                      </span>
                      <span v-if="getDaysSince(dish) !== null" class="dish-row__days">
                        {{ getDaysSince(dish) }}d ago
                      </span>
                    </div>
                  </v-list-item>
                </template>

                <v-list-item v-if="filteredDishes.length === 0 && !dishSearch">
                  <span class="text-body-2 text-medium-emphasis">No dishes yet</span>
                </v-list-item>

                <template v-if="dishSearch">
                  <v-divider v-if="filteredDishes.length > 0" class="my-1" />
                  <v-list-item class="create-dish-item" min-height="48" @click="createDishFromSheet">
                    <template #prepend>
                      <v-icon size="16" color="primary">mdi-plus-circle-outline</v-icon>
                    </template>
                    <span class="text-body-2">
                      Create <strong>"{{ dishSearch }}"</strong>
                    </span>
                  </v-list-item>
                </template>
              </v-list>
            </div>
          </v-card>
        </v-bottom-sheet>
      </template>

      <!-- Desktop: compact autocomplete -->
      <v-autocomplete
        v-else
        ref="dishSelector"
        v-model="selectedDish"
        v-model:search="dishSearch"
        :items="dishesStore.dishes"
        item-title="name"
        item-value="id"
        return-object
        variant="outlined"
        density="compact"
        label="Add dish to menu"
        placeholder="Search dishes..."
        class="dinner-details__autocomplete"
        @update:model-value="onDishSelected"
        @keyup.enter="createDish"
      >
        <template #item="{ item, props: itemProps }">
          <v-list-item v-bind="itemProps" :title="undefined" min-height="36" class="dish-row">
            <div class="dish-row__inner">
              <span class="dish-row__name">{{ item.raw.name }}</span>
              <span class="dish-row__rating">
                <v-icon size="12" color="primary">mdi-heart</v-icon>
                {{ formatRating(item.raw.rating) }}
              </span>
              <span v-if="getDaysSince(item.raw) !== null" class="dish-row__days">
                {{ getDaysSince(item.raw) }}d ago
              </span>
            </div>
          </v-list-item>
        </template>

        <template #no-data>
          <v-list-item v-if="!dishSearch">
            <span class="text-body-2 text-medium-emphasis">Start typing to search</span>
          </v-list-item>
          <v-list-item v-else class="create-dish-item" @click="createDish">
            <template #prepend>
              <v-icon size="16" color="primary">mdi-plus-circle-outline</v-icon>
            </template>
            <span class="text-body-2">
              Create <strong>"{{ dishSearch }}"</strong>
            </span>
          </v-list-item>
        </template>

        <template #append-item>
          <template v-if="dishSearch">
            <v-divider class="my-1" />
            <v-list-item class="create-dish-item" @click="createDish">
              <template #prepend>
                <v-icon size="16" color="primary">mdi-plus-circle-outline</v-icon>
              </template>
              <span class="text-body-2">
                Create <strong>"{{ dishSearch }}"</strong>
              </span>
            </v-list-item>
          </template>
        </template>
      </v-autocomplete>

        <!-- Opt-out toggle -->
        <div class="dinner-details__optout-toggle-row">
          <button class="dinner-details__optout-toggle" @click="showOptOut = !showOptOut">
            <v-icon size="13">{{ showOptOut ? 'mdi-chevron-up' : 'mdi-calendar-remove-outline' }}</v-icon>
            {{ showOptOut ? 'Cancel' : 'Skip day' }}
          </button>
        </div>

        <!-- Opt-out quick picks (collapsed by default) -->
        <div v-if="showOptOut" class="dinner-details__optout-section">
          <div class="dinner-details__optout-picks">
            <button
              v-for="pick in optOutQuickPicks"
              :key="pick"
              class="dinner-details__optout-chip"
              :disabled="optOutLoading"
              @click="setOptOut(pick)"
            >
              {{ pick }}
            </button>
          </div>
          <div class="dinner-details__optout-custom">
            <input
              v-model="customOptOutReason"
              class="dinner-details__optout-input"
              placeholder="Other reason..."
              maxlength="50"
              @keyup.enter="setOptOut(customOptOutReason)"
            >
            <button
              v-if="customOptOutReason.trim()"
              class="dinner-details__optout-submit"
              :disabled="optOutLoading"
              @click="setOptOut(customOptOutReason)"
            >
              <v-icon size="16">mdi-check</v-icon>
            </button>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
.dinner-details {
  border-top: 1px solid rgba(0, 0, 0, 0.07);
}

/* Body */
.dinner-details__body {
  padding: var(--space-4);
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

/* Menu pills */
.dinner-details__menu {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}

/* Autocomplete */
.dinner-details__autocomplete {
  margin-bottom: 0;
}

/* Opted-out status row */
.dinner-details__optout-status {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-2) var(--space-3);
  background: rgba(0, 0, 0, 0.035);
  border-radius: var(--radius-md);
}

.dinner-details__optout-icon {
  color: var(--color-text-muted) !important;
  flex-shrink: 0;
}

.dinner-details__optout-label {
  flex: 1;
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
  font-style: italic;
}

.dinner-details__optout-remove {
  text-transform: none !important;
  letter-spacing: 0 !important;
  padding: 0 var(--space-2) !important;
  height: auto !important;
  min-width: 0 !important;
}

/* Opt-out toggle row */
.dinner-details__optout-toggle-row {
  display: flex;
}

.dinner-details__optout-toggle {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: var(--text-xs);
  font-family: inherit;
  color: var(--color-text-muted);
  background: none;
  border: none;
  padding: 0;
  cursor: pointer;
  transition: color var(--duration-fast) var(--ease-standard);
}

.dinner-details__optout-toggle:hover {
  color: var(--color-text-secondary);
}

/* Opt-out quick pick section */
.dinner-details__optout-section {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.dinner-details__optout-picks {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-1);
}

.dinner-details__optout-chip {
  font-size: var(--text-xs);
  font-family: inherit;
  color: var(--color-text-secondary);
  background: rgba(0, 0, 0, 0.055);
  border: 1px solid rgba(0, 0, 0, 0.1);
  border-radius: var(--radius-full);
  padding: 3px 10px;
  cursor: pointer;
  transition:
    background var(--duration-fast) var(--ease-standard),
    color var(--duration-fast) var(--ease-standard);
  white-space: nowrap;
}

.dinner-details__optout-chip:hover:not(:disabled) {
  background: rgba(0, 0, 0, 0.1);
  color: var(--color-text-primary);
}

.dinner-details__optout-chip:disabled {
  opacity: 0.5;
  cursor: default;
}

.dinner-details__optout-custom {
  display: flex;
  align-items: center;
  gap: var(--space-1);
}

.dinner-details__optout-input {
  flex: 1;
  font-size: var(--text-sm);
  font-family: inherit;
  color: var(--color-text-primary);
  background: transparent;
  border: none;
  border-bottom: 1px solid rgba(0, 0, 0, 0.15);
  padding: 2px 2px 3px;
  outline: none;
  transition: border-color var(--duration-fast) var(--ease-standard);
}

.dinner-details__optout-input:focus {
  border-bottom-color: var(--color-primary);
}

.dinner-details__optout-input::placeholder {
  color: var(--color-text-muted);
}

.dinner-details__optout-submit {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border: none;
  border-radius: var(--radius-full);
  background: var(--color-primary);
  color: white;
  cursor: pointer;
  flex-shrink: 0;
  transition: opacity var(--duration-fast) var(--ease-standard);
}

.dinner-details__optout-submit:disabled {
  opacity: 0.5;
  cursor: default;
}

/* Mobile trigger */
.dinner-details__mobile-trigger {
  text-transform: none;
  letter-spacing: 0;
}

/* Compact dish row (shared desktop + mobile) */
.dish-row__inner {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  width: 100%;
  min-height: 0;
  padding: 0;
}

.dish-row__name {
  flex: 1;
  font-size: var(--text-sm);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.dish-row__rating {
  display: flex;
  align-items: center;
  gap: 3px;
  font-size: var(--text-xs);
  color: var(--color-text-muted);
  white-space: nowrap;
  flex-shrink: 0;
}

.dish-row__days {
  font-size: var(--text-xs);
  color: var(--color-text-muted);
  white-space: nowrap;
  flex-shrink: 0;
}

/* Create new dish row */
.create-dish-item {
  cursor: pointer;
}

.create-dish-item:hover {
  background: var(--color-surface-variant);
}

/* Bottom sheet */
.sheet-card {
  border-radius: var(--radius-xl) var(--radius-xl) 0 0 !important;
  max-height: 80vh;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  padding-bottom: env(safe-area-inset-bottom, 0);
}

.sheet-handle {
  width: 36px;
  height: 4px;
  border-radius: var(--radius-full);
  background: rgba(0, 0, 0, 0.15);
  margin: 12px auto 0;
  flex-shrink: 0;
}

.sheet-header {
  padding: 12px var(--space-4) 8px;
  flex-shrink: 0;
}

.sheet-title {
  font-size: var(--text-base);
  font-weight: 600;
}

.sheet-search {
  padding: 0 var(--space-4) var(--space-3);
  flex-shrink: 0;
}

.sheet-list {
  overflow-y: auto;
  overscroll-behavior: contain;
  flex: 1;
}
</style>

<script setup lang="ts">
import { DateTime } from 'luxon'
import { useId } from 'vue'
import { useDisplay } from 'vuetify'
import type { Dinner, Dish } from '~/types'
import DinnerDayDetails from './DinnerDayDetails.vue'

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
const { t } = useI18n()
const { show: showSnackbar } = useSnackbar()

const optOutLoading = ref(false)

async function setOptOut(reason: string) {
  if (!reason.trim()) return
  optOutLoading.value = true
  try {
    const trimmed = reason.trim()
    await dinnerRepo.setOptOut(appStore.activeFamilyId, props.dinner.date, trimmed)
    dinnersStore.addOptOutReason(trimmed)
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

type DishChoice =
  | { kind: 'dish'; id: string; name: string; dish: Dish }
  | { kind: 'create'; id: 'create-dish'; name: string; dishName: string }

const dishSearch = ref('')
const selectedChoice = ref<DishChoice | null>(null)
const assigningDish = ref(false)
const mobileSheetOpen = ref(false)
const mobileSearchRef = ref<InstanceType<typeof import('vuetify/components').VTextField> | null>(null)
const mobileList = ref<HTMLElement | null>(null)
const mobileActiveIndex = ref(0)
const mobileListId = useId()

const wishlistStore = useWishlistStore()

const filteredDishes = computed(() => {
  const q = dishSearch.value.toLowerCase().trim()
  const dishes = q
    ? dishesStore.dishes.filter((d) => d.name.toLowerCase().includes(q))
    : dishesStore.dishes
  // Wished dishes float to the top; within each group sort by last-used (longest ago first)
  const wishedIds = new Set(wishlistStore.wishes.map((w) => w.dishId))
  return [...dishes].sort((a, b) => {
    const aWished = wishedIds.has(a.id) ? 0 : 1
    const bWished = wishedIds.has(b.id) ? 0 : 1
    if (aWished !== bWished) return aWished - bWished
    // Sort by last-used ascending (oldest date = longest ago = highest priority)
    const aLastUsed = a.dishStats?.lastUsed
    const bLastUsed = b.dishStats?.lastUsed
    const aLast = aLastUsed instanceof DateTime ? aLastUsed.toMillis() : (aLastUsed ? DateTime.fromISO(aLastUsed as unknown as string).toMillis() : -Infinity)
    const bLast = bLastUsed instanceof DateTime ? bLastUsed.toMillis() : (bLastUsed ? DateTime.fromISO(bLastUsed as unknown as string).toMillis() : -Infinity)
    return aLast - bLast
  })
})

const dishChoices = computed<DishChoice[]>(() => {
  const choices: DishChoice[] = filteredDishes.value.map(dish => ({ kind: 'dish', id: dish.id, name: dish.name, dish }))
  const dishName = dishSearch.value.trim()
  const exactMatch = dishesStore.dishes.some(dish => dish.name.trim().toLowerCase() === dishName.toLowerCase())
  if (dishName && !exactMatch) {
    choices.push({ kind: 'create', id: 'create-dish', name: t('plan.createDish', { name: dishName }), dishName })
  }
  return choices
})

async function selectDishChoice(choice: DishChoice | null) {
  if (!choice || assigningDish.value) return
  assigningDish.value = true
  try {
    if (choice.kind === 'create') await createDish(choice.dishName)
    if (choice.kind === 'dish') await addDishToMenu(choice.dish.id)
    mobileSheetOpen.value = false
  } finally {
    selectedChoice.value = null
    dishSearch.value = ''
    assigningDish.value = false
  }
}

async function createDish(dishName: string) {
  const dishId = (await dishRepo.create(appStore.activeFamilyId, dishName)) as string
  dishRepo.enrich(appStore.activeFamilyId, dishId).catch(() => {
    showSnackbar(t('dishes.aiAnalysisFailed'), { type: 'error' })
  })
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

function onMobileSearchKeydown(event: KeyboardEvent) {
  if (event.isComposing || event.repeat || assigningDish.value) return
  if (event.key === 'Enter') {
    event.preventDefault()
    const choice = dishChoices.value[mobileActiveIndex.value]
    if (choice) void selectDishChoice(choice)
    return
  }
  if (!['ArrowDown', 'ArrowUp'].includes(event.key)) return
  event.preventDefault()
  const direction = event.key === 'ArrowDown' ? 1 : -1
  mobileActiveIndex.value = Math.max(0, Math.min(dishChoices.value.length - 1, mobileActiveIndex.value + direction))
  nextTick(() => mobileList.value?.querySelector('.v-list-item--active')?.scrollIntoView({ block: 'nearest' }))
}

function onDesktopKeydown(event: KeyboardEvent) {
  if (event.key === 'Tab' || (event.key === 'Enter' && (event.isComposing || event.repeat))) event.stopPropagation()
}

watch(dishChoices, () => { mobileActiveIndex.value = 0 })

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
  <DinnerDayDetails :dinner="dinner" :reasons="dinnersStore.previousOptOutReasons" :busy="optOutLoading" @set-opt-out="setOptOut" @remove-opt-out="removeOptOut" @remove-dish="removeDishFromMenu">
    <template #assignment>
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
          {{ $t('plan.addDishToMenu') }}
        </v-btn>

        <v-bottom-sheet
          v-model="mobileSheetOpen"
          :max-height="'80vh'"
          @after-enter="focusMobileSearch"
        >
          <v-card class="sheet-card">
            <div class="sheet-handle" />
            <div class="sheet-header">
              <span class="sheet-title">{{ $t('plan.addDish') }}</span>
            </div>

            <div class="sheet-search">
              <v-text-field
                ref="mobileSearchRef"
                :model-value="dishSearch"
                variant="outlined"
                density="compact"
                :placeholder="$t('plan.searchDishes')"
                prepend-inner-icon="mdi-magnify"
                clearable
                hide-details
                :disabled="assigningDish"
                :loading="assigningDish"
                role="combobox"
                aria-autocomplete="list"
                aria-expanded="true"
                :aria-controls="mobileListId"
                :aria-activedescendant="dishChoices.length ? `${mobileListId}-${mobileActiveIndex}` : undefined"
                @update:model-value="dishSearch = $event ?? ''"
                @keydown="onMobileSearchKeydown"
              />
            </div>

            <div ref="mobileList" class="sheet-list">
              <v-list :id="mobileListId" density="compact" role="listbox" :aria-label="$t('plan.searchDishes')">
                <v-list-item
                  v-for="(choice, index) in dishChoices"
                  :id="`${mobileListId}-${index}`"
                  :key="choice.id"
                  :title="undefined"
                  class="dish-picker-item"
                  :class="choice.kind === 'create' ? 'create-dish-item' : 'dish-row'"
                  :active="index === mobileActiveIndex"
                  :aria-selected="index === mobileActiveIndex"
                  :disabled="assigningDish"
                  role="option"
                  min-height="48"
                  @focus="mobileActiveIndex = index"
                  @click="selectDishChoice(choice)"
                >
                  <template v-if="choice.kind === 'create'" #prepend>
                    <v-icon size="16" color="primary">mdi-plus-circle-outline</v-icon>
                  </template>
                  <PlanDishRow v-if="choice.kind === 'dish'" :dish="choice.dish" />
                  <span v-else class="text-body-2">{{ choice.name }}</span>
                </v-list-item>

                <v-list-item v-if="dishChoices.length === 0">
                  <span class="text-body-2 text-medium-emphasis">{{ $t('plan.noDishesYet') }}</span>
                </v-list-item>
              </v-list>
            </div>
          </v-card>
        </v-bottom-sheet>
      </template>

      <!-- Desktop: compact autocomplete -->
      <v-autocomplete
        v-else
        v-model="selectedChoice"
        v-model:search="dishSearch"
        :items="dishChoices"
        item-title="name"
        item-value="id"
        return-object
        no-filter
        auto-select-first
        :disabled="assigningDish"
        :loading="assigningDish"
        variant="outlined"
        density="compact"
        :label="$t('plan.addDishToMenu')"
        :placeholder="$t('plan.searchDishes')"
        class="dinner-details__autocomplete"
        @keydown.capture="onDesktopKeydown"
        @update:model-value="selectDishChoice"
      >
        <template #item="{ item, props: itemProps }">
          <v-list-item v-bind="itemProps" :title="undefined" min-height="36" class="dish-picker-item" :class="item.raw.kind === 'create' ? 'create-dish-item' : 'dish-row'" role="option">
            <template v-if="item.raw.kind === 'create'" #prepend>
              <v-icon size="16" color="primary">mdi-plus-circle-outline</v-icon>
            </template>
            <PlanDishRow v-if="item.raw.kind === 'dish'" :dish="item.raw.dish" />
            <span v-else class="text-body-2">{{ item.raw.name }}</span>
          </v-list-item>
        </template>

        <template #no-data>
          <v-list-item>
            <span class="text-body-2 text-medium-emphasis">{{ $t('plan.startTypingToSearch') }}</span>
          </v-list-item>
        </template>
      </v-autocomplete>

    </template>
  </DinnerDayDetails>
</template>

<style scoped>
.dinner-details__autocomplete {
  margin-bottom: 0;
}

/* Mobile trigger */
.dinner-details__mobile-trigger {
  text-transform: none;
  letter-spacing: 0;
}


/* Create new dish row */
.create-dish-item {
  cursor: pointer;
}

.create-dish-item:hover {
  background: var(--color-surface-variant);
}

.dish-picker-item.v-list-item--active,
.dish-picker-item:focus-visible {
  background: rgba(var(--color-primary-rgb), 0.12);
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

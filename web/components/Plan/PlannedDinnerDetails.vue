<script setup lang="ts">
import { DateTime } from 'luxon'
import { useDisplay } from 'vuetify'
import type { Dinner } from '~/types'
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

const dishSearch = ref('')
const selectedDish = ref<Dish | null>(null)
const dishSelector = ref<HTMLElement | null>(null)
const mobileSheetOpen = ref(false)
const mobileSearchRef = ref<InstanceType<typeof import('vuetify/components').VTextField> | null>(null)

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
                v-model="dishSearch"
                variant="outlined"
                density="compact"
                :placeholder="$t('plan.searchDishes')"
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
                    <PlanDishRow :dish="dish" />
                  </v-list-item>
                </template>

                <v-list-item v-if="filteredDishes.length === 0 && !dishSearch">
                  <span class="text-body-2 text-medium-emphasis">{{ $t('plan.noDishesYet') }}</span>
                </v-list-item>

                <template v-if="dishSearch">
                  <v-divider v-if="filteredDishes.length > 0" class="my-1" />
                  <v-list-item class="create-dish-item" min-height="48" @click="createDishFromSheet">
                    <template #prepend>
                      <v-icon size="16" color="primary">mdi-plus-circle-outline</v-icon>
                    </template>
                    <span class="text-body-2">
                      {{ $t('plan.createDish', { name: dishSearch }) }}
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
        :items="filteredDishes"
        item-title="name"
        item-value="id"
        return-object
        no-filter
        variant="outlined"
        density="compact"
        :label="$t('plan.addDishToMenu')"
        :placeholder="$t('plan.searchDishes')"
        class="dinner-details__autocomplete"
        @update:model-value="onDishSelected"
        @keyup.enter="createDish"
      >
        <template #item="{ item, props: itemProps }">
          <v-list-item v-bind="itemProps" :title="undefined" min-height="36" class="dish-row">
            <PlanDishRow :dish="item.raw" />
          </v-list-item>
        </template>

        <template #no-data>
          <v-list-item v-if="!dishSearch">
            <span class="text-body-2 text-medium-emphasis">{{ $t('plan.startTypingToSearch') }}</span>
          </v-list-item>
          <v-list-item v-else class="create-dish-item" @click="createDish">
            <template #prepend>
              <v-icon size="16" color="primary">mdi-plus-circle-outline</v-icon>
            </template>
            <span class="text-body-2">
              {{ $t('plan.createDish', { name: dishSearch }) }}
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
                {{ $t('plan.createDish', { name: dishSearch }) }}
              </span>
            </v-list-item>
          </template>
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

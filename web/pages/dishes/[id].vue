<script setup lang="ts">
import type { Dish } from '~/types'

useHead({ title: 'Dish' })

const route = useRoute()
const appStore = useAppStore()
const familiesStore = useFamiliesStore()
const dishesStore = useDishesStore()
const { dishes: dishRepo, dinners: dinnerRepo } = useRepositories()
const { $msal } = useNuxtApp()
const { show: showSnackbar } = useSnackbar()

// ── Data ───────────────────────────────────────────────────────────────────────

const dish = ref<Dish | null>(null)
const loading = shallowRef(true)

const userId = computed(() => $msal.getObjectId())
const familyMembers = computed(() => familiesStore.activeFamily?.familyMembers ?? [])

async function loadDish() {
  loading.value = true
  try {
    dish.value = await dishRepo.getFull(route.params.id as string, appStore.activeFamilyId)
    useHead({ title: dish.value.name })
  } finally {
    loading.value = false
  }
}

onMounted(loadDish)
watch(() => appStore.activeFamilyId, () => navigateTo('/dishes'))

// ── Move occurrences dialog ────────────────────────────────────────────────────

const moveDialog = shallowRef(false)
const moveToDish = shallowRef<Dish | null>(null)

const otherDishes = computed(() =>
  dishesStore.dishes
    .filter((d) => d.id !== dish.value?.id)
    .sort((a, b) => a.name.localeCompare(b.name)),
)

async function doMove() {
  if (!dish.value || !moveToDish.value) return
  try {
    await dinnerRepo.moveDinnerDishes(appStore.activeFamilyId, dish.value.id, moveToDish.value.id)
    showSnackbar('Occurrences moved', { type: 'success' })
  } catch {
    showSnackbar('Failed to move occurrences', { type: 'error' })
  }
  moveDialog.value = false
  moveToDish.value = null
}

// ── Delete dialog ──────────────────────────────────────────────────────────────

const deleteDialog = shallowRef(false)

async function doDelete() {
  if (!dish.value) return
  try {
    await dishRepo.delete(appStore.activeFamilyId, dish.value.id)
    dishesStore.populateDishes()
    await navigateTo('/dishes')
  } catch {
    showSnackbar('Failed to delete dish', { type: 'error' })
    deleteDialog.value = false
  }
}
</script>

<template>
  <div class="dish-detail">
    <!-- Header: breadcrumb + name + rating + stat + overflow menu -->
    <DishDetailHeader
      v-if="dish || loading"
      :dish="dish ?? undefined"
      :dish-stats="dish?.dishStats"
      :loading="loading"
      @move="moveDialog = true"
      @delete="deleteDialog = true"
    />

    <!-- Two-column layout: notes on left, ratings + dates on right -->
    <!-- align="start" prevents Vuetify from stretching columns to equal height -->
    <v-row align="start">
      <!-- Main column -->
      <v-col cols="12" md="7">
        <DishNotesCard
          :dish-id="(route.params.id as string)"
          :initial-notes="dish?.notes ?? ''"
          :initial-url="dish?.url ?? ''"
          :loading="loading"
        />
      </v-col>

      <!-- Side column -->
      <v-col cols="12" md="5">
        <div class="dish-detail__side">
          <!-- Family ratings -->
          <DishFamilyRatings
            v-if="dish || loading"
            :dish="dish ?? undefined"
            :family-members="familyMembers"
            :user-id="userId"
            :loading="loading"
            @updated="loadDish"
          />

          <!-- Dates visualization -->
          <!-- Nuxt auto-import: Dish/DatesVisualization.vue → <DishDatesVisualization> -->
          <DishDatesVisualization
            :dates="dish?.dates ?? []"
            :dish-name="dish?.name ?? ''"
            :loading="loading"
          />
        </div>
      </v-col>
    </v-row>
    <!-- Move occurrences dialog -->
    <v-dialog v-model="moveDialog" width="440">
      <v-card>
        <v-card-title>Move occurrences</v-card-title>
        <v-card-text>
          Move all {{ dish?.dishStats?.timesUsed ?? 0 }}
          dinner occurrence{{ (dish?.dishStats?.timesUsed ?? 0) === 1 ? '' : 's' }} of
          <strong>{{ dish?.name }}</strong> to:
          <v-autocomplete
            v-model="moveToDish"
            :items="otherDishes"
            item-title="name"
            return-object
            density="compact"
            variant="outlined"
            placeholder="Select a dish…"
            class="mt-3"
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="moveDialog = false">Cancel</v-btn>
          <v-btn
            variant="text"
            color="primary"
            :disabled="!moveToDish"
            @click="doMove"
          >
            Move
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete confirmation dialog -->
    <v-dialog v-model="deleteDialog" width="400">
      <v-card>
        <v-card-title>Delete dish?</v-card-title>
        <v-card-text>
          Are you sure you want to delete <strong>{{ dish?.name }}</strong>?
          This cannot be undone.
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="deleteDialog = false">Cancel</v-btn>
          <v-btn variant="text" color="error" @click="doDelete">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<style scoped>
.dish-detail {
  padding: 0 var(--space-2);
}

.dish-detail__side {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

</style>

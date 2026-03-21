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
const enriching = shallowRef(false)

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

// Enriching on name/notes change: the dish name and notes are the primary inputs
// to AI enrichment, so any change to them should re-run analysis.
async function triggerEnrich() {
  if (enriching.value) return
  await loadDish()
  enriching.value = true
  try {
    await dishRepo.enrich(appStore.activeFamilyId, route.params.id as string)
    await loadDish()
  } catch {
    showSnackbar('AI analysis failed — metadata not updated', { type: 'error' })
  } finally {
    enriching.value = false
  }
}

onMounted(loadDish)
onUnmounted(() => { enriching.value = false })
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

// ── Archive / Reactivate ───────────────────────────────────────────────────────

const archiveDialog = shallowRef(false)
const reactivateDialog = shallowRef(false)
const archiveLoading = shallowRef(false)
const reactivateLoading = shallowRef(false)

async function doArchive() {
  if (!dish.value) return
  archiveLoading.value = true
  try {
    await dishRepo.archive(appStore.activeFamilyId, dish.value.id)
    archiveDialog.value = false
    await loadDish()
    dishesStore.populateDishes()
    showSnackbar('Dish archived', { type: 'success' })
  } catch {
    showSnackbar('Failed to archive dish', { type: 'error' })
    archiveDialog.value = false
  } finally {
    archiveLoading.value = false
  }
}

async function doReactivate() {
  if (!dish.value) return
  reactivateLoading.value = true
  try {
    await dishRepo.reactivate(appStore.activeFamilyId, dish.value.id)
    reactivateDialog.value = false
    await loadDish()
    dishesStore.populateDishes()
    showSnackbar('Dish reactivated', { type: 'success' })
  } catch {
    showSnackbar('Failed to reactivate dish', { type: 'error' })
    reactivateDialog.value = false
  } finally {
    reactivateLoading.value = false
  }
}
</script>

<template>
  <div class="dish-detail">
    <!-- Archived status banner -->
    <div v-if="dish?.isArchived" class="dish-detail__archived-banner">
      <v-icon size="14" icon="mdi-archive-outline" class="dish-detail__archived-icon" />
      This dish is archived — it won't appear in suggestions or the active catalog.
      <span class="dish-detail__archived-hint">Reactivate anytime below.</span>
    </div>

    <!-- Header: breadcrumb + name + rating + stat + overflow menu -->
    <DishDetailHeader
      v-if="dish || loading"
      :dish="dish ?? undefined"
      :dish-stats="dish?.dishStats"
      :loading="loading"
      @move="moveDialog = true"
      @delete="deleteDialog = true"
      @renamed="triggerEnrich"
    />

    <!-- Archive / Reactivate action -->
    <div v-if="dish && !loading" class="dish-detail__archive-action mb-4">
      <button
        v-if="!dish.isArchived"
        class="dish-detail__archive-btn"
        @click="archiveDialog = true"
      >
        <v-icon size="14" icon="mdi-archive-arrow-down-outline" />
        Archive dish
      </button>
      <button
        v-else
        class="dish-detail__archive-btn dish-detail__archive-btn--reactivate"
        @click="reactivateDialog = true"
      >
        <v-icon size="14" icon="mdi-archive-arrow-up-outline" />
        Reactivate dish
      </button>
    </div>

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
          @updated="triggerEnrich"
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

          <!-- Dish metadata (AI enrichment) -->
          <!-- Nuxt auto-import: Dish/DishMetadataCard.vue → <DishMetadataCard> -->
          <DishMetadataCard
            v-if="dish"
            :dish="dish"
            :family-id="appStore.activeFamilyId"
            :enriching="enriching"
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

    <!-- Archive confirmation dialog -->
    <v-dialog v-model="archiveDialog" width="400">
      <v-card>
        <v-card-title>Archive dish?</v-card-title>
        <v-card-text>
          <strong>{{ dish?.name }}</strong> will be removed from suggestions and the active catalog.
          You can reactivate it at any time.
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" :disabled="archiveLoading" @click="archiveDialog = false">Cancel</v-btn>
          <v-btn variant="text" color="warning" :loading="archiveLoading" @click="doArchive">Archive</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Reactivate confirmation dialog -->
    <v-dialog v-model="reactivateDialog" width="400">
      <v-card>
        <v-card-title>Reactivate dish?</v-card-title>
        <v-card-text>
          <strong>{{ dish?.name }}</strong> will be restored to the active catalog and become eligible for suggestions again.
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" :disabled="reactivateLoading" @click="reactivateDialog = false">Cancel</v-btn>
          <v-btn variant="text" color="primary" :loading="reactivateLoading" @click="doReactivate">Reactivate</v-btn>
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

/* Archived banner — warm strip using app palette, not Vuetify warning blue */
.dish-detail__archived-banner {
  display: flex;
  align-items: baseline;
  gap: var(--space-2);
  padding: var(--space-3) var(--space-4);
  margin-bottom: var(--space-4);
  background-color: rgba(var(--color-primary-rgb), 0.06);
  border-left: 3px solid var(--color-accent);
  border-radius: var(--radius-sm);
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
}

.dish-detail__archived-icon {
  color: var(--color-accent);
  flex-shrink: 0;
  position: relative;
  top: 1px;
}

.dish-detail__archived-hint {
  margin-left: var(--space-1);
  font-size: var(--text-xs);
  color: var(--color-text-muted);
}

/* Archive / reactivate button — understated, dashed for archive; solid for reactivate */
.dish-detail__archive-btn {
  display: inline-flex;
  align-items: center;
  gap: var(--space-2);
  padding: 6px var(--space-3);
  background: none;
  border: 1px dashed var(--color-border-medium);
  border-radius: var(--radius-sm);
  font-family: var(--font-body);
  font-size: var(--text-sm);
  font-weight: 500;
  color: var(--color-text-muted);
  cursor: pointer;
  transition:
    border-color var(--duration-fast) var(--ease-out),
    color var(--duration-fast) var(--ease-out),
    background-color var(--duration-fast) var(--ease-out);
}

.dish-detail__archive-btn:hover {
  border-color: var(--color-text-secondary);
  color: var(--color-text-secondary);
  background-color: var(--color-surface-variant);
}

.dish-detail__archive-btn--reactivate {
  border-style: solid;
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.dish-detail__archive-btn--reactivate:hover {
  background-color: rgba(var(--color-primary-rgb), 0.06);
  border-color: var(--color-primary-dark);
  color: var(--color-primary-dark);
}
</style>

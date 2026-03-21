<script setup lang="ts">
import type { Dish } from '~/types'

const props = defineProps<{
  dish?: Dish
}>()

const emit = defineEmits<{
  'edit-name': []
  move: []
  delete: []
  converted: []
  archived: []
}>()

const route = useRoute()
const appStore = useAppStore()
const dishesStore = useDishesStore()
const { dishes: dishRepo, dinners: dinnerRepo } = useRepositories()
const { show: showSnackbar } = useSnackbar()

// Use prop if provided (overview cards), fall back to route param (detail page)
const routeDishId = computed(() => route.params.id as string | undefined)
const currentDish = computed(() =>
  props.dish ?? dishesStore.dishes.find(d => d.id === routeDishId.value),
)
const dishId = computed(() => currentDish.value?.id)

const archiveDialog = shallowRef(false)
const archiveLoading = shallowRef(false)

async function doArchive() {
  if (!dishId.value) return
  archiveLoading.value = true
  const isCurrentlyArchived = currentDish.value?.isArchived ?? false
  try {
    if (isCurrentlyArchived) {
      await dishRepo.reactivate(appStore.activeFamilyId, dishId.value)
      showSnackbar('Dish reactivated', { type: 'success' })
    } else {
      await dishRepo.archive(appStore.activeFamilyId, dishId.value)
      showSnackbar('Dish archived', { type: 'success' })
    }
    dishesStore.populateDishes()
    archiveDialog.value = false
    emit('archived')
  } catch {
    showSnackbar('Failed, please try again', { type: 'error' })
    archiveDialog.value = false
  } finally {
    archiveLoading.value = false
  }
}

const convertDialog = shallowRef(false)
const convertReason = ref('')
const convertLoading = shallowRef(false)

const CONVERT_QUICK_PICKS = [
  'Vacation',
  'Eating out',
  'Restaurant',
  'Guests',
  'Leftovers',
]

function openConvertDialog() {
  convertReason.value = currentDish.value?.name ?? ''
  convertDialog.value = true
}

async function doConvert() {
  if (!convertReason.value.trim() || !dishId.value) return
  convertLoading.value = true
  try {
    await dinnerRepo.convertDishToOptOut(appStore.activeFamilyId, dishId.value, convertReason.value.trim())
    await dishRepo.delete(appStore.activeFamilyId, dishId.value)
    dishesStore.populateDishes()
    showSnackbar('Converted to opt-outs', { type: 'success' })
    emit('converted')
  } catch {
    showSnackbar('Failed to convert', { type: 'error' })
    convertDialog.value = false
    convertLoading.value = false
  }
}
</script>

<template>
  <v-menu location="bottom end" :close-on-content-click="true">
    <template #activator="{ props: menuProps }">
      <v-btn
        v-bind="menuProps"
        icon="mdi-dots-vertical"
        variant="text"
        density="compact"
        size="small"
        aria-label="More actions"
        class="overflow-btn"
        @click.stop
      />
    </template>
    <v-list density="compact" min-width="168">
      <v-list-item
        prepend-icon="mdi-pencil-outline"
        title="Rename"
        @click="emit('edit-name')"
      />
      <v-list-item
        prepend-icon="mdi-transfer-right"
        title="Move occurrences"
        @click="emit('move')"
      />
      <v-list-item
        v-if="dishId"
        prepend-icon="mdi-calendar-remove-outline"
        title="Convert to opt-outs"
        @click="openConvertDialog"
      />
      <v-list-item
        v-if="dishId"
        :prepend-icon="currentDish?.isArchived ? 'mdi-archive-arrow-up-outline' : 'mdi-archive-arrow-down-outline'"
        :title="currentDish?.isArchived ? 'Reactivate' : 'Archive'"
        @click="archiveDialog = true"
      />
      <v-divider />
      <v-list-item
        prepend-icon="mdi-delete-outline"
        title="Delete"
        class="text-error"
        @click="emit('delete')"
      />
    </v-list>
  </v-menu>

  <!-- Archive / Reactivate dialog -->
  <v-dialog v-model="archiveDialog" width="400">
    <v-card>
      <v-card-title>{{ currentDish?.isArchived ? 'Reactivate dish?' : 'Archive dish?' }}</v-card-title>
      <v-card-text>
        <template v-if="currentDish?.isArchived">
          <strong>{{ currentDish?.name }}</strong> will be restored to the active catalog and become eligible for suggestions again.
        </template>
        <template v-else>
          <strong>{{ currentDish?.name }}</strong> will be removed from suggestions and the active catalog.
          You can reactivate it at any time.
        </template>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" :disabled="archiveLoading" @click="archiveDialog = false">Cancel</v-btn>
        <v-btn
          variant="text"
          :color="currentDish?.isArchived ? 'primary' : undefined"
          :loading="archiveLoading"
          @click="doArchive"
        >
          {{ currentDish?.isArchived ? 'Reactivate' : 'Archive' }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <!-- Convert to opt-outs dialog -->
  <v-dialog v-model="convertDialog" width="440">
    <v-card>
      <v-card-title>Convert to opt-outs</v-card-title>
      <v-card-text>
        <p class="text-body-2 mb-3">
          Convert all occurrences of
          <strong>{{ currentDish?.name }}</strong> to opt-outs with this reason:
        </p>
        <div class="convert-picks">
          <button
            v-for="pick in CONVERT_QUICK_PICKS"
            :key="pick"
            class="convert-pick-chip"
            :class="{ 'convert-pick-chip--active': convertReason === pick }"
            @click="convertReason = pick"
          >
            {{ pick }}
          </button>
        </div>
        <v-text-field
          v-model="convertReason"
          label="Reason"
          variant="outlined"
          density="compact"
          class="mt-3"
          hide-details
        />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="convertDialog = false">Cancel</v-btn>
        <v-btn
          variant="text"
          color="primary"
          :disabled="!convertReason.trim()"
          :loading="convertLoading"
          @click="doConvert"
        >
          Convert &amp; delete dish
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.overflow-btn {
  opacity: 0;
  transition: opacity var(--duration-fast) var(--ease-out);
}

/* Show on keyboard focus always */
.overflow-btn:focus-visible {
  opacity: 1;
}

.convert-picks {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-1);
}

.convert-pick-chip {
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
    color var(--duration-fast) var(--ease-standard),
    border-color var(--duration-fast) var(--ease-standard);
}

.convert-pick-chip:hover {
  background: rgba(0, 0, 0, 0.1);
  color: var(--color-text-primary);
}

.convert-pick-chip--active {
  background: oklch(from var(--color-primary) l c h / 0.12);
  border-color: var(--color-primary);
  color: var(--color-primary-dark);
}
</style>

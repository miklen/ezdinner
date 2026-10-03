<script setup lang="ts">
import { shallowRef } from 'vue'
import type { RecipeSnapshot } from '~/types/recipe-snapshot'
import { recipeImportErrorKey } from '~/formatting/recipe-import-error'
import DishUserNotesSection from './DishUserNotesSection.vue'
import DishRecipeSnapshotSection from './DishRecipeSnapshotSection.vue'
import DishRecipeImportDialog from './DishRecipeImportDialog.vue'

const props = defineProps<{
  dishId: string
  familyId: string
  initialNotes: string
  initialUrl: string
  snapshot?: RecipeSnapshot | null
  loading?: boolean
}>()
const emit = defineEmits<{ updated: [{ notes: string; url: string }]; snapshotUpdated: [] }>()
const { dishes: dishRepo } = useRepositories()
const { t } = useI18n()
const { show: showSnackbar } = useSnackbar()
const importing = shallowRef(false)
const removalOpen = shallowRef(false)
const removing = shallowRef(false)
const removalError = shallowRef('')

function snapshotSaved() {
  importing.value = false
  showSnackbar(t('recipeSnapshot.saved'), { type: 'success' })
  emit('snapshotUpdated')
}

async function removeSnapshot() {
  if (removing.value) return
  removing.value = true
  removalError.value = ''
  try {
    await dishRepo.removeRecipe(props.familyId, props.dishId)
    removalOpen.value = false
    showSnackbar(t('recipeSnapshot.removed'), { type: 'success' })
    emit('snapshotUpdated')
  }
  catch (error) { removalError.value = recipeImportErrorKey(error) }
  finally { removing.value = false }
}
</script>

<template>
  <v-card class="notes-card">
    <v-card-text v-if="loading">
      <v-skeleton-loader type="text" width="120" class="mb-4" />
      <v-skeleton-loader type="paragraph" />
    </v-card-text>
    <template v-else>
      <DishUserNotesSection :dish-id="dishId" :initial-notes="initialNotes" :initial-url="initialUrl" @updated="emit('updated', $event)" />
      <DishRecipeSnapshotSection :snapshot="snapshot" :url="initialUrl" :busy="importing || removalOpen" @import="importing = true" @refresh="importing = true" @remove="removalOpen = true" />
      <DishRecipeImportDialog v-if="importing" :family-id="familyId" :dish-id="dishId" :replacing="!!snapshot" @cancelled="importing = false" @confirmed="snapshotSaved" />
      <v-dialog v-model="removalOpen" max-width="440" :persistent="removing">
        <v-card>
          <v-card-title>{{ t('recipeSnapshot.removeTitle') }}</v-card-title>
          <v-card-text>
            <p>{{ t('recipeSnapshot.removeWarning') }}</p>
            <p v-if="removalError" role="alert">{{ t(removalError) }}</p>
          </v-card-text>
          <v-card-actions>
            <v-btn variant="text" :disabled="removing" @click="removalOpen = false">{{ t('common.cancel') }}</v-btn>
            <v-btn variant="text" color="error" :loading="removing" :disabled="removing" @click="removeSnapshot">{{ t('recipeSnapshot.remove') }}</v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>
    </template>
  </v-card>
</template>

<style scoped>
.notes-card { background-color: var(--color-surface-variant) !important; }
</style>

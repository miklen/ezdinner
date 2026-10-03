<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, shallowRef } from 'vue'
import type { RecipeCandidate, RecipePreview } from '~/types/recipe-snapshot'
import { renderMarkdown, publicWebLink } from '~/formatting/render-markdown'
import { recipeImportErrorKey } from '~/formatting/recipe-import-error'

const props = defineProps<{ familyId: string; dishId: string; replacing: boolean }>()
const emit = defineEmits<{ confirmed: [RecipeCandidate]; cancelled: [] }>()
const { dishes: dishRepo } = useRepositories()
const { t } = useI18n()
type PreviewState = { kind: 'loading' } | { kind: 'error'; errorKey: string } | { kind: 'preview'; preview: RecipePreview }
const state = shallowRef<PreviewState>({ kind: 'loading' })
const saving = shallowRef(false)
const saveError = shallowRef('')
const abort = new AbortController()
const preview = computed(() => state.value.kind === 'preview' ? state.value.preview : null)
const previewHtml = computed(() => renderMarkdown(preview.value?.candidate.content ?? ''))
const sourceLink = computed(() => publicWebLink(preview.value?.candidate.sourceUrl ?? ''))
const warningKeys = computed(() => preview.value?.warnings.map(warning =>
  warning === 'RECIPE_TITLE_OMITTED' ? 'recipeSnapshot.titleOmitted' : 'recipeSnapshot.extractionWarning') ?? [])

async function loadPreview() {
  state.value = { kind: 'loading' }
  try {
    const result = await dishRepo.previewRecipe(props.familyId, props.dishId, abort.signal)
    if (!abort.signal.aborted) state.value = { kind: 'preview', preview: result }
  }
  catch (error) {
    if (!abort.signal.aborted) state.value = { kind: 'error', errorKey: recipeImportErrorKey(error) }
  }
}

function cancel() {
  if (saving.value) return
  abort.abort()
  emit('cancelled')
}

async function confirm() {
  if (saving.value || !preview.value || !preview.value.sourceChanged) return
  const candidate = preview.value.candidate
  saving.value = true
  saveError.value = ''
  try {
    await dishRepo.confirmRecipe(props.familyId, props.dishId, candidate)
    emit('confirmed', candidate)
  }
  catch (error) { saveError.value = recipeImportErrorKey(error) }
  finally { saving.value = false }
}
onMounted(loadPreview)
onBeforeUnmount(() => abort.abort())
</script>

<template>
  <v-dialog :model-value="true" max-width="720" :persistent="saving" @update:model-value="value => { if (!value) cancel() }">
    <v-card class="recipe-import">
      <v-card-title class="recipe-import__title">{{ t(replacing ? 'recipeSnapshot.refreshTitle' : 'recipeSnapshot.previewTitle') }}</v-card-title>
      <v-card-text class="recipe-import__body">
        <p v-if="state.kind === 'loading'" role="status">{{ t('recipeSnapshot.loading') }}</p>
        <div v-else-if="state.kind === 'error'" role="alert">
          <p>{{ t(state.errorKey) }}</p>
          <v-btn variant="text" color="primary" @click="loadPreview">{{ t('recipeSnapshot.retry') }}</v-btn>
        </div>
        <template v-else-if="preview">
          <p v-if="replacing" class="recipe-import__notice">{{ t('recipeSnapshot.replacementWarning') }}</p>
          <p v-if="!preview.sourceChanged" role="status">{{ t('recipeSnapshot.unchanged') }}</p>
          <p v-for="(warning, index) in warningKeys" :key="index" class="recipe-import__notice">{{ t(warning) }}</p>
          <a v-if="sourceLink" :href="sourceLink" target="_blank" rel="noopener noreferrer" class="recipe-import__source">{{ preview.candidate.sourceUrl }}</a>
          <!-- eslint-disable-next-line vue/no-v-html -->
          <div class="recipe-markdown" v-html="previewHtml" />
          <p v-if="saveError" role="alert">{{ t(saveError) }}</p>
        </template>
      </v-card-text>
      <v-card-actions class="recipe-import__actions">
        <v-btn variant="text" :disabled="saving" @click="cancel">{{ t(preview && !preview.sourceChanged ? 'recipeSnapshot.close' : 'common.cancel') }}</v-btn>
        <v-btn v-if="preview?.sourceChanged" variant="flat" color="primary" :loading="saving" :disabled="saving" @click="confirm">{{ t(replacing ? 'recipeSnapshot.replace' : 'recipeSnapshot.save') }}</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.recipe-import { background: var(--color-surface-variant); }
.recipe-import__title { font-family: var(--font-display); white-space: normal; }
.recipe-import__body { max-height: 65vh; overflow-y: auto; overflow-wrap: anywhere; }
.recipe-import__notice { border-left: 3px solid var(--color-accent); padding: var(--space-2) var(--space-3); margin-bottom: var(--space-3); }
.recipe-import__source { display: block; color: var(--color-primary-dark); font-size: var(--text-sm); margin-bottom: var(--space-4); }
.recipe-import__source:focus-visible { outline: 2px solid var(--color-primary-dark); outline-offset: 3px; }
.recipe-import__actions { justify-content: flex-end; flex-wrap: wrap; padding: var(--space-3); }
</style>

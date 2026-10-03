<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, shallowRef, useTemplateRef, watch } from 'vue'
import EasyMDE from 'easymde'
import 'easymde/dist/easymde.min.css'
import { publicWebLink, renderMarkdown } from '~/formatting/render-markdown'

const props = defineProps<{ dishId: string; initialNotes: string; initialUrl: string }>()
const emit = defineEmits<{ updated: [{ notes: string; url: string }] }>()
const { dishes: dishRepo } = useRepositories()
const { show: showSnackbar } = useSnackbar()
const { t } = useI18n()
const notes = shallowRef(props.initialNotes)
const url = shallowRef(props.initialUrl)
const editMode = shallowRef(false)
const saving = shallowRef(false)
const editUrl = shallowRef('')
const editNotes = shallowRef('')
const editor = shallowRef<EasyMDE | null>(null)
const textarea = useTemplateRef<HTMLTextAreaElement>('textarea')
const notesHtml = computed(() => renderMarkdown(notes.value))
const sourceLink = computed(() => publicWebLink(url.value))
watch(() => props.initialNotes, value => { notes.value = value })
watch(() => props.initialUrl, value => { url.value = value })

async function startEdit() {
  editUrl.value = url.value
  editNotes.value = notes.value
  editMode.value = true
  await nextTick()
  if (!textarea.value || !editMode.value) return
  editor.value = new EasyMDE({
    element: textarea.value, spellChecker: false, initialValue: editNotes.value,
    toolbar: ['bold', 'italic', 'heading', '|', 'unordered-list', 'ordered-list', '|', 'link', 'preview'],
    previewRender: renderMarkdown,
  })
  editor.value.codemirror.on('change', () => { editNotes.value = editor.value?.value() ?? '' })
}

function cancelEdit() {
  editor.value?.toTextArea()
  editor.value = null
  editMode.value = false
}

async function saveEdit() {
  if (saving.value) return
  saving.value = true
  try {
    await dishRepo.updateNotes(props.dishId, editNotes.value, editUrl.value)
    notes.value = editNotes.value
    url.value = editUrl.value
    emit('updated', { notes: notes.value, url: url.value })
    showSnackbar(t('dishes.notesSaved'), { type: 'success' })
    cancelEdit()
  }
  catch { showSnackbar(t('dishes.failedToSaveNotes'), { type: 'error' }) }
  finally { saving.value = false }
}
onBeforeUnmount(cancelEdit)
</script>

<template>
  <section class="user-notes">
    <div class="user-notes__header">
      <h2 class="text-card-title">{{ t('recipeSnapshot.userNotes') }}</h2>
      <div v-if="editMode" class="user-notes__actions">
        <v-btn variant="text" size="small" :disabled="saving" @click="cancelEdit">{{ t('common.cancel') }}</v-btn>
        <v-btn variant="text" size="small" color="primary" :loading="saving" @click="saveEdit">{{ t('common.save') }}</v-btn>
      </div>
      <v-btn v-else icon="mdi-pencil-outline" variant="text" size="small" :aria-label="t('dishes.editNotes')" @click="startEdit" />
    </div>
    <div v-if="url || editMode" class="user-notes__url">
      <v-text-field v-if="editMode" v-model="editUrl" :label="t('dishes.recipeUrl')" variant="outlined" density="compact" hide-details />
      <a v-else-if="sourceLink" :href="sourceLink" target="_blank" rel="noopener noreferrer">{{ url }}</a>
      <span v-else>{{ url }}</span>
    </div>
    <div v-if="editMode">
      <textarea ref="textarea" :aria-label="t('recipeSnapshot.userNotes')" />
      <p class="user-notes__hint">{{ t('dishes.mentionPrepHint') }}</p>
    </div>
    <template v-else>
      <!-- eslint-disable-next-line vue/no-v-html -->
      <div v-if="notes" class="recipe-markdown" v-html="notesHtml" />
      <p v-else class="user-notes__hint">{{ t('dishes.noNotesAdded') }}</p>
    </template>
  </section>
</template>

<style scoped>
.user-notes { padding: var(--space-3); }
.user-notes__header { display: flex; align-items: center; justify-content: space-between; gap: var(--space-2); margin-bottom: var(--space-2); }
.user-notes__actions { display: flex; }
.user-notes__url { margin-bottom: var(--space-3); overflow-wrap: anywhere; font-size: var(--text-sm); }
.user-notes__url a { color: var(--color-primary-dark); }
.user-notes__url a:focus-visible { outline: 2px solid var(--color-primary-dark); outline-offset: 3px; }
.user-notes__hint { font-size: var(--text-sm); color: var(--color-text-muted); margin: var(--space-2) 0 0; }
</style>

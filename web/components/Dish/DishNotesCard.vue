<script setup lang="ts">
import { marked } from 'marked'
import EasyMDE from 'easymde'
import 'easymde/dist/easymde.min.css'

const props = defineProps<{
  dishId: string
  initialNotes: string
  initialUrl: string
  loading?: boolean
}>()

const emit = defineEmits<{
  updated: [{ notes: string; url: string }]
}>()

const { dishes: dishRepo } = useRepositories()
const { show: showSnackbar } = useSnackbar()

// View state — mirrors incoming props, updated on save
const url = shallowRef(props.initialUrl)
const notes = shallowRef(props.initialNotes)

watch(() => props.initialNotes, (val) => { notes.value = val })
watch(() => props.initialUrl, (val) => { url.value = val })

const notesHtml = computed(() => marked.parse(notes.value || '') as string)

// Edit state
const editMode = shallowRef(false)
const editUrl = shallowRef('')
const editNotes = shallowRef('')
const mde = ref<EasyMDE | null>(null)
const textareaRef = ref<HTMLElement | null>(null)

function startEdit() {
  editUrl.value = url.value
  editNotes.value = notes.value
  editMode.value = true
  nextTick(() => {
    if (!textareaRef.value) return
    mde.value = new EasyMDE({
      element: textareaRef.value,
      spellChecker: false,
      initialValue: editNotes.value,
      toolbar: ['bold', 'italic', 'heading', '|', 'unordered-list', 'ordered-list', '|', 'link', 'preview'],
    })
    mde.value.codemirror.on('change', () => {
      editNotes.value = mde.value?.value() ?? ''
    })
  })
}

function cancelEdit() {
  mde.value?.toTextArea()
  mde.value = null
  editMode.value = false
}

async function saveEdit() {
  try {
    await dishRepo.updateNotes(props.dishId, editNotes.value, editUrl.value)
    notes.value = editNotes.value
    url.value = editUrl.value
    showSnackbar('Notes saved', { type: 'success' })
    emit('updated', { notes: notes.value, url: url.value })
  } catch {
    showSnackbar('Failed to save notes', { type: 'error' })
  }
  cancelEdit()
}
</script>

<template>
  <!-- Skeleton -->
  <v-card v-if="loading" class="notes-card">
    <v-card-text>
      <v-skeleton-loader type="text" width="120" class="mb-4" />
      <v-skeleton-loader type="paragraph" />
    </v-card-text>
  </v-card>

  <!-- Loaded -->
  <v-card v-else class="notes-card">
    <div class="notes-card__header">
      <span class="text-card-title">Recipe &amp; notes</span>
      <div v-if="editMode" class="notes-card__edit-actions">
        <v-btn variant="text" size="small" @click="cancelEdit">Cancel</v-btn>
        <v-btn variant="text" size="small" color="primary" @click="saveEdit">Save</v-btn>
      </div>
      <v-btn
        v-else
        icon="mdi-pencil-outline"
        variant="text"
        size="small"
        aria-label="Edit notes"
        @click="startEdit"
      />
    </div>

    <div class="notes-card__body">
      <!-- Recipe URL -->
      <div v-if="url || editMode" class="notes-card__url-row">
        <v-icon size="15" color="primary">mdi-open-in-new</v-icon>
        <v-text-field
          v-if="editMode"
          v-model="editUrl"
          label="Recipe URL"
          variant="outlined"
          density="compact"
          hide-details
          class="notes-card__url-input"
        />
        <a
          v-else
          :href="url"
          target="_blank"
          rel="noopener noreferrer"
          class="notes-card__url-chip"
        >
          {{ url }}
        </a>
      </div>

      <!-- Notes: edit mode -->
      <div v-if="editMode" class="notes-card__editor">
        <textarea ref="textareaRef" />
      </div>

      <!-- Notes: view mode -->
      <template v-else>
        <!-- eslint-disable-next-line vue/no-v-html -->
        <div v-if="notes" class="notes-card__html" v-html="notesHtml" />
        <p v-else class="notes-card__empty">No notes added yet.</p>
      </template>
    </div>
  </v-card>
</template>

<style scoped>
.notes-card {
  /* Warm tint to distinguish from white surface cards */
  background-color: var(--color-surface-variant) !important;
}

.notes-card__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--space-3) var(--space-3) var(--space-2);
}

.notes-card__edit-actions {
  display: flex;
  gap: var(--space-1);
}

.notes-card__body {
  padding: 0 var(--space-3) var(--space-3);
}

/* URL row */
.notes-card__url-row {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  margin-bottom: var(--space-3);
}

.notes-card__url-chip {
  display: inline-block;
  padding: 3px var(--space-3);
  border-radius: var(--radius-sm);
  background-color: var(--color-surface);
  border: 1px solid var(--color-border-medium);
  font-size: var(--text-sm);
  color: var(--color-primary-dark);
  text-decoration: none;
  max-width: 420px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  transition:
    background-color var(--duration-fast) var(--ease-out),
    border-color var(--duration-fast) var(--ease-out);
}

.notes-card__url-chip:hover {
  background-color: rgba(var(--color-primary-rgb), 0.08);
  border-color: var(--color-primary);
}

.notes-card__url-input {
  flex: 1;
}

/* Editor */
.notes-card__editor {
  margin-top: var(--space-2);
}

/* Rendered markdown */
.notes-card__html {
  font-size: var(--text-base);
  line-height: 1.6;
  color: var(--color-text-primary);
}

.notes-card__html :deep(h1),
.notes-card__html :deep(h2),
.notes-card__html :deep(h3) {
  font-family: var(--font-display);
  margin: var(--space-4) 0 var(--space-2);
  color: var(--color-text-primary);
}

.notes-card__html :deep(a) {
  color: var(--color-primary-dark);
}

.notes-card__html :deep(p) {
  margin: 0 0 var(--space-3);
}

.notes-card__html :deep(ul),
.notes-card__html :deep(ol) {
  padding-left: var(--space-6);
  margin: 0 0 var(--space-3);
}

.notes-card__empty {
  font-size: var(--text-sm);
  color: var(--color-text-muted);
  font-style: italic;
  margin: 0;
}
</style>

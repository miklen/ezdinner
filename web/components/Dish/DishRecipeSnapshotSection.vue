<script setup lang="ts">
import { computed } from 'vue'
import type { RecipeSnapshot } from '~/types/recipe-snapshot'
import { publicWebLink, renderMarkdown } from '~/formatting/render-markdown'

const props = defineProps<{ snapshot?: RecipeSnapshot | null; url: string; busy?: boolean }>()
const emit = defineEmits<{ import: []; refresh: []; remove: [] }>()
const { t, locale } = useI18n()
const contentHtml = computed(() => renderMarkdown(props.snapshot?.content ?? ''))
const mismatch = computed(() => !!props.snapshot && props.snapshot.sourceUrl !== props.url)
const sourceLink = computed(() => publicWebLink(props.snapshot?.sourceUrl ?? ''))
const capturedTime = computed(() => props.snapshot
  ? new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(props.snapshot.capturedAt))
  : '')
</script>

<template>
  <section v-if="snapshot || url" class="captured-recipe">
    <div class="captured-recipe__header">
      <h2 class="text-card-title">{{ t('recipeSnapshot.heading') }}</h2>
      <div class="captured-recipe__actions">
        <v-btn v-if="!snapshot && url" variant="text" size="small" color="primary" :disabled="busy" @click="emit('import')">{{ t('recipeSnapshot.import') }}</v-btn>
        <template v-if="snapshot">
          <v-btn variant="text" size="small" color="primary" :disabled="!url || busy" @click="emit('refresh')">{{ t('recipeSnapshot.refresh') }}</v-btn>
          <v-btn variant="text" size="small" :disabled="busy" @click="emit('remove')">{{ t('recipeSnapshot.remove') }}</v-btn>
        </template>
      </div>
    </div>
    <template v-if="snapshot">
      <div class="captured-recipe__provenance">
        <span>{{ t('recipeSnapshot.capturedAt', { date: capturedTime }) }}</span>
        <a v-if="sourceLink" :href="sourceLink" target="_blank" rel="noopener noreferrer">{{ snapshot.sourceUrl }}</a>
        <span v-else>{{ snapshot.sourceUrl }}</span>
      </div>
      <p v-if="mismatch" class="captured-recipe__notice" role="status">{{ t(url ? 'recipeSnapshot.urlMismatch' : 'recipeSnapshot.urlRemoved') }}</p>
      <!-- eslint-disable-next-line vue/no-v-html -->
      <div class="recipe-markdown" v-html="contentHtml" />
    </template>
    <p v-else class="captured-recipe__empty">{{ t('recipeSnapshot.empty') }}</p>
  </section>
</template>

<style scoped>
.captured-recipe { border-top: 1px dashed var(--color-border-medium); padding: var(--space-4) var(--space-3); }
.captured-recipe__header { display: flex; align-items: center; justify-content: space-between; gap: var(--space-2); flex-wrap: wrap; margin-bottom: var(--space-2); }
.captured-recipe__actions { display: flex; flex-wrap: wrap; }
.captured-recipe__provenance { display: grid; gap: var(--space-1); font-size: var(--text-xs); color: var(--color-text-secondary); margin-bottom: var(--space-4); overflow-wrap: anywhere; }
.captured-recipe__provenance a { color: var(--color-primary-dark); }
.captured-recipe__provenance a:focus-visible { outline: 2px solid var(--color-primary-dark); outline-offset: 3px; }
.captured-recipe__notice { padding: var(--space-2) var(--space-3); border-left: 3px solid var(--color-accent); font-size: var(--text-sm); margin-bottom: var(--space-3); }
.captured-recipe__empty { font-size: var(--text-sm); color: var(--color-text-muted); }
@media (max-width: 480px) { .captured-recipe__header { align-items: flex-start; flex-direction: column; } }
</style>

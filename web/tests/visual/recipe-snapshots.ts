import { createApp, defineComponent, h, shallowRef } from 'vue'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import 'vuetify/styles'
import '../../assets/global.scss'
import DishNotesCard from '../../components/Dish/DishNotesCard.vue'
import type { RecipeCandidate, RecipeSnapshot } from '../../types/recipe-snapshot'
import english from '../../i18n/locales/en.json'
import danish from '../../i18n/locales/da.json'

const notes = shallowRef('## Family adjustments\n\n- Use less salt.\n- Buy carrots for tomorrow. 🥕')
const url = shallowRef('https://example.com/family-carrot-soup')
const snapshot = shallowRef<RecipeSnapshot | null>(null)
const locale = shallowRef('en')
const refreshed = shallowRef(false)
const unchanged = shallowRef(false)
const status = shallowRef('')

function translate(key: string, parameters: Record<string, string> = {}): string {
  let value: unknown = locale.value === 'da' ? danish : english
  for (const part of key.split('.')) {
    if (typeof value !== 'object' || value === null || !(part in value)) return key
    value = Reflect.get(value, part)
  }
  if (typeof value !== 'string') return key
  return Object.entries(parameters).reduce((text, [name, replacement]) => text.replace(`{${name}}`, replacement), value)
}

Object.assign(globalThis, {
  useI18n: () => ({ t: translate, locale }),
  useSnackbar: () => ({ show: (message: string) => { status.value = message } }),
  useRepositories: () => ({ dishes: {
    updateNotes: async (_dish: string, editedNotes: string, editedUrl: string) => { notes.value = editedNotes; url.value = editedUrl },
    previewRecipe: async () => ({
      candidate: {
        content: `# Carrot soup\n\n- 4 carrots\n- 1 litre vegetable stock\n${refreshed.value ? '- Fresh parsley\n' : ''}\n1. Chop the carrots.\n2. Simmer in stock for 20 minutes.`,
        sourceUrl: url.value, capturedAt: new Date().toISOString(), sourceHash: (refreshed.value ? 'b' : 'a').repeat(64),
      },
      warnings: ['RECIPE_LLM_EXTRACTED'], sourceChanged: !unchanged.value,
    }),
    confirmRecipe: async (_family: string, _dish: string, candidate: RecipeCandidate) => { snapshot.value = candidate },
    removeRecipe: async () => { snapshot.value = null },
  } }),
})

const App = defineComponent({
  setup: () => () => h(components.VApp, {}, {
    default: () => h(components.VMain, {}, {
      default: () => h('main', { style: 'max-width:780px;margin:24px auto;padding:16px;' }, [
        h('h1', { style: 'font-family:var(--font-display);margin-bottom:20px;' }, 'Family carrot soup'),
        h(DishNotesCard, { familyId: 'family', dishId: 'dish', initialNotes: notes.value, initialUrl: url.value, snapshot: snapshot.value }),
        h('p', { role: 'status', style: 'margin-top:16px;' }, status.value),
        h('aside', { style: 'display:flex;flex-wrap:wrap;gap:12px;margin-top:24px;' }, [
          h('button', { onClick: () => { refreshed.value = true; unchanged.value = false } }, 'Simulate source change'),
          h('button', { onClick: () => { unchanged.value = true } }, 'Simulate unchanged source'),
          h('button', { onClick: () => { url.value = 'https://example.com/new-link' } }, 'Change source link'),
          h('button', { onClick: () => { url.value = '' } }, 'Remove source link'),
          h('button', { onClick: () => { locale.value = locale.value === 'en' ? 'da' : 'en' } }, 'English / Dansk'),
        ]),
      ]),
    }),
  }),
})
createApp(App).use(createVuetify({ components, directives })).mount('#app')

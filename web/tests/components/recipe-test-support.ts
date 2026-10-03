import { defineComponent, h, shallowRef } from 'vue'
import { vi } from 'vitest'
import type { VueWrapper } from '@vue/test-utils'
import english from '~/i18n/locales/en.json'

export const candidate = { content: '# Soup\n\n- Salt\n\n1. Boil', sourceUrl: 'https://example.com/soup', capturedAt: '2026-10-03T10:00:00+00:00', sourceHash: 'a'.repeat(64) }
export const preview = { candidate, warnings: [], sourceChanged: true }
export const repository = {
  updateNotes: vi.fn().mockResolvedValue(undefined),
  previewRecipe: vi.fn().mockResolvedValue(preview),
  confirmRecipe: vi.fn().mockResolvedValue(undefined),
  removeRecipe: vi.fn().mockResolvedValue(undefined),
}

function translate(key: string, parameters: Record<string, string> = {}): string {
  let value: unknown = english
  for (const part of key.split('.')) {
    if (typeof value !== 'object' || value === null || !(part in value)) return key
    value = Reflect.get(value, part)
  }
  if (typeof value !== 'string') return key
  return Object.entries(parameters).reduce((text, [name, replacement]) => text.replace(`{${name}}`, replacement), value)
}

const Block = defineComponent({ setup: (_, { slots }) => () => h('div', slots.default?.()) })
const Button = defineComponent({
  props: { disabled: Boolean, loading: Boolean },
  setup: (props, { slots }) => () => h('button', { disabled: props.disabled || props.loading }, slots.default?.()),
})
const Dialog = defineComponent({
  props: { modelValue: Boolean },
  setup: (props, { slots }) => () => props.modelValue ? h('div', { role: 'dialog' }, slots.default?.()) : null,
})
const TextField = defineComponent({
  props: { modelValue: String, label: String },
  emits: ['update:modelValue'],
  setup: (props, { emit }) => () => h('input', {
    value: props.modelValue, 'aria-label': props.label,
    onInput: (event: Event) => { if (event.target instanceof HTMLInputElement) emit('update:modelValue', event.target.value) },
  }),
})

export const global = { stubs: {
  VCard: Block, VCardText: Block, VCardTitle: Block, VCardActions: Block,
  VBtn: Button, VDialog: Dialog, VTextField: TextField, VSkeletonLoader: Block,
} }

export function setupRecipeTests() {
  vi.clearAllMocks()
  repository.previewRecipe.mockResolvedValue(preview)
  repository.confirmRecipe.mockResolvedValue(undefined)
  repository.removeRecipe.mockResolvedValue(undefined)
  repository.updateNotes.mockResolvedValue(undefined)
  vi.stubGlobal('useRepositories', () => ({ dishes: repository }))
  vi.stubGlobal('useSnackbar', () => ({ show: vi.fn() }))
  vi.stubGlobal('useI18n', () => ({ t: translate, locale: shallowRef('en') }))
}

export function button(wrapper: VueWrapper, name: string) {
  const result = wrapper.findAll('button').find(element => element.text() === name || element.attributes('aria-label') === name)
  if (!result) throw new Error(`Button ${name} was not rendered`)
  return result
}

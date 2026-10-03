import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import DishUserNotesSection from '~/components/Dish/DishUserNotesSection.vue'
import DishRecipeSnapshotSection from '~/components/Dish/DishRecipeSnapshotSection.vue'
import DishRecipeImportDialog from '~/components/Dish/DishRecipeImportDialog.vue'
import DishNotesCard from '~/components/Dish/DishNotesCard.vue'
import { button, candidate, global, preview, repository, setupRecipeTests } from './recipe-test-support'
import type { RecipePreview } from '~/types/recipe-snapshot'

vi.mock('easymde', () => ({ default: class {
  private element: HTMLTextAreaElement
  private changed = () => {}
  private listener = () => this.changed()
  codemirror = { on: (_event: string, listener: () => void) => { this.changed = listener } }
  constructor(options: { element: HTMLTextAreaElement; initialValue: string }) {
    this.element = options.element
    this.element.value = options.initialValue
    this.element.addEventListener('input', this.listener)
  }
  value() { return this.element.value }
  toTextArea() { this.element.removeEventListener('input', this.listener) }
} }))

enableAutoUnmount(afterEach)
beforeEach(setupRecipeTests)
const notes = '  My adjustments\r\n\n- Less salt\t'
const props = { dishId: 'dish', familyId: 'family', initialNotes: notes, initialUrl: candidate.sourceUrl }

describe('user notes section', () => {
  it('saves edited notes and URL through its public event', async () => {
    const wrapper = mount(DishUserNotesSection, { props, global })
    await button(wrapper, 'Edit notes').trigger('click')
    await flushPromises()
    await wrapper.find('textarea').setValue('Updated notes')
    await wrapper.find('input').setValue('https://example.com/new')
    await button(wrapper, 'Save').trigger('click')
    await flushPromises()
    expect(repository.updateNotes).toHaveBeenCalledWith('dish', 'Updated notes', 'https://example.com/new')
    expect(wrapper.emitted('updated')).toEqual([[{ notes: 'Updated notes', url: 'https://example.com/new' }]])
    expect(wrapper.text()).toContain('Updated notes')
  })

  it('cancels editing without saving or losing existing notes', async () => {
    const wrapper = mount(DishUserNotesSection, { props, global })
    await button(wrapper, 'Edit notes').trigger('click')
    await flushPromises()
    await wrapper.find('textarea').setValue('Discard this')
    await button(wrapper, 'Cancel').trigger('click')
    expect(wrapper.text()).toContain('My adjustments')
    expect(wrapper.text()).not.toContain('Discard this')
    expect(repository.updateNotes).not.toHaveBeenCalled()
  })
})

describe('captured recipe section', () => {
  it('offers import when a URL exists without a snapshot', async () => {
    const wrapper = mount(DishRecipeSnapshotSection, { props: { url: candidate.sourceUrl }, global })
    await button(wrapper, 'Import recipe').trigger('click')
    expect(wrapper.emitted('import')).toEqual([[]])
  })

  it('shows recipe provenance and emits refresh and removal intents', async () => {
    const wrapper = mount(DishRecipeSnapshotSection, { props: { url: candidate.sourceUrl, snapshot: candidate }, global })
    expect(wrapper.text()).toContain('Soup')
    expect(wrapper.text()).toContain(candidate.sourceUrl)
    expect(wrapper.text()).toContain('Captured')
    await button(wrapper, 'Refresh recipe').trigger('click')
    await button(wrapper, 'Remove recipe').trigger('click')
    expect(wrapper.emitted('refresh')).toEqual([[]])
    expect(wrapper.emitted('remove')).toEqual([[]])
  })

  it('keeps captured content and disables refresh when the URL is removed', () => {
    const wrapper = mount(DishRecipeSnapshotSection, { props: { url: '', snapshot: candidate }, global })
    expect(wrapper.text()).toContain('This captured recipe is still saved')
    expect(wrapper.text()).toContain('Soup')
    expect(button(wrapper, 'Refresh recipe').attributes('disabled')).toBeDefined()
  })

  it('identifies provenance mismatch while retaining captured content', () => {
    const wrapper = mount(DishRecipeSnapshotSection, { props: { url: 'https://example.com/new', snapshot: candidate }, global })
    expect(wrapper.text()).toContain('captured from a different link')
    expect(wrapper.find('a').attributes('href')).toBe(candidate.sourceUrl)
  })
})

describe('recipe import dialog', () => {
  const dialogProps = { familyId: 'family', dishId: 'dish', replacing: false }

  it('loads a preview and saves only after explicit confirmation', async () => {
    const wrapper = mount(DishRecipeImportDialog, { props: dialogProps, global })
    expect(wrapper.text()).toContain('Reading the recipe')
    await flushPromises()
    expect(wrapper.text()).toContain('Soup')
    expect(repository.confirmRecipe).not.toHaveBeenCalled()
    await button(wrapper, 'Save captured recipe').trigger('click')
    await flushPromises()
    expect(repository.confirmRecipe).toHaveBeenCalledWith('family', 'dish', candidate)
    expect(wrapper.emitted('confirmed')).toEqual([[candidate]])
  })

  it('cancels a pending preview without confirming even if the request later resolves', async () => {
    let resolvePreview: (value: RecipePreview) => void = () => {}
    repository.previewRecipe.mockReturnValueOnce(new Promise<RecipePreview>(resolve => { resolvePreview = resolve }))
    const wrapper = mount(DishRecipeImportDialog, { props: dialogProps, global })
    await button(wrapper, 'Cancel').trigger('click')
    resolvePreview(preview)
    await flushPromises()
    expect(wrapper.emitted('cancelled')).toEqual([[]])
    expect(wrapper.emitted('confirmed')).toBeUndefined()
    expect(repository.confirmRecipe).not.toHaveBeenCalled()
    expect(wrapper.text()).not.toContain('Save captured recipe')
  })

  it('leaves an unchanged source unwritten by default', async () => {
    repository.previewRecipe.mockResolvedValueOnce({ ...preview, sourceChanged: false })
    const wrapper = mount(DishRecipeImportDialog, { props: { ...dialogProps, replacing: true }, global })
    await flushPromises()
    expect(wrapper.text()).toContain('No source change was detected')
    expect(wrapper.text()).not.toContain('Replace captured recipe')
    await button(wrapper, 'Close').trigger('click')
    expect(repository.confirmRecipe).not.toHaveBeenCalled()
  })

  it('shows replacement and extraction warnings before saving', async () => {
    repository.previewRecipe.mockResolvedValueOnce({ ...preview, warnings: ['RECIPE_LLM_EXTRACTED', 'RECIPE_TITLE_OMITTED'] })
    const wrapper = mount(DishRecipeImportDialog, { props: { ...dialogProps, replacing: true }, global })
    await flushPromises()
    expect(wrapper.text()).toContain('Saving replaces the captured recipe')
    expect(wrapper.text()).toContain('Check ingredients and instructions')
    expect(wrapper.text()).toContain('title could not be verified')
  })

  it('shows actionable fetch failure and allows retry', async () => {
    repository.previewRecipe.mockRejectedValueOnce({ data: 'RECIPE_FETCH_TIMEOUT' })
    const wrapper = mount(DishRecipeImportDialog, { props: dialogProps, global })
    await flushPromises()
    expect(wrapper.find('[role="alert"]').text()).toContain('took too long')
    await button(wrapper, 'Try again').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Soup')
  })

  it('shows a stale-candidate save error without emitting confirmation', async () => {
    repository.confirmRecipe.mockRejectedValueOnce({ data: 'RECIPE_SOURCE_CHANGED' })
    const wrapper = mount(DishRecipeImportDialog, { props: dialogProps, global })
    await flushPromises()
    await button(wrapper, 'Save captured recipe').trigger('click')
    await flushPromises()
    expect(wrapper.find('[role="alert"]').text()).toContain('link changed after this preview')
    expect(wrapper.emitted('confirmed')).toBeUndefined()
  })
})

describe('composed notes card', () => {
  it('keeps notes visible through import, refresh, and cancellation', async () => {
    const wrapper = mount(DishNotesCard, { props, global })
    await button(wrapper, 'Import recipe').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('My adjustments')
    await button(wrapper, 'Save captured recipe').trigger('click')
    await flushPromises()
    expect(wrapper.emitted('snapshotUpdated')).toEqual([[]])
    await wrapper.setProps({ snapshot: candidate })
    await button(wrapper, 'Refresh recipe').trigger('click')
    await flushPromises()
    await button(wrapper, 'Cancel').trigger('click')
    expect(wrapper.text()).toContain('My adjustments')
    expect(wrapper.text()).toContain('Soup')
    expect(repository.confirmRecipe).toHaveBeenCalledTimes(1)
    expect(repository.updateNotes).not.toHaveBeenCalled()
  })

  it('keeps notes and recipe after URL changes and URL removal', async () => {
    const wrapper = mount(DishNotesCard, { props: { ...props, snapshot: candidate }, global })
    await wrapper.setProps({ initialUrl: 'https://example.com/new' })
    expect(wrapper.text()).toContain('captured from a different link')
    await wrapper.setProps({ initialUrl: '' })
    expect(wrapper.text()).toContain('My adjustments')
    expect(wrapper.text()).toContain('Soup')
    expect(button(wrapper, 'Refresh recipe').attributes('disabled')).toBeDefined()
    expect(repository.removeRecipe).not.toHaveBeenCalled()
  })

  it('requires removal confirmation and preserves notes after reloading', async () => {
    const wrapper = mount(DishNotesCard, { props: { ...props, snapshot: candidate }, global })
    await button(wrapper, 'Remove recipe').trigger('click')
    expect(wrapper.find('[role="dialog"]').text()).toContain('Only the captured recipe will be removed')
    expect(repository.removeRecipe).not.toHaveBeenCalled()
    await wrapper.find('[role="dialog"]').findAll('button').find(element => element.text() === 'Remove recipe')?.trigger('click')
    await flushPromises()
    expect(repository.removeRecipe).toHaveBeenCalledWith('family', 'dish')
    await wrapper.setProps({ snapshot: null })
    expect(wrapper.text()).toContain('My adjustments')
    expect(wrapper.text()).not.toContain('Soup')
    expect(repository.updateNotes).not.toHaveBeenCalled()
  })
})

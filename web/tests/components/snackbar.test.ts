import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { computed, defineComponent, h, nextTick, onMounted, reactive, shallowRef, watch } from 'vue'
import { createVuetify } from 'vuetify'
import { VSnackbar } from 'vuetify/components/VSnackbar'
import Layout from '~/layouts/default.vue'
import { useSnackbar } from '~/composables/useSnackbar'

enableAutoUnmount(afterEach)
const snackbar = useSnackbar()
const Block = defineComponent({ setup: (_, { slots }) => () => h('div', slots.default?.()) })
const Overlay = defineComponent({ props: { modelValue: Boolean }, setup: (props, { slots }) => () => props.modelValue ? h('div', slots.default?.()) : null })
const Button = defineComponent({ props: { disabled: Boolean }, setup: (props, { slots }) => () => h('button', { disabled: props.disabled }, slots.default?.()) })

beforeEach(() => {
  vi.useFakeTimers()
  snackbar.dismiss()
  vi.stubGlobal('useSnackbar', useSnackbar)
  vi.stubGlobal('useDisplay', () => ({ smAndDown: shallowRef(false), md: shallowRef(false) }))
  vi.stubGlobal('useAppStore', () => ({ activeFamilyId: '' }))
  vi.stubGlobal('useFamiliesStore', () => ({ getFamilySelectors: async () => {}, getActiveFamily: () => {} }))
  vi.stubGlobal('useRoute', () => ({ path: '/plan-your-week' }))
  vi.stubGlobal('useI18n', () => ({ t: (key: string) => key === 'common.close' ? 'Close' : key }))
  vi.stubGlobal('computed', computed)
  vi.stubGlobal('reactive', reactive)
  vi.stubGlobal('shallowRef', shallowRef)
  vi.stubGlobal('watch', watch)
  vi.stubGlobal('onMounted', onMounted)
})

afterEach(() => { snackbar.dismiss(); vi.useRealTimers(); vi.unstubAllGlobals() })

function layout() {
  return mount(Layout, { global: {
    plugins: [createVuetify()],
    components: { VSnackbar },
    stubs: { VApp: Block, VMain: Block, VContainer: Block, VBtn: Button, VIcon: Block, VNavigationDrawer: Block, VList: Block, VListItem: Block, VOverlay: Overlay, VProgressCircular: Block, TopbarLarge: Block, TopbarSmall: Block, BottomNav: Block },
  } })
}

describe('shared snackbar dismissal and actions', () => {
  it('expires Undo feedback after its configured duration', async () => {
    const wrapper = layout()
    const undo = vi.fn()
    snackbar.show('Added soup to Monday.', { duration: 6000, action: { label: 'Undo', onClick: undo } })
    await nextTick()
    expect(wrapper.find('[role="status"]').text()).toBe('Added soup to Monday.')
    await vi.advanceTimersByTimeAsync(5999)
    expect(wrapper.find('[role="status"]').exists()).toBe(true)
    await vi.advanceTimersByTimeAsync(1)
    expect(wrapper.find('[role="status"]').exists()).toBe(false)
    expect(snackbar.action.value).toBeNull()
    expect(undo).not.toHaveBeenCalled()
  })

  it('starts a fresh timeout when another notification replaces a visible one', async () => {
    const wrapper = layout()
    snackbar.show('First notice')
    await nextTick()
    await vi.advanceTimersByTimeAsync(2000)
    snackbar.show('Second notice')
    await nextTick()
    await vi.advanceTimersByTimeAsync(1000)
    expect(wrapper.find('[role="status"]').text()).toBe('Second notice')
    await vi.advanceTimersByTimeAsync(2000)
    expect(wrapper.find('[role="status"]').exists()).toBe(false)
  })

  it('closes feedback without running its action', async () => {
    const wrapper = layout()
    const undo = vi.fn()
    snackbar.show('Saved', { action: { label: 'Undo', onClick: undo } })
    await nextTick()
    await wrapper.find('button[aria-label="Close"]').trigger('click')
    expect(wrapper.find('[role="status"]').exists()).toBe(false)
    expect(snackbar.action.value).toBeNull()
    expect(undo).not.toHaveBeenCalled()
  })

  it('runs Undo once and allows its result to replace the notification', async () => {
    const wrapper = layout()
    const undo = vi.fn(() => { snackbar.show('Your change was undone.', { type: 'success' }) })
    snackbar.show('Saved', { action: { label: 'Undo', onClick: undo } })
    await nextTick()
    const button = wrapper.findAll('button').find(button => button.text() === 'Undo')
    if (!button) throw new Error('Missing Undo button')
    await button.trigger('click')
    await flushPromises()
    expect(undo).toHaveBeenCalledTimes(1)
    expect(wrapper.find('[role="status"]').text()).toBe('Your change was undone.')
    expect(snackbar.action.value).toBeNull()
  })

  it('keeps Undo disabled while a planning mutation is busy', async () => {
    const wrapper = layout()
    const busy = shallowRef(true)
    const undo = vi.fn()
    snackbar.show('Saved', { action: { label: 'Undo', onClick: undo, disabled: busy } })
    await nextTick()
    const button = wrapper.findAll('button').find(button => button.text() === 'Undo')
    if (!button) throw new Error('Missing Undo button')
    expect(button.attributes('disabled')).toBeDefined()
    await snackbar.runAction()
    expect(undo).not.toHaveBeenCalled()
    busy.value = false
    await nextTick()
    expect(button.attributes('disabled')).toBeUndefined()
    await button.trigger('click')
    expect(undo).toHaveBeenCalledTimes(1)
  })

  it('keeps a newer unrelated notification when a planning owner cleans up', async () => {
    const wrapper = layout()
    const dismissPlanning = snackbar.show('Planning feedback', { action: { label: 'Undo', onClick: vi.fn() } })
    snackbar.show('Other page saved')
    dismissPlanning()
    await nextTick()
    expect(wrapper.find('[role="status"]').text()).toBe('Other page saved')
    expect(snackbar.action.value).toBeNull()
  })
})

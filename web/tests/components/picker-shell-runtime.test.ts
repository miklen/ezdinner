import { afterEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { defineComponent, h } from 'vue'
import { createVuetify } from 'vuetify'
import PickerShell from '~/components/Plan/PickerShell.vue'

enableAutoUnmount(afterEach)
afterEach(() => { vi.unstubAllGlobals() })
const Block = defineComponent({ setup: (_, { slots }) => () => h('div', slots.default?.()) })

describe('picker shell without global Vuetify component registration', () => {
  it.each([1280, 390])('renders a real modal overlay at viewport width %s', async width => {
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: width })
    vi.stubGlobal('visualViewport', null)
    const wrapper = mount(PickerShell, { props: { open: true, title: 'Family dinner', closeLabel: 'Close' }, slots: { default: '<button>Choose a day</button>' }, global: { plugins: [createVuetify()], stubs: { VCard: Block, VIcon: Block } }, attachTo: document.body })
    await flushPromises()
    const modal = document.querySelector('[role="dialog"]')
    expect(modal).not.toBeNull()
    expect(modal?.getAttribute('aria-modal')).toBe('true')
    expect(modal?.textContent).toContain('Choose a day')
    expect(modal?.classList.contains(width < 960 ? 'v-bottom-sheet' : 'v-dialog')).toBe(true)
    await wrapper.setProps({ open: false })
    await flushPromises()
    expect(document.querySelector('.v-overlay--active')).toBeNull()
  })
})

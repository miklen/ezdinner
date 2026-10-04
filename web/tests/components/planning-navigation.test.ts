import { afterEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { computed, defineComponent, h, onMounted, reactive, shallowRef, watch } from 'vue'
import Layout from '~/layouts/default.vue'
import BottomNav from '~/components/BottomNav.vue'

enableAutoUnmount(afterEach)
const Block = defineComponent({ setup: (_, { slots }) => () => h('div', slots.default?.()) })
const Link = defineComponent({ props: { to: String }, setup: (props, { slots }) => () => h('a', { href: props.to }, [slots.prepend?.(), slots.default?.()]) })

describe('additive planning navigation', () => {
  it.each([[false, '/home'], [true, '/home'], [false, '/plan-your-week'], [true, '/plan-your-week']])('retains all routes and shows the planning icon with mobile=%s on %s', async (mobile, path) => {
    vi.stubGlobal('useDisplay', () => ({ smAndDown: shallowRef(mobile), md: shallowRef(false) }))
    vi.stubGlobal('useAppStore', () => ({ activeFamilyId: 'family' }))
    vi.stubGlobal('useFamiliesStore', () => ({ getFamilySelectors: async () => {}, getActiveFamily: async () => {} }))
    vi.stubGlobal('useSnackbar', () => ({ visible: shallowRef(false), color: shallowRef(''), timeout: shallowRef(0), message: shallowRef(''), dismiss: () => {} }))
    vi.stubGlobal('useRoute', () => ({ path }))
    vi.stubGlobal('useI18n', () => ({ t: (key: string) => key }))
    vi.stubGlobal('computed', computed)
    vi.stubGlobal('reactive', reactive)
    vi.stubGlobal('shallowRef', shallowRef)
    vi.stubGlobal('watch', watch)
    vi.stubGlobal('onMounted', onMounted)
    const wrapper = mount(Layout, { global: { components: { BottomNav }, stubs: {
      VApp: Block, VNavigationDrawer: Block, VList: Block, VListItem: Link, VBtn: Link, VIcon: Block,
      VMain: Block, VContainer: Block, VOverlay: Block, VProgressCircular: Block, VSnackbar: Block,
      VBottomNavigation: Block, TopbarLarge: Block, TopbarSmall: Block,
    } } })
    await flushPromises()
    expect(wrapper.findAll('a').map(link => link.attributes('href'))).toEqual(['/home', '/families', '/dishes', '/plan', '/plan-your-week'])
    expect(wrapper.find('a[href="/plan-your-week"]').attributes('aria-label')).toBe('weekPlanning.nav')
    expect(wrapper.find('a[href="/plan-your-week"]').text()).toContain('mdi-calendar-plus')
    expect(wrapper.find('a[href="/plan-your-week"]').text()).not.toContain('mdi-calendar-plus-outline')
  })
})

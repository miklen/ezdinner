import { readFileSync } from 'node:fs'
import { runInNewContext } from 'node:vm'
import { describe, expect, it, vi } from 'vitest'

const destination = '/dishes/00000001-0000-0000-0000-000000000000?familyId=00000002-0000-0000-0000-000000000000&ratingReminderDate=2026-10-11#my-rating'
type WorkerEvent = { data?: { json: () => unknown; text: () => string }; notification?: { close: () => void; data: { destination?: string } }; waitUntil: (promise: Promise<unknown>) => void }
function worker() {
  const listeners = new Map<string, (event: WorkerEvent) => void>()
  const showNotification = vi.fn().mockResolvedValue(undefined)
  const focus = vi.fn().mockResolvedValue(undefined)
  const navigate = vi.fn().mockResolvedValue(undefined)
  const openWindow = vi.fn().mockResolvedValue(undefined)
  const matchAll = vi.fn().mockResolvedValue([{ url: 'https://ezdinner.test/home', focus, navigate }])
  runInNewContext(readFileSync(new URL('../public/sw.js', import.meta.url), 'utf8'), {
    self: { location: { origin: 'https://ezdinner.test' }, addEventListener: (name: string, callback: (event: WorkerEvent) => void) => listeners.set(name, callback), registration: { showNotification } },
    clients: { matchAll, openWindow }, navigator: { language: 'en' }, URL, Date,
  })
  async function click(target?: string) {
    const close = vi.fn()
    let pending = Promise.resolve<unknown>(undefined)
    listeners.get('notificationclick')!({ notification: { close, data: { destination: target } }, waitUntil: promise => { pending = promise } })
    await pending
    expect(close).toHaveBeenCalledOnce()
  }
  async function push(payload: unknown) {
    let pending = Promise.resolve<unknown>(undefined)
    listeners.get('push')!({ data: { json: () => payload, text: () => '' }, waitUntil: promise => { pending = promise } })
    await pending
  }
  return { showNotification, focus, navigate, openWindow, matchAll, click, push }
}
describe('notification destinations', () => {
  it('stores and localizes rating payload then navigates the existing app window', async () => {
    const sw = worker()
    await sw.push({ type: 'rating_reminder', dishName: 'Lasagne', dinnerDate: '2026-10-11', lang: 'da', destination })
    expect(sw.showNotification).toHaveBeenCalledWith('EzDinner', expect.objectContaining({ body: expect.stringContaining('På din menu'), data: { destination } }))
    await sw.click(destination)
    expect(sw.navigate).toHaveBeenCalledWith(destination)
    expect(sw.focus).toHaveBeenCalledOnce()
  })
  it('opens the target on cold entry and ignores unrelated-origin windows', async () => {
    const sw = worker()
    sw.matchAll.mockResolvedValue([{ url: 'https://other.test/', focus: sw.focus, navigate: sw.navigate }])
    await sw.click(destination)
    expect(sw.openWindow).toHaveBeenCalledWith(destination)
    expect(sw.focus).not.toHaveBeenCalled()
  })
  it.each([undefined, 'https://evil.test' + destination, '//evil.test', '/plan', destination.replace('2026-10-11', '2026-02-30')])('keeps root behavior for missing or unsafe destinations', async (target) => {
    const sw = worker()
    await sw.click(target)
    expect(sw.focus).toHaveBeenCalledOnce()
    expect(sw.navigate).not.toHaveBeenCalled()
    sw.matchAll.mockResolvedValue([])
    await sw.click(target)
    expect(sw.openWindow).toHaveBeenCalledWith('/')
  })
  it('preserves destinationless dinner and wish copy', async () => {
    const sw = worker()
    await sw.push({ dishes: ['Lasagne'], lang: 'en' })
    expect(sw.showNotification).toHaveBeenLastCalledWith('EzDinner', expect.objectContaining({ body: "Tonight's dinner: Lasagne", data: null }))
    await sw.push({ type: 'wish_upvoted', dishName: 'Tacos', lang: 'en' })
    expect(sw.showNotification).toHaveBeenLastCalledWith('EzDinner', expect.objectContaining({ body: 'Someone also wants Tacos!', data: null }))
  })
})

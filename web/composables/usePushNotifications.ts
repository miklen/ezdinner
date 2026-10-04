// Module-level refs intentionally create a singleton: push subscription state is shared
// across all callers within the same browser session. This is client-only state.
// If SSR is ever enabled for authenticated routes, migrate to a Pinia store.
const isSubscribed = ref(false)
let _initPromise: Promise<void> | null = null
let subscriptionOwner = ''
let subscriptionGeneration = 0

export function usePushNotifications() {
  const { $msal } = useNuxtApp()
  const account = computed(() => $msal.getObjectId() ?? '')
  const { apiFetch, apiFetchRaw } = useApiFetch()
  const config = useRuntimeConfig()
  const baseUrl = config.public.apiBaseUrl as string
  const { locale } = useI18n()

  const isSupported = computed<boolean>(() => {
    if (import.meta.server) return false
    return 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window
  })

  const isIosSafariWithoutPwa = computed<boolean>(() => {
    if (import.meta.server) return false
    const ua = navigator.userAgent
    const isIos = /iphone|ipad|ipod/i.test(ua)
    const isSafari = /safari/i.test(ua) && !/chrome|crios|fxios/i.test(ua)
    const isPwa = (window.navigator as Navigator & { standalone?: boolean }).standalone === true
    return isIos && isSafari && !isPwa
  })

  function init(): Promise<void> {
    if (subscriptionOwner !== account.value) {
      subscriptionOwner = account.value
      subscriptionGeneration++
      isSubscribed.value = false
      _initPromise = null
    }
    if (_initPromise) return _initPromise
    if (!isSupported.value || !account.value) return Promise.resolve()
    const identity = subscriptionGeneration

    _initPromise = apiFetch<{ isSubscribed: boolean }>('/api/push/subscriptions/me')
      .then(({ isSubscribed: serverState }) => {
        if (identity === subscriptionGeneration) isSubscribed.value = serverState
      })
      .catch(() => {
        if (identity === subscriptionGeneration) isSubscribed.value = false
      })

    return _initPromise
  }

  async function subscribe(): Promise<'ok' | 'denied' | 'error'> {
    const identity = subscriptionGeneration
    if (!isSupported.value) return 'error'

    const appStore = useAppStore()
    if (!appStore.activeFamilyId) return 'error'

    const permission = await Notification.requestPermission()
    if (identity !== subscriptionGeneration) return 'error'
    if (permission !== 'granted') {
      isSubscribed.value = false
      return 'denied'
    }

    try {
      const vapidPublicKey: string = await $fetch(baseUrl + '/api/push/vapid-public-key')
      const registration = await swReady()
      const pushSubscription = await registration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(vapidPublicKey),
      })

      const json = pushSubscription.toJSON()
      if (identity !== subscriptionGeneration) return 'error'
      await apiFetch('/api/push/subscriptions', {
        method: 'POST',
        body: JSON.stringify({
          familyId: appStore.activeFamilyId,
          endpoint: json.endpoint,
          p256dh: json.keys?.p256dh,
          auth: json.keys?.auth,
          language: locale.value,
        }),
      })

      if (identity !== subscriptionGeneration) return 'error'
      isSubscribed.value = true
      return 'ok'
    } catch {
      if (identity === subscriptionGeneration) isSubscribed.value = false
      return 'error'
    }
  }

  async function unsubscribe(): Promise<'ok' | 'error'> {
    const identity = subscriptionGeneration
    try {
      const registration = await swReady()
      const pushSubscription = await registration.pushManager.getSubscription()
      if (pushSubscription) {
        await pushSubscription.unsubscribe()
      }

      if (identity !== subscriptionGeneration) return 'error'

      await apiFetchRaw('/api/push/subscriptions', { method: 'DELETE' })
      if (identity !== subscriptionGeneration) return 'error'
      isSubscribed.value = false
      return 'ok'
    } catch {
      // Do not modify isSubscribed — leave it reflecting server state
      return 'error'
    }
  }

  watch(account, () => { void init() }, { immediate: true, flush: 'sync' })
  return { isSupported, isIosSafariWithoutPwa, isSubscribed, init, subscribe, unsubscribe }
}

function swReady(): Promise<ServiceWorkerRegistration> {
  const timeout = new Promise<never>((_, reject) =>
    setTimeout(() => reject(new Error('Service worker timed out')), 5000),
  )
  return Promise.race([navigator.serviceWorker.ready, timeout])
}

function urlBase64ToUint8Array(base64String: string): Uint8Array<ArrayBuffer> {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4)
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/')
  const rawData = atob(base64)
  return new Uint8Array([...rawData].map((char) => char.charCodeAt(0)))
}

type SnackbarType = 'success' | 'error' | 'info'

// Module-level refs intentionally create a singleton: one snackbar shared across
// the entire app. This is client-only state — the snackbar component only mounts
// inside the default layout which is never SSR-rendered for unauthenticated users.
// If SSR is ever enabled for these routes, migrate this to a Pinia store.
const message = ref('')
const visible = ref(false)
const color = ref<string>('surface-variant')
const timeout = ref(3000)

const colorMap: Record<SnackbarType, string> = {
  success: 'success',
  error: 'error',
  info: 'surface-variant',
}

export function useSnackbar() {
  function show(text: string, options?: { type?: SnackbarType; duration?: number }) {
    message.value = text
    color.value = colorMap[options?.type ?? 'info']
    timeout.value = options?.duration ?? 3000
    visible.value = true
  }

  function dismiss() {
    visible.value = false
  }

  return { show, dismiss, message, visible, color, timeout }
}

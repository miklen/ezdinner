import { ref, shallowRef } from 'vue'
import type { Ref } from 'vue'

type SnackbarType = 'success' | 'error' | 'info'

interface SnackbarAction {
  label: string
  onClick: () => void | Promise<void>
  disabled?: Ref<boolean>
}

// Module-level refs intentionally create a singleton: one snackbar shared across
// the entire app. This is client-only state — the snackbar component only mounts
// inside the default layout which is never SSR-rendered for unauthenticated users.
// If SSR is ever enabled for these routes, migrate this to a Pinia store.
const message = ref('')
const visible = ref(false)
const color = ref<string>('surface-variant')
const timeout = ref(3000)
const action = shallowRef<SnackbarAction | null>(null)
const id = ref(0)

const colorMap: Record<SnackbarType, string> = {
  success: 'success',
  error: 'error',
  info: 'primary',
}

export function useSnackbar() {
  function show(text: string, options?: { type?: SnackbarType; duration?: number; action?: SnackbarAction }) {
    const notificationId = ++id.value
    message.value = text
    color.value = colorMap[options?.type ?? 'info']
    timeout.value = options?.duration ?? 3000
    action.value = options?.action ?? null
    visible.value = true
    return () => { if (id.value === notificationId) dismiss() }
  }

  function dismiss() {
    visible.value = false
    action.value = null
  }

  async function runAction() {
    const currentAction = action.value
    if (!visible.value || !currentAction || currentAction.disabled?.value) return
    dismiss()
    await currentAction.onClick()
  }

  return { show, dismiss, runAction, message, visible, color, timeout, action, id }
}

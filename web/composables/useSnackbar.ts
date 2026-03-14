type SnackbarType = 'success' | 'error' | 'info'

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

  return { show, message, visible, color, timeout }
}

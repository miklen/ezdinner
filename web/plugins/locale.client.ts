const STORAGE_KEY = 'ezdinner_locale'

export default defineNuxtPlugin((nuxtApp) => {
  const i18n = nuxtApp.$i18n as { locale: Ref<string>; setLocale: (locale: string) => Promise<void> }

  // Restore saved locale on startup
  const saved = localStorage.getItem(STORAGE_KEY)
  if (saved && saved !== i18n.locale.value) {
    i18n.setLocale(saved)
  }

  // Persist whenever locale changes
  watch(i18n.locale, (val) => {
    localStorage.setItem(STORAGE_KEY, val)
  })
})

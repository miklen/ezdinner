export default defineNuxtPlugin(() => {
  if (!('serviceWorker' in navigator)) return

  navigator.serviceWorker.register('/sw.js').catch((err) => {
    console.warn('Service Worker registration failed:', err)
  })
})

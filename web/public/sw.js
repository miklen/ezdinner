const i18n = {
  da: { tonight: 'Aftensmad' },
  en: { tonight: "Tonight's dinner" },
}

self.addEventListener('push', function (event) {
  let title = 'EzDinner'
  let body = ''

  if (event.data) {
    try {
      const payload = event.data.json()
      title = payload.title || title
      if (Array.isArray(payload.dishes) && payload.dishes.length > 0) {
        const lang = (payload.lang || navigator.language || '').startsWith('da') ? 'da' : 'en'
        body = i18n[lang].tonight + ': ' + payload.dishes.join(', ')
      } else {
        body = payload.body || body
      }
    } catch {
      body = event.data.text()
    }
  }

  event.waitUntil(
    self.registration.showNotification(title, {
      body,
      icon: '/icon.png',
    })
  )
})

self.addEventListener('notificationclick', function (event) {
  event.notification.close()

  event.waitUntil(
    clients
      .matchAll({ type: 'window', includeUncontrolled: true })
      .then(function (clientList) {
        for (const client of clientList) {
          if ('focus' in client) {
            return client.focus()
          }
        }
        if (clients.openWindow) {
          return clients.openWindow('/')
        }
      })
  )
})

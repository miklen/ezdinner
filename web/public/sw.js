const i18n = {
  da: {
    tonight: 'Aftensmad',
    ratingReminder: (dishName, date) => `Hvordan var ${dishName}? På din menu ${date}.`,
    wishUpvoted: (dishName) => `Nogen vil også have ${dishName}!`,
    wishGranted: (dishName, dateCtx) => `${dishName} er på menuen ${dateCtx}! 🎉`,
    dateCtx: {
      tonight: 'i aften',
      tomorrow: 'i morgen',
      thisWeek: (date) => 'på ' + date.toLocaleDateString('da', { weekday: 'long' }),
      nextWeek: 'næste uge',
      later: (date) => 'den ' + date.toLocaleDateString('da', { day: 'numeric', month: 'long' }),
    },
  },
  en: {
    tonight: "Tonight's dinner",
    ratingReminder: (dishName, date) => `How was ${dishName}? On your menu ${date}.`,
    wishUpvoted: (dishName) => `Someone also wants ${dishName}!`,
    wishGranted: (dishName, dateCtx) => `${dishName} is on the menu ${dateCtx}! 🎉`,
    dateCtx: {
      tonight: 'tonight',
      tomorrow: 'tomorrow',
      thisWeek: (date) => 'on ' + date.toLocaleDateString('en', { weekday: 'long' }),
      nextWeek: 'next week',
      later: (date) => 'on ' + date.toLocaleDateString('en', { day: 'numeric', month: 'long' }),
    },
  },
}

function getLang(payload) {
  return (payload.lang || navigator.language || '').startsWith('da') ? 'da' : 'en'
}

function formatDateContext(isoDate, t) {
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const target = new Date(isoDate)
  target.setHours(0, 0, 0, 0)
  const daysAhead = Math.round((target - today) / 86400000)

  if (daysAhead === 0) return t.tonight
  if (daysAhead === 1) return t.tomorrow
  if (daysAhead <= 6) return t.thisWeek(target)
  if (daysAhead <= 13) return t.nextWeek
  return t.later(target)
}

// Take control immediately when a new version is deployed, rather than
// waiting for all open tabs to close. Prevents stale sw.js from handling
// push events that were meant for the new version.
self.addEventListener('install', (event) => {
  self.skipWaiting()
})

self.addEventListener('push', function (event) {
  let title = 'EzDinner'
  let body = ''
  let destination = null

  if (event.data) {
    try {
      const payload = event.data.json()
      title = payload.title || title
      const lang = getLang(payload)
      const t = i18n[lang]

      if (payload.type === 'rating_reminder') {
        destination = ratingDestination(payload.destination)
        const date = new Date(payload.dinnerDate + 'T12:00:00Z')
        body = t.ratingReminder(payload.dishName, date.toLocaleDateString(lang, { day: 'numeric', month: 'long', timeZone: 'Europe/Copenhagen' }))
      } else if (payload.type === 'wish_upvoted') {
        body = t.wishUpvoted(payload.dishName)
      } else if (payload.type === 'wish_granted') {
        const dateCtx = formatDateContext(payload.date, t.dateCtx)
        body = t.wishGranted(payload.dishName, dateCtx)
      } else if (Array.isArray(payload.dishes) && payload.dishes.length > 0) {
        body = t.tonight + ': ' + payload.dishes.join(', ')
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
      data: destination ? { destination } : null,
    })
  )
})

self.addEventListener('notificationclick', function (event) {
  event.notification.close()

  const destination = ratingDestination(event.notification.data?.destination)
  event.waitUntil(
    clients
      .matchAll({ type: 'window', includeUncontrolled: true })
      .then(async function (clientList) {
        for (const client of clientList) {
          if (new URL(client.url).origin !== self.location.origin) continue
          if ('focus' in client) {
            if (destination) await client.navigate(destination)
            return client.focus()
          }
        }
        if (clients.openWindow) {
          return clients.openWindow(destination || '/')
        }
      })
  )
})

function ratingDestination(value) {
  if (typeof value !== 'string' || !value.startsWith('/') || value.startsWith('//')) return null
  try {
    const url = new URL(value, self.location.origin)
    const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
    const dish = /^\/dishes\/([^/]+)$/.exec(url.pathname)?.[1]
    const family = url.searchParams.get('familyId')
    const date = url.searchParams.get('ratingReminderDate')
    if (url.origin !== self.location.origin || url.hash !== '#my-rating' || url.searchParams.size !== 2
      || !guid.test(dish || '') || !guid.test(family || '') || dish === '00000000-0000-0000-0000-000000000000' || family === '00000000-0000-0000-0000-000000000000'
      || !/^\d{4}-\d{2}-\d{2}$/.test(date || '') || new Date(date + 'T00:00:00Z').toISOString().slice(0, 10) !== date) return null
    return `/dishes/${dish}?familyId=${family}&ratingReminderDate=${date}#my-rating`
  } catch { return null }
}

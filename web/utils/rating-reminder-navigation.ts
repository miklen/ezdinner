import { reminderDateSchema, reminderIdentitySchema } from '~/types/rating-reminders'

const returnKey = 'ezdinner:rating-reminder-return'
export function parseRatingReminderDestination(destination: unknown) {
  if (typeof destination !== 'string' || !destination.startsWith('/') || destination.startsWith('//')) return null
  try {
    const url = new URL(destination, 'https://ezdinner.invalid')
    if (url.origin !== 'https://ezdinner.invalid' || url.hash !== '#my-rating') return null
    const match = /^\/dishes\/([0-9a-f-]+)$/i.exec(url.pathname)
    if (!match || url.searchParams.size !== 2 || url.searchParams.getAll('familyId').length !== 1 || url.searchParams.getAll('ratingReminderDate').length !== 1) return null
    const dishId = reminderIdentitySchema.safeParse(match[1])
    const familyId = reminderIdentitySchema.safeParse(url.searchParams.get('familyId'))
    const dinnerDate = reminderDateSchema.safeParse(url.searchParams.get('ratingReminderDate'))
    if (!dishId.success || !familyId.success || !dinnerDate.success) return null
    const to = `/dishes/${dishId.data}?familyId=${familyId.data}&ratingReminderDate=${dinnerDate.data}#my-rating`
    return { dishId: dishId.data, familyId: familyId.data, dinnerDate: dinnerDate.data, to }
  } catch { return null }
}

export function clearRatingReminderReturn(storage: Pick<Storage, 'removeItem'> = sessionStorage) { storage.removeItem(returnKey) }
export function rememberRatingReminderReturn(destination: unknown, storage: Pick<Storage, 'setItem' | 'removeItem'> = sessionStorage) {
  const intent = parseRatingReminderDestination(destination)
  if (intent) storage.setItem(returnKey, intent.to)
  else storage.removeItem(returnKey)
}
export function consumeRatingReminderReturn(storage: Pick<Storage, 'getItem' | 'removeItem'> = sessionStorage) {
  const intent = parseRatingReminderDestination(storage.getItem(returnKey))
  storage.removeItem(returnKey)
  return intent?.to ?? '/home'
}

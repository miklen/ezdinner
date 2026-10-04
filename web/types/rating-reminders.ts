import { z } from 'zod'
import { DateTime } from 'luxon'

export const reminderDateSchema = z.string().regex(/^\d{4}-\d{2}-\d{2}$/).refine(value => DateTime.fromISO(value).isValid)
export const reminderIdentitySchema = z.guid().refine(value => value !== '00000000-0000-0000-0000-000000000000')
export const ratingReminderSchema = z.object({ dishId: reminderIdentitySchema, dishName: z.string().min(1), dinnerDate: reminderDateSchema })
export const ratingReminderQueueSchema = z.object({ today: reminderDateSchema, reminders: z.array(ratingReminderSchema) })
export const ratingReminderPreferenceSchema = z.object({ pushEnabled: z.boolean() })
export type RatingReminder = z.infer<typeof ratingReminderSchema>
export type ReminderAction = 'idle' | 'rating' | 'dismissing'
export type ReminderRequestState = { status: 'idle' | 'loading' | 'ready' } | { status: 'failed'; error: 'loadFailed' }
export type ReminderActionState = { status: ReminderAction } | { status: 'failed'; action: 'rating' | 'dismissing' }

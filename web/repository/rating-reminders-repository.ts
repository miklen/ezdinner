import { reminderDateSchema, ratingReminderQueueSchema, ratingReminderPreferenceSchema } from '~/types/rating-reminders'

type ApiFetch = <T>(path: string, options?: Parameters<typeof $fetch>[1]) => Promise<T>

export class RatingRemindersRepository {
  constructor(private apiFetch: ApiFetch) {}

  async get(familyId: string, signal?: AbortSignal) {
    return ratingReminderQueueSchema.parse(await this.apiFetch<unknown>(`/api/families/${familyId}/rating-reminders`, { signal }))
  }

  dismiss(familyId: string, dishId: string, dinnerDate: string) {
    return this.apiFetch<unknown>(`/api/families/${familyId}/rating-reminders/${dishId}/dismiss`, {
      method: 'PUT', body: { dinnerDate: reminderDateSchema.parse(dinnerDate) },
    })
  }

  async preferences(signal?: AbortSignal) {
    return ratingReminderPreferenceSchema.parse(await this.apiFetch<unknown>('/api/rating-reminders/preferences', { signal }))
  }

  async setPreference(pushEnabled: boolean) {
    return ratingReminderPreferenceSchema.parse(await this.apiFetch<unknown>('/api/rating-reminders/preferences', {
      method: 'PUT', body: ratingReminderPreferenceSchema.parse({ pushEnabled }),
    }))
  }
}

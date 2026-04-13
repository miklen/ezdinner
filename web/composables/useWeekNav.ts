import { DateTime } from 'luxon'

export function useWeekNav() {
  const todayWeekday = DateTime.now().weekday
  const defaultWeekStart = todayWeekday >= 6
    ? DateTime.now().plus({ weeks: 1 }).startOf('week')
    : DateTime.now().startOf('week')

  const weekStart = ref(defaultWeekStart)

  return { weekStart }
}

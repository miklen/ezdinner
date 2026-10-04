import { rememberRatingReminderReturn } from '~/utils/rating-reminder-navigation'
export default defineNuxtRouteMiddleware((to) => {
  if (to.path === '/') return
  const { $msal } = useNuxtApp()
  if (!$msal.isAuthenticated.value) {
    rememberRatingReminderReturn(to.fullPath)
    return navigateTo('/')
  }
})

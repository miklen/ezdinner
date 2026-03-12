// Protects all routes except the landing page
export default defineNuxtRouteMiddleware((to) => {
  if (to.path === '/') return
  const { $msal } = useNuxtApp()
  if (!$msal.isAuthenticated.value) return navigateTo('/')
})

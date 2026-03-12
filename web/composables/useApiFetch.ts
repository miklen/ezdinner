/**
 * Wraps $fetch with automatic Bearer token injection.
 * Replaces the old @nuxtjs/axios + axios-auth.ts interceptor.
 */
export function useApiFetch() {
  const { $msal } = useNuxtApp()
  const config = useRuntimeConfig()
  const baseUrl = config.public.apiBaseUrl as string

  async function buildHeaders(extra?: Record<string, string>): Promise<Record<string, string>> {
    const token = await $msal.acquireToken()
    const headers: Record<string, string> = { 'Content-Type': 'application/json', ...extra }
    if (token) headers.Authorization = `Bearer ${token}`
    return headers
  }

  async function apiFetch<T>(path: string, options: Parameters<typeof $fetch>[1] = {}): Promise<T> {
    return $fetch<T>(baseUrl + path, {
      ...options,
      headers: await buildHeaders(options.headers as Record<string, string>),
    })
  }

  // Raw fetch for endpoints where the status code matters (e.g. 200 vs 204)
  async function apiFetchRaw(path: string, options: RequestInit = {}): Promise<{ status: number; data: unknown }> {
    const headers = await buildHeaders(options.headers as Record<string, string>)
    const response = await fetch(baseUrl + path, { ...options, headers })
    const data = response.status === 204 ? null : await response.json().catch(() => null)
    return { status: response.status, data }
  }

  return { apiFetch, apiFetchRaw }
}

import type { RouterConfig } from '@nuxt/schema'

// MSAL redirect returns a hash like #state=eyJ...&client_info=...&code=...
// Vue Router's default scrollBehavior tries document.querySelector(hash) on
// these, which fails because the value is not a valid CSS selector.
// Detect OAuth/MSAL response hashes and skip scroll — MSAL will consume and
// remove the hash itself via handleRedirectPromise().
function isMsalHash(hash: string): boolean {
  return hash.includes('state=') && hash.includes('code=')
}

export default <RouterConfig>{
  scrollBehavior(to, _from, savedPosition) {
    if (savedPosition) return savedPosition
    if (to.hash && !isMsalHash(to.hash)) return { el: to.hash, behavior: 'smooth' }
    return { top: 0 }
  },
}

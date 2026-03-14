import type { MsalService } from '~/plugins/msal.client'

declare module '#app' {
  interface NuxtApp {
    $msal: MsalService
  }
}

declare module 'vue' {
  interface ComponentCustomProperties {
    $msal: MsalService
  }
}

export {}

import * as msal from '@azure/msal-browser'
import { ref } from 'vue'

const SCOPES = [
  'openid',
  'profile',
  'offline_access',
  'https://ezlifehacks.onmicrosoft.com/98d97e1a-7c16-4f2f-89e3-ca839335a122/backendapi',
]

export class MsalService {
  private instance!: msal.PublicClientApplication
  private options!: { passwordAuthority: string }

  readonly isAuthenticated = ref(false)
  private initPromise!: Promise<void>

  async init(config: {
    clientId: string
    loginAuthority: string
    passwordAuthority: string
    knownAuthority: string
    redirectUri: string
  }) {
    this.options = { passwordAuthority: config.passwordAuthority }

    const msalConfig: msal.Configuration = {
      auth: {
        clientId: config.clientId,
        authority: config.loginAuthority,
        knownAuthorities: [config.knownAuthority],
        redirectUri: config.redirectUri,
      },
      cache: { cacheLocation: 'localStorage' },
      system: {
        loggerOptions: {
          piiLoggingEnabled: false,
          logLevel: msal.LogLevel.Warning,
          loggerCallback: (level, message, containsPii) => {
            if (containsPii) return
            if (level === msal.LogLevel.Error) console.error(message)
            else if (level === msal.LogLevel.Warning) console.warn(message)
          },
        },
      },
    }

    this.instance = new msal.PublicClientApplication(msalConfig)
    this.initPromise = this.instance.initialize().then(() => this.handleRedirect())
    await this.initPromise
  }

  private async handleRedirect() {
    const response = await this.instance.handleRedirectPromise()
    if (response?.account) {
      this.isAuthenticated.value = true
    } else {
      const accounts = this.instance.getAllAccounts()
      this.isAuthenticated.value = accounts.length > 0
    }
  }

  async signIn() {
    await this.initPromise
    try {
      await this.instance.loginRedirect({ scopes: SCOPES })
    } catch (err: any) {
      if (err?.message?.includes('AADB2C90118')) {
        // Password reset flow
        try {
          const result = await this.instance.loginPopup({
            scopes: SCOPES,
            authority: this.options.passwordAuthority,
          })
          this.isAuthenticated.value = !!result.account
        } catch (e) {
          console.error(e)
        }
      }
    }
  }

  async signOut() {
    await this.initPromise
    await this.instance.logoutRedirect()
    this.isAuthenticated.value = false
  }

  async acquireToken(): Promise<string | null> {
    await this.initPromise
    const account = this.instance.getAllAccounts()[0]
    if (!account) return null
    try {
      const response = await this.instance.acquireTokenSilent({ account, scopes: SCOPES })
      return response.accessToken
    } catch (error) {
      if (error instanceof msal.InteractionRequiredAuthError) {
        await this.instance.acquireTokenRedirect({ account, scopes: SCOPES })
      } else {
        await this.signOut()
      }
      return null
    }
  }

  getFirstName(): string | undefined {
    const claims = this.instance.getAllAccounts()[0]?.idTokenClaims as any
    return claims?.given_name
  }

  getLastName(): string | undefined {
    const claims = this.instance.getAllAccounts()[0]?.idTokenClaims as any
    return claims?.family_name
  }

  getObjectId(): string | undefined {
    return this.instance.getAllAccounts()[0]?.localAccountId
  }
}

export default defineNuxtPlugin(async () => {
  const config = useRuntimeConfig()
  const msalService = new MsalService()

  await msalService.init({
    clientId: config.public.clientId as string,
    loginAuthority: config.public.loginAuthority as string,
    passwordAuthority: config.public.passwordAuthority as string,
    knownAuthority: config.public.knownAuthority as string,
    redirectUri: window.location.origin,
  })

  return {
    provide: { msal: msalService },
  }
})

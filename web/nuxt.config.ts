export default defineNuxtConfig({
  ssr: false,

  app: {
    head: {
      titleTemplate: 'Dinner Planner - %s',
      title: 'Dinner Planner',
      meta: [
        { charset: 'utf-8' },
        { name: 'viewport', content: 'width=device-width, initial-scale=1' },
        { name: 'description', content: 'Plan and track your dinner - get suggestions based on your history!' },
        { property: 'og:title', content: 'Easy Dinner Planner' },
        { property: 'og:description', content: 'Plan and track your dinner - get suggestions based on your history!' },
        { property: 'og:site_name', content: 'Easy Dinner Planner' },
      ],
      link: [{ rel: 'icon', type: 'image/x-icon', href: '/favicon.ico' }],
    },
  },

  modules: [
    'vuetify-nuxt-module',
    '@pinia/nuxt',
    '@nuxt/eslint',
  ],

  vuetify: {
    moduleOptions: {
      importComposables: true,
    },
    vuetifyOptions: {
      theme: {
        defaultTheme: 'light',
      },
    },
  },

  runtimeConfig: {
    public: {
      apiBaseUrl: process.env.NUXT_PUBLIC_API_BASE_URL || 'http://localhost:7071',
      clientId: '654aa80d-4783-43db-8ed9-4e160bb1d765',
      loginAuthority: 'https://ezlifehacks.b2clogin.com/ezlifehacks.onmicrosoft.com/B2C_1A_signup_signin/',
      passwordAuthority: 'https://ezlifehacks.b2clogin.com/ezlifehacks.onmicrosoft.com/B2C_1A_PasswordReset/',
      knownAuthority: 'https://ezlifehacks.b2clogin.com',
    },
  },

  experimental: {
    appManifest: false,
  },

  typescript: {
    strict: true,
  },

  compatibilityDate: '2025-01-01',
})

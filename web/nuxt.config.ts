export default defineNuxtConfig({
  ssr: false,

  app: {
    head: {
      titleTemplate: 'EzDinner - %s',
      title: 'EzDinner',
      meta: [
        { charset: 'utf-8' },
        { name: 'viewport', content: 'width=device-width, initial-scale=1' },
        { name: 'description', content: 'Plan and track your family dinners — get suggestions based on your history!' },
        { property: 'og:title', content: 'EzDinner' },
        { property: 'og:description', content: 'Plan and track your family dinners — get suggestions based on your history!' },
        { property: 'og:site_name', content: 'EzDinner' },
        { name: 'theme-color', content: '#3A6B4A' },
      ],
      link: [
        { rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' },
        { rel: 'icon', type: 'image/x-icon', href: '/favicon.ico' },
        { rel: 'apple-touch-icon', sizes: '180x180', href: '/apple-touch-icon.png' },
        { rel: 'manifest', href: '/site.webmanifest' },
        { rel: 'preconnect', href: 'https://fonts.googleapis.com' },
        { rel: 'preconnect', href: 'https://fonts.gstatic.com', crossorigin: '' },
        {
          rel: 'stylesheet',
          href: 'https://fonts.googleapis.com/css2?family=DM+Serif+Display&family=DM+Sans:ital,opsz,wght@0,9..40,300;0,9..40,400;0,9..40,500;0,9..40,600;1,9..40,300;1,9..40,400&display=swap',
        },
      ],
    },
  },

  css: ['~/assets/global.scss'],

  modules: [
    'vuetify-nuxt-module',
    '@pinia/nuxt',
    '@nuxt/eslint',
    '@nuxtjs/i18n',
  ],

  i18n: {
    strategy: 'no_prefix',
    locales: [
      { code: 'en', language: 'en-US', file: 'en.json' },
      { code: 'da', language: 'da-DK', file: 'da.json' },
    ],
    defaultLocale: 'en',
    langDir: 'locales',
    lazy: true,
    detectBrowserLanguage: false,
  },

  vuetify: {
    moduleOptions: {
      importComposables: true,
      styles: {
        configFile: 'assets/settings.scss',
      },
    },
    vuetifyOptions: {
      theme: {
        defaultTheme: 'light',
        themes: {
          light: {
            colors: {
              primary: '#6FAF7A',
              'primary-darken-1': '#3A6B4A',
              'primary-lighten-1': '#8FC49A',
              secondary: '#8C7060',
              accent: '#D4A84B',
              background: '#FAF7F4',
              surface: '#FFFFFF',
              'surface-variant': '#F5F0EB',
              success: '#4A7C3F',
              error: '#C62828',
            },
          },
        },
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

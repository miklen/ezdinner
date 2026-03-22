## Why

EzDinner is primarily used by Danish families, but all UI text is in English. Adding internationalization with Danish and English allows the app to feel native to its primary audience, defaulting to Danish for users in Denmark while remaining accessible to English speakers.

## What Changes

- Install and configure `@nuxtjs/i18n` module in the Nuxt 3 frontend
- Create translation files for English (`en.json`) and Danish (`da.json`) covering all UI strings
- Replace all hardcoded strings in Vue components and pages with `$t()` / `useI18n()` calls
- Default language is auto-detected from the browser/OS locale (Danish for `da-*` locales, English otherwise)
- A language switcher UI component is added to the user menu (TopbarProfile) and the mobile top bar
- The user's language choice is persisted to `localStorage` so it survives page reloads and new sessions

## Capabilities

### New Capabilities

- `language-switching`: Language switcher UI that lets users toggle between English and Danish, persisted via localStorage and accessible from the top bar on both desktop and mobile

### Modified Capabilities

<!-- No existing spec-level behavior is changing — this is additive infrastructure -->

## Impact

- **Frontend only** — no backend changes required; all strings are client-side UI text
- **`web/nuxt.config.ts`** — add `@nuxtjs/i18n` module with locale configuration
- **All `.vue` files under `web/`** — replace hardcoded strings with `$t()` calls
- **New files**: `web/i18n/en.json`, `web/i18n/da.json`, `web/components/LanguageSwitcher.vue`
- **`web/components/TopbarProfile.vue`** and **`web/components/TopbarSmall.vue`** — embed language switcher
- **Dependency**: `@nuxtjs/i18n` v9 (Nuxt 3 compatible)

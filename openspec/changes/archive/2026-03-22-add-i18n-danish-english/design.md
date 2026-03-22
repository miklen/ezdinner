## Context

EzDinner's frontend is a Nuxt 3 SPA using Vuetify 3 and Pinia. All UI strings are currently hardcoded in English inside `.vue` files. The app has no existing i18n infrastructure. The primary user base is Danish families; English support is retained as the default for non-Danish locales.

## Goals / Non-Goals

**Goals:**
- Add `@nuxtjs/i18n` v9 as the i18n solution, integrated with Nuxt 3's module system
- Cover 100% of user-visible strings across all pages and components
- Auto-detect locale from browser/OS (`navigator.language`), defaulting to Danish for `da-*` locales
- Persist language choice in `localStorage` so it survives reloads
- Provide a compact language switcher accessible from both desktop (TopbarProfile menu) and mobile (TopbarSmall)
- Translate all existing English strings to Danish

**Non-Goals:**
- Backend/API translation (dish names, opt-out reasons, family names remain as entered by users)
- RTL layout support
- More than two languages (EN + DA) in this change
- URL-based locale prefixing (e.g. `/da/plan`) — unnecessary for a single-page app with auth

## Decisions

### Decision: `@nuxtjs/i18n` v9 over a custom composable
`@nuxtjs/i18n` is the standard Nuxt 3 i18n solution. It provides auto-imports (`useI18n`, `$t`), Nuxt devtools integration, and lazy-loaded locale files. A custom solution would replicate this work without benefit.

**Alternatives considered**: `vue-i18n` directly (requires manual plugin wiring, no Nuxt devtools support).

### Decision: `strategy: 'no_prefix'`
Since EzDinner is an authenticated SPA with no SEO requirements, URL-prefixed locales add routing complexity with no benefit. `no_prefix` keeps all existing routes unchanged.

**Alternatives considered**: `prefix_except_default` — adds `/da/` prefix; unnecessary friction, breaks existing bookmarks.

### Decision: JSON translation files in `web/i18n/`
Flat JSON files (`en.json`, `da.json`) with namespaced dot-notation keys (e.g. `nav.home`, `dishes.addDish`) are simple to maintain and directly supported by `@nuxtjs/i18n`'s lazy loading.

### Decision: Language switcher in TopbarProfile menu (desktop) + TopbarSmall (mobile)
The profile menu is the natural home for account/preference settings on desktop. On mobile, TopbarSmall currently only shows the app title and has space for a compact globe icon button.

### Decision: Locale detection order
1. `localStorage` key `ezdinner_locale` (user's saved preference)
2. Browser `navigator.language` — map `da*` → `da`, everything else → `en`
3. Fallback: `en`

`@nuxtjs/i18n`'s `detectBrowserLanguage` option handles steps 2–3. Step 1 is handled automatically by `@nuxtjs/i18n` when `useCookie: false` and `storageKey` is configured.

### Decision: Translation key structure
Namespaced by feature area using dot-notation:
- `nav.*` — navigation labels
- `home.*` — home page strings
- `plan.*` — plan page strings
- `dishes.*` — dish catalog and detail strings
- `families.*` — families page strings
- `common.*` — shared strings (cancel, create, save, etc.)

## Risks / Trade-offs

- **Risk**: Untranslated strings missed during initial pass → **Mitigation**: Grep for untranslated string patterns after implementation; `@nuxtjs/i18n` logs warnings for missing keys in dev mode.
- **Risk**: `@nuxtjs/i18n` v9 compatibility with Nuxt 3 version pinned in project → **Mitigation**: Check exact version compatibility before installing; `@nuxtjs/i18n` v9 targets Nuxt 3.x.
- **Risk**: Vuetify component labels (e.g. `v-text-field` `label` prop) need `$t()` — easy to miss → **Mitigation**: Search for `label="` and `placeholder="` patterns as a dedicated pass.
- **Trade-off**: Storing locale in localStorage instead of a user profile setting means the preference is device-local. Acceptable for now given no backend preference storage exists.

## Migration Plan

1. Install `@nuxtjs/i18n` and register in `nuxt.config.ts`
2. Create `web/i18n/en.json` and `web/i18n/da.json` with all keys
3. Replace strings in components/pages in feature order (nav → home → plan → dishes → families)
4. Add `LanguageSwitcher` component and wire into TopbarProfile and TopbarSmall
5. Manual smoke test: switch language, reload, confirm persistence; verify both locales render correctly on all pages

Rollback: removing the module registration and reverting `.vue` files restores the previous state with no data loss.

## Open Questions

- Should the date format also switch locale? (`DinnerHeroCard` uses `en-US` for `toLocaleDateString`). Recommended: yes — use `useI18n().locale` to pass the active locale to date formatters.

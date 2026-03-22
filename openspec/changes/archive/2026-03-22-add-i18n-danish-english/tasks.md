## 1. Install and configure @nuxtjs/i18n

- [x] 1.1 Install `@nuxtjs/i18n` v9 (`npm install @nuxtjs/i18n`)
- [x] 1.2 Register `@nuxtjs/i18n` module in `web/nuxt.config.ts` with `strategy: 'no_prefix'`, locales `en` and `da`, `defaultLocale: 'en'`, and `detectBrowserLanguage` configured to map `da*` → `da` with `localStorage` persistence via `storageKey: 'ezdinner_locale'`

## 2. Create translation files

- [x] 2.1 Create `web/i18n/en.json` with all English strings for navigation, home page, plan page, dishes catalog, dish detail, families page, and common labels (cancel, create, save, remove, etc.)
- [x] 2.2 Create `web/i18n/da.json` with Danish translations for every key in `en.json` — covering navigation, home, plan, dishes, families, common, and all dialog/error/status strings

## 3. Replace strings in layout and navigation

- [x] 3.1 Update `web/layouts/default.vue` — translate nav link titles (`Home`, `Families`, `Dishes`, `Plan`) in the `links` computed and rail tooltip
- [x] 3.2 Update `web/components/BottomNav.vue` — ensure nav labels use translated values passed from layout
- [x] 3.3 Update `web/components/TopbarProfile.vue` — translate `Sign out` list item title

## 4. Replace strings in Home page

- [x] 4.1 Update `web/components/Home/DinnerHeroCard.vue` — translate greeting (`Good morning/afternoon/evening`), `TONIGHT` eyebrow, `View plan`, `Nothing planned for tonight`, `Plan tonight`; pass active locale to `toLocaleDateString` so day/month names localize
- [x] 4.2 Update `web/pages/home.vue` — translate `TOMORROW` label, `Nothing planned —`, `plan tomorrow` link text, and the `EmptyState` `message` and `action-label`
- [x] 4.3 Update `web/components/Home/QuickStats.vue` — translate `dishes planned this week`, `top rated, not had in X days`, `no rated dishes yet`

## 5. Replace strings in Plan page

- [x] 5.1 Update `web/pages/plan.vue` — translate `Week N weekend` section label
- [x] 5.2 Update `web/components/Plan/SuggestionBar.vue` — translate `Suggestions`, `Suggest week`, `Suggest again`, `Clear suggestions` title, the algo-info ranked-by description, and `No more options — all days are covered or candidates exhausted`
- [x] 5.3 Update `web/components/Plan/SuggestedDish.vue` — translate `Use`, `Reroll this day` title, `never` / `Xd ago` day-format labels, and aria-labels for the info button
- [x] 5.4 Update `web/components/Plan/PlannedDinnerDetails.vue` — translate `Add dish to menu`, `Search dishes...`, `Add dish` sheet title, `No dishes yet`, `Start typing to search`, `Skip day`, `Cancel`, `Remove`, `Other reason...` placeholder, `Create "X"` create-dish entry, and opt-out quick picks default list (`Vacation`, `Eating out`, `Restaurant`, `Guests`, `Leftovers`)
- [x] 5.5 Update `web/components/Plan/EffortSelector.vue` — translate effort level labels if any are hardcoded

## 6. Replace strings in Dishes catalog and detail

- [x] 6.1 Update `web/pages/dishes/index.vue` — translate page heading `Dishes`, `Add dish` button, `Find a dish…` placeholder, sort chip labels (`Name`, `Rating`, `Times used`, `Last used`), `Filter` / `× clear` filter controls, `Archived` filter tag, archived banner text, empty state messages, `New dish` dialog title/label/buttons
- [x] 6.2 Update `web/pages/dishes/[id].vue` — translate all dialog titles, button labels, snackbar messages, and section headings on the dish detail page
- [x] 6.3 Update `web/components/Dish/DishDetailHeader.vue` — translate any hardcoded strings
- [x] 6.4 Update `web/components/Dish/DishMetadataCard.vue` — translate metadata field labels and season/effort enum display values
- [x] 6.5 Update `web/components/Dish/DishNotesCard.vue` — translate labels and placeholder text
- [x] 6.6 Update `web/components/Dish/DishOverflowMenu.vue` — translate menu item labels
- [x] 6.7 Update `web/components/Dish/DishCard.vue` — translate any visible strings (ratings labels, stats)
- [x] 6.8 Update `web/components/Plan/TopDishes.vue` — translate section heading and any labels
- [x] 6.9 Update `web/components/EmptyState.vue` — ensure it renders translated strings passed as props (no internal hardcoded text to change, but verify)

## 7. Replace strings in Families page

- [x] 7.1 Update `web/pages/families.vue` — translate `Family members` subtitle, `Create family` card title and body text, dialog titles (`Invite family member`, `New family`, `Merge into account`, `Create family member`), button labels (`Invite`, `Create`, `Merge`, `Cancel`), field placeholders, alert messages, tooltip text (`A family must have at least one owner`, `Remove Owner`), and `Make Owner` title attribute
- [x] 7.2 Update `web/components/Family/FamilyListItems.vue` — translate any visible strings

## 8. Build LanguageSwitcher component

- [x] 8.1 Create `web/components/LanguageSwitcher.vue` — a compact globe icon button (`mdi-web`) that opens a `v-menu` with two items: "English" and "Dansk"; active locale gets a checkmark; clicking an item calls `setLocale()` from `useI18n()`; styled to match the existing topbar aesthetic (subtle icon button)

## 9. Wire language switcher into top bars

- [x] 9.1 Add `<LanguageSwitcher>` to `web/components/TopbarProfile.vue` — insert before the avatar button so it appears in the top-right area on desktop
- [x] 9.2 Add `<LanguageSwitcher>` to `web/components/TopbarSmall.vue` — insert at the right side of the mobile top bar

## 10. Verify and clean up

- [x] 10.1 Run `npm run lint` and fix any TypeScript/ESLint errors introduced by the i18n migration
- [x] 10.2 Grep for remaining hardcoded English strings in `.vue` files that were missed (search for common patterns like `label="`, `placeholder="`, hardcoded sentence-case text in templates)
- [x] 10.3 Manual smoke test in both languages: navigate all pages in English then Danish, verify all strings translate, reload and confirm locale persists, test on mobile viewport

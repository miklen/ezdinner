# EzDinner UX Overhaul — From Engineer-Built to Beautiful

## Context

EzDinner is a family dinner planning app that works but looks and feels like an engineer built it — stock Vuetify grey, no visual hierarchy, no personality. The app serves two distinct usage patterns: **desktop weekly planning** (browsing dishes, building the week) and **mobile quick-checks** (what's for dinner tonight, quick swaps). The current UI treats both the same, with mobile being a shrunk-down desktop. Future features like AI suggestions and calendar integration need a design foundation that can accommodate them gracefully.

---

## Visual Personality

Before touching code, commit to a design personality that drives every decision in the phases below.

**Statement:** A confident family kitchen — not a restaurant, not a wellness brand. The app lives between a weathered recipe notebook and a modern bistro menu. Heavy, warm typography for headings. Generous breathing room. One bold accent that earns its place. The UI should feel like it belongs in the same world as the food it plans.

**What this rules out:** Pastel gradients, wellness-app roundness, meal-kit startup terracotta-for-terracotta's-sake. Every color and font choice should serve the kitchen-notebook personality, not abstract "food warmth."

**The one thing someone will remember:** The typography contrast — a heavy, editorial heading typeface against clean, small-weight body text. The size jump creates instant visual hierarchy that feels designed, not assembled.

This personality statement should be referenced when making any visual decision not explicitly specified in the plan. When in doubt, ask: does this feel like a confident family kitchen?

---

## Status Tracker

Each phase is independently deployable after its prerequisites. Deploy incrementally — do not batch phases.

| Phase | Status | Prerequisites | Deploys |
|-------|--------|---------------|---------|
| 1. Design System Foundation | ✅ Complete | — | Warm palette + fonts, EmptyState, DishPill, snackbar infra, a11y |
| 2. Layout + Navigation | ✅ Complete | Phase 1 | Bottom nav, icon rail, page transitions |
| 3. Home Page | Not started | Phases 1 + 2 | Redesigned home page + home skeleton loaders |
| 4. Dish Cards + Catalog | Not started | Phase 1 | Redesigned dish cards + catalog + skeleton loaders |
| 5. Plan Page | Not started | Phases 1 + 2 | Redesigned plan page + plan skeleton loaders |
| 6. Dish Detail | Not started | Phase 4 | Redesigned dish detail page + skeleton loader |
| 7. Landing Page Refresh | Not started | Phase 1 | Redesigned landing page (run alongside or after Phase 1) |

Note: Phases 3, 4, and 5 are independent of each other and can be done in any order.

---

## Phase 1: Design System Foundation

Establish the visual identity without changing any page logic. The app immediately feels warmer.

### 1a. Color Palette + Vuetify Theme

**File:** `web/nuxt.config.ts` — define custom theme in `vuetify.theme`

| Role | Color | Rationale |
|------|-------|-----------|
| Primary | `#D4652A` (warm terracotta) | Food, warmth, appetite |
| Primary-darken | `#B8511D` | Hover/active states |
| Primary-lighten | `#E8884F` | Backgrounds, gradients |
| Secondary | `#6B8F5E` (sage green) | Fresh, natural complement |
| Accent/Rating | `#E5A83B` (warm gold) | Stars, hearts, highlights |
| Background | `#FAF7F4` (warm off-white) | Replace cold grey-lighten-3 |
| Surface | `#FFFFFF` | Cards |
| Surface-variant | `#F5F0EB` | Subtle differentiation |
| Success | `#4A7C3F` | "Planned" indicators |
| Error | `#C62828` | Destructive actions |

### 1b. Typography

- **Headings:** `DM Serif Display` — warm, editorial, heavy contrast against body
- **Body:** `DM Sans` — clean, modern companion
- Load via Google Fonts link in `nuxt.config.ts` head
- Override Vuetify font-family in `web/assets/settings.scss`

**Full type scale** (define as CSS custom properties in `global.scss`):

| Token | Size | Weight | Line Height | Letter Spacing | Usage |
|-------|------|--------|-------------|----------------|-------|
| `--text-xs` | 12px | 400 | 1.4 | +0.02em | captions, labels |
| `--text-sm` | 14px | 400 | 1.5 | 0 | secondary body |
| `--text-base` | 16px | 400 | 1.6 | 0 | primary body |
| `--text-lg` | 20px | 500 | 1.4 | 0 | card titles, nav |
| `--text-xl` | 24px | 400 | 1.2 | -0.01em | section headings (DM Serif Display) |
| `--text-2xl` | 32px | 400 | 1.1 | -0.02em | page headings (DM Serif Display) |
| `--text-3xl` | 48px | 400 | 1.0 | -0.03em | hero headings (DM Serif Display) |

All-caps labels (e.g. "TODAY" badge, stat strip labels) use `--text-xs` + `letter-spacing: +0.08em` + `font-weight: 600`.

### 1c. Global Styles + Full Token System

**New file:** `web/assets/global.scss`

**Color custom properties** (mirroring the Vuetify palette for use outside components):
- All palette colors as `--color-*` vars

**Spacing scale** (8px base grid):

| Token | Value | Usage |
|-------|-------|-------|
| `--space-1` | 4px | tight internal padding |
| `--space-2` | 8px | icon gaps, small insets |
| `--space-3` | 12px | compact card padding |
| `--space-4` | 16px | standard card padding |
| `--space-6` | 24px | section gaps |
| `--space-8` | 32px | page section spacing |
| `--space-12` | 48px | large page sections |

**Border radius scale:**

| Token | Value | Usage |
|-------|-------|-------|
| `--radius-sm` | 6px | chips, badges, small pills |
| `--radius-md` | 12px | buttons, inputs, small cards |
| `--radius-lg` | 16px | standard cards |
| `--radius-xl` | 24px | hero cards, bottom sheets |
| `--radius-full` | 9999px | avatar, icon buttons, FAB |

**Shadow scale:**

| Token | Value | Usage |
|-------|-------|-------|
| `--shadow-xs` | `0 1px 2px rgba(0,0,0,0.05)` | subtle card separation |
| `--shadow-sm` | `0 1px 3px rgba(0,0,0,0.08), 0 1px 2px rgba(0,0,0,0.04)` | resting cards |
| `--shadow-md` | `0 4px 6px rgba(0,0,0,0.07), 0 2px 4px rgba(0,0,0,0.04)` | hovered cards, dropdowns |
| `--shadow-lg` | `0 10px 15px rgba(0,0,0,0.08), 0 4px 6px rgba(0,0,0,0.04)` | modals, bottom nav |

**Z-index scale** (define in `:root`):

| Token | Value | Layer |
|-------|-------|-------|
| `--z-base` | 0 | normal flow |
| `--z-raised` | 10 | cards on hover |
| `--z-dropdown` | 100 | autocomplete, menus |
| `--z-sticky` | 200 | sticky headers, week nav |
| `--z-nav` | 300 | bottom nav, icon rail |
| `--z-snackbar` | 400 | toast notifications |
| `--z-modal` | 500 | dialogs, overlays |

**Motion vocabulary** (define as CSS custom properties):

| Token | Value | Use case |
|-------|-------|----------|
| `--duration-instant` | 100ms | button press, toggle |
| `--duration-fast` | 200ms | card hover, chip add/remove |
| `--duration-normal` | 300ms | panel expand, page transition |
| `--duration-slow` | 500ms | hero entrance, celebration |
| `--ease-out` | `cubic-bezier(0.0, 0.0, 0.2, 1.0)` | entrances |
| `--ease-in` | `cubic-bezier(0.4, 0.0, 1.0, 1.0)` | exits |
| `--ease-standard` | `cubic-bezier(0.4, 0.0, 0.2, 1.0)` | general transitions |
| `--ease-spring` | `cubic-bezier(0.34, 1.56, 0.64, 1)` | playful pop moments |

Add `@media (prefers-reduced-motion: reduce)` block that sets all duration vars to `0ms`.

**Card overrides:** `rounded-lg` (using `--radius-lg`), `elevation-0`, `border: 1px solid rgba(0,0,0,0.06)`, `box-shadow: var(--shadow-sm)`

**Page transition classes** (`.page-enter-active`, `.page-leave-to`) using `--duration-normal` + `--ease-out`

**Typography utility classes** using the scale defined in 1b: `.text-page-title` (2xl, DM Serif Display), `.text-section-title` (xl, DM Serif Display), `.text-card-title` (lg, DM Sans 500), `.text-caption` (xs, all-caps variant)

Import in `nuxt.config.ts` via `css: ['~/assets/global.scss']`

### 1d. Quick wins in existing files

- `web/layouts/default.vue` — change `bg-grey-lighten-3` to warm background
- `web/components/TopbarLarge.vue` — restyle with logotype in DM Serif Display, warm tones; this is the primary brand moment — treat the logotype with care, not as an afterthought
- `web/components/TopbarProfile.vue` — family name as styled pill/chip next to avatar

### 1e. Dish Primitive Component

**New component:** `web/components/Dish/DishPill.vue`

A dish represented as a UI element appears in at least three places before DishCard is redesigned in Phase 4: the DinnerHeroCard (Phase 3), the expanded day view (Phase 5), and autocomplete results (Phase 5). Define the canonical dish-as-pill now to avoid three divergent implementations.

- Props: `name`, `size` (sm/md), `removable` (bool)
- Styling: `--radius-sm`, warm border, dish name truncated to one line
- Emits: `remove` when removable + close icon clicked
- This component is composed by DishCard, DinnerHeroCard, and PlannedDinnerDetails

### 1f. EmptyState Component

**New component:** `web/components/EmptyState.vue`

Moved here from Phase 4 — Phase 3 needs it immediately for "no dinner planned tonight."

- Props: `icon`, `message`, `actionLabel`, `actionTo`
- Warm muted icon centered, friendly message, CTA button
- Reusable across home (no dinner), dishes (no dishes), plan (no plans)

### 1g. Feedback Infrastructure

**New composable:** `web/composables/useSnackbar.ts`

Moved here from Phase 6 — mutations happen in Phases 3–5. Building those interactions without feedback means every phase ships a broken UX that gets fixed retroactively.

- `show(message, options?)` — queues a snackbar notification
- Options: `type` (success/error/info), `duration` (default 3000ms)
- State stored in `appStore` or a standalone composable
- The `v-snackbar` in `layouts/default.vue` subscribes to this state
- Snackbar *styling* can be refined in Phase 6; the infrastructure ships now

### 1h. Accessibility Baseline

Before writing a single page component, verify and document:

- **Contrast ratios**: Check `#D4652A` on `#FAF7F4` — this fails WCAG AA for body text (< 4.5:1). Use `#B8511D` (primary-darken) for text-on-background uses, reserve `#D4652A` for large headings and icon fills only
- **Focus states**: Vuetify's default focus ring is often invisible against custom themes. Define a global focus style in `global.scss`: `outline: 2px solid var(--color-primary); outline-offset: 2px`
- **Touch targets**: 48px minimum height for all interactive elements — chips, rating hearts, nav items, close buttons
- **Reduced motion**: The `@media (prefers-reduced-motion: reduce)` block from 1c must zero out all durations

**Files touched:** `nuxt.config.ts`, `assets/settings.scss`, `assets/global.scss` (new), `layouts/default.vue`, `TopbarLarge.vue`, `TopbarProfile.vue`, `Dish/DishPill.vue` (new), `EmptyState.vue` (new), `composables/useSnackbar.ts` (new), `stores/appStore.ts` (snackbar state)

### Verification
- `npm run dev` — visual check at localhost:3000
- Confirm warm palette applies globally
- Verify `#D4652A` is not used for small body text — use a contrast checker
- Check TopbarLarge logotype and TopbarProfile styling
- Confirm focus ring visible on keyboard navigation
- `EmptyState` renders with all prop combinations
- `useSnackbar().show('test')` displays a snackbar
- `DishPill` renders in sm and md sizes, removable variant emits correctly
- `npm run lint` and `npm test` pass

---

## Phase 2: Layout + Navigation

Fix the navigation model — bottom nav on mobile, slim icon rail on desktop.

### 2a. Mobile: Bottom Navigation

**New component:** `web/components/BottomNav.vue`
- Vuetify `v-bottom-navigation` with 4 tabs: Home, Plan, Dishes, Families
- Filled icons for active tab, outlined for inactive
- Primary color active indicator
- Only rendered on `smAndDown` breakpoint

**Why bottom nav over hamburger:** The primary mobile use case is "what's for dinner?" — a persistent bottom nav is one tap vs hamburger's two taps. The 56px cost is worth it.

### 2b. Desktop: Icon Rail

- Replace the `cols="2"` left nav column with a Vuetify `v-navigation-drawer` in `rail` mode
- Icons only + tooltips, primary color for active item
- Recovers ~14% horizontal space for content (critical for plan page)
- **Icon set decision**: Confirm which MDI icons to use for each nav item (Home → `mdi-home`, Plan → `mdi-calendar-week`, Dishes → `mdi-silverware-fork-knife`, Families → `mdi-account-group`) and document in `global.scss` as a comment block. Consistency in icon weight and style across the whole app should be decided here, not per-component.

### 2c. Simplify Content.vue

- Remove the outer `v-sheet` wrapper that forces all content into one white box
- Keep as a thin layout helper: optional two-column split (main + support slot) without visual container
- Pages compose their own card layouts directly

### 2d. Mobile Top Bar

- `web/components/TopbarSmall.vue` — remove hamburger drawer entirely, show family name + avatar only

### 2e. Page Transitions

**File:** `web/app.vue`
- Add `<NuxtPage>` transition: subtle fade + slide-up
- Use `--duration-normal` + `--ease-out` from Phase 1c token system
- Transition CSS classes (`.page-enter-active`, `.page-leave-to`) are already defined in `global.scss` from Phase 1c — just wire them to `<NuxtPage :transition="...">` here

Placed in Phase 2 because it's a layout-level concern (lives in `app.vue` and `layouts/default.vue`), not page-level. Every page phase that follows benefits from transitions on deploy.

**Files touched:** `BottomNav.vue` (new), `layouts/default.vue`, `Content.vue`, `TopbarSmall.vue`, `app.vue`

### Verification
- Mobile (375px DevTools): bottom nav visible, all 4 tabs navigate correctly, no hamburger
- Desktop (1440px): icon rail visible, tooltips on hover, active state highlighted
- Content area uses full available width
- Navigating between pages shows subtle fade + slide-up transition
- Transition is absent when `prefers-reduced-motion: reduce` is set
- `npm run lint` and `npm test` pass

---

## Phase 3: Home Page — "What's for Dinner?"

The emotional center of the app. Mobile-first design.

### 3a. Greeting + Hero Card

**New component:** `web/components/Home/DinnerHeroCard.vue`
- Time-aware greeting: "Good evening, [firstName]"
- Today's date: "Saturday, March 14"
- Tonight's dinner as a hero card with warm gradient background (primary-lighten to surface)
- Dish names rendered using `DishPill` (defined in Phase 1e) — not custom pill markup
- Empty state: use `EmptyState` component (defined in Phase 1f) with "Plan tonight" CTA — not a one-off inline empty state
- Tapping navigates to plan page with today pre-selected

### 3b. Tomorrow Preview

- Smaller card below hero, secondary/muted colors
- Dish name preview line or "Plan tomorrow" link

### 3c. Quick Stats Strip

**New component:** `web/components/Home/QuickStats.vue`
- Horizontal row of 3 compact stat cards:
  - "X dishes this week" (fork icon)
  - "Top dish: [name]" (heart icon)
  - "X days since [favorite]" (calendar icon)
- Makes insights discoverable without burying them in a sidebar
- Data comes from existing `dishRepo.allUsageStats()` and `dinnerRepo.getRange()`

### 3d. Desktop Sidebar — Visual Top Dishes

- Restyle `web/components/Plan/TopDishes.vue` (or new `Home/TopDishesVisual.vue`)
- Replace plain table with horizontal bar charts: dish name + mini hearts + frequency bar
- On desktop: shown as right column (4 cols), on mobile: hidden or below stats

### 3e. Skeleton Loading — Home

Replace the full-page overlay spinner with contextual skeleton loaders scoped to this page:
- Skeleton for DinnerHeroCard (one large card placeholder)
- Skeleton for QuickStats strip (three small card placeholders)
- Use Vuetify `v-skeleton-loader` — no custom skeleton markup needed

**Files touched:** `pages/home.vue` (rewrite template), `DinnerHeroCard.vue` (new), `QuickStats.vue` (new), `Plan/TopDishes.vue` (restyle or replace)

Note: `EmptyState.vue` and `DishPill.vue` are consumed here but defined in Phase 1 — no need to create them in this phase.

### Verification
- Mobile: greeting visible, hero card prominent, stats strip scrollable if needed
- Desktop: two-column layout with visual top dishes in sidebar
- Empty state renders correctly when no dinners are planned
- Tonight/tomorrow data loads and displays correctly
- Skeleton loaders appear during data fetch, replaced by content on load
- `npm run lint` and `npm test` pass

---

## Phase 4: Dish Cards + Catalog

### 4a. DishCard Cleanup

**File:** `web/components/Dish/DishCard.vue`

Current problem: edit/move/delete icons share visual prominence with the dish name.

Changes:
- Move admin actions (edit name, move occurrences, delete) behind a three-dot `v-menu` overflow in the top-right corner
- **New component:** `web/components/Dish/DishOverflowMenu.vue`
- Card layout: dish name (large, clickable) -> heart rating -> one-line stat caption ("Had 12 times, last 5 days ago")
- Subtle left border color accent based on rating tier (gold 4-5, primary 2.5-3.5, grey 0-2.5)
- Desktop hover: `translateY(-2px)` + deeper shadow, 150ms transition

### 4b. Catalog Page

**File:** `web/pages/dishes/index.vue`
- Search bar: full-width, rounded, warm border, "Find a dish..." placeholder
- Sort: filter chips below search (A-Z, Rating, Most Used, Last Used) instead of dropdown
- Grid: maintain responsive cols but with refreshed card style

### 4c. Empty State

`EmptyState.vue` was moved to Phase 1 — it's needed by Phase 3 before this phase runs. Wire it into the catalog page here (`dishes/index.vue` when no dishes exist), but do not recreate it.

### 4d. Skeleton Loading — Dishes Catalog

Replace the full-page overlay spinner with contextual skeleton loaders scoped to this page:
- 3–4 skeleton dish card placeholders while the catalog loads
- Use Vuetify `v-skeleton-loader`

**Files touched:** `Dish/DishCard.vue`, `Dish/DishOverflowMenu.vue` (new), `pages/dishes/index.vue`

### Verification
- Dish cards show clean layout: name -> rating -> stat line
- Three-dot menu opens with edit/move/delete actions, all still functional
- Search and filter chips work correctly on catalog page
- Empty state renders when no dishes exist
- Hover effect works on desktop
- Skeleton loaders appear during data fetch, replaced by content on load
- `npm run lint` and `npm test` pass

---

## Phase 5: Plan Page — Weekly Planning

The most complex page. Benefits from all prior phases being complete.

### 5a. Week Navigation

**New component:** `web/components/Plan/WeekNav.vue`
- Horizontal week tabs: "This Week", "Next Week", forward/back arrows
- Replaces the hidden date-range-picker-behind-a-text-field pattern
- On mobile: swipeable; on desktop: clickable tabs
- Emits date range changes to parent
- **Swipe implementation**: Vuetify does not provide touch gestures. Use `useSwipe` from VueUse (`@vueuse/core` — already a transitive dependency via Nuxt). `useSwipe(el, { onSwipeEnd: (e, dir) => dir === 'left' ? nextWeek() : prevWeek() })`. Do not hand-roll touchstart/touchend tracking.

### 5b. Day Cards (replacing timeline)

**Restyle:** `web/components/Plan/PlannedDinner.vue`
- Each day becomes a distinct card instead of a timeline row
- **Today:** highlighted primary border, warm background tint, "TODAY" badge, auto-scrolled into view on mobile
- **Past + planned:** normal styling, muted text, green check
- **Past + unplanned:** dimmed single line "Monday — not tracked", collapsed
- **Future + planned:** normal styling, dish names shown
- **Future + unplanned:** dashed border, "+" icon, "Tap to plan" hint
- **Weekend days:** subtle background variation for visual scanning

### 5c. Expanded Day Details

**Restyle:** `web/components/Plan/PlannedDinnerDetails.vue`
- Smooth expand transition — **avoid `max-height` animation** (animating to `max-height: 1000px` when content is 200px produces non-linear, jarring easing). Use the modern CSS technique instead: `grid-template-rows: 0fr` → `1fr` with `transition: grid-template-rows var(--duration-normal) var(--ease-out)`. The inner element needs `overflow: hidden`. This gives a true content-height animation.
- Current menu items as removable `DishPill` components (Phase 1e), not v-list
- Autocomplete dropdown: show dish rating + "last had X days ago" per item
- "Create new dish" option styled as a distinct action at bottom of dropdown

### 5d. Desktop Sidebar

**New component (optional):** `web/components/Plan/MiniCalendar.vue`
- Wrap `v-date-picker` with custom day slots — dots on planned days
- Below it: condensed Top Dishes

### 5e. Skeleton Loading — Plan

Replace the full-page overlay spinner with contextual skeleton loaders scoped to this page:
- 7 skeleton day card placeholders (one per day of the week) while the plan loads
- Use Vuetify `v-skeleton-loader`

**Files touched:** `pages/plan.vue`, `Plan/WeekNav.vue` (new), `Plan/PlannedDinner.vue`, `Plan/PlannedDinnerDetails.vue`, `Plan/MiniCalendar.vue` (new, optional)

### Verification
- Week navigation switches weeks correctly, date range updates
- Today's card is visually distinct and auto-scrolled on mobile
- Past/future/planned/unplanned all render with correct visual states
- Expanding a day card shows smooth transition
- Adding/removing dishes from menu still works
- Creating a new dish from autocomplete still works
- Skeleton day cards appear during data fetch, replaced by content on load
- `npm run lint` and `npm test` pass

---

## Phase 6: Dish Detail

The dish detail page is the last major page to redesign. Page transitions, skeleton loaders, and snackbar infrastructure were distributed to earlier phases — this phase is focused and independently deployable after Phase 4 (Dish Cards).

### 6a. Dish Detail Page

**File:** `web/pages/dishes/[id].vue`

- **Header:** Custom section replacing embedded DishCard — large dish name in DM Serif Display, prominent heart rating, usage stat line, breadcrumb ("Dishes > Pasta Carbonara")
- **Admin actions:** Overflow menu or toolbar, not inline with title
- **Notes/Recipe:** Styled view mode with warm-tinted card background, recipe link as chip with link icon, subtle edit icon button
- **Family ratings:** Avatar + name + interactive hearts per row, current user highlighted with "You" badge, average rating shown at section top
- **Dates history:** Replace plain `v-table` with visual representation

### 6b. Dates Visualization

**New component:** `web/components/Dish/DatesVisualization.vue`

Recommended approach: Summary line ("Had 12 times in 6 months, roughly every 15 days") + styled date list with "days since" labels instead of raw table. Optionally, a dot chart showing last 3-6 months with dots on dates the dish was had — reveals frequency patterns visually.

### 6c. Skeleton Loading — Dish Detail

- Skeleton for header area (large title + rating placeholders) while dish data loads
- Use Vuetify `v-skeleton-loader`

**Files touched:** `pages/dishes/[id].vue`, `Dish/DatesVisualization.vue` (new)

### Verification
- Dish detail page renders with new header, breadcrumb, styled notes
- Family ratings interactive — own rating editable, others read-only
- Dates visualization shows meaningful summary
- Skeleton loaders appear during data fetch, replaced by content on load
- Snackbar fires on rating update (wired via `useSnackbar` from Phase 1)
- `npm run lint` and `npm test` pass

---

## Phase 7: Landing Page Refresh

> **Timing note:** The landing page is the first thing new users see — it sets brand expectations before they ever reach the app. Consider running this phase in parallel with Phase 1 (palette + fonts apply to both), or at minimum immediately after Phase 1 rather than after all 6 app phases. Shipping 6 polished internal phases with a dated public facade creates a mismatch that undermines first impressions.

**File:** `web/layouts/landing.vue`
- Apply new color palette, typography, and token system from Phase 1
- Restyle hero section, features grid, footer with the Visual Personality in mind (§ "Visual Personality" above)
- Subtle CSS entrance animations for feature cards — use motion vocabulary from Phase 1c (staggered `animation-delay`, `--ease-out`, `--duration-normal`)
- Ensure the landing page sets the visual expectation for the quality inside

### Verification
- Landing page uses new palette and fonts
- Visual quality matches internal app
- Entrance animations respect `prefers-reduced-motion`
- Sign-in flow still works
- Responsive on mobile and desktop

---

## Key Design Decisions

| Decision | Trade-off | Rationale |
|----------|-----------|-----------|
| Bottom nav on mobile | Uses 56px vertical space | Primary use case ("what's for dinner?") needs one-tap navigation, not hamburger's two-tap |
| Icon rail on desktop | No text labels | Only 4 nav items; icons + tooltips are sufficient, recovers 14% width for content |
| Overflow menu on DishCard | Admin actions require extra tap | Viewing is 10x more frequent than editing; visual clarity wins |
| No new UI libraries | Constrains design options | Vuetify 3 theming + custom CSS is sufficient; avoids bundle bloat and framework conflicts |
| Skeleton loaders over spinner | More code per page | Reduces perceived load time; full-page spinner blocks all interaction |
| Day cards over timeline | More vertical space | Each day gets clear tap target and visual state; timeline dots are too subtle |

---

## Learnings Log

Record insights, gotchas, and adjustments discovered during implementation here so future sessions benefit:

### Phase 2 Learnings
- [2026-03-14] `mdi-calendar-week-outline` does not exist in MDI — use `mdi-calendar-blank-outline` instead. Check MDI icon list before using `-outline` variants; not all icons have them.
- [2026-03-14] Do not set `top: Npx !important` on `v-navigation-drawer` — this breaks Vuetify's layout offset calculations. Let `v-app` handle positioning automatically.
- [2026-03-14] `v-bottom-navigation` with routed `v-btn :to` does not need a `v-model` — Vue Router active state drives the active class natively.
- [2026-03-14] Don't remove the hamburger until the bottom nav is confirmed working in the browser — lesson from Phase 1 repeated; stage mobile nav changes only after testing.

### Phase 1 Learnings
- [2026-03-14] `$body-font-family` is NOT a configurable variable in `vuetify/settings` (not `!default`) — overriding it via `@forward ... with (...)` in `settings.scss` throws a Sass error. Override font via `.v-application, .v-application *` in `global.scss` instead.
- [2026-03-14] `useSnackbar` implemented as module-level refs (not inside `defineStore`) — singleton state without Pinia overhead, simpler to consume
- [2026-03-14] Vuetify `$body-font-family` SCSS variable in `settings.scss` overrides the font globally; no need for per-component overrides
- [2026-03-14] TopbarSmall: keep hamburger drawer until Phase 2 bottom nav ships — removing it early leaves mobile with no navigation. Only replace when Phase 2 is fully wired.
- [2026-03-14] `--color-primary` (#D4652A) fails WCAG AA on light backgrounds for body text — `--color-primary-dark` (#B8511D) is the text-safe alias; documented and enforced via `--color-text-*` token names

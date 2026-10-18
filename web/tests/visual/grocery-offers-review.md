# Grocery UI review — 2026-10-18

## Recommendation status and offer presentation

Replaced stacked coverage paragraphs with a dedicated status card using the existing green palette, DM Sans/DM Serif typography, spacing and radii. Routine missing offers use a quiet badge and reassurance; store/language changes and stale offers have an explicit refresh action. Request failures have a separate alert, retry action, and different copy for retained versus absent results. Native details/summary controls reveal the reason and daily check schedule without overwhelming the default view. Both languages distinguish unavailable offers, incomplete ingredient matching, disabled offers, no selected stores, checked offers with no matches, unknown status and loading.

The request form now has its own compact panel and a two-column desktop filter row. Mobile uses full-width explanation text and an accessible icon refresh button. Offer cards emphasise store, product, price and savings, retain visible purchase/price-check deadlines, and put supporting evidence in expandable details. Existing dish cards, ratings, navigation and assignment controls are reused.

Screenshots successfully reviewed in a separate Edge tab: authenticated Danish app at desktop width, expanded Danish explanations at 390px, and the actual components with English fixture data at 390px. The English fixture had no horizontal overflow (clientWidth and scrollWidth both 390). Initial screenshots of the user's original tab failed; using a separate review tab resolved that. A later 320px/error-state visual check stalled, so it is not claimed as visually verified. A browser viewport reset was attempted but the connection stalled before confirmation. Error/retry, refresh, loading, optional setup, both languages and retained planning interactions are covered by tests.

203 focused grocery/planning tests passed; the final 50 grocery tests passed after copy refinements and empty-result retry assertions. Production build passed, lint has only the existing DishMetadataCard warning, and whitespace checks passed. The fixture now accepts `coverage=Unavailable` (or another contract status) and `request-error=1` for repeatable state review. No family preferences or dinners were changed.

## Live app refinement

Reviewed the authenticated local `/families` page after the user reported the configuration was visually poor. The original 21-row list made a narrow family card multiple screens tall and repeated the same unavailable copy for every shop.

Replaced it with a compact summary, saved-store chips, and a Choose/Edit stores action. Editing reuses `Plan/PickerShell.vue`: centered desktop dialog, mobile bottom sheet, scrollable choices, persistent footer, and focus restoration. Store choices use selectable outlined tiles with the existing green accent; border shops have their own group, flyer labels remain visible, and all-unavailable coverage is explained once. Members see only their saved selection.

Live checks: Danish desktop and 390px mobile; English at 320px; all 21 options present; Space toggles selection and count; long border-store names wrap; Cancel retains saved preferences and restores focus to the initiating button. There is no horizontal page overflow. Narrow screens use one column below 361px, and mobile actions have 44px minimum height. No family store preferences were saved or changed during visual inspection.

Validation after refinement: 384 frontend tests passed, lint passed with the existing DishMetadataCard warning, production build passed. Added interaction coverage for opening/cancelling the chooser, member summaries, saving/retrying, late account responses, and one shared unavailable notice.

## Earlier fixture review

Store coverage follow-up (2026-10-18): inspected the added "Worth checking yourself" / "Værd at tjekke selv" section in the actual GroceryStatus component through the Vite fixture. Danish desktop and 390px mobile screenshots show Lidl with a completed empty check and Nemlig with an unavailable check; Netto, which has a usable offer, is excluded. English at 390px also wraps cleanly within the existing card. Existing headings, green status footer, refinement controls and dish cards remain consistent. The temporary viewport override was reset. These are synthetic source results, not claims about the stores' live availability.

Used the local Vite fixture with the actual Family/GroceryStores, RecommendationExplorer, PlanningDishCard, DishCard and DishRating components, global tokens, Vuetify and both locale dictionaries. No live grocery source or authenticated backend was involved.

- Desktop English and Danish: existing dish title, heart rating, metadata, compact outlined controls, card border/radius and assignment actions retained. Grocery facts occupy the existing reason area. Purchase deadline and evidence cutoff are separate, and the future dinner date does not imply future offer availability.
- 375px mobile Danish: long dish title, grocery evidence, prices and Danish date labels wrap inside the existing card. Assignment actions wrap without horizontal overflow. Store choices use one column in the narrow family card.
- Keyboard Space selected REMA 1000; Save stores enabled and returned to disabled after the fixture confirmed persistence. The same dynamic list rendered 21 entries, including flyer and border labels.
- Dish title retained the /dishes/{id} destination. Its click and the separate assignment button produced distinct fixture outcomes. The grocery source link remains separate.
- A continuously visible 15-second evidence deadline switched both the single coverage status and the card to stale evidence without reloading, reranking or removing the dish. Assignment remained available.
- Unit/component coverage supplements visual checks for member read-only controls, failed save/retry, late account responses, source-disabled but selectable stores, retained language/selection guards, exclusive midday/midnight deadlines, timeout disposal, malformed contracts and legacy unknown coverage.

Limitations: the fixture confirms presentation and local component interactions; it does not verify real authentication, Cosmos persistence, live source coverage, or a complete owner/member production workflow. No claim is made that all source stores have live offer support.

Reproduce with `npx.cmd vite --config tests/visual/vite.config.ts --port 3036`, then `/tests/visual/grocery-offers.html`. Query options: `lang=da`, `mobile=1`, `member=1`, `fail=1`, `expire=1`.

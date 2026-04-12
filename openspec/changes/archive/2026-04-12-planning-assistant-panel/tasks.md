## 1. Dish Picker Quick Wins (Dinner Card)

- [x] 1.1 Sort `filteredDishes` in `PlannedDinnerDetails.vue` by last-used descending (longest ago first), with wished dishes still floating above all others
- [x] 1.2 Add effort level badge to `PlanDishRow.vue` — colored badge showing Quick/Medium/Elaborate; hidden when effort level is unset
- [x] 1.3 Add effort badge i18n keys and translations to `en.json` and `da.json`

## 2. Retire Suggestion Bar

- [x] 2.1 Remove `PlanSuggestionBar` component reference and `@dish:used` handler from `plan.vue`
- [x] 2.2 Remove `onSuggestionUsed` function from `plan.vue`

## 3. Load Dish Stats on Plan Page

- [x] 3.1 Call `dishRepo.allUsageStats` in `plan.vue`'s `loadWeek` function and attach stats to dishes in `dishesStore` so the Planning Assistant panel has last-used data
- [x] 3.2 Verify `dishStats` is populated on dish objects in `dishesStore.dishes` after load (check `DishRow.vue` daysSince path)

## 4. Planning Assistant Panel — Core Component

- [x] 4.1 Create `web/components/Plan/AssistantPanel.vue` — accepts `weekStart` prop (DateTime) and emits `dish:assigned` event
- [x] 4.2 Implement dish list: read from `dishesStore.dishes` + `wishlistStore.wishes`; sort by last-used descending (never-used at top); render compact `AssistantDishRow` sub-component per dish
- [x] 4.3 Create `web/components/Plan/AssistantDishRow.vue` — shows dish name, effort badge, days-ago, wish vote indicator, and assign `+` button
- [x] 4.4 Implement search field in panel — filters dish list by name (case-insensitive)
- [x] 4.5 Implement effort filter in panel — Quick / Medium / Elaborate toggle; excludes dishes with no effort set when filter is active; combines with search
- [x] 4.6 Add "never used" visual distinction in `AssistantDishRow` — distinct label and style when `dishStats.lastUsed` is absent

## 5. Planning Assistant Panel — Day Assignment

- [x] 5.1 Implement inline day-picker in `AssistantDishRow` — activated by `+` button; shows 7 day slots for the current week (short weekday + date)
- [x] 5.2 Day slot shows dish-count indicator when the day already has dishes planned (pass current dinner state from panel props)
- [x] 5.3 Day slot shows checkmark when the current dish is already assigned to that day
- [x] 5.4 On day slot click: call `dinnerRepo.addDishToMenu`, emit `dish:assigned`, close the day picker
- [x] 5.5 Close day picker when clicking outside or pressing Escape

## 6. Planning Assistant Panel — Wishlist Toggle

- [x] 6.1 Add Plan / Wishlist mode toggle at top of `AssistantPanel.vue`
- [x] 6.2 In Wishlist mode: render `PlanWishList` component (reuse existing)
- [x] 6.3 Show active wish count badge on Wishlist toggle when `wishlistStore.wishes.length > 0`
- [x] 6.4 Add toggle i18n keys to `en.json` and `da.json`

## 7. Wire Panel into Plan Page

- [x] 7.1 Remove `PlanWishList` from the `#support` slot in `plan.vue`
- [x] 7.2 Add `PlanAssistantPanel` to the `#support` slot, passing `weekStart` and current dinners state
- [x] 7.3 Handle `dish:assigned` event from panel — refresh dinners and wishlist (same as existing `menuUpdated` flow)

## 8. Backend — AI Week Planner Endpoint

- [x] 8.1 Create `AiWeekPlanRequest` model in `EzDinner.Functions/Models` — fields: `weekStart` (string ISO date), `context` (string, optional)
- [x] 8.2 Create `AiWeekPlanSuggestion` response model — array of `{ date, dishId, dishName, reason }`
- [x] 8.3 Create `AiWeekPlanFunction.cs` in `EzDinner.Functions` — `POST /api/families/{familyId}/suggest/ai-week`; authorize family membership; load dishes + stats + wishes + current week plan; call `IAiWeekPlannerService`; return result
- [x] 8.4 Create `IAiWeekPlannerService` interface in `EzDinner.Application`
- [x] 8.5 Create `AnthropicWeekPlannerService` in `EzDinner.Infrastructure` implementing `IAiWeekPlannerService` — builds dish catalog prompt (compact table: name | effort | weeks-since | wish-votes), sends to Haiku with prompt caching on system block, parses JSON array response
- [x] 8.6 Validate returned dish IDs against the loaded catalog; silently drop any dishId not found
- [x] 8.7 Register `AnthropicWeekPlannerService` in DI (`Program.cs` / `Startup`)
- [x] 8.8 Add i18n: no backend i18n needed; reason strings are returned in English from AI (display as-is or omit if locale matters)

## 9. Frontend — AI Week Planner UI

- [x] 9.1 Add "Plan this week with AI" trigger button in `AssistantPanel.vue` (Plan mode only)
- [x] 9.2 Implement draft state in panel: loading skeleton while request is in-flight; draft result list when returned
- [x] 9.3 Draft list shows each suggested day with dish name, effort badge, reason text, and Accept / Skip actions
- [x] 9.4 Accept action calls `dinnerRepo.addDishToMenu` for that day and marks the slot as accepted
- [x] 9.5 Skip action dismisses that day's suggestion without assignment
- [x] 9.6 "Accept all" button applies all non-skipped suggestions in sequence
- [x] 9.7 Add AI planner i18n keys to `en.json` and `da.json` (button label, loading text, accept/skip labels, error message)
- [x] 9.8 Add repository method `aiWeekPlan(familyId, weekStart, context)` in `web/repositories/dinners.ts` (or suitable repo file) calling the new backend endpoint

## 10. Polish and Verification

- [x] 10.1 Verify panel layout at md breakpoint (~960px) — dish rows must not overflow; effort badge degrades gracefully
- [x] 10.2 Verify plan page still works correctly on mobile (panel must not render; week plan full-width)
- [x] 10.3 Verify wishlist management is accessible via Wishlist toggle — add/upvote/remove still functional
- [x] 10.4 Verify dish assignment from panel updates the week plan column in real time
- [x] 10.5 Verify "same dish twice" scenario (leftovers) — dish can be assigned to two days without error
- [x] 10.6 Run frontend lint (`npm run lint`) and fix any issues
- [x] 10.7 Verify all new user-visible strings have translations in both `en.json` and `da.json`

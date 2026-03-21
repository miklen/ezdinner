## 1. Domain Layer

- [x] 1.1 Add `IsArchived` property to `Dish` aggregate (default `false`)
- [x] 1.2 Add `Dish.Archive()` method — throws `InvalidOperationException` if already archived
- [x] 1.3 Add `Dish.Reactivate()` method — throws `InvalidOperationException` if not archived
- [x] 1.4 Add unit tests for `Archive()` and `Reactivate()` state transitions and guard conditions

## 2. Application Layer

- [x] 2.1 Create `ArchiveDishCommand` with `FamilyId` and `DishId`
- [x] 2.2 Create `ArchiveDishCommandHandler` — load dish, call `Archive()`, save
- [x] 2.3 Create `ReactivateDishCommand` with `FamilyId` and `DishId`
- [x] 2.4 Create `ReactivateDishCommandHandler` — load dish, call `Reactivate()`, save
- [x] 2.5 Map `InvalidOperationException` from domain to 409 Conflict in handlers (or Functions layer)

## 3. Query Layer

- [x] 3.1 Add `includeArchived` parameter to dish catalog query / repository fetch
- [x] 3.2 Filter archived dishes from `DinnerSuggestionService` before passing to `DishCandidateFactory`

## 4. API Layer (Azure Functions)

- [x] 4.1 Create `ArchiveDish` function — `PATCH /api/families/{familyId}/dishes/{dishId}/archive`
- [x] 4.2 Create `ReactivateDish` function — `PATCH /api/families/{familyId}/dishes/{dishId}/reactivate`
- [x] 4.3 Add Casbin authorization check (`Dish.Write`) to both endpoints
- [x] 4.4 Expose `isArchived` field on dish query response model / DTO

## 5. Frontend — Catalog

- [x] 5.1 Add "Show archived dishes" toggle to the catalog view
- [x] 5.2 Pass `includeArchived=true` to the dish list API when toggle is on
- [x] 5.3 Apply muted/faded visual treatment to archived dishes in the catalog list

## 6. Frontend — Dish Detail

- [x] 6.1 Add archive button to dish detail page (visible when dish is active)
- [x] 6.2 Add reactivate button to dish detail page (visible when dish is archived)
- [x] 6.3 Show archived status indicator on dish detail page
- [x] 6.4 Wire archive/reactivate buttons to respective API endpoints with confirmation prompt
- [x] 6.5 Refresh dish state in store after archive/reactivate action

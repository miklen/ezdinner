## 1. Extend SuggestionContextValueObject

- [x] 1.1 Add `IReadOnlyDictionary<Guid, int> WishedDishIds` property to `SuggestionContextValueObject` with a default empty dictionary
- [x] 1.2 Add a constructor overload (or update the existing one) to accept `wishedDishIds` — keep the parameter optional with `null` defaulting to empty dictionary

## 2. Add WishlistBoostRule

- [x] 2.1 Create `WishlistBoostRule.cs` in `EzDinner.Core/DomainServices/DinnerSuggestions/` implementing both `IScoringRule` and `IExplainableScoringRule`
- [x] 2.2 Implement `Score`: return `voteCount × 0.3` if `context.WishedDishIds` contains the candidate's `DishId`, else 0
- [x] 2.3 Implement `Explain`: return `"Wished for by the family (N votes)"` when the dish is wished for, else `null`

## 3. Inject Wishlist into DinnerSuggestionService

- [x] 3.1 Add `IWishlistRepository` constructor parameter to `DinnerSuggestionService` in `EzDinner.Query.Core`
- [x] 3.2 In `SuggestDay`: call `_wishlistRepository.GetActiveAsync(familyId)`, filter non-expired items, build `wishedDishIds` dictionary, and pass it to `SuggestionContextValueObject`
- [x] 3.3 In `SuggestWeek`: same as above — fetch once before the day-loop and pass the same wish context to every day's `SuggestionContextValueObject`

## 4. Register WishlistBoostRule in DI

- [x] 4.1 In `EzDinner.Functions/Program.cs`, add `.AddScoped<IScoringRule, WishlistBoostRule>()` alongside the existing rule registrations

## 5. Unit Tests

- [x] 5.1 Add `WishlistBoostRuleTests` in `EzDinner.UnitTests/SuggestionTests/` covering: wished dish receives correct score, unwished dish scores 0, expired wish does not boost, reason string emitted for wished dish, no reason for unwished dish
- [x] 5.2 Update `DinnerSuggestionEngineTests` (or add a new test) to verify that a wished dish outranks an otherwise equal candidate when `WishedDishIds` is populated in context

## 1. Create domain types in EzDinner.Core

- [x] 1.1 Create `EzDinner.Core/Suggestions/` directory
- [x] 1.2 Create `IScoringRule.cs` in `EzDinner.Core/Suggestions/` with namespace `EzDinner.Core.Suggestions`
- [x] 1.3 Create `DishCandidate.cs` in `EzDinner.Core/Suggestions/` with namespace `EzDinner.Core.Suggestions`
- [x] 1.4 Create `DishScore.cs` in `EzDinner.Core/Suggestions/` with namespace `EzDinner.Core.Suggestions`
- [x] 1.5 Create `SuggestionContext.cs` in `EzDinner.Core/Suggestions/` with namespace `EzDinner.Core.Suggestions`
- [x] 1.6 Create `DinnerSuggestionEngine.cs` in `EzDinner.Core/Suggestions/` with namespace `EzDinner.Core.Suggestions`
- [x] 1.7 Create `DishCandidateFactory.cs` in `EzDinner.Core/Suggestions/` with namespace `EzDinner.Core.Suggestions` — include only the pure `BuildCandidates` and `BuildCandidate` static methods; no repository dependencies

## 2. Create scoring rules in EzDinner.Core

- [x] 2.1 Create `OverdueScoringRule.cs` in `EzDinner.Core/Suggestions/` with namespace `EzDinner.Core.Suggestions`
- [x] 2.2 Create `RatingScoringRule.cs` in `EzDinner.Core/Suggestions/` with namespace `EzDinner.Core.Suggestions`
- [x] 2.3 Create `RecencyPenaltyRule.cs` in `EzDinner.Core/Suggestions/` with namespace `EzDinner.Core.Suggestions`
- [x] 2.4 Create `LeftoverPatternRule.cs` in `EzDinner.Core/Suggestions/` with namespace `EzDinner.Core.Suggestions`

## 3. Update EzDinner.Query.Core

- [x] 3.1 Delete `IScoringRule.cs`, `DishCandidate.cs`, `DishScore.cs`, `SuggestionContext.cs`, `DinnerSuggestionEngine.cs` from `EzDinner.Query.Core/SuggestionQueries/`
- [x] 3.2 Delete `OverdueScoringRule.cs`, `RatingScoringRule.cs`, `RecencyPenaltyRule.cs`, `LeftoverPatternRule.cs` from `EzDinner.Query.Core/SuggestionQueries/`
- [x] 3.3 Delete `DishCandidateFactory.cs` from `EzDinner.Query.Core/SuggestionQueries/`
- [x] 3.4 Update `DinnerSuggestionService.cs`: add `using EzDinner.Core.Suggestions;` and inline the data loading from `BuildCandidatesAsync` directly into `SuggestDay` (load dishes and allDinners from repos, then call `DishCandidateFactory.BuildCandidates`); remove the `DishCandidateFactory` constructor parameter and field
- [x] 3.5 Update `IDinnerSuggestionService.cs`: add `using EzDinner.Core.Suggestions;` (references `DishScore`, `DaySuggestion` stays local)
- [x] 3.6 Update `DaySuggestion.cs`: add `using EzDinner.Core.Suggestions;` for `DishScore` reference

## 4. Update EzDinner.Functions

- [x] 4.1 Update `SuggestDay.cs` and `SuggestWeek.cs`: replace `using EzDinner.Query.Core.SuggestionQueries;` references for domain types with `using EzDinner.Core.Suggestions;` where applicable
- [x] 4.2 Update `Program.cs` DI registrations: add `using EzDinner.Core.Suggestions;` for scoring rule types; remove `DishCandidateFactory` registration (it is now a static factory with no DI required); verify `DinnerSuggestionEngine` and `IScoringRule` registrations reference the new namespace

## 5. Update EzDinner.IntegrationTests

- [x] 5.1 Update `using` directives in `DinnerSuggestionServiceTests.cs` and `LeftoverPatternRuleTests.cs` to reference `EzDinner.Core.Suggestions` for domain types

## 6. Unit tests — scoring rules

Create `api/test/EzDinner.UnitTests/SuggestionTests/` directory and add tests for each scoring rule in isolation. No mocks, no I/O — construct a `DishCandidate` and `SuggestionContext` directly.

- [x] 6.1 `OverdueScoringRuleTests.cs` — assert score equals `(daysSinceLast / typicalFrequencyDays) * 10.0`; verify a dish used twice as long ago as its frequency scores higher than one used on schedule
- [x] 6.2 `RatingScoringRuleTests.cs` — assert score equals `(rating / 10.0) * 5.0`; verify a rating-10 dish scores 5.0 and a rating-0 dish scores 0.0
- [x] 6.3 `RecencyPenaltyRuleTests.cs` — assert −1000 when `daysSinceLast <= 3`, −20 when `daysSinceLast <= 7`, 0 otherwise
- [x] 6.4 `LeftoverPatternRuleTests.cs` — assert +15 when `leftoverFrequencyRatio >= 0.3` AND `DishId` is in `AdjacentDishIds`; assert 0 when ratio is below threshold; assert 0 when ratio qualifies but dish is not adjacent

## 7. Unit tests — suggestion engine and candidate factory

- [x] 7.1 `DinnerSuggestionEngineTests.cs` — assert `Rank` returns candidates sorted by descending total score; assert same inputs always produce the same order (determinism); assert higher-scored candidate appears before lower-scored one
- [x] 7.2 `DishCandidateFactoryTests.cs` — assert deleted dishes are excluded from candidates; assert `DaysSinceLast` is `365` when dish has never been used; assert `DaysSinceLast` is computed correctly from the most recent usage date relative to `targetDate`; assert `TypicalFrequencyDays` defaults to `14` when dish has been used only once; assert `LeftoverFrequencyRatio` is `0` when dish has been used only once; assert `LeftoverFrequencyRatio` is computed correctly when consecutive-day pairs are present

## 8. Verify

- [x] 8.1 Run `cd api/src/EzDinner.Functions && dotnet build` — confirm zero errors
- [x] 8.2 Run `cd api && dotnet test` — confirm all unit and integration tests pass

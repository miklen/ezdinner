## REMOVED Requirements

### Requirement: Dish has legacy Tag field
**Reason**: The `Tag` type (`{ value: string; color: string }`) and `tags: Tag[]` field on `Dish`, `DishSelector`, and `Dinner` were scaffolded but never implemented — the backend never populated `tags` and no UI rendered them. They are replaced by the structured metadata fields (roles, effort, season, cuisine) which are already stored and returned by the API.
**Migration**: Remove `Tag` interface and `tags` fields from frontend types. Remove `tags: []` from test fixtures. No backend or data migration required.

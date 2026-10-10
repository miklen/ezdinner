# Tasks

## 1. Domain model and backward-compatible reads

- [x] 1.1 Add the optional `RecipeSnapshotValueObject` and `Dish` lifecycle methods that never mutate `Notes` or `Url`; verify focused domain unit tests cover validation, replacement, removal, and exact note preservation.
- [x] 1.2 Extend the dish serialization constructor, query result model, and mappings with a nullable snapshot default; verify unit tests deserialize/project a legacy dish with no snapshot and preserve its current URL and notes.

## 2. Safe source retrieval and deterministic extraction

- [x] 2.1 Implement the Infrastructure remote source reader with HTTP(S)-only validation, public-address enforcement for every hop, redirect/timeout/content-type/byte limits, and stable failure codes; verify unit tests cover allowed pages plus loopback, private/link-local, redirect, oversized, unsupported-content, and timeout cases.
- [x] 2.2 Implement schema.org `Recipe` JSON-LD extraction and stable Markdown/hash normalization; verify unit tests cover object/array/graph forms, instruction variants, malformed JSON-LD, incomplete recipes, and deterministic output.

## 3. Provider-agnostic fallback extraction

- [x] 3.1 Add Application-owned recipe extraction request/result contracts and `IRecipeExtractionProvider`; verify project references keep vendor SDK types out of Core and Application and compile successfully.
- [x] 3.2 Implement the Anthropic recipe extraction provider with bounded/delimited untrusted page text, strict JSON parsing, no-invention guidance, and result validation; verify unit tests cover valid output, empty/malformed output, missing ingredients/instructions, and hostile page instructions.
- [x] 3.3 Register source retrieval and extraction services with configuration limits; verify Infrastructure/Functions startup tests or a backend build resolve the complete dependency graph.

## 4. Import preview and snapshot commands

- [x] 4.1 Implement the preview use case to authorize through the existing Function boundary, load the dish's saved URL, prefer deterministic extraction, fall back to the provider, and return warnings/change status without saving; verify application/function tests cover success, no URL, fetch failure, extraction failure, unchanged source, and zero repository writes.
- [x] 4.2 Implement snapshot confirmation with candidate revalidation and stale-source rejection; verify tests prove a valid snapshot is persisted while user notes remain byte-for-byte unchanged and stale candidates change nothing.
- [x] 4.3 Implement explicit snapshot removal; verify tests prove removal preserves URL and user notes and enforces dish update authorization.
- [x] 4.4 Expose thin Azure Function endpoints and stable request/response DTOs for preview, confirmation, and removal; verify function tests cover authentication, family authorization, validation errors, and successful status mappings.

## 5. Metadata enrichment integration

- [x] 5.1 Extend the enrichment input contract and command handler to provide optional snapshot content while retaining confirmed-field merge behavior; verify enrichment unit tests cover dishes with and without snapshots and prove confirmed fields are never overwritten.
- [x] 5.2 Trigger best-effort metadata enrichment after snapshot confirmation without rolling back a saved snapshot on enrichment failure; verify tests cover successful enrichment and independent failure handling.

## 6. Frontend API models and safe Markdown rendering

- [x] 6.1 Add optional snapshot/candidate TypeScript types and repository methods for preview, confirmation, and removal; verify repository tests assert request routes, bodies, response mapping, and error propagation.
- [x] 6.2 Add one shared sanitized Markdown rendering utility for imported recipes and existing user notes; verify tests demonstrate ordinary Markdown rendering and removal of scripts, event handlers, and unsafe links.

## 7. Responsive dish recipe experience

- [x] 7.1 Refactor the existing notes area into a focused `DishUserNotesSection` with typed props/events while preserving EasyMDE save/cancel and URL editing behavior; verify black-box component tests exercise visible editing and emitted updates.
- [x] 7.2 Add `DishRecipeSnapshotSection` to display sanitized content, source provenance, capture time, URL mismatch state, and contextual import/refresh/remove actions on mobile and desktop; verify component tests assert visible states and user-triggered events.
- [x] 7.3 Add `DishRecipeImportDialog` with loading, actionable errors, preview, extraction warnings, unchanged-source messaging, replacement warning, and explicit confirmation; verify async black-box tests await user interactions and API promises and prove cancellation emits no confirmation.
- [x] 7.4 Compose the sections in `DishNotesCard` and keep the dish route as a data/reload composition surface; verify component/page tests prove user notes remain visible and unchanged through import, refresh, cancellation, URL mismatch, URL removal, and snapshot removal flows.
- [x] 7.5 Add all recipe snapshot interface strings to English and Danish locale files and apply the existing kitchen-notebook design tokens with accessible focus states and responsive layout; verify locale-key parity tests and frontend lint pass.

## 8. Integrated compatibility verification

- [ ] 8.1 Run backend unit tests and build, then verify a legacy snapshot-free dish and a newly snapshot-bearing dish can both be read and updated without a migration.
- [ ] 8.2 Run frontend tests, lint, and production build, then manually verify the complete import/refresh/remove flow at mobile and desktop breakpoints with user notes preserved.
- [x] 8.3 Run `openspec validate capture-recipe-snapshots --strict` and verify the completed implementation remains consistent with both capability deltas.

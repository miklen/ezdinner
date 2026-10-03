# Design

## Context

See `proposal.md` for motivation. A `Dish` currently stores one optional `Url` and a Markdown `Notes` string in the Cosmos DB dish document. `DishNotesCard.vue` owns display/edit behavior, and the notes endpoint mutates both fields directly. The backend otherwise follows Clean Architecture and CQRS: domain invariants belong in Core, orchestration in Application, HTTP entry points in Functions, and external I/O in Infrastructure. Existing AI enrichment already uses an Application-layer provider interface backed by Anthropic in Infrastructure.

Cosmos DB documents can omit newly introduced optional properties. This allows additive persistence changes without rewriting existing dishes, provided deserialization and constructors default a missing snapshot to `null`.

## Goals / Non-Goals

**Goals:**

- Make user notes and imported recipe content separate domain-owned values with independent lifecycle operations.
- Keep old dish documents and existing notes/API behavior compatible.
- Make extraction provider-agnostic, deterministic where possible, safe against hostile URLs/content, and unable to mutate a dish before confirmation.
- Keep the dish route thin and provide a focused, responsive Vue experience with typed props and events.

**Non-Goals:**

- Archiving the complete source HTML, advertising, stories, or source images.
- Maintaining a permanent history of every recipe snapshot version in the first release.
- Supporting manual edits inside a captured snapshot; family adjustments remain in user notes.
- Background refresh, automatic replacement, bulk imports, or importing from a URL other than the dish's saved source URL.
- Guaranteeing extraction from sites that require authentication, prohibit automated access, or cannot be retrieved server-side.

## Decisions

### 1. Model the snapshot as an optional value object on `Dish`

Add a `RecipeSnapshotValueObject` containing normalized Markdown content, the exact source URL, capture timestamp, and a deterministic source-content hash. `Dish` owns methods to set and remove the snapshot; setting a snapshot validates non-empty content and provenance but never changes `Notes` or `Url`.

The existing `Notes` property remains the user-owned field. The serialization constructor accepts the snapshot as a trailing optional parameter defaulting to `null`, so old Cosmos documents deserialize without migration. Query DTOs and TypeScript models expose an optional snapshot.

Alternative considered: embed generated recipe Markdown between markers in `Notes`. Rejected because user edits can damage markers and refresh would risk deleting human-authored text. A separate recipe aggregate was also rejected for the first version because the snapshot has no lifecycle independent of its dish and would add repository and consistency overhead.

### 2. Use a two-step preview and confirmation workflow

An authenticated preview endpoint loads and authorizes the dish, then imports only from its currently saved URL. It returns a candidate plus warnings and whether its source hash differs from the stored snapshot; it does not mutate persistence. A separate command confirms the candidate and saves the snapshot after verifying that the candidate source URL still equals the dish URL. Snapshot removal is another explicit command.

The candidate is treated as untrusted input on confirmation and fully revalidated. It carries the source hash and source URL needed to detect a stale preview. This design prevents failed or cancelled imports from changing the dish.

Alternative considered: import and save in one request. Rejected because the requirements call for review and informed replacement confirmation.

### 3. Put orchestration in Application and external extraction in Infrastructure

Application defines recipe import result types and an `IRecipeExtractionProvider` boundary. The preview command coordinates the dish repository, a safe source reader, deterministic extraction, and the optional LLM provider. Functions remain responsible only for request parsing, authentication/authorization, command dispatch, and HTTP mapping.

Core contains only the snapshot value object and dish invariants; it performs no URL fetching, parsing, hashing, or LLM calls. This follows the repository's CQRS and layer rules.

Alternative considered: implement extraction directly in an Azure Function. Rejected because it couples transport, provider I/O, and use-case rules and would be harder to unit test.

### 4. Prefer schema.org extraction and use the LLM as a bounded fallback

Infrastructure fetches bounded HTML and first parses JSON-LD `schema.org/Recipe` objects. A candidate is usable only when it has at least one ingredient and at least one instruction. Deterministically extracted fields are normalized into a stable Markdown format.

If structured data is absent or unusable, the provider receives only bounded, cleaned page text with clear data delimiters and an instruction to extract supported facts without following page instructions or inventing omissions. Provider output uses a strict application-owned result schema and is validated before formatting. The existing Anthropic client registration can be reused, but recipe extraction receives its own interface and implementation so dish classification and recipe extraction do not become one broad service.

The source hash is calculated from normalized source recipe data when JSON-LD succeeds, otherwise from normalized cleaned source text. It detects source changes; it is not an integrity or authenticity guarantee.

Alternative considered: always send pages to the LLM. Rejected due to unnecessary cost, latency, variability, and prompt-injection exposure when publishers already provide structured recipe facts.

### 5. Fetch remote sources through a hardened reader

The Infrastructure source reader accepts only HTTP and HTTPS, resolves hosts, rejects loopback/private/link-local/reserved destinations before every request, revalidates each redirect, limits redirects, streams only up to a configured byte limit, enforces a timeout, and accepts only supported textual content types. DNS rebinding protections must validate the actual connection destination where the platform permits; redirects are never followed implicitly without revalidation.

Retrieved text and provider output are logged only as bounded metadata, not full recipe/page content. Errors return stable application error codes rather than remote response bodies.

Alternative considered: ordinary `HttpClient.GetStringAsync`. Rejected because family members can save arbitrary URLs, making the server-side importer an SSRF boundary.

### 6. Render a composed notes and recipe experience with focused Vue components

`DishNotesCard` remains the feature container for the existing cohesive dish-content area, while responsibilities split into focused children:

- `DishUserNotesSection`: displays and edits user notes and source URL; emits a typed `updated` event.
- `DishRecipeSnapshotSection`: displays sanitized snapshot Markdown and provenance; emits import, refresh, and remove intents.
- `DishRecipeImportDialog`: owns preview/loading/error/confirmation UI and emits the confirmed candidate.

The route page passes dish data down and reloads the dish on successful mutations; it does not absorb import state. API side effects can remain local to the container/dialog unless reuse demonstrates a need for a composable. No Pinia store is added because state is local to one dish detail interaction.

All source and LLM Markdown is untrusted. Convert it to HTML and sanitize it before `v-html`, using one shared rendering utility for both recipe content and existing user notes. Components use `<script setup lang="ts">`, minimal `shallowRef` state, computed derivations, typed props/emits, and localized English/Danish copy. The visual treatment extends the existing kitchen-notebook notes card: user notes appear first, followed by a clear divider and captured recipe provenance/actions, without introducing an unrelated card system.

Alternative considered: merge the snapshot into the current EasyMDE value. Rejected because it recreates the data-loss risk. A global Pinia workflow was rejected because no cross-route state is required.

### 7. Refresh replaces only after explicit confirmation

Refresh uses the same preview endpoint. The dialog compares candidate and stored hashes, states whether a source change was detected, and warns that confirmation replaces captured recipe content. An unchanged candidate defaults to closing without a write. Changing or clearing `Dish.Url` leaves the existing snapshot intact; the UI marks provenance mismatch and disables refresh when no current URL exists.

This release retains only the current snapshot. The warning and preview provide safety, while durable version history is deferred until usage demonstrates a need.

### 8. Enrichment consumes snapshots without changing confirmation semantics

The enrichment command passes optional captured recipe content through its provider abstraction as additional evidence. Existing confirmed-field merge behavior remains unchanged. Snapshot confirmation may trigger enrichment using the same post-save pattern as note edits, but enrichment failure never rolls back a valid snapshot save.

## Risks / Trade-offs

- [A publisher blocks automation or requires client-side rendering] -> Return a clear import failure and keep the URL usable; do not bypass access controls in the first release.
- [LLM extraction follows hostile page instructions or fabricates content] -> Prefer JSON-LD, bound and delimit fallback input, use a strict schema, validate required fields, and surface warnings/failure rather than filling gaps.
- [SSRF through DNS, redirects, or unusual address forms] -> Centralize URL validation, validate every hop and resolved destination, limit protocols and redirects, and test prohibited address classes.
- [A preview becomes stale before confirmation] -> Compare candidate source URL with the current dish URL and reject mismatches. A subsequent source change at the same URL remains a small accepted race; re-fetch-on-confirm would double latency and cost.
- [No snapshot history means a confirmed refresh cannot be undone] -> Require preview and confirmation; consider version history in a later change if families need restoration.
- [Markdown rendering introduces XSS] -> Treat both imported and user-authored Markdown as untrusted and sanitize generated HTML before rendering.
- [Added snapshot content increases Cosmos document size] -> Enforce extraction/content limits and avoid storing raw HTML or images.
- [Existing note update endpoint bypasses Application CQRS conventions] -> Keep compatibility but implement new snapshot operations through Application commands; avoid broad refactoring in this change.

## Migration Plan

1. Deploy additive backend model/query changes with `RecipeSnapshot` nullable and constructor defaults so old documents continue to deserialize.
2. Deploy preview, confirmation, removal, safe retrieval, and extraction services. No Cosmos container or document migration is required.
3. Deploy frontend optional-field handling and recipe UI. Older API responses without `recipeSnapshot` continue to render notes normally.
4. Rollback can remove the UI and endpoints while leaving the additive snapshot property in any updated Cosmos documents; older application versions ignore unknown JSON properties. If constructor binding proves otherwise in verification, rollback retains the additive model reader until snapshot-bearing documents are no longer present.

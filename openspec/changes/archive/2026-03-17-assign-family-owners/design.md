## Context

`FamilyMember` already carries an `IsOwner: bool` flag and Casbin already has two roles — `Owner` and `FamilyMember`. The `UpdateAuthorizationPoliciesCommand` (fired via CosmosDB change-feed trigger) re-seeds permissions from family state, but it only ever **adds** policies; it never removes a stale role assignment. As a result, simply flipping `IsOwner` on a member and relying on the trigger is insufficient — the actor's old Casbin role must be explicitly revoked.

Current authorization policy lifecycle:
1. CosmosDB trigger fires → `UpdateAuthorizationPoliciesCommand` adds role permissions and role assignments for all members.
2. No path today to demote a member or promote one to Owner.

## Goals / Non-Goals

**Goals:**
- Allow any Owner in a family to promote a FamilyMember to Owner
- Allow any Owner to demote another Owner to FamilyMember
- Enforce "at least 1 Owner" as a domain invariant (backend) and optimistic UI guard (frontend)
- Keep Casbin in sync on every role change (add new role, remove old role, reload)

**Non-Goals:**
- Changing ownership of non-autonomous members (they can't log in, so role is irrelevant)
- Bulk role changes
- Transferring family ownership to a completely different user account

## Decisions

### D1 — Domain invariant lives in the Family aggregate

The "at least 1 Owner" rule is a domain constraint, not an application or infrastructure concern. A new method `Family.SetMemberRole(memberId, isOwner)` encapsulates it:
- When `isOwner = false`, count current owners; reject if `== 1`.
- When `isOwner = true`, no constraint — any FamilyMember can be promoted.

Alternatives considered:
- Validate in the Application command — rejected; leaks business rules out of the domain.
- Validate only on the frontend — rejected; unsafe, not a real constraint.

### D2 — Explicit Casbin role swap in the Application command, not via trigger

The `UpdateAuthorizationPoliciesCommand` trigger cannot reliably remove stale Casbin role assignments (it only adds). The new `SetMemberRoleCommand` will:
1. Load and mutate the Family aggregate (via `SetMemberRole`).
2. Persist the updated Family.
3. Explicitly call `IAuthzRepository.RemoveRoleFromUserAsync(userId, familyId, oldRole)`.
4. Call `IAuthzRepository.AssignRoleToUserAsync(userId, familyId, newRole)`.
5. Call `_authz.ReloadPoliciesAsync()`.

Alternatives considered:
- Extend the trigger to diff old/new state and remove stale roles — rejected; complex and fragile (CosmosDB change feed delivers full new state only, not a diff).
- Skip saving Family and only update Casbin — rejected; `IsOwner` on `FamilyMember` would be stale and misrepresent actual state.

### D3 — New HTTP endpoint: `PUT /api/families/{familyId}/member/{memberId}/role`

Single endpoint that accepts `{ "isOwner": true/false }` in the body. Requires the caller to be an Owner in that family (enforced via existing `_authz.Authorize(userId, familyId, Resources.Family, Actions.Update)`).

Alternatives considered:
- Two separate endpoints `POST .../promote` and `POST .../demote` — rejected; unnecessary surface area.
- PATCH on the member resource — possible but overloaded; role change is a distinct action.

### D4 — `IAuthzRepository` gets a `RemoveRoleFromUserAsync` method

The Casbin.NET enforcer exposes `DeleteRoleForUserInDomainAsync`. The Infrastructure `AuthzRepository` wraps this via a new interface method. This keeps the adapter contract explicit and testable.

## Risks / Trade-offs

- **Multi-instance staleness** → Mitigated by calling `ReloadPoliciesAsync()` in the command (consistent with existing policy write points). Other running instances stay stale until restarted — documented in CLAUDE.md as a known limitation.
- **Race condition: two owners simultaneously demote each other** → The "last owner" guard runs after loading Family from CosmosDB. If two concurrent requests both see two owners and both try to demote, one will succeed, the second will load the (now one-owner) Family and be rejected. Cosmos optimistic concurrency (`_etag`) can reinforce this if needed — not required for MVP.
- **Non-autonomous members shown in role UI** → Non-autonomous members have no login, so promoting them is meaningless. The frontend will hide role controls for members where `hasAutonomy == false`.

## Migration Plan

No data migration required. Existing families retain their current owner(s). The new endpoint is purely additive. No Casbin schema changes — existing `Owner` and `FamilyMember` roles are reused.

## Open Questions

- None — requirements are clear and the authorization model already supports multiple owners per family.

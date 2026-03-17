## 1. Domain — Family Aggregate

- [x] 1.1 Add `SetMemberRole(memberId, isOwner)` method to `Family` aggregate that flips `FamilyMember.IsOwner` and throws if demoting the last Owner
- [x] 1.2 Add a guard in `SetMemberRole` rejecting role changes on non-autonomous members

## 2. Infrastructure — Authorization Repository

- [x] 2.1 Add `RemoveRoleFromUserAsync(userId, familyId, role)` to `IAuthzRepository` interface
- [x] 2.2 Implement `RemoveRoleFromUserAsync` in `AuthzRepository` using Casbin `DeleteRoleForUserInDomainAsync`

## 3. Application — Command

- [x] 3.1 Create `SetMemberRoleCommand` record with `FamilyId`, `MemberId`, `IsOwner`, `RequestingUserId` fields
- [x] 3.2 Implement `SetMemberRoleCommandHandler` that: loads Family, calls `SetMemberRole`, saves Family, removes old Casbin role, assigns new Casbin role, reloads policies

## 4. API — Azure Function Endpoint

- [x] 4.1 Create `FamilySetMemberRole.cs` Azure Function: `PUT /api/families/{familyId}/member/{memberId}/role`
- [x] 4.2 Authorize caller as Owner (`Resources.Family`, `Actions.Update`) before dispatching command
- [x] 4.3 Map domain exception ("last owner") to `400 Bad Request` response; map not-found to `404`

## 5. Frontend — Repository

- [x] 5.1 Add `setMemberRole(familyId, memberId, isOwner)` method to `family-repository.ts` calling the new endpoint

## 6. Frontend — UI

- [x] 6.1 In `families.vue` member list, add "Make Owner" button for autonomous non-owner members (visible to Owners only)
- [x] 6.2 Add "Remove Owner" button for other-owner members (visible to Owners only)
- [x] 6.3 Disable "Remove Owner" when the family has exactly one Owner; show tooltip explaining the constraint
- [x] 6.4 On button click, call `setMemberRole`, reload families, and show a snackbar confirmation
- [x] 6.5 Hide all role controls for non-autonomous members

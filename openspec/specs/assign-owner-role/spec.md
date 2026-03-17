## ADDED Requirements

### Requirement: Owner can promote a FamilyMember to Owner
An Owner of a family SHALL be able to promote any autonomous FamilyMember in that family to the Owner role.

#### Scenario: Successful promotion
- **WHEN** an Owner sends `PUT /api/families/{familyId}/member/{memberId}/role` with `{ "isOwner": true }` for an autonomous FamilyMember
- **THEN** the member's role is updated to Owner in both the Family aggregate and Casbin, and the endpoint returns `200 OK`

#### Scenario: Non-owner cannot promote
- **WHEN** a FamilyMember (non-Owner) attempts to promote another member
- **THEN** the endpoint returns `403 Forbidden`

#### Scenario: Non-autonomous member cannot be promoted
- **WHEN** an Owner attempts to promote a non-autonomous member (hasAutonomy = false)
- **THEN** the endpoint returns `400 Bad Request`

#### Scenario: Already an owner
- **WHEN** an Owner sends `{ "isOwner": true }` for a member who is already an Owner
- **THEN** the endpoint returns `200 OK` with no change (idempotent)

---

### Requirement: Owner can demote another Owner to FamilyMember
Any Owner of a family SHALL be able to demote another Owner to FamilyMember, provided at least one Owner would remain after the change.

#### Scenario: Successful demotion
- **WHEN** an Owner sends `PUT /api/families/{familyId}/member/{memberId}/role` with `{ "isOwner": false }` for another Owner, and at least two Owners currently exist
- **THEN** the target member's role is updated to FamilyMember in both the Family aggregate and Casbin, and the endpoint returns `200 OK`

#### Scenario: Cannot remove last owner
- **WHEN** an Owner sends `{ "isOwner": false }` for the last remaining Owner in the family
- **THEN** the endpoint returns `400 Bad Request` with a message indicating at least one Owner must remain

#### Scenario: Owner cannot demote themselves (last owner)
- **WHEN** the sole Owner of a family sends `{ "isOwner": false }` targeting their own member record
- **THEN** the endpoint returns `400 Bad Request`

---

### Requirement: Role change is reflected immediately in authorization
After a role change, the updated Casbin policies MUST be reloaded on the running instance so subsequent requests reflect the new role.

#### Scenario: Promoted member gains owner permissions
- **WHEN** a FamilyMember is promoted to Owner
- **THEN** that member can immediately perform Owner-only actions (e.g., invite members) without a restart

#### Scenario: Demoted member loses owner permissions
- **WHEN** an Owner is demoted to FamilyMember
- **THEN** that member's subsequent Owner-only requests return `403 Forbidden`

---

### Requirement: Frontend shows role controls to Owners
The family member list in the frontend SHALL display role assignment controls (promote/demote) for each autonomous member, visible and actionable only when the current user is an Owner.

#### Scenario: Owner sees promote button for FamilyMembers
- **WHEN** the current user is an Owner and views a family member with `isOwner = false` and `hasAutonomy = true`
- **THEN** a "Make Owner" control is displayed for that member

#### Scenario: Owner sees demote button for other Owners
- **WHEN** the current user is an Owner and views another member with `isOwner = true`
- **THEN** a "Remove Owner" control is displayed for that member

#### Scenario: Demote button disabled for last owner
- **WHEN** there is only one Owner in the family
- **THEN** the "Remove Owner" control for that member is disabled with a tooltip explaining the constraint

#### Scenario: Non-Owner sees no role controls
- **WHEN** the current user is a FamilyMember (non-Owner)
- **THEN** no role assignment controls are displayed

#### Scenario: No controls for non-autonomous members
- **WHEN** a member has `hasAutonomy = false`
- **THEN** no role assignment controls are displayed for that member

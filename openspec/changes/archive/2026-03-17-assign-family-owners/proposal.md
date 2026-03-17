## Why

Family owners need a way to share governance responsibilities. Currently only the original creator holds the Owner role, leaving no path to delegate control or hand off the family to another member.

## What Changes

- Owners can promote any FamilyMember to the Owner role
- Any Owner can demote another Owner back to FamilyMember, provided at least one Owner would remain
- The last remaining Owner cannot be removed from the Owner role (enforced on both backend and frontend)
- The family member list UI surfaces role assignment controls visible only to Owners

## Capabilities

### New Capabilities

- `assign-owner-role`: An Owner can promote a FamilyMember to Owner; any Owner can demote another Owner, with a guard that at least 1 Owner must always exist

### Modified Capabilities

<!-- None — no existing spec-level behavior changes -->

## Impact

- **Backend**: New endpoint (or extended existing member endpoint) to change a member's role within a family; authorization check that caller is Owner; business rule enforcing minimum 1 Owner
- **Frontend**: Family member list shows role badge + promote/demote action per member, gated by caller's Owner role
- **Authorization**: Casbin policy update — Owners may invoke the role-change action on family members

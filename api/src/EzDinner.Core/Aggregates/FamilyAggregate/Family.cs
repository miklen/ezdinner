using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EzDinner.Core.Aggregates.FamilyAggregate
{
    public class Family : AggregateRoot<Guid>
    {
        private readonly List<FamilyMember> _familyMembers;

        public Guid OwnerId => _familyMembers.FirstOrDefault(w => w.IsOwner)?.Id
            ?? throw new InvalidOperationException("Family has no owner.");
        public string Name { get; private set; }
        public IEnumerable<FamilyMember> FamilyMembers => _familyMembers;

        public DateTime CreatedDate { get; }
        public DateTime UpdatedDate { get; private set; }

        /// <summary>
        /// For deserialization purpose only. One argument for each property to be deserialized.
        /// 
        /// This ctor breaks encapsulation and doesn't enforce invariants. 
        /// Since the Cosmos Client uses it's own CosmosJsonSerializer, then we have to be careful with clever serialization tricks.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ownerId"></param>
        /// <param name="name"></param>
        /// <param name="familyMembers"></param>
        /// <param name="createdDate"></param>
        /// <param name="updatedDate"></param>
        public Family(Guid id, string name, List<FamilyMember> familyMembers, DateTime createdDate, DateTime updatedDate) : base(id)
        {
            Name = name;
            _familyMembers = familyMembers;
            CreatedDate = createdDate;
            UpdatedDate = updatedDate;
        }

        public static Family CreateNew(Guid ownerId, string name)
        {
            if (ownerId == Guid.Empty) throw new ArgumentException($"'{nameof(ownerId)}' cannot be empty", nameof(ownerId));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));

            var createdDate = DateTime.UtcNow;
            return new Family(id: Guid.NewGuid(), name, familyMembers: new List<FamilyMember>() { FamilyMember.CreateOwner(ownerId) }, createdDate, updatedDate: createdDate);
        }

        /// <summary>
        /// Update family name
        /// </summary>
        /// <param name="name"></param>
        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
            Name = name;
        }

        /// <summary>
        /// For now an invitation directly adds a family member to the family.
        /// TODO: Add add invitation state, so that you cannot force familyMembers to join
        /// </summary>
        /// <param name="familyMemberId"></param>
        public void InviteFamilyMember(Guid familyMemberId)
        {
            if (_familyMembers.Any(w => w.Id == familyMemberId)) return;
            _familyMembers.Add(FamilyMember.CreateFamilyMember(familyMemberId));
            UpdatedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Create a family member that does not have autonomy on it's own (i.e. no login account accociated)
        /// </summary>
        /// <param name="name"></param>
        public void CreateFamilyMember(string name)
        {
            _familyMembers.Add(FamilyMember.CreateFamilyMemberWithoutAutonomy(name));
            UpdatedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Remove a family member from family
        /// </summary>
        /// <param name="familyMemberId"></param>
        public void RemoveFamilyMember(Guid familyMemberId)
        {
            _familyMembers.RemoveAll(w => w.Id == familyMemberId);
            UpdatedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Assign or revoke the Owner role for a family member.
        /// At least one Owner must remain after any change.
        /// </summary>
        public void SetMemberRole(Guid memberId, bool isOwner)
        {
            var member = _familyMembers.FirstOrDefault(m => m.Id == memberId)
                ?? throw new InvalidOperationException("MEMBER_NOT_FOUND");
            if (!member.HasAutonomy)
                throw new InvalidOperationException("CANNOT_CHANGE_ROLE_OF_NON_AUTONOMOUS_MEMBER");
            if (!isOwner && _familyMembers.Count(m => m.IsOwner) <= 1)
                throw new InvalidOperationException("LAST_OWNER_CANNOT_BE_DEMOTED");

            member.IsOwner = isOwner;
            UpdatedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Merge a non-autonomous member into an autonomous account, transferring identity.
        /// </summary>
        public void MergeNonAutonomousMember(Guid nonAutonomousId, Guid autonomousId)
        {
            var source = _familyMembers.FirstOrDefault(m => m.Id == nonAutonomousId)
                ?? throw new InvalidOperationException("NON_AUTONOMOUS_MEMBER_NOT_FOUND");
            if (source.HasAutonomy)
                throw new InvalidOperationException("SOURCE_MEMBER_HAS_AUTONOMY");

            var target = _familyMembers.FirstOrDefault(m => m.Id == autonomousId)
                ?? throw new InvalidOperationException("AUTONOMOUS_MEMBER_NOT_FOUND");
            if (!target.HasAutonomy)
                throw new InvalidOperationException("TARGET_MEMBER_LACKS_AUTONOMY");

            _familyMembers.RemoveAll(m => m.Id == nonAutonomousId);
            UpdatedDate = DateTime.UtcNow;
        }
    }
}

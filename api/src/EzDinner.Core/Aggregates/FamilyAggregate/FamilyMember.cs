using System;
using System.Collections.Generic;
using System.Text;

namespace EzDinner.Core.Aggregates.FamilyAggregate
{
    public class FamilyMember
    {
        public Guid Id { get; private set; }
        public string? Name { get; private set; }
        public bool HasAutonomy { get; private set; }
        public bool IsOwner { get; internal set; }

        public FamilyMember(Guid id, string? name, bool hasAutonomy, bool isOwner)
        {
            Id = id;
            Name = name;
            HasAutonomy = hasAutonomy;
            IsOwner = isOwner;
        }

        /// <summary>
        /// Family members without autonomy are used for e.g. children which does not have an account of their own.
        /// Any actions they do are done by another use on behalf of the child. Like rating dishes.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static FamilyMember CreateFamilyMemberWithoutAutonomy(string name)
        {
            return new FamilyMember(Guid.NewGuid(), name, hasAutonomy: false, isOwner: false);
        }

        /// <summary>
        /// Family members which have own login. The Id should be the ObjectId from B2C
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static FamilyMember CreateFamilyMember(Guid id)
        {
            return new FamilyMember(id, null, hasAutonomy: true, isOwner: false);
        }

        /// <summary>
        /// Owner of the family entity. Has full permissions within the family. Id should be an ObjectId from B2C
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static FamilyMember CreateOwner(Guid id)
        {
            return new FamilyMember(id, null, hasAutonomy: true, isOwner: true);
        }
    }
}

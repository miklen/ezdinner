using System;
using System.Collections.Generic;

namespace EzDinner.Query.Core.FamilyQueries
{
    public class FamilyDetails
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public string? Name { get; set; }
        public IEnumerable<FamilyMemberDetails>? FamilyMembers { get; set; }
        public DateTime CreatedDate { get; }
        public DateTime UpdatedDate { get; private set; }
    }
}

using System;

namespace EzDinner.Query.Core.FamilyQueries
{
    public class FamilyMemberDetails
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public bool HasAutonomy { get; set; }
        public bool IsOwner { get; set; }
    }
}

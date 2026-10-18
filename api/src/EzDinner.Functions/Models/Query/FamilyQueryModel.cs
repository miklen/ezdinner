using AutoMapper;
using EzDinner.Core.Aggregates.FamilyAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace EzDinner.Functions.Models.Query
{
    /// <summary>
    /// Model to display the family. Both as cards and in dropdown.
    /// </summary>
    public class FamilyQueryModel
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public List<FamilyMemberQueryModel> FamilyMembers { get; set; } = new List<FamilyMemberQueryModel>();
        public IReadOnlyList<string> GroceryStores { get; set; } = Array.Empty<string>();
    }

    public class FamilyMemberQueryModel
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public bool IsOwner { get; set; }

        public FamilyMemberQueryModel(Guid id, string name, bool isOwner = false)
        {
            Id = id;
            Name = name;
            IsOwner = isOwner;
        }
    }

    public class FamilyMapping : Profile
    {
        public FamilyMapping()
        {
            CreateMap<Family, FamilyQueryModel>();
        }
    }
}

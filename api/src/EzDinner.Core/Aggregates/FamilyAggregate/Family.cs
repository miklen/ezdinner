﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EzDinner.Core.Aggregates.FamilyAggregate
{
    public class Family
    {
        private Guid _id;

        public Guid Id { get => _id; private set { PartitionKey = value; _id = value; } }

        /// <summary>
        /// Use the Core model as db model for now - to avoid unnessesary mapping complexity between models
        /// </summary>
        public Guid PartitionKey { get; private set; }
        public Guid OwnerId { get; set; }
        public string Name { get; set; }
        public List<Guid> FamilyMemberIds { get; set; }

        public Family(Guid ownerId, string name)
        {
            if (ownerId == Guid.Empty) throw new ArgumentException($"'{nameof(ownerId)}' cannot be empty", nameof(ownerId));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));

            Id = Guid.NewGuid();
            OwnerId = ownerId;
            Name = name;
            FamilyMemberIds = new List<Guid>();
        }
    }
}
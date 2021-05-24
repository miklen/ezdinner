﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EzDinner.Core.Aggregates.FamilyAggregate
{
  public interface IFamilyRepository
  {
    public Task<IEnumerable<Family>> GetFamilySelectorsAsync(Guid userId);

    public Task<Family?> GetFamily(Guid familyId);
    public Task SaveAsync(Family family);
  }
}

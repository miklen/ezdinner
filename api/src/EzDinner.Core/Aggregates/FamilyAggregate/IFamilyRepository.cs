using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EzDinner.Core.Aggregates.FamilyAggregate
{
  public interface IFamilyRepository
  {
    public Task<Family?> GetFamily(Guid familyId);
    public Task SaveAsync(Family family);
    public Task SaveGroceryStoresAsync(Family family);
  }
}

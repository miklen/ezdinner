using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EzDinner.Core.Aggregates.DinnerAggregate
{
    public interface IDinnerRepository
    {
        Task SaveAsync(Dinner dinner);
        Task DeleteAsync(Dinner dinner);
        Task<Dinner?> GetAsync(Guid familyId, LocalDate exactDate);
        IAsyncEnumerable<Dinner> GetAsync(Guid familyId, LocalDate fromDate, LocalDate toDate);
        IAsyncEnumerable<Dinner> GetAsync(Guid familyId, Guid dishId);
        IAsyncEnumerable<string> GetOptOutReasonsAsync(Guid familyId);
    }
}

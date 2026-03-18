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
        /// <summary>
        /// Returns dinners for a family within the given date range, ordered by date ascending.
        /// Callers (e.g. DinnerService) depend on this ordering — implementations must guarantee it.
        /// </summary>
        IAsyncEnumerable<Dinner> GetAsync(Guid familyId, LocalDate fromDate, LocalDate toDate);
        IAsyncEnumerable<Dinner> GetAsync(Guid familyId, Guid dishId);
        IAsyncEnumerable<string> GetOptOutReasonsAsync(Guid familyId);
    }
}

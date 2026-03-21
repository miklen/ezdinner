using System.Threading;
using System.Threading.Tasks;

namespace EzDinner.Application.Commands.Dishes
{
    public interface IDishEnrichmentProvider
    {
        Task<DishEnrichmentResult> EnrichAsync(string dishName, string? notes, CancellationToken ct);
    }
}

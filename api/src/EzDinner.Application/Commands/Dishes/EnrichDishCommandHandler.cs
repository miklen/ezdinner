using EzDinner.Core.Aggregates.DishAggregate;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EzDinner.Application.Commands.Dishes
{
    public class EnrichDishCommandHandler
    {
        private readonly IDishRepository _dishRepository;
        private readonly IDishEnrichmentProvider _enrichmentProvider;

        public EnrichDishCommandHandler(
            IDishRepository dishRepository,
            IDishEnrichmentProvider enrichmentProvider)
        {
            _dishRepository = dishRepository;
            _enrichmentProvider = enrichmentProvider;
        }

        public async Task Handle(EnrichDishCommand command, CancellationToken ct = default)
        {
            if (command.FamilyId == Guid.Empty) throw new ArgumentException("MISSING_FAMILYID");
            if (command.DishId == Guid.Empty) throw new ArgumentException("MISSING_DISHID");

            var dish = await _dishRepository.GetDishAsync(command.DishId);
            if (dish is null) throw new KeyNotFoundException($"Dish {command.DishId} not found.");
            if (dish.FamilyId != command.FamilyId) throw new UnauthorizedAccessException("DISH_NOT_IN_FAMILY");

            DishEnrichmentResult result;
            try
            {
                result = await _enrichmentProvider.EnrichAsync(dish.Name, dish.Notes, ct);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("ENRICHMENT_FAILED", ex);
            }

            var incoming = DishMetadataValueObject.FromAiSuggestion(
                dish.Metadata,
                result.Roles,
                result.EffortLevel,
                result.SeasonAffinity,
                result.Cuisine);

            dish.UpdateMetadata(incoming);
            await _dishRepository.SaveAsync(dish);
        }
    }
}

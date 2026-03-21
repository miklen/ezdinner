using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.FamilyAggregate;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EzDinner.Application.Commands.FamilyMembers
{
    public class MergeNonAutonomousMemberCommand
    {
        private readonly IFamilyRepository _familyRepository;
        private readonly IDishRepository _dishRepository;

        public MergeNonAutonomousMemberCommand(IFamilyRepository familyRepository, IDishRepository dishRepository)
        {
            _familyRepository = familyRepository;
            _dishRepository = dishRepository;
        }

        public async Task Handle(Guid familyId, Guid nonAutonomousId, Guid autonomousId)
        {
            var family = await _familyRepository.GetFamily(familyId)
                ?? throw new InvalidOperationException("FAMILY_NOT_FOUND");

            family.MergeNonAutonomousMember(nonAutonomousId, autonomousId);

            var dishes = await _dishRepository.GetDishesAsync(familyId, includeArchived: true);
            var toSave = dishes.Where(d => d.MigrateRating(nonAutonomousId, autonomousId)).ToList();

            await Task.WhenAll(toSave.Select(d => _dishRepository.SaveAsync(d)));
            await _familyRepository.SaveAsync(family);
        }
    }
}

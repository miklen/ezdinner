using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.FamilyAggregate;

namespace EzDinner.Application.Commands.RatingReminders;

public sealed class RatingReminderRecipientAccess(IFamilyRepository families, IAuthzService authorization)
{
    public async Task<bool> CanReceiveAsync(Guid userId, Guid familyId, CancellationToken cancellationToken)
    {
        if (!authorization.Authorize(userId, familyId, Resources.Family, Actions.Read)
            || !authorization.Authorize(userId, familyId, Resources.Dish, Actions.Read)) return false;
        var family = await families.GetFamily(familyId).WaitAsync(cancellationToken);
        return family is not null && family.FamilyMembers.Any(member => member.Id == userId && member.HasAutonomy);
    }
}

using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.FamilyAggregate;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EzDinner.Functions;

public sealed class RatingReminderAccess(IAuthzService authorization, IFamilyRepository families)
{
    public static Guid? Caller(HttpRequest request) => request.HttpContext.User.Identity?.IsAuthenticated == true
        && Guid.TryParse(request.HttpContext.User.GetNameIdentifierId(), out var userId) && userId != Guid.Empty ? userId : null;

    public async Task<bool> CanAccessAsync(Guid userId, Guid familyId, string dishAction, CancellationToken cancellationToken)
    {
        if (!authorization.Authorize(userId, familyId, Resources.Family, Actions.Read)
            || !authorization.Authorize(userId, familyId, Resources.Dish, dishAction)) return false;
        var family = await families.GetFamily(familyId).WaitAsync(cancellationToken);
        return family is not null && family.FamilyMembers.Any(member => member.Id == userId && member.HasAutonomy);
    }
}

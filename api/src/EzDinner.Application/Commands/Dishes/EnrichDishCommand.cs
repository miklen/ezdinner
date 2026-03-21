using System;

namespace EzDinner.Application.Commands.Dishes
{
    public class EnrichDishCommand
    {
        public Guid FamilyId { get; }
        public Guid DishId { get; }

        public EnrichDishCommand(Guid familyId, Guid dishId)
        {
            FamilyId = familyId;
            DishId = dishId;
        }
    }
}

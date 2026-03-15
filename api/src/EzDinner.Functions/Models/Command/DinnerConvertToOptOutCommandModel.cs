using System;

namespace EzDinner.Functions.Models.Command
{
    public class DinnerConvertToOptOutCommandModel
    {
        public Guid FamilyId { get; set; }
        public Guid DishId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}

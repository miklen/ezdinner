using System;

namespace EzDinner.Functions.Models.Command
{
    public class AddWishCommandModel
    {
        public Guid DishId { get; set; }
        public string DishName { get; set; } = string.Empty;
    }
}

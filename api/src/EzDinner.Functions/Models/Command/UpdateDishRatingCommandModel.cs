using System;
using System.Collections.Generic;
using System.Text;

namespace EzDinner.Functions.Models.Command
{
    public class UpdateDishRatingCommandModel
    {
        public double Rating { get; set; }
        public string? FamilyMemberId { get; set; }
    }
}

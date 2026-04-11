using System;
using System.Collections.Generic;
using System.Text;

namespace EzDinner.Authorization.Core
{
    public static class Resources
    {
        public static readonly string Family = $"{nameof(Resources)}.{nameof(Family)}";
        public static readonly string Dish = $"{nameof(Resources)}.{nameof(Dish)}";
        public static readonly string Dinner = $"{nameof(Resources)}.{nameof(Dinner)}";
        public static readonly string Wishlist = $"{nameof(Resources)}.{nameof(Wishlist)}";
        public static readonly string All = "*";
    }
}

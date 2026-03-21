using System.Collections.Generic;

namespace EzDinner.Core.Aggregates.DishAggregate
{
    public class DishMetadataValueObject
    {
        public static readonly DishMetadataValueObject Empty = new(
            System.Array.Empty<DishRole>(), false,
            null, false,
            null, false,
            null, false);

        public IReadOnlyList<DishRole> Roles { get; }
        public bool RolesConfirmed { get; }
        public EffortLevel? EffortLevel { get; }
        public bool EffortLevelConfirmed { get; }
        public SeasonAffinity? SeasonAffinity { get; }
        public bool SeasonAffinityConfirmed { get; }
        public string? Cuisine { get; }
        public bool CuisineConfirmed { get; }

        public DishMetadataValueObject(
            IReadOnlyList<DishRole> roles, bool rolesConfirmed,
            EffortLevel? effortLevel, bool effortLevelConfirmed,
            SeasonAffinity? seasonAffinity, bool seasonAffinityConfirmed,
            string? cuisine, bool cuisineConfirmed)
        {
            Roles = roles;
            RolesConfirmed = rolesConfirmed;
            EffortLevel = effortLevel;
            EffortLevelConfirmed = effortLevelConfirmed;
            SeasonAffinity = seasonAffinity;
            SeasonAffinityConfirmed = seasonAffinityConfirmed;
            Cuisine = cuisine;
            CuisineConfirmed = cuisineConfirmed;
        }

        public static DishMetadataValueObject FromUserEdit(
            DishMetadataValueObject current,
            IReadOnlyList<DishRole>? roles,
            EffortLevel? effortLevel,
            SeasonAffinity? seasonAffinity,
            string? cuisine)
        {
            return new DishMetadataValueObject(
                roles: roles ?? current.Roles,
                rolesConfirmed: roles != null ? true : current.RolesConfirmed,
                effortLevel: effortLevel ?? current.EffortLevel,
                effortLevelConfirmed: effortLevel.HasValue ? true : current.EffortLevelConfirmed,
                seasonAffinity: seasonAffinity ?? current.SeasonAffinity,
                seasonAffinityConfirmed: seasonAffinity.HasValue ? true : current.SeasonAffinityConfirmed,
                cuisine: cuisine ?? current.Cuisine,
                cuisineConfirmed: cuisine != null ? true : current.CuisineConfirmed);
        }

        public static DishMetadataValueObject FromAiSuggestion(
            DishMetadataValueObject current,
            IReadOnlyList<DishRole>? roles,
            EffortLevel? effortLevel,
            SeasonAffinity? seasonAffinity,
            string? cuisine)
        {
            // Never overwrite a field that the user has already manually confirmed.
            // AI suggestions only fill in unconfirmed or empty fields.
            var applyRoles = roles?.Count > 0 && !current.RolesConfirmed;
            var applyEffort = effortLevel.HasValue && !current.EffortLevelConfirmed;
            var applySeason = seasonAffinity.HasValue && !current.SeasonAffinityConfirmed;
            var applyCuisine = cuisine != null && !current.CuisineConfirmed;

            return new DishMetadataValueObject(
                roles: applyRoles ? roles! : current.Roles,
                rolesConfirmed: applyRoles ? false : current.RolesConfirmed,
                effortLevel: applyEffort ? effortLevel : current.EffortLevel,
                effortLevelConfirmed: applyEffort ? false : current.EffortLevelConfirmed,
                seasonAffinity: applySeason ? seasonAffinity : current.SeasonAffinity,
                seasonAffinityConfirmed: applySeason ? false : current.SeasonAffinityConfirmed,
                cuisine: applyCuisine ? cuisine : current.Cuisine,
                cuisineConfirmed: applyCuisine ? false : current.CuisineConfirmed);
        }

        public DishMetadataValueObject MergeWith(DishMetadataValueObject incoming)
        {
            var overwriteRoles = incoming.RolesConfirmed || !RolesConfirmed;
            var overwriteEffortLevel = incoming.EffortLevelConfirmed || !EffortLevelConfirmed;
            var overwriteSeasonAffinity = incoming.SeasonAffinityConfirmed || !SeasonAffinityConfirmed;
            var overwriteCuisine = incoming.CuisineConfirmed || !CuisineConfirmed;

            return new DishMetadataValueObject(
                overwriteRoles ? incoming.Roles : Roles,
                overwriteRoles ? incoming.RolesConfirmed : RolesConfirmed,
                overwriteEffortLevel ? incoming.EffortLevel : EffortLevel,
                overwriteEffortLevel ? incoming.EffortLevelConfirmed : EffortLevelConfirmed,
                overwriteSeasonAffinity ? incoming.SeasonAffinity : SeasonAffinity,
                overwriteSeasonAffinity ? incoming.SeasonAffinityConfirmed : SeasonAffinityConfirmed,
                overwriteCuisine ? incoming.Cuisine : Cuisine,
                overwriteCuisine ? incoming.CuisineConfirmed : CuisineConfirmed);
        }
    }
}

using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.DomainServices.DishRecommendations;
using NodaTime;
using Xunit;

namespace EzDinner.UnitTests.DishRecommendationTests;

public class RecommendationPolicyTests
{
    private static readonly LocalDate Today = new(2026, 10, 3);
    private static readonly Guid Family = Guid.NewGuid();

    [Fact]
    public void CandidatesExcludeInactiveAndOtherFamilyDishes()
    {
        var active = Dish.CreateNew(Family, "Active");
        var deleted = Dish.CreateNew(Family, "Deleted");
        deleted.Delete();
        var archived = Dish.CreateNew(Family, "Archived");
        archived.Archive();
        var foreign = Dish.CreateNew(Guid.NewGuid(), "Foreign");
        var candidates = DishRecommendationCandidateFactory.Create(Family, [active, deleted, archived, foreign], [], new Dictionary<Guid, int>(), Today, Today);
        Assert.Equal([active.Id], candidates.Select(candidate => candidate.DishId));
    }

    [Fact]
    public void UnratedNeverUsedDishHasNoSyntheticHistoryOrRating()
    {
        var dish = Dish.CreateNew(Family, "New dish");
        var candidate = Assert.Single(DishRecommendationCandidateFactory.Create(Family, [dish], [], new Dictionary<Guid, int>(), Today, Today));
        Assert.Null(candidate.Rating);
        Assert.True(candidate.IsUnclassified);
        Assert.True(candidate.Usage.NeverUsed);
        Assert.Null(candidate.Usage.LastServed);
        Assert.Null(candidate.Usage.TypicalSpacingDays);
        Assert.Null(candidate.Usage.DaysSinceLastServing(Today));
    }

    [Fact]
    public void MedianRotationGroupsLeftoversAndIgnoresDuplicatesAndFutureDates()
    {
        var first = Today.PlusDays(-107);
        var dates = new[] { first, first, first.PlusDays(1), first.PlusDays(30), first.PlusDays(31), first.PlusDays(70), Today, Today.PlusDays(20) };
        var usage = new DishUsagePatternValueObject(dates, Today);
        Assert.Equal(5, usage.ServingCount);
        Assert.Equal([first, first.PlusDays(30), first.PlusDays(70)], usage.OccurrenceStarts);
        Assert.Equal(35d, usage.TypicalSpacingDays);
        Assert.Equal(37, usage.DaysSinceLastServing(Today));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SparseHistoryDoesNotClaimRotation(int occurrences)
    {
        var usage = new DishUsagePatternValueObject(Enumerable.Range(1, occurrences).Select(index => Today.PlusDays(-index * 35)), Today);
        Assert.Null(usage.TypicalSpacingDays);
    }

    [Fact]
    public void AssignmentsRemainSeparateFromPastServingHistory()
    {
        var dish = Dish.CreateNew(Family, "Planned");
        var monday = new LocalDate(2026, 10, 5);
        var future = Dinner.CreateNew(Family, monday.PlusDays(2));
        future.AddMenuItem(new MenuItem(dish.Id));
        var foreign = Dinner.CreateNew(Guid.NewGuid(), Today.PlusDays(-1));
        foreign.AddMenuItem(new MenuItem(dish.Id));
        var candidate = Assert.Single(DishRecommendationCandidateFactory.Create(Family, [dish], [future, foreign], new Dictionary<Guid, int>(), Today, monday));
        Assert.True(candidate.Usage.NeverUsed);
        Assert.Equal([monday.PlusDays(2)], candidate.AssignedDates);
    }

    [Fact]
    public void ForgottenFavouriteHasEvidenceFromEstablishedRotation()
    {
        var candidate = Candidate("Favourite", 9, [ -175, -140, -105, -70 ]);
        var result = new ResurfacingRule().Score(candidate, Today);
        Assert.Contains(new(DishRecommendationReasonKind.ForgottenFavourite, 70), result.Reasons);
        Assert.Contains(new(DishRecommendationReasonKind.ObservedRotation, 35), result.Reasons);
    }

    [Fact]
    public void FormerRegularHasOccurrenceEvidenceRatherThanLeftoverCount()
    {
        var candidate = Candidate("Regular", null, [-210, -180, -150, -120, -90]);
        var result = new ResurfacingRule().Score(candidate, Today);
        Assert.Contains(new(DishRecommendationReasonKind.FormerRegular, 5), result.Reasons);
    }

    [Fact]
    public void IndefiniteAbsenceCannotOutrankEveryRatingAndWishSignal()
    {
        var ancient = Candidate("Ancient", 1, [-10000, -9965, -9930]);
        var wished = Candidate("Wished", 10, [], true, 3);
        var rule = new ResurfacingRule();
        Assert.True(rule.Score(wished, Today).Score > rule.Score(ancient, Today).Score);
        var older = Candidate("Older", 1, [-20000, -19965, -19930]);
        Assert.Equal(rule.Score(ancient, Today).Score, rule.Score(older, Today).Score);
    }

    [Fact]
    public void AutomaticResultsIncludeUnclassifiedAndExcludeSideOnlyDishes()
    {
        var unknown = Candidate("Unknown", null, []);
        var side = new DishRecommendationCandidateValueObject(Guid.NewGuid(), "Side", 10, [DishRole.Side], true, 5, new([], Today), []);
        var service = new DishRecommendationService(new ResurfacingRule());
        Assert.Equal([unknown.DishId], service.Recommend([unknown, side], Today, new HashSet<Guid>()).Select(result => result.Candidate.DishId));
        Assert.Equal([side.DishId], service.Recommend([unknown, side], Today, new HashSet<Guid>(), role: DishRole.Side).Select(result => result.Candidate.DishId));
    }

    [Fact]
    public void EqualScoresUseCanonicalNameThenIdRegardlessOfInputOrder()
    {
        var first = new DishRecommendationCandidateValueObject(Guid.Parse("00000000-0000-0000-0000-000000000001"), "Same", null, [], false, 0, new([], Today), []);
        var second = new DishRecommendationCandidateValueObject(Guid.Parse("00000000-0000-0000-0000-000000000002"), "Same", null, [], false, 0, new([], Today), []);
        var service = new DishRecommendationService(new ResurfacingRule());
        Assert.Equal([first.DishId, second.DishId], service.Recommend([second, first], Today, new HashSet<Guid>()).Select(result => result.Candidate.DishId));
        Assert.Equal([second.DishId], service.Recommend([first, second], Today, new HashSet<Guid> { first.DishId }).Select(result => result.Candidate.DishId));
    }

    private static DishRecommendationCandidateValueObject Candidate(string name, double? rating, int[] offsets, bool wished = false, int votes = 0)
        => new(Guid.NewGuid(), name, rating, [], wished, votes, new(offsets.Select(Today.PlusDays), Today), []);
}

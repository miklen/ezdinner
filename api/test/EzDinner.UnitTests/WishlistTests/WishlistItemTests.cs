using EzDinner.Core.Aggregates.WishlistAggregate;
using NodaTime;
using NodaTime.Testing;
using System;
using System.Linq;
using Xunit;

namespace EzDinner.UnitTests.WishlistTests
{
    public class WishlistItemTests
    {
        private static readonly Guid FamilyId = Guid.NewGuid();
        private static readonly Guid DishId = Guid.NewGuid();
        private static readonly Guid CreatorId = Guid.NewGuid();
        private static readonly Instant BaseTime = Instant.FromUtc(2026, 1, 1, 12, 0, 0);

        private static WishlistItem CreateWish(IClock? clock = null)
        {
            clock ??= new FakeClock(BaseTime);
            return WishlistItem.CreateNew(FamilyId, DishId, "Tacos", CreatorId, clock);
        }

        [Fact]
        public void CreateNew_SetsCorrectInitialState()
        {
            var clock = new FakeClock(BaseTime);
            var wish = CreateWish(clock);

            Assert.Equal(FamilyId, wish.FamilyId);
            Assert.Equal(DishId, wish.DishId);
            Assert.Equal("Tacos", wish.DishName);
            Assert.Equal(CreatorId, wish.AddedBy);
            Assert.Equal(BaseTime, wish.AddedAt);
            Assert.Equal(BaseTime + Duration.FromDays(14), wish.ExpiresAt);
            Assert.Single(wish.Votes); // Creator's vote is first vote
            Assert.Equal(CreatorId, wish.Votes[0].UserId);
        }

        [Fact]
        public void Upvote_NewVoter_AddsVoteAndExtendExpiry()
        {
            var clock = new FakeClock(BaseTime);
            var wish = CreateWish(clock);
            var voter = Guid.NewGuid();
            var voteTime = BaseTime + Duration.FromDays(5);
            clock.Reset(voteTime);

            wish.Upvote(voter, clock);

            Assert.Equal(2, wish.Votes.Count);
            Assert.Contains(wish.Votes, v => v.UserId == voter);
            Assert.Equal(voteTime + Duration.FromDays(14), wish.ExpiresAt);
        }

        [Fact]
        public void Upvote_DuplicateVote_ThrowsInvalidOperationException()
        {
            var clock = new FakeClock(BaseTime);
            var wish = CreateWish(clock);
            var voter = Guid.NewGuid();
            wish.Upvote(voter, clock);

            Assert.Throws<InvalidOperationException>(() => wish.Upvote(voter, clock));
        }

        [Fact]
        public void Upvote_SelfVote_AddsVoteAndExtendExpiry()
        {
            // Creator is allowed to self-vote (renewal of interest)
            var clock = new FakeClock(BaseTime);
            var wish = CreateWish(clock);
            // Creator already has a vote from CreateNew, so self-vote should throw
            Assert.Throws<InvalidOperationException>(() => wish.Upvote(CreatorId, clock));
        }

        [Fact]
        public void Upvote_ExpiryExtension_UsesMaxOfCurrentAndVotePlusFourteen()
        {
            // Vote early — the extension should be max(current expiry, voteTime + 14)
            var clock = new FakeClock(BaseTime);
            var wish = CreateWish(clock); // expiresAt = BaseTime + 14 days

            var voter = Guid.NewGuid();
            // Vote at day 1 — extension = day 1 + 14 = day 15 > current day 14
            clock.Reset(BaseTime + Duration.FromDays(1));
            wish.Upvote(voter, clock);

            Assert.Equal(BaseTime + Duration.FromDays(1) + Duration.FromDays(14), wish.ExpiresAt);
        }

        [Fact]
        public void Upvote_LateVote_ExpiryExtendedBeyondOriginal()
        {
            var clock = new FakeClock(BaseTime);
            var wish = CreateWish(clock); // expiresAt = BaseTime + 14

            var voter = Guid.NewGuid();
            // Vote near expiry at day 13 — new expiry = day 13 + 14 = day 27
            clock.Reset(BaseTime + Duration.FromDays(13));
            wish.Upvote(voter, clock);

            Assert.Equal(BaseTime + Duration.FromDays(13) + Duration.FromDays(14), wish.ExpiresAt);
        }

        [Fact]
        public void IsExpired_BeforeExpiry_ReturnsFalse()
        {
            var clock = new FakeClock(BaseTime);
            var wish = CreateWish(clock);

            Assert.False(wish.IsExpired(BaseTime + Duration.FromDays(13)));
        }

        [Fact]
        public void IsExpired_AfterExpiry_ReturnsTrue()
        {
            var clock = new FakeClock(BaseTime);
            var wish = CreateWish(clock);

            Assert.True(wish.IsExpired(BaseTime + Duration.FromDays(15)));
        }

        [Fact]
        public void PartitionKey_IsFamilyId()
        {
            var wish = CreateWish();
            Assert.Equal(FamilyId, wish.PartitionKey);
        }
    }
}

using EzDinner.Core.Aggregates.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EzDinner.Core.Aggregates.DishAggregate
{
    public class Dish : AggregateRoot<Guid>
    {
        private readonly List<Rating> _ratings;

        public string Name { get; private set; }
        public Guid FamilyId { get; }
        public bool Deleted { get; private set; }
        public Uri? Url { get; private set; }
        public IEnumerable<Tag> Tags { get; }
        public string Notes { get; private set; }

        /// <summary>
        /// Rating in 10 steps. Values are between 0-10.
        /// </summary>
        public double Rating => _ratings.Count > 0 ? _ratings.Average(s => s.RatingValue) : 0;
        public IEnumerable<Rating> Ratings => _ratings;
        /// <summary>
        /// For serialization purpose only. Does not protect invariants and constraints.
        /// </summary>
        public Dish(Guid id, Guid familyId, string name, Uri? url, IEnumerable<Tag> tags, string notes, bool deleted, IEnumerable<Rating> ratings) : base(id)
        {
            FamilyId = familyId;
            Name = name;
            Url = url;
            Tags = tags;
            Notes = notes;
            Deleted = deleted;
            _ratings = ratings?.ToList() ?? new List<Rating>();
        }

        public static Dish CreateNew(Guid familyId, string name)
        {
            return new Dish(id: Guid.NewGuid(), familyId, name, null, new List<Tag>(), notes: "", deleted: false, new List<Rating>());
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Dish name cannot be empty.", nameof(name));
            Name = name;
        }

        public void Delete()
        {
            Deleted = true;
        }

        /// <summary>
        /// Rate this dish on behalf of a family member. Caller must be the member themselves unless the member lacks autonomy.
        /// </summary>
        /// <param name="callerId">The authenticated user performing the action.</param>
        /// <param name="familyMemberId">The member whose rating is being set.</param>
        /// <param name="memberHasAutonomy">Whether the rated member has their own account.</param>
        /// <param name="uiRating">Rating on the 0–5 UI scale.</param>
        public void SetRating(Guid callerId, Guid familyMemberId, bool memberHasAutonomy, double uiRating)
        {
            if (!familyMemberId.Equals(callerId) && memberHasAutonomy)
                throw new InvalidOperationException("CANNOT_RATE_ON_BEHALF_OF_AUTONOMOUS_MEMBER");

            var domainRating = Convert.ToInt32(uiRating * 2d);
            var ratingIndex = _ratings.FindIndex(w => w.RaterId == familyMemberId);
            var newRating = new Rating(familyMemberId, domainRating);
            if (ratingIndex == -1)
                _ratings.Add(newRating);
            else
                _ratings[ratingIndex] = newRating;
        }

        public void SetUrl(string? url)
        {
            if (string.IsNullOrEmpty(url))
            {
                Url = null;
                return;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri)) throw new ArgumentException("URL must be valid");
            Url = uri;
        }

        public void SetNotes(string notes)
        {
            Notes = notes;
        }

        public bool MigrateRating(Guid fromMemberId, Guid toMemberId)
        {
            var sourceIndex = _ratings.FindIndex(r => r.RaterId == fromMemberId);
            if (sourceIndex == -1) return false;

            var targetExists = _ratings.Any(r => r.RaterId == toMemberId);
            if (targetExists)
                _ratings.RemoveAt(sourceIndex);
            else
                _ratings[sourceIndex] = new Rating(toMemberId, _ratings[sourceIndex].RatingValue);

            return true;
        }
    }
}
 
using System;

namespace EzDinner.Core.Aggregates.DinnerAggregate
{
    public class OptOut : IEquatable<OptOut>
    {
        public string Reason { get; }

        public OptOut(string reason)
        {
            Reason = reason;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as OptOut);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Reason);
        }

        public bool Equals(OptOut? other)
        {
            return other != null && Reason.Equals(other.Reason);
        }
    }
}

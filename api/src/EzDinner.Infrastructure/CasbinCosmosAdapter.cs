using Casbin.Model;
using Casbin.Persist.Adapter.EFCore;
using Casbin.Persist.Adapter.EFCore.Entities;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace EzDinner.Infrastructure
{
    public class CasbinCosmosAdapter : EFCoreAdapter<string>
    {
        private readonly CasbinDbContext<string> _dbContext;

        public CasbinCosmosAdapter(CasbinDbContext<string> casbinDbContext)
            : base(casbinDbContext)
        {
            _dbContext = casbinDbContext;
        }

        /// <summary>
        /// Overrides the base EFCoreAdapter to bypass a LINQ existence check that generates
        /// invalid CosmosDB SQL in EF Core 9. Instead, we insert directly and handle conflicts.
        /// EF Core maps Casbin fields to shadow properties named "Type" and "Value1"–"Value6".
        /// </summary>
        public override async Task AddPolicyAsync(string section, string policyType, IPolicyValues values)
        {
            _dbContext.ChangeTracker.Clear();

            var rule = new EFCorePersistPolicy<string> { Id = GetDeterministicId(policyType, values) };
            _dbContext.Add(rule);
            var entry = _dbContext.Entry(rule);
            entry.Property("Type").CurrentValue = policyType;
            for (int i = 0; i < Math.Min(values.Count, 6); i++)
                entry.Property($"Value{i + 1}").CurrentValue = values[i];

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is CosmosException cosmosEx && cosmosEx.StatusCode == HttpStatusCode.Conflict)
            {
                // Policy already exists in DB — no action needed.
                _dbContext.ChangeTracker.Clear();
            }
        }

        private static string GetDeterministicId(string policyType, IPolicyValues values)
        {
            return string.Join("|", new[] { policyType }.Concat(values));
        }
    }
}

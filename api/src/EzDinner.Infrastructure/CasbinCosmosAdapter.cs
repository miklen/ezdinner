using Casbin.Persist.Adapter.EFCore;

namespace EzDinner.Infrastructure
{
    public class CasbinCosmosAdapter : EFCoreAdapter<string>
    {
        public CasbinCosmosAdapter(CasbinDbContext<string> casbinDbContext)
            : base(casbinDbContext)
        {
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DinnerAggregate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace EzDinner.Functions
{
    public class DinnerGetOptOutReasons
    {
        private readonly ILogger<DinnerGetOptOutReasons> _logger;
        private readonly IDinnerRepository _dinnerRepository;
        private readonly IAuthzService _authz;

        public DinnerGetOptOutReasons(ILogger<DinnerGetOptOutReasons> logger, IDinnerRepository dinnerRepository, IAuthzService authz)
        {
            _logger = logger;
            _dinnerRepository = dinnerRepository;
            _authz = authz;
        }

        [Function(nameof(DinnerGetOptOutReasons))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dinners/family/{familyId}/optout/reasons")] HttpRequest req,
            string familyId
            )
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, familyId, Resources.Dinner, Actions.Read)) return new UnauthorizedResult();

            var parsedId = Guid.Parse(familyId);
            var reasons = await _dinnerRepository.GetOptOutReasonsAsync(parsedId)
                .Distinct()
                .ToListAsync();

            return new OkObjectResult(reasons);
        }
    }
}

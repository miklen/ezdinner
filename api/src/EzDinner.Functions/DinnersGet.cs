using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Functions.Models.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using NodaTime;
using NodaTime.Text;

namespace EzDinner.Functions
{
    public class DinnersGet
    {
        private readonly ILogger<DishCreate> _logger;
        private readonly IMapper _mapper;
        private readonly IDinnerService _dinnerService;
        private readonly IAuthzService _authz;

        public DinnersGet(ILogger<DishCreate> logger, IMapper mapper, IDinnerService dinnerService, IAuthzService authz)
        {
            _logger = logger;
            _mapper = mapper;
            _dinnerService = dinnerService;
            _authz = authz;
        }

        [FunctionName(nameof(DinnersGet))]
        [RequiredScope("backendapi")]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dinners/family/{familyId}/dates/{fromDate}/{toDate}")] HttpRequest req,
            string familyId,
            string fromDate,
            string toDate
            )
        {
            var (authenticationStatus, authenticationResponse) = await req.HttpContext.AuthenticateAzureFunctionAsync();
            if (!authenticationStatus) return authenticationResponse;
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId(), familyId, Resources.Dinner, Actions.Read)) return new UnauthorizedResult();

            var pattern = LocalDatePattern.Iso;
            var parsedId = Guid.Parse(familyId);
            var dinners = _dinnerService.GetAsync(parsedId, pattern.Parse(fromDate).GetValueOrThrow(), pattern.Parse(toDate).GetValueOrThrow()).Select(_mapper.Map<DinnersQueryModel>);
            return new OkObjectResult(dinners);
        }
    }
}


using System;
using AutoMapper;
using EzDinner.Authorization.Core;
using EzDinner.Query.Core.FamilyQueries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace EzDinner.Functions
{
    public class FamilyGetFull
    {
        private readonly ILogger<FamilyGetFull> _logger;
        private readonly IFamilyQueryService _familyService;
        private readonly IMapper _mapper;
        private readonly IAuthzRepository _authz;

        public FamilyGetFull(ILogger<FamilyGetFull> logger, IMapper mapper, IAuthzRepository authz, IFamilyQueryService familyService)
        {
            _logger = logger;
            _familyService = familyService;
            _mapper = mapper;
            _authz = authz;
        }

        [Function(nameof(FamilyGetFull))]
        public async Task<IActionResult?> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "families/{familyId}")] HttpRequest req, string familyId)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            var userId = Guid.Parse(req.HttpContext.User.GetNameIdentifierId() ?? "");
            if (!_authz.VerifyUserPermission(userId.ToString(), familyId, Resources.Family, Actions.Read)) return new UnauthorizedResult();
            var family = await _familyService.GetFamilyDetailsAsync(Guid.Parse(familyId));
            return new OkObjectResult(family);
        }
    }
}


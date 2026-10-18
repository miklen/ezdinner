using System;
using EzDinner.Application.Commands.Families;
using EzDinner.Functions.Models.Query;
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
        private readonly IAuthzService _authz;
        private readonly IGroceryStoreCatalog _stores;

        public FamilyGetFull(ILogger<FamilyGetFull> logger, IMapper mapper, IAuthzService authz, IFamilyQueryService familyService, IGroceryStoreCatalog stores)
        {
            _logger = logger;
            _familyService = familyService;
            _mapper = mapper;
            _authz = authz;
            _stores = stores;
        }

        [Function(nameof(FamilyGetFull))]
        public async Task<IActionResult?> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "families/{familyId}")] HttpRequest req, string familyId)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            var userId = Guid.Parse(req.HttpContext.User.GetNameIdentifierId() ?? "");
            if (!_authz.Authorize(userId.ToString(), familyId, Resources.Family, Actions.Read)) return new UnauthorizedResult();
            var family = await _familyService.GetFamilyDetailsAsync(Guid.Parse(familyId));
            if (family is null) return new NotFoundObjectResult("FAMILY_NOT_FOUND");
            return new OkObjectResult(new FamilyDetailsQueryModel(family, await _stores.GetStoresAsync(req.HttpContext.RequestAborted)));
        }
    }
}


using System;
using AutoMapper;
using EzDinner.Query.Core.FamilyQueries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace EzDinner.Functions
{
    public class FamiliesGetFull
    {
        private readonly ILogger<FamiliesGet> _logger;
        private readonly IFamilyQueryService _familyService;
        private readonly IMapper _mapper;

        public FamiliesGetFull(ILogger<FamiliesGet> logger, IMapper mapper, IFamilyQueryService familyService)
        {
            _logger = logger;
            _familyService = familyService;
            _mapper = mapper;
        }

        [Function(nameof(FamiliesGetFull))]
        public async Task<IActionResult?> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "families")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();

            var families = await _familyService.GetFamiliesDetailsAsync(Guid.Parse(req.HttpContext.User.GetNameIdentifierId()!));

            return new OkObjectResult(families);
        }
    }
}


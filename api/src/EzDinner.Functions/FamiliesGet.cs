using System;
using AutoMapper;
using EzDinner.Core.Aggregates.UserAggregate;
using EzDinner.Functions.Models.Query;
using EzDinner.Query.Core.FamilyQueries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace EzDinner.Functions
{
    public class FamiliesGet
    {
        private readonly ILogger<FamiliesGet> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IFamilyQueryRepository _familyRepository;
        private readonly IMapper _mapper;

        public FamiliesGet(ILogger<FamiliesGet> logger, IMapper mapper, IUserRepository userRepository, IFamilyQueryRepository familyRepository)
        {
            _logger = logger;
            _userRepository = userRepository;
            _familyRepository = familyRepository;
            _mapper = mapper;
        }

        [Function(nameof(FamiliesGet))]
        public async Task<IActionResult?> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "families/select")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();

            var userId = Guid.Parse(req.HttpContext.User.GetNameIdentifierId() ?? "");
            var families = await _familyRepository.GetFamiliesDetailsAsync(userId);

            var familyQueryModels = families.Select(_mapper.Map<FamilySelectQueryModel>);
            return new OkObjectResult(familyQueryModels);
        }
    }
}


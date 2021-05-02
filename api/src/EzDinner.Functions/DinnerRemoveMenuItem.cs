using System.IO;
using System.Threading.Tasks;
using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Functions.Models.Command;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using Newtonsoft.Json;

namespace EzDinner.Functions
{
    public class DinnerRemoveMenuItem
    {
        private readonly ILogger<DinnerAddMenuItem> _logger;
        private readonly IDinnerService _dinnerService;
        private readonly IDinnerRepository _dinnerRepository;

        public DinnerRemoveMenuItem(ILogger<DinnerAddMenuItem> logger, IDinnerService dinnerService, IDinnerRepository dinnerRepository)
        {
            _logger = logger;
            _dinnerService = dinnerService;
            _dinnerRepository = dinnerRepository;
        }
        
        [FunctionName(nameof(DinnerRemoveMenuItem))]
        [RequiredScope("backendapi")]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "dinners/menuitem/remove")] HttpRequest req
            )
        {
            var (authenticationStatus, authenticationResponse) = await req.HttpContext.AuthenticateAzureFunctionAsync();
            if (!authenticationStatus) return authenticationResponse;

            // TODO check that the user has access to the familyId
            var menuItem = await req.GetBodyAs<DinnerAddRemoveMenuItemCommandModel>();
            _logger.LogInformation($"Adding dish: {menuItem.DishId} to date: {menuItem.Date}");

            var dinner = await _dinnerService.GetAsync(menuItem.FamilyId, menuItem.Date);
            dinner.RemoveMenuItem(menuItem.DishId, menuItem.RecipeId);
            await _dinnerRepository.SaveAsync(dinner);

            return new OkResult();
        }
    }
}


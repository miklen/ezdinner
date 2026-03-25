using EzDinner.Application.Commands.PushSubscriptions;
using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.PushSubscriptionAggregate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebPush;

namespace EzDinner.Functions
{
    public class PushSendTonight
    {
        private readonly ILogger<PushSendTonight> _logger;
        private readonly IPushSubscriptionRepository _pushRepository;
        private readonly IDinnerRepository _dinnerRepository;
        private readonly IDishRepository _dishRepository;
        private readonly WebPushClient _webPushClient;
        private readonly byte[] _sendTonightSecretBytes;

        public PushSendTonight(
            ILogger<PushSendTonight> logger,
            IPushSubscriptionRepository pushRepository,
            IDinnerRepository dinnerRepository,
            IDishRepository dishRepository,
            WebPushClient webPushClient,
            IConfiguration configuration)
        {
            _logger = logger;
            _pushRepository = pushRepository;
            _dinnerRepository = dinnerRepository;
            _dishRepository = dishRepository;
            _webPushClient = webPushClient;
            _sendTonightSecretBytes = Encoding.UTF8.GetBytes(configuration.GetValue<string>("WebPush:SendTonightSecret")!);
        }

        [Function(nameof(PushSendTonight))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "push/send-tonight")] HttpRequest req)
        {
            var secret = req.Headers["X-Push-Secret"].ToString() ?? string.Empty;
            var secretBytes = Encoding.UTF8.GetBytes(secret);

            if (secret.Length == 0 || !CryptographicOperations.FixedTimeEquals(secretBytes, _sendTonightSecretBytes))
            {
                _logger.LogWarning("PushSendTonight called with invalid or missing secret.");
                return new UnauthorizedResult();
            }

            var command = new SendTonightNotificationsCommand(
                _pushRepository,
                _dinnerRepository,
                _dishRepository,
                _webPushClient,
                _logger);

            await command.Handle();
            return new OkResult();
        }
    }
}

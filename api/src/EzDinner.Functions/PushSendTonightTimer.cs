using EzDinner.Application.Commands.PushSubscriptions;
using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.PushSubscriptionAggregate;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using WebPush;

namespace EzDinner.Functions
{
    public class PushSendTonightTimer
    {
        private readonly ILogger<PushSendTonightTimer> _logger;
        private readonly IPushSubscriptionRepository _pushRepository;
        private readonly IDinnerRepository _dinnerRepository;
        private readonly IDishRepository _dishRepository;
        private readonly WebPushClient _webPushClient;

        public PushSendTonightTimer(
            ILogger<PushSendTonightTimer> logger,
            IPushSubscriptionRepository pushRepository,
            IDinnerRepository dinnerRepository,
            IDishRepository dishRepository,
            WebPushClient webPushClient)
        {
            _logger = logger;
            _pushRepository = pushRepository;
            _dinnerRepository = dinnerRepository;
            _dishRepository = dishRepository;
            _webPushClient = webPushClient;
        }

        // Runs at 16:00 daily. WEBSITE_TIME_ZONE must be set to "Central European Standard Time"
        // (Windows) or "Europe/Copenhagen" (Linux) so the cron is interpreted in Copenhagen time.
        [Function(nameof(PushSendTonightTimer))]
        public async Task Run([TimerTrigger("0 0 16 * * *")] TimerInfo timer)
        {
            _logger.LogInformation("PushSendTonightTimer fired.");

            var command = new SendTonightNotificationsCommand(
                _pushRepository,
                _dinnerRepository,
                _dishRepository,
                _webPushClient,
                _logger);

            await command.Handle();
        }
    }
}

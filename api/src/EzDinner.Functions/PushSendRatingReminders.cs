using EzDinner.Application.Commands.RatingReminders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EzDinner.Functions;

public sealed class PushSendRatingReminders(SendRatingRemindersCommand command, IConfiguration configuration)
{
    [Function(nameof(PushSendRatingReminders))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "push/send-rating-reminders")] HttpRequest request)
    {
        var expected = configuration["WebPush:SendTonightSecret"];
        var supplied = request.Headers["X-Push-Secret"].ToString();
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(supplied)
            || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(supplied))) return new UnauthorizedResult();
        await command.SendAsync(request.HttpContext.RequestAborted);
        return new OkResult();
    }
}

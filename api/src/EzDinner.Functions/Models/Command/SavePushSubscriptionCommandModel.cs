using System;

namespace EzDinner.Functions.Models.Command
{
    public class SavePushSubscriptionCommandModel
    {
        public string FamilyId { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
    }
}

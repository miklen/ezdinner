using EzDinner.Functions.Models.Json;
using Newtonsoft.Json;
using NodaTime;
using System;

namespace EzDinner.Functions.Models.Command
{
    public class DinnerOptOutCommandModel
    {
        public Guid FamilyId { get; set; }

        [JsonProperty]
        [JsonConverter(typeof(LocalDateConverter))]
        public LocalDate Date { get; set; }

        public string Reason { get; set; } = string.Empty;
    }
}

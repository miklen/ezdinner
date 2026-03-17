using AutoMapper;
using EzDinner.Query.Core.SuggestionQueries;
using NodaTime.Text;
using System;

namespace EzDinner.Functions.Models.Query
{
    public class SuggestionQueryModel
    {
        public Guid DishId { get; set; }
        public string DishName { get; set; } = string.Empty;
        public double Rating { get; set; }
        public int DaysSinceLast { get; set; }
    }

    public class WeekSuggestionItemQueryModel
    {
        public string Date { get; set; } = string.Empty;
        public SuggestionQueryModel? Suggestion { get; set; }
    }

    public class SuggestionMapping : Profile
    {
        public SuggestionMapping()
        {
            CreateMap<DishScore, SuggestionQueryModel>();
            CreateMap<DaySuggestion, WeekSuggestionItemQueryModel>()
                .ForMember(d => d.Date, opt => opt.MapFrom(s => LocalDatePattern.Iso.Format(s.Date)));
        }
    }
}

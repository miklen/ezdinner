using AutoMapper;
using EzDinner.Core.Aggregates.DinnerAggregate;
using NodaTime;
using NodaTime.Text;
using System;
using System.Collections.Generic;

namespace EzDinner.Functions.Models.Query
{
    public class DinnersQueryModel
    {
        public class MenuItemQueryModel
        {
            public Guid DishId { get; set; }
        }

        public string Date { get; set; } = string.Empty;
        public IEnumerable<MenuItemQueryModel>? Menu { get; set; }
        public bool IsPlanned { get; set; }
    }

    public class DinnersMapping : Profile
    {
        public DinnersMapping()
        {
            CreateMap<Dinner, DinnersQueryModel>()
                .ForMember(d => d.Date, opt => opt.MapFrom(s => LocalDatePattern.Iso.Format(s.Date)));
            CreateMap<MenuItem, DinnersQueryModel.MenuItemQueryModel>();
        }
    }
}

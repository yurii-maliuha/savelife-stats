using AutoMapper;
using SaveLife.Stats.Domain.Models;

namespace SaveLife.Stats.Domain.Mappers
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<SLTransaction, Transaction>()
                .ForMember(x => x.Amount, opt => opt.MapFrom(src => double.Parse(src.Amount)))
                .ForMember(x => x.Identity, opt => opt.Ignore());

            CreateMap<Donator, DonatorEntity>()
                .ForMember(x => x.Id, opt => opt.Ignore());

        }

    }
}

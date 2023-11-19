using AutoMapper;
using SaveLife.Stats.Domain.Models;

namespace SaveLife.Stats.Domain.Mappers
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<SLTransaction, Transaction>();

        }

    }
}

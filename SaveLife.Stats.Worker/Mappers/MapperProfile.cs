using AutoMapper;
using SaveLife.Stats.Domain.Models;
using SaveLife.Stats.Worker.Models;

namespace SaveLife.Stats.Worker.Mappers
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<SLOriginTransaction, SLTransaction>();

        }

    }
}

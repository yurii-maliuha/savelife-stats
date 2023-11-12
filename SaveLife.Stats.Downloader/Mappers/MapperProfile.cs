using AutoMapper;
using SaveLife.Stats.Domain.Models;
using SaveLife.Stats.Downloader.Models;

namespace SaveLife.Stats.Downloader.Mappers
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<SLOriginTransaction, SLTransaction>();

        }

    }
}

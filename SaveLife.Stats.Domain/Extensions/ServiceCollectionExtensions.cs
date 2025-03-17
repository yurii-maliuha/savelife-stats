using Microsoft.Extensions.DependencyInjection;
using SaveLife.Stats.Domain.Domains;
using SaveLife.Stats.Domain.Mappers;

namespace SaveLife.Stats.Domain.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSLDomainServices(this IServiceCollection services)
        {
            services.AddSingleton<DataParsingDomain>();
            services.AddAutoMapper(typeof(MapperProfile));
            return services;
        }
    }
}

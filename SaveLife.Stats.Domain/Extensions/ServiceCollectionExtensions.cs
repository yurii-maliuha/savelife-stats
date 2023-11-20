using Microsoft.Extensions.DependencyInjection;
using SaveLife.Stats.Domain.Domains;

namespace SaveLife.Stats.Domain.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSLDomainServices(this IServiceCollection services)
        {
            services.AddSingleton<DataParsingDomain>();
            return services;
        }
    }
}

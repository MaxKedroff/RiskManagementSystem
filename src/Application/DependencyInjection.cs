using Application.Org.Directorates;
using Application.Org.Persons;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services)
        {
            services.AddScoped<DirectorateService>();
            services.AddScoped<PersonService>();
            return services;
        }
    }
}

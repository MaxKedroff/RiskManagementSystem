using Application.Org.Assets;
using Application.Org.Directorates;
using Application.Org.Persons;
using Application.Org.Processes;
using Application.Org.Responsibilities;
using Application.Org.Roles;
using Application.Org.Users;
using Application.Risk.HazardGroups;
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
            services.AddScoped<ProcessService>();
            services.AddScoped<AssetService>();
            services.AddScoped<ResponsibilityService>();
            services.AddScoped<UserService>();
            services.AddScoped<RoleService>();
            services.AddScoped<HazardGroupService>();



            return services;
        }
    }
}

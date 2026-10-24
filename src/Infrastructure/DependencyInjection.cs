using Application.Abstractions;
using Infrastructure.Database;
using Infrastructure.Repositories;
using Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public static class DependencyInjection
    {

        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddSingleton<IDbConnectionFactory>(_ =>
            {
                return new NpgsqlConnectionFactory(configuration);
            });

            services.AddScoped<IDirectorateRepository, DirectorateRepository>();
            services.AddScoped<IPersonRepository, PersonRepository>();
            services.AddScoped<IProcessRepository, ProcessRepository>();
            services.AddScoped<IAssetRepository, AssetRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();


            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IPasswordHashService, PasswordHashService>();
            services.AddScoped<ITemporaryPasswordGenerator, TemporaryPasswordGenerator>();
            services.AddScoped<IResponsibilityRepository, ResponsibilityRepository>();
            services.AddScoped<IHazardGroupRepository, HazardGroupRepository>();
            services.AddScoped<IHazardRepository, HazardRepository>();
            services.AddScoped<IRiskCategoryRepository, RiskCategoryRepository>();
            services.AddScoped<IRiskRepository, RiskRepository>();
            services.AddScoped<IRiskProcessRepository, RiskProcessRepository>();
            services.AddScoped<IRiskAssessmentRepository, RiskAssessmentRepository>();
            services.AddScoped<IUserPermissionRepository, UserPermissionRepository>();
            services.AddScoped<IImpactLevelRepository, ImpactLevelRepository>();
            services.AddScoped<IRiskAcceptanceSettingsRepository, RiskAcceptanceSettingsRepository>();
            services.AddScoped<ITreatmentApprovalSettingsRepository, TreatmentApprovalSettingsRepository>();
            services.AddScoped<IFileUploadRepository, FileUploadRepository>();

            return services;

        }
    }
}

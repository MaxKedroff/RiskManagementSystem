using Application.Abstractions;
using Application.Comments;
using Application.Org.Assets;
using Application.Org.Directorates;
using Application.Org.Persons;
using Application.Org.Processes;
using Application.Org.Responsibilities;
using Application.Org.Roles;
using Application.Org.Users;
using Application.Assessment.AcceptanceSettings;
using Application.Assessment.ImpactLevels;
using Application.Import.FileUploads;
using Application.Risk.Categories;
using Application.Risk.HazardGroups;
using Application.Risk.Hazards;
using Application.Risk.Risks;
using Application.Treatment;
using Application.Treatment.ApprovalSettings;
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
            services.AddScoped<HazardService>();
            services.AddScoped<RiskCategoryService>();
            services.AddScoped<ImpactLevelService>();
            services.AddScoped<RiskAcceptanceSettingsService>();
            services.AddScoped<TreatmentApprovalSettingsService>();
            services.AddScoped<FileUploadService>();
            services.AddScoped<RiskService>();
            services.AddScoped<IUserPermissionService, UserPermissionService>();
            services.AddScoped<TreatmentService>();
            services.AddScoped<CommentService>();



            return services;
        }
    }
}

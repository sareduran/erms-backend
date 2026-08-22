using ERMS.Application.Interfaces;
using ERMS.Application.Services;
using Microsoft.Extensions.DependencyInjection;
namespace ERMS.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services) => services
        .AddScoped<IAuthService, AuthService>()
        .AddScoped<IRequestService, RequestService>()
        .AddScoped<IRequestTypeService, RequestTypeService>()
        .AddScoped<IApprovalService, ApprovalService>()
        .AddScoped<IAdminService, AdminService>()
        .AddScoped<INotificationService, NotificationService>();
}

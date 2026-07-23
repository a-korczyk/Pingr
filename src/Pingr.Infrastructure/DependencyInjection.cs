using Pingr.Application.Abstractions;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Application.Abstractions.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;
using Pingr.Application.Abstractions.Services.Authentication;
using Pingr.Application.Abstractions.Services.Logs;
using Pingr.Infrastructure.Repositories;
using Pingr.Infrastructure.Services;
using Pingr.Infrastructure.Services.Authentication;
using Pingr.Infrastructure.Services.Logs;
using Pingr.Infrastructure.Services.Logs.Digest;
using QRCoder;
using StackExchange.Redis;

namespace Pingr.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        });
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Redis
        services.AddSingleton<IConnectionMultiplexer>(_ => 
            ConnectionMultiplexer.Connect(
                configuration.GetConnectionString("Cache")
                ?? throw new InvalidOperationException("Redis connection string not found.")));
        services.AddSingleton<ICacheService, CacheService>();
        
        // AI chat
        services.AddOptions<ChatServiceOptions>()
            .BindConfiguration(ChatServiceOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var chatServiceOptions = configuration
            .GetSection(ChatServiceOptions.SectionName)
            .Get<ChatServiceOptions>() ?? throw new InvalidOperationException("Chat service configuration not found.");
        
        services.AddSingleton<IChatClient>(_ =>
            new OllamaApiClient(
                new HttpClient
                {
                    BaseAddress = new Uri(chatServiceOptions.BaseAddress),
                    Timeout = TimeSpan.FromMinutes(5)
                },
                chatServiceOptions.Model));
        services.AddSingleton<IChatService, ChatService>();
        
        // Email
        services.AddSingleton<IEmailSender, EmailSender>();
        services.AddOptions<EmailOptions>()
            .BindConfiguration(EmailOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        services.AddScoped<ILogNotificationService, LogNotificationService>();
        
        // User
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddOptions<PasswordHasherOptions>()
            .BindConfiguration(PasswordHasherOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ITokenGenerator, TokenGenerator>();
        services.AddScoped<IEmailVerificationRequestRepository, EmailVerificationRequestRepository>();
        
        // 2FA
        services.AddScoped<ITwoFactorChallengeRepository, TwoFactorChallengeRepository>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        
        // Refresh token
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        
        // Workspaces
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IWorkspaceUserRepository, WorkspaceUserRepository>();
        services.AddScoped<IWorkspaceService, WorkspaceService>();

        // Logs
        services.AddScoped<ILogRepository, LogRepository>();
        services.AddSingleton<ILogDigestQueue, LogDigestQueue>();
        services.AddSingleton<ILogDigestStatisticsBuilder, LogDigestStatisticsBuilder>();
        services.AddSingleton<ILogDigestEmailBuilder, LogDigestEmailBuilder>();
        services.AddHostedService<LogDigestBackgroundService>();
        
        return services;
    }
}

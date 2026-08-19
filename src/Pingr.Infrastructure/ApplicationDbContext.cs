using Pingr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Monitor = Pingr.Domain.Entities.Monitor;

namespace Pingr.Infrastructure;

/// <summary>
/// Entity Framework Core database context for the application.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        
    }

    public DbSet<User> Users => Set<User>();
    
    public DbSet<EmailVerificationRequest> EmailVerificationRequests => Set<EmailVerificationRequest>();

    public DbSet<TwoFactorChallenge> TwoFactorChallenges => Set<TwoFactorChallenge>();
    
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    
    public DbSet<WorkspaceUser> WorkspaceUsers => Set<WorkspaceUser>();
    
    public DbSet<Monitor> Monitors => Set<Monitor>();
    
    public DbSet<Log> Logs => Set<Log>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        base.OnModelCreating(modelBuilder);
    }
}
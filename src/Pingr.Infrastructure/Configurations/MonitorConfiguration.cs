using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monitor = Pingr.Domain.Entities.Monitor;

namespace Pingr.Infrastructure.Configurations;

public sealed class MonitorConfiguration : IEntityTypeConfiguration<Monitor>
{
    public void Configure(EntityTypeBuilder<Monitor> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Workspace)
            .WithMany(x => x.Monitors)
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
        
        builder.Property(x => x.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Enabled)
            .IsRequired();

        builder.Property(x => x.Interval)
            .IsRequired();

        builder.Property(x => x.Url)
            .IsRequired();

        builder.Property(x => x.HttpMethod)
            .IsRequired();
        
        builder.Property(x => x.HttpHeaders)
            .IsRequired();

        builder.Property(x => x.Body);
        
        builder.Property(x => x.TimeoutSeconds)
            .IsRequired();
        
        builder.Property(x => x.ExpectedStatusCodes)
            .IsRequired();

        builder.ComplexProperty(
            x => x.LastCheckResult,
            result =>
            {
                result.Property(r => r.Status)
                    .IsRequired();

                result.Property(r => r.StatusCode);

                result.Property(r => r.ResponseTime);

                result.Property(r => r.FailureReason);

                result.Property(r => r.Message);

                result.Property(r => r.CheckedAt)
                    .IsRequired();
            });
        
        builder.Property(x => x.LastCheckedAt);
        builder.Property(x => x.LastSuccessfulCheckAt);
    }
}
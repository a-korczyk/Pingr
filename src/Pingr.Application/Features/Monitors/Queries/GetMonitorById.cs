using FluentValidation;
using MediatR;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services.Authentication;
using Pingr.Domain.Common;
using Pingr.Domain.Entities;

namespace Pingr.Application.Features.Monitors.Queries;

public record GetMonitorByIdQuery(
    Guid WorkspaceId,
    Guid MonitorId) : IRequest<Result<MonitorResponse>>;
    
public sealed class GetMonitorByIdQueryHandler(
    ICurrentUser currentUser,
    IWorkspaceUserRepository workspaceUserRepository,
    IMonitorRepository monitorRepository) : IRequestHandler<GetMonitorByIdQuery, Result<MonitorResponse>>
{
    public async Task<Result<MonitorResponse>> Handle(GetMonitorByIdQuery request, CancellationToken cancellationToken)
    {
        Guid userId = currentUser.GetUserId();
        if (!await workspaceUserRepository.IsMemberAsync(
                userId,
                request.WorkspaceId,
                cancellationToken))
            return MonitorErrors.Forbidden;
        
        var monitor = await monitorRepository.GetByIdAsync(
            request.MonitorId,
            cancellationToken);

        if (monitor == null)
            return MonitorErrors.NotFound;
        
        if (monitor.WorkspaceId != request.WorkspaceId)
            return MonitorErrors.Forbidden;

        return new MonitorResponse(
            monitor.Id,
            monitor.Name,
            monitor.Enabled,
            monitor.Interval,
            monitor.Url,
            monitor.HttpMethod,
            monitor.HttpHeaders,
            monitor.Body,
            monitor.TimeoutSeconds,
            monitor.ExpectedStatusCodes,
            monitor.LastCheckResult,
            monitor.LastCheckedAt,
            monitor.LastSuccessfulCheckAt);
    }
}

public sealed record MonitorResponse(
    Guid Id,
    string Name,
    bool Enabled,
    TimeSpan Interval,
    string Url,
    string HttpMethod,
    Dictionary<string, string> HttpHeaders,
    string? Body,
    int TimeoutSeconds,
    ICollection<int> ExpectedStatusCodes,
    MonitorCheckResult? LastCheckResult,
    DateTimeOffset? LastCheckedAt,
    DateTimeOffset? LastSuccessfulCheckAt);

public sealed class GetMonitorByIdValidator : AbstractValidator<GetMonitorByIdQuery>
{
    public GetMonitorByIdValidator()
    {
        RuleFor(query => query.WorkspaceId)
            .NotEmpty().WithMessage("Workspace Id must not be empty.");
        
        RuleFor(query => query.MonitorId)
            .NotEmpty().WithMessage("Monitor Id must not be empty.");
    }
}
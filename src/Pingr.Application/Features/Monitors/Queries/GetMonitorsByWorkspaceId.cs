using FluentValidation;
using MediatR;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services.Authentication;
using Pingr.Application.Features.Logs.Queries;
using Pingr.Domain.Common;

namespace Pingr.Application.Features.Monitors.Queries;

public sealed record GetMonitorsByWorkspaceIdQuery(
    Guid WorkspaceId,
    int? Page,
    int? PageSize) : IRequest<Result<GetMonitorsByWorkspaceIdResponse>>;
    
public sealed class GetMonitorsByWorkspaceIdQueryHandler(
    IMonitorRepository monitorRepository,
    ICurrentUser currentUser,
    IWorkspaceUserRepository workspaceUserRepository) : IRequestHandler<GetMonitorsByWorkspaceIdQuery, Result<GetMonitorsByWorkspaceIdResponse>>
{
    public async Task<Result<GetMonitorsByWorkspaceIdResponse>> Handle(GetMonitorsByWorkspaceIdQuery request, CancellationToken cancellationToken)
    {
        Guid userId = currentUser.GetUserId();
        if (!await workspaceUserRepository.IsMemberAsync(
                userId,
                request.WorkspaceId,
                cancellationToken))
            return MonitorErrors.Forbidden;
        
        var monitors = await monitorRepository.GetByWorkspaceIdAsync(
            request.WorkspaceId,
            new Pagination(
                request.Page ?? Pagination.DefaultPage,
                request.PageSize ?? Pagination.DefaultPageSize),
            cancellationToken);

        var responseMonitors = monitors
            .Select(x => new MonitorResponse(
                x.Id,
                x.Name,
                x.Enabled,
                x.Interval,
                x.Url,
                x.HttpMethod,
                x.HttpHeaders,
                x.Body,
                x.TimeoutSeconds,
                x.ExpectedStatusCodes,
                x.LastCheckResult,
                x.LastCheckedAt,
                x.LastSuccessfulCheckAt
            ))
            .ToList();
        
        return new GetMonitorsByWorkspaceIdResponse(responseMonitors);
    }
}

public sealed record GetMonitorsByWorkspaceIdResponse(
    ICollection<MonitorResponse> Monitors);

public sealed class GetMonitorsByWorkspaceIdValidator : AbstractValidator<GetMonitorsByWorkspaceIdQuery>
{
    public GetMonitorsByWorkspaceIdValidator()
    {
         RuleFor(x => x.WorkspaceId)
             .NotEmpty().WithMessage("Workspace Id must not be empty.");
                                 
         RuleFor(query => query.Page)
            .GreaterThan(0).WithMessage("Page must be greater than zero")
            .When(query => query.Page != null);
         
         RuleFor(query => query.PageSize)
            .GreaterThan(0).WithMessage("PageSize must be greater than zero")
            .LessThan(101).WithMessage("PageSize must not be greater than 100")
            .When(query => query.PageSize != null);
    }
}
using System.Collections.Immutable;
using FluentValidation;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Domain.Common;
using Pingr.Domain.Entities;
using MediatR;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Application.Features.Logs.Queries;

/// <summary>
/// Retrieves a paginated collection of logs belonging to the currently
/// authenticated user that match the optional filters.
/// </summary>
public sealed record GetLogsQuery(
    int? Page,
    int? PageSize,
    Guid WorkspaceId,
    IReadOnlyList<LogStatus>? Statuses,
    IReadOnlyList<LogType>? Types,
    string? TitleContains,
    DateTimeOffset? CreatedBefore,
    DateTimeOffset? CreatedAfter) : IRequest<Result<GetLogsResponse>>;
    
/// <summary>
/// Handles <see cref="GetLogsQuery"/> requests.
/// </summary>
public sealed class GetLogsQueryHandler(
    ILogRepository logRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetLogsQuery, Result<GetLogsResponse>>
{
    public async Task<Result<GetLogsResponse>> Handle(GetLogsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Log> logs = await logRepository.GetByWorkspaceIdAsync(
            request.WorkspaceId,
            new Pagination(
                request.Page ?? Pagination.DefaultPage,
                request.PageSize ?? Pagination.DefaultPageSize),
            new LogFilters(
                request.Statuses,
                request.Types,
                request.TitleContains,
                request.CreatedBefore,
                request.CreatedAfter),
            cancellationToken);

        IReadOnlyList<LogResponse> responseLogs = logs
            .Select(log => new LogResponse(
                log.Id, 
                log.Status,
                log.Type,
                log.Title,
                log.Data,
                log.CreatedAt))
            .ToList();

        return new GetLogsResponse(responseLogs);
    }
}

/// <summary>
/// Response on successful retrieval of all logs associated with a user.
/// </summary>
public sealed record GetLogsResponse(
    IReadOnlyList<LogResponse> Logs);

/// <summary>
/// Validates <see cref="GetLogsQuery"/> requests.
/// </summary>
public sealed class GetLogsValidator : AbstractValidator<GetLogsQuery>
{
    public GetLogsValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0).WithMessage("Page must be greater than zero")
            .When(query => query.Page != null);

        RuleFor(query => query.PageSize)
            .GreaterThan(0).WithMessage("PageSize must be greater than zero")
            .LessThan(101).WithMessage("PageSize must not be greater than 100")
            .When(query => query.PageSize != null);

        RuleForEach(query => query.Statuses)
            .IsInEnum().WithMessage("Statuses must only contain LogStatus enum values.")
            .When(query => query.Statuses != null);
        
        RuleForEach(query => query.Types)
            .IsInEnum().WithMessage("Types must only contain LogType enum values.")
            .When(query => query.Types != null);
        
        RuleFor(query => query.TitleContains)
            .MaximumLength(255).WithMessage("TitleContains must not exceed 255 characters.")
            .When(query => query.TitleContains != null);
        
        RuleFor(query => query)
            .Must(query => query.CreatedBefore >= query.CreatedAfter)
            .WithMessage("CreatedAfter must be before or equal to CreatedBefore.")
            .When(query => 
                query.CreatedBefore.HasValue && query.CreatedAfter.HasValue);
    }
}
using System.Text.Json;
using FluentValidation;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Domain.Common;
using Pingr.Domain.Entities;
using MediatR;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Application.Features.Logs.Queries;

/// <summary>
/// Retrieves a log by its identifier.
/// </summary>
/// <param name="Id">The identifier of the specified log.</param>
public sealed record GetLogByIdQuery(
    Guid Id) : IRequest<Result<LogResponse>>;

/// <summary>
/// Handles <see cref="GetLogByIdQuery"/> requests.
/// </summary>
public sealed class GetLogByIdQueryHandler(
    ILogRepository logRepository,
    IWorkspaceUserRepository workspaceUserRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetLogByIdQuery, Result<LogResponse>>
{
    public async Task<Result<LogResponse>> Handle(GetLogByIdQuery request, CancellationToken cancellationToken)
    {
        Log? log = await logRepository.GetByIdAsync(request.Id, cancellationToken);
        if (log is null)
            return LogErrors.LogWithIdNotFound;
        
        bool isMemberOfWorkspace = await workspaceUserRepository.IsMemberAsync(
            currentUser.GetUserId(),
            log.WorkspaceId,
            cancellationToken);
        
        if (isMemberOfWorkspace is false)
            return LogErrors.LogWithIdNotFound;

        return new LogResponse(
            log.Id,
            log.Status,
            log.Type,
            log.Title,
            log.Data,
            log.CreatedAt);
    }
}

/// <summary>
/// The response representation of an individual log.
/// </summary>
public sealed record LogResponse(
    Guid Id,
    LogStatus Status,
    LogType Type,
    string Title,
    JsonDocument Data,
    DateTimeOffset CreatedAt);

/// <summary>
/// Validates <see cref="GetLogByIdQuery"/> requests.
/// </summary>
public sealed class GetLogByIdValidator : AbstractValidator<GetLogByIdQuery>
{
    public GetLogByIdValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty().WithMessage("Id must not be empty"); 
    }
}
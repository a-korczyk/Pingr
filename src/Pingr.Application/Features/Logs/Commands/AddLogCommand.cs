using System.Text.Json;
using FluentValidation;
using Pingr.Application.Abstractions;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Application.Abstractions.Services.Email;
using Pingr.Domain.Common;
using Pingr.Domain.Entities;
using MediatR;
using Pingr.Application.Abstractions.Services.Authentication;
using Pingr.Application.Abstractions.Services.Logs;

namespace Pingr.Application.Features.Logs.Commands;

/// <summary>
/// Adds a log with the provided details.
/// </summary>
/// <param name="Type">The log's <see cref="LogType"/>.</param>
/// <param name="Title">The log's title.</param>
/// <param name="Data">Additional custom details.</param>
public sealed record AddLogCommand(
    Guid WorkspaceId,
    LogType Type,
    string Title,
    JsonDocument Data) : IRequest<Result<AddLogResponse>>;

/// <summary>
/// Handles adding the log and returns <see cref="AddLogResponse"/>.
/// </summary>
public sealed class AddLogCommandHandler(
    ILogRepository logRepository,
    IWorkspaceUserRepository workspaceUserRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogNotificationService logNotificationService,
    ILogDigestQueue digestQueue)
    : IRequestHandler<AddLogCommand, Result<AddLogResponse>>
{
    public async Task<Result<AddLogResponse>> Handle(AddLogCommand request, CancellationToken cancellationToken)
    {
        Guid userId = currentUser.GetUserId();
        User user = (await userRepository.GetByIdAsync(userId, cancellationToken))!;

        if (!await workspaceUserRepository.IsMemberAsync(
                userId,
                request.WorkspaceId,
                cancellationToken))
            return LogErrors.Forbidden;
        
        Log log = new Log(
            request.WorkspaceId,
            userId,
            request.Type,
            request.Title,
            request.Data);
        
        await logRepository.AddAsync(log, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            if (log.Type == LogType.CriticalError)
                await logNotificationService.NotifyCriticalErrorAsync(
                    log,
                    user,
                    cancellationToken);

            await digestQueue.UpsertAsync(
                log.WorkspaceId,
                new LogDigestEntry(
                    log.Id,
                    log.Status,
                    log.Type,
                    log.Title)
            );
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.ToString());
        }

        return new AddLogResponse(
            log.Id);
    }
}

/// <summary>
/// Response after a log has been successfully added.
/// </summary>
/// <param name="Id">The identifier of the newly added log.</param>
public sealed record AddLogResponse(
    Guid Id);

/// <summary>
/// Validates <see cref="AddLogCommand"/> requests.
/// </summary>
public sealed class AddLogValidator : AbstractValidator<AddLogCommand>
{
    public AddLogValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("WorkspaceId must not be empty.");
            
        RuleFor(command => command.Type)
            .IsInEnum().WithMessage("Type must match LogType enum.");

        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("Title must not be empty.")
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters.");
    }
}
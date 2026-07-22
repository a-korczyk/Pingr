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
/// Updates an existing log with provided details.
/// </summary>
/// <param name="Id">Identifier of the log to update.</param>
/// <param name="Status">The log's new <see cref="LogStatus"/>.</param>
/// <param name="Type">The log's new <see cref="LogType"/>.</param>
/// <param name="Title">The log's new title.</param>
/// <param name="Data">Updated additional log details.</param>
public sealed record UpdateLogCommand(
    Guid WorkspaceId,
    Guid Id,
    LogStatus? Status,
    LogType? Type,
    string? Title,
    JsonDocument? Data) : IRequest<Result>;

/// <summary>
/// Handles <see cref="UpdateLogCommand"/> requ.
/// </summary>
public sealed class UpdateLogCommandHandler(
    ILogRepository logRepository,
    IWorkspaceUserRepository workspaceUserRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IUserRepository userRepository,
    ILogNotificationService logNotificationService,
    ILogDigestQueue digestQueue)
    : IRequestHandler<UpdateLogCommand, Result>
{
    public async Task<Result> Handle(UpdateLogCommand request, CancellationToken cancellationToken)
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
        
        log.Update(
            request.Status,
            request.Type,
            request.Title,
            request.Data);
        
        await unitOfWork.SaveChangesAsync(cancellationToken);

        
        var user = await userRepository.GetByIdAsync(currentUser.GetUserId(), cancellationToken);

        try
        {
            if (log.Type == LogType.CriticalError && log.Status == LogStatus.Resolved)
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
                    log.Title));
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.ToString());
        }

        
        return Result.Success();
    }
}

/// <summary>
/// Validates <see cref="UpdateLogCommand"/> requests.
/// </summary>
public sealed class UpdateLogCommandValidator : AbstractValidator<UpdateLogCommand>
{
    public UpdateLogCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("Id must not be empty");
        
        RuleFor(command => command.Status)
            .IsInEnum().WithMessage("Status must match LogStatus enum.")
            .When(command => command.Status != null);
        
        RuleFor(command => command.Type)
            .IsInEnum().WithMessage("Type must match LogType enum.")
            .When(command => command.Type != null);

        RuleFor(command => command.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title)).WithMessage("Title must not be empty.")
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters.")
            .When(command => command.Data != null);
    }
}
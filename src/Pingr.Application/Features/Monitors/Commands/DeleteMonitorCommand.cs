using FluentValidation;
using MediatR;
using Pingr.Application.Abstractions;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Application.Abstractions.Services.Authentication;
using Pingr.Domain.Common;

namespace Pingr.Application.Features.Monitors;

public sealed record DeleteMonitorCommand(
    Guid WorkspaceId,
    Guid MonitorId) : IRequest<Result>;

public sealed class DeleteMonitorCommandHandler(
    IUnitOfWork unitOfWork,
    IWorkspaceUserRepository workspaceUserRepository,
    ICurrentUser currentUser,
    IMonitorRepository monitorRepository) : IRequestHandler<DeleteMonitorCommand, Result>
{
    public async Task<Result> Handle(DeleteMonitorCommand request, CancellationToken cancellationToken)
    {
        Guid userId = currentUser.GetUserId();

        if (!await workspaceUserRepository.IsMemberAsync(
                userId,
                request.WorkspaceId,
                cancellationToken))
            return MonitorErrors.Forbidden;

        var monitor = await monitorRepository.GetByIdAsync(request.MonitorId, cancellationToken);

        if (monitor is null)
            return MonitorErrors.NotFound;
        
        if (monitor.WorkspaceId != request.WorkspaceId)
            return MonitorErrors.Forbidden;
        
        monitorRepository.Delete(monitor);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class DeleteMonitorCommandValidator : AbstractValidator<DeleteMonitorCommand>
{
    public DeleteMonitorCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("WorkspaceId must not be empty.");
        
        RuleFor(x => x.MonitorId)
            .NotEmpty().WithMessage("MonitorId must not be empty.");
    }
}

using FluentValidation;
using MediatR;
using Pingr.Application.Abstractions;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Application.Abstractions.Services.Authentication;
using Pingr.Domain.Common;

namespace Pingr.Application.Features.Monitors;

public sealed record UpdateMonitorCommand(
    Guid WorkspaceId,
    Guid MonitorId,
    string? Name,
    TimeSpan? Interval,
    string? Url,
    string? Method,
    Dictionary<string, string>? HttpHeaders,
    string? Body,
    int? TimeoutSeconds,
    ICollection<int>? ExpectedStatusCodes) : IRequest<Result>;

public sealed class UpdateMonitorCommandHandler(
    IUnitOfWork unitOfWork,
    IWorkspaceUserRepository workspaceUserRepository,
    ICurrentUser currentUser,
    IMonitorRepository monitorRepository) : IRequestHandler<UpdateMonitorCommand, Result>
{
    public async Task<Result> Handle(UpdateMonitorCommand request, CancellationToken cancellationToken)
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
        
        monitor.Update(
            request.Name,
            request.Interval,
            request.Url,
            request.Method,
            request.HttpHeaders,
            request.Body,
            request.TimeoutSeconds,
            request.ExpectedStatusCodes);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}

public sealed class UpdateMonitorCommandValidator : AbstractValidator<UpdateMonitorCommand>
{
    public UpdateMonitorCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("WorkspaceId must not be empty.");
        
        RuleFor(x => x.MonitorId)
            .NotEmpty().WithMessage("MonitorId must not be empty.");
        
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name must not be empty.")
            .MaximumLength(255).WithMessage("Name must not exceed 255 characters.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Interval)
            .NotEmpty().WithMessage("Interval must not be empty.")
            .Must(x => x?.TotalSeconds % 5 == 0).WithMessage("Interval must be a multiple of 5.")
            .Must(x => x?.TotalSeconds >= 5).WithMessage("Interval must not be less than 5 seconds.")
            .When(x => x.Interval is not null);

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Url must not be empty.")
            .Must(x => Uri.TryCreate(x, UriKind.Absolute, out _))
            .WithMessage("Url must be a valid URI.")
            .When(x => x.Url is not null);

        RuleFor(x => x.Method)
            .NotEmpty().WithMessage("Method must not be empty.")
            .When(x => x.Method is not null);
        
        RuleFor(x => x.TimeoutSeconds)
            .NotEmpty().WithMessage("TimeoutSeconds must not be empty.")
            .GreaterThan(0).WithMessage("TimeoutSeconds must be greater than zero.")
            .When(x => x.TimeoutSeconds is not null);
        
        RuleFor(x => x.ExpectedStatusCodes)
            .NotEmpty().WithMessage("ExpectedStatusCodes must not be empty.")
            .When(x => x.ExpectedStatusCodes is not null);
        RuleForEach(x => x.ExpectedStatusCodes)
            .InclusiveBetween(100, 599);
    }
}
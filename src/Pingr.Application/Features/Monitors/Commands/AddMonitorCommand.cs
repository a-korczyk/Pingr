using FluentValidation;
using MediatR;
using Pingr.Application.Abstractions;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Application.Abstractions.Services.Authentication;
using Pingr.Domain.Common;
using Monitor = Pingr.Domain.Entities.Monitor;

namespace Pingr.Application.Features.Monitors;

public sealed record AddMonitorCommand(
    Guid WorkspaceId,
    string Name,
    TimeSpan Interval,
    string Url,
    string Method,
    Dictionary<string, string>? HttpHeaders,
    string? Body,
    int TimeoutSeconds,
    ICollection<int> ExpectedStatusCodes) : IRequest<Result<AddMonitorResponse>>;

public sealed class AddMonitorCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IWorkspaceUserRepository workspaceUserRepository,
    IMonitorRepository monitorRepository,
    IMonitorQueue monitorQueue) : IRequestHandler<AddMonitorCommand, Result<AddMonitorResponse>>
{
    public async Task<Result<AddMonitorResponse>> Handle(AddMonitorCommand request, CancellationToken cancellationToken)
    {
        Guid userId = currentUser.GetUserId();

        if (!await workspaceUserRepository.IsMemberAsync(
                userId,
                request.WorkspaceId,
                cancellationToken))
            return MonitorErrors.Forbidden;
        
        var monitor = new Monitor(
            request.WorkspaceId,
            request.Name,
            request.Interval,
            request.Url,
            request.Method,
            request.HttpHeaders,
            request.Body,
            request.TimeoutSeconds,
            request.ExpectedStatusCodes);

        await monitorRepository.AddAsync(monitor, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        monitorQueue.Add(monitor);
        
        return new AddMonitorResponse(monitor.Id);
    }
}

public record AddMonitorResponse(
    Guid MonitorId);

public sealed class AddMonitorCommandValidator : AbstractValidator<AddMonitorCommand>
{
    public AddMonitorCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("WorkspaceId must not be empty.");
        
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name must not be empty.")
            .MaximumLength(255).WithMessage("Name must not exceed 255 characters.");
        
        RuleFor(x => x.Interval)
            .NotEmpty().WithMessage("Interval must not be empty.")
            .Must(x => x.TotalSeconds % 5 == 0).WithMessage("Interval must be a multiple of 5.")
            .Must(x => x.TotalSeconds >= 5).WithMessage("Interval must not be less than 5 seconds.");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Url must not be empty.")
            .Must(x => Uri.TryCreate(x, UriKind.Absolute, out _))
            .WithMessage("Url must be a valid URI.");

        RuleFor(x => x.Method)
            .NotEmpty().WithMessage("Method must not be empty.");
        
        RuleFor(x => x.TimeoutSeconds)
            .NotEmpty().WithMessage("TimeoutSeconds must not be empty.")
            .GreaterThan(0).WithMessage("TimeoutSeconds must be greater than zero.");
        
        RuleFor(x => x.ExpectedStatusCodes)
            .NotEmpty().WithMessage("ExpectedStatusCodes must not be empty.");
        RuleForEach(x => x.ExpectedStatusCodes)
            .InclusiveBetween(100, 599);
    }
}

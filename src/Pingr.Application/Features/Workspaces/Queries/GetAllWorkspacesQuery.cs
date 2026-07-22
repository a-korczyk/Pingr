using FluentValidation;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Application.Features.Logs.Queries;
using Pingr.Domain.Common;
using Pingr.Domain.Entities;
using MediatR;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Application.Features.Workspaces.Queries;

/// <summary>
/// Gets all the workspaces a user is a member in.
/// </summary>
public sealed record GetAllWorkspacesQuery(
    int? Page,
    int? PageSize) : IRequest<Result<GetAllWorkspacesResponse>>;

public sealed class GetAllWorkspacesQueryHandler(
    IWorkspaceUserRepository workspaceUserRepository,
    IWorkspaceRepository workspaceRepository,
    ICurrentUser currentUser) : IRequestHandler<GetAllWorkspacesQuery, Result<GetAllWorkspacesResponse>>
{
    public async Task<Result<GetAllWorkspacesResponse>> Handle(GetAllWorkspacesQuery request, CancellationToken cancellationToken)
    {
        var allWorkspaces = await workspaceRepository.GetByUserIdAsync(
            currentUser.GetUserId(),
            new Pagination(
                request.Page ?? Pagination.DefaultPage,
                request.PageSize ?? Pagination.DefaultPageSize),
            cancellationToken);

        return new GetAllWorkspacesResponse(
            allWorkspaces
                .Select(x => new WorkspaceResponse(
                    x.Id,
                    x.OwnerUserId,
                    x.Name,
                    x.CreatedAt))
                .ToList());
    }
}

public sealed record GetAllWorkspacesResponse(
    ICollection<WorkspaceResponse> AllWorkspaces);

public sealed class GetAllWorkspacesQueryValidator : AbstractValidator<GetAllWorkspacesQuery>
{
    public GetAllWorkspacesQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0).WithMessage("Page must be greater than zero")
            .When(query => query.Page != null);

        RuleFor(query => query.PageSize)
            .GreaterThan(0).WithMessage("PageSize must be greater than zero")
            .LessThan(101).WithMessage("PageSize must not be greater than 100")
            .When(query => query.PageSize != null);
    }
}
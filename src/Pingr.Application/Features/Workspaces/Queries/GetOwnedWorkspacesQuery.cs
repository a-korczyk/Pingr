using FluentValidation;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Domain.Common;
using MediatR;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Application.Features.Workspaces.Queries;

/// <summary>
/// Gets all the workspaces that the user is an owner of.
/// </summary>
public sealed record GetOwnedWorkspacesQuery(
    int? Page,
    int? PageSize) : IRequest<Result<GetOwnedWorkspacesResponse>>;

public sealed class GetOwnedWorkspacesQueryHandler(
    IWorkspaceRepository workspaceRepository,
    ICurrentUser currentUser) : IRequestHandler<GetOwnedWorkspacesQuery, Result<GetOwnedWorkspacesResponse>>
{
    public async Task<Result<GetOwnedWorkspacesResponse>> Handle(GetOwnedWorkspacesQuery request, CancellationToken cancellationToken)
    {
        var ownedWorkspaces = await workspaceRepository.GetByOwnerUserIdAsync(
            currentUser.GetUserId(),
            new Pagination(
                request.Page ?? Pagination.DefaultPage,
                request.PageSize ?? Pagination.DefaultPageSize),
            cancellationToken);

        return new GetOwnedWorkspacesResponse(
            ownedWorkspaces
                .Select(x => new WorkspaceResponse(
                    x.Id,
                    x.OwnerUserId,
                    x.Name,
                    x.CreatedAt))
                .ToList());
    }
}

public sealed record GetOwnedWorkspacesResponse(
    ICollection<WorkspaceResponse> OwnedWorkspaces);

public sealed class GetOwnedWorkspacesQueryValidator : AbstractValidator<GetOwnedWorkspacesQuery>
{
    public GetOwnedWorkspacesQueryValidator()
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
